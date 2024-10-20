using Nucleus.Engine;
using Nucleus.Core;
using CloneDash.Game.Input;

using System.Numerics;
using Nucleus.Types;
using CloneDash.Game.Entities;
using Nucleus;
using Raylib_cs;
using System.ComponentModel;
using Nucleus.UI.Elements;
using Nucleus.UI;
using MouseButton = Nucleus.Types.MouseButton;
using CloneDash.Game.Logic;
using CloneDash.Levels;
using Nucleus.ManagedMemory;
using CloneDash.Data;

namespace CloneDash.Game
{
    public partial class CD_GameLevel(ChartSheet Sheet) : Level
    {
        public const string STRING_HP = "HP: {0}";
        public const string STRING_FEVERY = "FEVER! {0}s";
        public const string STRING_FEVERN = "FEVER: {0}/{1}";
        public const string STRING_COMBO = "COMBO";
        public const string STRING_SCORE = "SCORE";
        public const string FONT = "Noto Sans";

        public bool InHit { get; private set; } = false;

        public bool SuppressHitMessages { get; set; }
        public void EnterHitState() {
            InHit = true;
            SuppressHitMessages = false;
        }
        public void ExitHitState() {
            InHit = false;
        }

        public bool InMashState { get; private set; }
        public CD_BaseMEntity? MashingEntity;

        /// <summary>
        /// Enters the mash state, which causes all attacks to be redirected into this entity.
        /// </summary>
        /// <param name="ent"></param>
        public void EnterMashState(CD_BaseMEntity ent) {
            InMashState = true;
            MashingEntity = ent;
        }

        /// <summary>
        /// Exits the mash state.
        /// </summary>
        public void ExitMashState() {
            InMashState = false;
            MashingEntity = null;
        }

        /// <summary>
        /// Is an entity on-screen and/or event currently warning the player? Used to draw the "!" warning on the side (and, if the entity wants to, on the entity itself)
        /// </summary>
        public bool IsWarning { get; set; } = false;


        // Player input system
        public InputState InputState { get; private set; }
        public List<IPlayerInput> InputReceivers { get; } = [];

        public AutoPlayer AutoPlayer { get; private set; }
        /// <summary>
        /// Timing system.
        /// </summary>
        public Conductor Conductor { get; private set; }
        public MusicTrack Music { get; private set; }
        public ModelEntity Player { get; set; }
        public ModelEntity HologramPlayer { get; set; }
        public Pathway TopPathway { get; set; }
        public Pathway BottomPathway { get; set; }

        /// <summary>
        /// Is the game currently paused
        /// </summary>
        public double UnpauseTime { get; private set; } = 0;
        public double DeltaUnpauseTime => Realtime - UnpauseTime;

        /// <summary>
        /// How many ticks have passed, meant for debugging
        /// </summary>
        public int Ticks { get; private set; } = 0;

        // WIP pausing
        private void startPause() {
            if (lastNoteHit)
                return;
            if (Conductor.Time < 0)
                return;

            Music.Paused = true;
            Paused = true;
            Model3AnimationChannel.GlobalPause = true;
            UnpauseTime = 0;
        }
        private void startUnpause() {
            Sounds.PlaySound("321.wav", true, 0.8f, 1f);
            UnpauseTime = Realtime;
            Timers.Simple(3, () => {
                fullUnpause();
            });
        }
        private void fullUnpause() {
            Music.Paused = false;
            Paused = false;
            Model3AnimationChannel.GlobalPause = false;
            UnpauseTime = 0;
        }

        private StatisticsData Stats { get; } = new();

        public override void Initialize(params object[] args) {
            Health = MaxHealth;
            Draw3DCoordinateStart = Draw3DCoordinateStart.TopLeft0_0;

            // build the input system
            var inputInterface = typeof(IPlayerInput);
            var inputs = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => inputInterface.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
                .Select(x => Activator.CreateInstance(x)).ToList();

            foreach (object input in inputs)
                InputReceivers.Add((IPlayerInput)input);

            Player = Add(ModelEntity.Create("cdmodeltest.glb"));
            HologramPlayer = Add(ModelEntity.Create("cdmodeltest.glb"));
            Player.PlayAnimation("Walk", loop: true);

            HologramPlayer.Visible = false;
            AutoPlayer = Add<AutoPlayer>();
            AutoPlayer.Enabled = (bool)args[0];
            TopPathway = Add<Pathway>(PathwaySide.Top);
            BottomPathway = Add<Pathway>(PathwaySide.Bottom);

            foreach (ChartEntity ChartEntity in Sheet.Entities)
                LoadEntity(ChartEntity);

            Conductor = Add<Conductor>();

            //foreach (var tempoChange in Sheet)
            Conductor.TempoChanges.Add(new TempoChange(0, (double)Sheet.Song.BPM));

            Music = Sheet.Song.GetAudioTrack();
            Music.Volume = 0.25f;

            Music.Loops = false;
            Music.Playing = true;

            UIBar = this.UI.Add<CD_Player_UIBar>();
            UIBar.Level = this;
            UIBar.Size = new(0, 64);

            Scorebar = this.UI.Add<CD_Player_Scorebar>();
            Scorebar.Level = this;
            Scorebar.Size = new(0, 128);

            Sounds.PlaySound("readygo.wav", true, 0.8f, 1.0f);
        }
        public bool Debug { get; set; } = true;
        public Window PauseWindow { get; private set; }
        private bool lastNoteHit = false;
        public override void PreThink(ref FrameState frameState) {
            if (lastNoteHit && Music.Paused) {
                EngineCore.LoadLevel(new CD_Statistics(), Sheet, Stats);
                return;
            }

            if (ShouldExitFever && InFever)
                ExitFever();
            
            if (InputState.TopClicked > 0 && CanJump)
                __whenjump = Conductor.Time;

            InputState inputState = new InputState();
            foreach (IPlayerInput playerInput in InputReceivers)
                playerInput.Poll(ref frameState, ref inputState);

            if (AutoPlayer.Enabled) {
                AutoPlayer.Play(ref inputState);
            }

            InputState = inputState;

            if (inputState.PauseButton) {
                if (Music.Paused) {
                    startUnpause();
                    if (IValidatable.IsValid(PauseWindow))
                        PauseWindow.Remove();
                }
                else {
                    startPause();

                    PauseWindow = this.UI.Add<Window>();
                    PauseWindow.Title = "Paused";
                    PauseWindow.Size = new(700, 200);
                    PauseWindow.Center();
                    PauseWindow.HideAllButtons();

                    var flex = PauseWindow.Add<FlexPanel>();
                    flex.Dock = Dock.Fill;
                    flex.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
                    flex.DockPadding = RectangleF.TLRB(4);

                    var back2menu = flex.Add<Button>();
                    back2menu.Text = "";
                    back2menu.Image = Textures.LoadTextureFromFile("ui/pause_exit.png");
                    back2menu.ImageOrientation = ImageOrientation.Fit;
                    back2menu.MouseReleaseEvent += delegate (Element self, FrameState state, MouseButton clickedButton) {
                        EngineCore.LoadLevel(new CD_MainMenu());
                    };
                    var settings = flex.Add<Button>();
                    settings.Text = "";
                    settings.Image = Textures.LoadTextureFromFile("ui/pause_settings.png");
                    settings.ImageOrientation = ImageOrientation.Fit;
                    settings.MouseReleaseEvent += delegate (Element self, FrameState state, MouseButton clickedButton) {

                    };
                    var restart = flex.Add<Button>();
                    restart.Text = "";
                    restart.Image = Textures.LoadTextureFromFile("ui/pause_restart.png");
                    restart.ImageOrientation = ImageOrientation.Fit;
                    restart.MouseReleaseEvent += delegate (Element self, FrameState state, MouseButton clickedButton) {
                        EngineCore.LoadLevel(new CD_GameLevel(Sheet), AutoPlayer.Enabled);
                    };
                    var play = flex.Add<Button>();
                    play.Text = "";
                    play.Image = Textures.LoadTextureFromFile("ui/pause_play.png");
                    play.ImageOrientation = ImageOrientation.Fit;
                    play.MouseReleaseEvent += delegate (Element self, FrameState state, MouseButton clickedButton) {
                        PauseWindow.Remove();
                        startUnpause();
                    };
                }
                return;
            }

            float yoff = 0;

            if (HoldingTopPathwaySustain != null)
                yoff -= frameState.WindowHeight * DashVars.PATHWAY_YDISTANCE / 2;
            if(HoldingBottomPathwaySustain != null)
                yoff += frameState.WindowHeight * DashVars.PATHWAY_YDISTANCE / 2;

            Player.Position = new Vector2F(frameState.WindowWidth * 0.15f, frameState.WindowHeight * 0.55f + yoff);
            HologramPlayer.Position = new Vector2F(frameState.WindowWidth * 0.15f, frameState.WindowHeight * 0.55f);

            if (HologramPlayer.PlayingAnimation) {
                var animator = HologramPlayer.Model;
                var animation = animator.Animations.FirstOrDefault(x => x.Value.Playing).Value ?? null;

                float a = (float)(animation.AnimationPlayhead / animation.AnimationData.AnimationLength);
                HologramPlayer.Visible = true;
                var alpha = Math.Clamp(NMath.Ease.InQuad(1 - a) * 255, 0, 255);
                HologramPlayer.Color = new(150, 206, 255, (int)alpha);
                FrameDebuggingStrings.Add("PlayerAlpha = " + alpha);
            }
            else {
                HologramPlayer.Visible = false;
            }

            /*if (frameState.KeyboardState.KeyPressed(KeyboardLayout.US.F)) {
                Player.PlayAnimation("HitTop.1", loop: false, fallback: "Walk");
            }

            if (frameState.KeyboardState.KeyPressed(KeyboardLayout.US.J)) {
                Player.PlayAnimation("HitBottom.1", loop: false, fallback: "Walk");
            }*/

            VisibleEntities.Clear();

            foreach (var entity in Entities) {
                if (entity is not CD_BaseEnemy)
                    continue;

                var entCD = entity as CD_BaseMEntity;
                // Visibility testing
                // ShouldDraw overrides ForceDraw here, which is intentional, although the naming convention is confusing and should be adjusted (maybe the names swapped?)
                if ((entCD.CheckVisTest(frameState) || entCD.ForceDraw) && entCD.ShouldDraw) {
                    VisibleEntities.Add(entCD);

                    if (entCD.Warns && !entCD.Dead && !InMashState)
                        IsWarning = true;
                }
            }

            var lastEntity = (CD_BaseMEntity)Entities.Last(x => x is CD_BaseEnemy);

            if (lastEntity.HitTime + lastEntity.Length < Conductor.Time && !lastNoteHit) {
                lastNoteHit = true;
                if (Stats.Misses == 0) {
                    Logs.Info("Full combo achieved.");
                    Sounds.PlaySound("fullcombo.wav", true, 0.8f, 1f);
                }
            }

            // Sort the visible entities by their hit time
            VisibleEntities.Sort((x, y) => x.HitTime.CompareTo(y.HitTime));

            //LockEntityBuffer();

            // Removes entities marked for removal safely
            foreach (var entity in Entities)
                if (entity is CD_BaseMEntity && ((CD_BaseMEntity)entity).MarkedForRemoval)
                    Remove(entity);

            //UnlockEntityBuffer(); LockEntityBuffer();

            //foreach (var e in Events)
            //e.TryCall();

            // Start input processing.
            // Bottom is executed first, so if two pathway attacks happen on the same frame, it can exit the jump state
            // before jumping again, allowing the attack to work as expected
            HitLogic(PathwaySide.Bottom);
            HitLogic(PathwaySide.Top);

            // This loop is mostly for per-tick polls that need to occur, ie. when entities have been fully missed.
            // It is ran after input processing.
            foreach (var entity in VisibleEntities) {
                switch (entity.Interactivity) {
                    case EntityInteractivity.Hit:
                        if (!entity.Dead) {
                            PathwaySide currentPathway = Pathway;

                            // Is it too late for the player to hit this entity anyway?
                            if (entity.DistanceToHit < -entity.PreGreatRange && !(entity is SustainBeam && ((SustainBeam)entity).HeldState == true)) {
                                entity.Miss();
                                Stats.Misses++;
                            }
                        }
                        break;
                    case EntityInteractivity.SamePath:
                        if (NMath.InRange(entity.DistanceToHit, -entity.PreGreatRange, 0)) {
                            PathwaySide pathCurrentCharacter = Pathway;
                            if (pathCurrentCharacter == entity.Pathway && entity.Hits == 0) {
                                entity.Hit(pathCurrentCharacter);
                                Stats.Hits++;
                            }
                        }
                        break;
                    case EntityInteractivity.Avoid:
                        // Checks if the player has completely failed to avoid the entity, and if so, damages the player.
                        if (Pathway == entity.Pathway && entity.DistanceToHit < -entity.PrePerfectRange && !entity.DidRewardPlayer) {
                            //entity.Hit(Game.PlayerController.Pathway);
                            entity.DamagePlayer();
                            Stats.Misses++;
                        }

                        // If the player is now avoiding the entity, then reward the player for missing it, and make it so they cant be damaged by it)
                        if (Pathway != entity.Pathway && entity.DistanceToHit < 0 && !entity.DidDamagePlayer) {
                            entity.Pass();
                            Stats.Avoids++;
                        }

                        break;
                }
                //entity.WhenVisible();
            }

            FrameDebuggingStrings.Add($"HoldingTopPathwaySustain {(HoldingTopPathwaySustain == null ? "<null>" : HoldingTopPathwaySustain)}");
            FrameDebuggingStrings.Add($"HoldingBottomPathwaySustain {(HoldingBottomPathwaySustain == null ? "<null>" : HoldingBottomPathwaySustain)}");
        }
        public override void Think(FrameState frameState) {

        }

        /// <summary>
        /// Gets the games <see cref="Pathway"/> from a <see cref="PathwaySide"/><br></br>
        /// Note: If strict is off (default), it will return the bottom pathway if PathwaySide.Middle is passed, otherwise it will throw an exception.
        /// </summary>
        /// <param name="pathway"></param>
        /// <param name="strict"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public Pathway GetPathway(PathwaySide pathway, bool strict = false) {
            switch (pathway) {
                case PathwaySide.Top:
                    return TopPathway;
                case PathwaySide.Bottom:
                    return BottomPathway;
                case PathwaySide.Both:
                    if (strict)
                        break;
                    else
                        return BottomPathway;
                case PathwaySide.None:
                    break;
            }

            throw new ArgumentException("pathway");
        }

        public Pathway GetPathway(CD_BaseMEntity ent) => GetPathway(ent.Pathway);

        /// <summary>
        /// Creates an entity from a C# type and adds it to <see cref="GameplayManager.Entities"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8604 // Possible null reference argument.
        public T CreateEntity<T>() where T : CD_BaseMEntity => (T)Add((T)Activator.CreateInstance(typeof(T)));
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

        /// <summary>
        /// Creates an event from an EventType enumeration, and adds it to <see cref="GameplayManager.Events"/>.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        /*public CD_BaseEvent AddEvent(EventType t) {
            CD_BaseEvent e = CD_BaseEvent.CreateFromType(this.Game, t);
            return e;
        }*/

        /// <summary>
        /// Polling function which figures out the closest, potentially-hit entity and returns the result.
        /// </summary>
        /// <param name="pathway"></param>
        /// <returns>A <see cref="PollResult"/>, if it hit something, Hit is true, and vice versa.</returns>
        public PollResult Poll(PathwaySide pathway) {
            foreach (CD_BaseMEntity entity in VisibleEntities) {
                // If the entity has no interactivity, ignore it in the poll
                if (!entity.Interactive)
                    continue;

                // If the entity says its dead, ignore it
                if (entity.Dead)
                    continue;

                switch (entity.Interactivity) {
                    case EntityInteractivity.Hit:
                    case EntityInteractivity.Sustain:
                        if (Game.Pathway.ComparePathwayType(entity.Pathway, pathway)) {
                            double distance = entity.DistanceToHit;
                            double pregreat = -entity.PreGreatRange, postgreat = entity.PostGreatRange;
                            double preperfect = -entity.PrePerfectRange, postperfect = entity.PostPerfectRange;
                            if (NMath.InRange(distance, pregreat, postgreat)) { // hit occured
                                var greatness = (NMath.InRange(distance, preperfect, postperfect) ? "PERFECT" : "GREAT") + " " + Math.Round(distance * 1000, 1) + "ms";
                                return PollResult.Create(entity, distance, greatness);
                            }
                        }
                        break;
                }
            }

            return PollResult.Empty;
        }

        /// <summary>
        /// Spawns a <see cref="TextEffect"/> into the game and adds it to the game.
        /// </summary>
        /// <param name="text">The text</param>
        /// <param name="position">Where it spawns (it will rise upwards after being spawned)</param>
        /// <param name="color">The color of the text</param>
        public void SpawnTextEffect(string text, Vector2F position, Color? color = null) {
            if (color == null)
                color = new Color(255, 255, 255, 255);

            Add(new TextEffect(text, position, color.Value));
        }

        /// <summary>
        /// Loads an event from a <see cref="ChartEvent"/> representation, builds a <see cref="MapEvent"/> out of it, and adds it to  <see cref="GameplayManager.Events"/>.
        /// </summary>
        /// <param name="ChartEvent"></param>
        public void LoadEvent(ChartEvent ChartEvent) {
            /*var ev = MapEvent.CreateFromType(this.Game, ChartEvent.Type);

            ev.Time = ChartEvent.Time;
            ev.Length = ChartEvent.Length;

            ev.Score = ChartEvent.Score;
            ev.Fever = ChartEvent.Fever;
            ev.Damage = ChartEvent.Damage;

            ev.Build();

            Events.Add(ev);*/
        }

        /// <summary>
        /// Loads an entity from a <see cref="ChartEntity"/> representation, builds a <see cref="MapEntity"/> out of it, and adds it to <see cref="GameplayManager.Entities"/>.
        /// </summary>
        /// <param name="ChartEntity"></param>
        public void LoadEntity(ChartEntity ChartEntity) {
            if (!CD_BaseEnemy.TypeConvert.ContainsKey(ChartEntity.Type)) {
                Console.WriteLine("No load entity handler for type " + ChartEntity.Type);
                return;
            }
            var ent = CD_BaseEnemy.CreateFromType(this, ChartEntity.Type);

            ent.Pathway = ChartEntity.Pathway;
            ent.EnterDirection = ChartEntity.EnterDirection;

            ent.HitTime = ChartEntity.HitTime;
            ent.ShowTime = ChartEntity.ShowTime;
            ent.Length = ChartEntity.Length;

            ent.FeverGiven = ChartEntity.Fever;
            ent.DamageTaken = ChartEntity.Damage;
            ent.ScoreGiven = ChartEntity.Score;

            ent.RelatedToBoss = ChartEntity.RelatedToBoss;

            ent.RendersItself = false;
            ent.DebuggingInfo = ChartEntity.DebuggingInfo;
            ent.Build();
        }

        public override void PreRenderBackground(FrameState frameState) {
            Graphics2D.SetTexture(Textures.LoadTextureFromFile("backgroundscroll.png"));
            var offset = ((float)Curtime * -600f) % frameState.WindowWidth;
            Graphics2D.SetDrawColor(255, 255, 255, 127);
            Graphics2D.DrawTexture(new(offset, 0), new(frameState.WindowWidth, frameState.WindowHeight));
            Graphics2D.DrawTexture(new(offset + frameState.WindowWidth, 0), new(frameState.WindowWidth, frameState.WindowHeight)); ;
        }
        public override void Render(FrameState frameState) {
            Rlgl.DrawRenderBatchActive();
            Rlgl.SetLineWidth(5);
            //Raylib.DrawLine3D(new(0, 2, 0), new(10000, 2, 0), Color.RED);
            //Raylib.DrawLine3D(new(2, 0, 0), new(2, 10000, 0), Color.GREEN);
            Rlgl.DrawRenderBatchActive();
            Rlgl.SetLineWidth(1);
            foreach (Entity ent in VisibleEntities) {
                if (ent is not CD_BaseEnemy)
                    continue;

                var entCD = (CD_BaseEnemy)ent;

                float yPosReal = frameState.WindowHeight / 2 * DashVars.PATHWAY_YDISTANCE;
                float yPosition = Game.Pathway.ValueDependantOnPathway(entCD.Pathway, -yPosReal, yPosReal);

                Graphics2D.SetDrawColor(255, 255, 255);
                var p = new Vector2F((float)entCD.XPos, (frameState.WindowHeight / 2) + yPosition); // Calculate the final beat position on the track
                entCD.ChangePosition(ref p); // Allow the entity to modify the position before it goes to the renderer
                ent.Position = p;
                ent.Render(frameState);
            }

            FrameDebuggingStrings.Add("Visible Entities: " + VisibleEntities.Count);
        }

        public override void Render2D(FrameState frameState) {
            base.Render2D(frameState);

            foreach (Entity ent in VisibleEntities) {
                if (ent is not CD_BaseEnemy)
                    continue;

                var entCD = (CD_BaseEnemy)ent;
                //Graphics2D.DrawText(ent.Position, entCD.DebuggingInfo, "Consolas", 20);
            }
        }
    }
}
