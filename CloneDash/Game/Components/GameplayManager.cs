using CloneDash.Game.Entities;
using CloneDash.Game.Events;
using CloneDash.Game.Sheets;
using Raylib_cs;

namespace CloneDash.Game.Components
{
    public class GameplayManager : DashGameComponent
    {
        /// <summary>
        /// Gameplay events
        /// </summary>
        public List<MapEvent> Events { get; private set; } = [];
        /// <summary>
        /// Gameplay entities
        /// </summary>
        public List<MapEntity> Entities { get; private set; } = [];
        /// <summary>
        /// Currently visible entities this tick
        /// </summary>
        public List<MapEntity> VisibleEntities { get; private set; } = [];

        public GameplayManager(DashGame game) : base(game) { }

        private double LastAttackTime;
        private PathwaySide LastAttackPathway;

        private void HitLogic(PathwaySide pathway) {
            int amountOfTimesHit = pathway == PathwaySide.Top ? Game.InputState.TopClicked : Game.InputState.BottomClicked;
            bool keyHitOnThisSide = amountOfTimesHit > 0;

            if (!keyHitOnThisSide)
                return;

            for (int i = 0; i < amountOfTimesHit; i++) {
                Game.EnterHitState();

                LastAttackTime = Game.Conductor.Time;
                LastAttackPathway = pathway;

                // Hit testing
                PollResult? pollResult = null;
                if (Game.InMashState) {
                    if (Game.Debug)
                        Console.WriteLine($"mashing entity = {Game.MashingEntity}");

                    Game.MashingEntity.Hit(pathway);
                }
                else {
                    var poll = Poll(pathway);
                    pollResult = poll;

                    if (poll.Hit) {
                        poll.HitEntity.Hit(pathway);
                        AudioSystem.PlaySound($"{Filesystem.Audio}punch.wav", 0.24f);

                        if (Game.SuppressHitMessages == false) {
                            Color c = poll.HitEntity.HitColor;
                            SpawnTextEffect(poll.Greatness, Game.GetPathway(pathway).Position, c);
                        }
                    }
                }

                // Trigger animation events on the player controller
                var hitSomething = pollResult.HasValue && pollResult.Value.Hit;
                if (pathway == PathwaySide.Top)
                    Game.PlayerController.AttackAir(hitSomething);
                else
                    Game.PlayerController.AttackGround();

                Game.ExitHitState();

                if (Game.Debug)
                    Console.WriteLine($"poll.Hit = {hitSomething}, entity = {((pollResult.HasValue && pollResult.Value.Hit) ? pollResult.Value.HitEntity.ToString() : "NULL")}");
            }
        }

        public override void OnDrawGameSpace() {
            foreach (var entity in VisibleEntities) {
                float yPosReal = Game.ScreenManager.ScrHeight / 2 * DashVars.PATHWAY_YDISTANCE;
                float yPosition = Pathway.ValueDependantOnPathway(entity.Pathway, -yPosReal, yPosReal);

                Graphics.SetDrawColor(255, 255, 255);
                var p = new Vector2F((float)entity.XPos, yPosition); // Calculate the final beat position on the track
                entity.ChangePosition(ref p); // Allow the entity to modify the position before it goes to the renderer

                if (!entity.Invisible)
                    entity.Draw(p); // Render the entity if not invisible

                // This just draws a hit marker on the entities for debugging purposes
                if (Game.Debug) {
                    Graphics.SetDrawColor(245, 220, 60, 100);
                    Graphics.DrawLine((float)entity.XPosFromTimeOffset((float)-entity.PreGreatRange), yPosition, (float)entity.XPosFromTimeOffset((float)entity.PostGreatRange), yPosition, 50);

                    Graphics.SetDrawColor(100, 245, 100, 100);
                    Graphics.DrawLine((float)entity.XPosFromTimeOffset((float)-entity.PrePerfectRange), yPosition, (float)entity.XPosFromTimeOffset((float)entity.PostPerfectRange), yPosition, 35);
                }
            }

            if (Game.IsWarning)
                Game.DrawWarning();
        }
        public override void OnDrawScreenSpace(float w, float h) {
            Graphics.SetDrawColor(255, 255, 255, 255);
            Graphics.DrawText((w / 2), h * 0.03f, "FPS: " + Raylib.GetFPS(), "Arial", 24, FontAlignment.Center, FontAlignment.Center);

            if (Game.Debug) {
                string[] prints = [
                    "FPS:" + Raylib.GetFPS(),
                    "Playhead:" + Game.Conductor.Time,
                    "BPM:" + Game.Conductor.BPM,
                    "Stage.Entities.Count: " + Game.GameplayManager.Entities.Count,
                    "VisibleEntities.Count: " + Game.GameplayManager.VisibleEntities.Count,
                    "MashState: " + Game.InMashState,
                    "",
                    $"UpPathway.Pressed: {Game.InputState.TopHeld}",
                    $"DownPathway.Pressed: {Game.InputState.BottomHeld}",
                ];

                for (int i = 0; i < prints.Length; i++) 
                    Graphics.DrawText(new(4, 4 + (16 * i)), prints[i], "Arial", 16);
            }
        }
        public override void OnTick() {
            VisibleEntities.Clear();

            foreach (var entity in Entities) {
                // Call the "every frame no matter what" function on all entities
                entity.WhenFrame();

                // Visibility testing
                // ShouldDraw overrides ForceDraw here, which is intentional, although the naming convention is confusing and should be adjusted (maybe the names swapped?)
                if ((entity.CheckVisTest() || entity.ForceDraw) && entity.ShouldDraw) {
                    VisibleEntities.Add(entity);

                    if (entity.Warns && !entity.Dead && !Game.InMashState)
                        Game.IsWarning = true;
                }
            }

            // Sort the visible entities by their hit time
            VisibleEntities.Sort((x, y) => x.HitTime.CompareTo(y.HitTime));

            LockEntityBuffer();

            // Removes entities marked for removal safely
            foreach (var entity in Entities)
                if (entity.MarkedForRemoval)
                    RemoveEntity(entity);

            UnlockEntityBuffer(); LockEntityBuffer();

            foreach (var e in Events)
                e.TryCall();

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
                            PathwaySide currentPathway = Game.PlayerController.Pathway;

                            // Is it too late for the player to hit this entity anyway?
                            if (entity.DistanceToHit < -entity.PreGreatRange) {
                                entity.Miss();
                            }
                        }
                        break;
                    case EntityInteractivity.SamePath:
                        if (DashMath.InRange(entity.DistanceToHit, -entity.PreGreatRange, 0)) {
                            PathwaySide pathCurrentCharacter = Game.PlayerController.Pathway;
                            if (pathCurrentCharacter == entity.Pathway && entity.Hits == 0) {
                                entity.Hit(pathCurrentCharacter);
                            }
                        }
                        break;
                    case EntityInteractivity.Avoid:
                        // Checks if the player has completely failed to avoid the entity, and if so, damages the player.
                        if (Game.PlayerController.Pathway == entity.Pathway && entity.DistanceToHit < -entity.PrePerfectRange && !entity.DidRewardPlayer) {
                            //entity.Hit(Game.PlayerController.Pathway);
                            entity.DamagePlayer();
                        }

                        // If the player is now avoiding the entity, then reward the player for missing it, and make it so they cant be damaged by it)
                        if (Game.PlayerController.Pathway != entity.Pathway && entity.DistanceToHit < 0 && !entity.DidDamagePlayer) {
                            entity.Pass();
                        }

                        break;
                }
                entity.WhenVisible();
            }

            UnlockEntityBuffer();
        }

        // Buffer system for adding entities mid-loops.

        private List<MapEntity> __entityAddBuffer = [];
        private List<MapEntity> __entityRemoveBuffer = [];
        private bool __lockBuffer = false;

        public void LockEntityBuffer() {
            __entityAddBuffer.Clear();
            __entityRemoveBuffer.Clear();
            if (__lockBuffer == true)
                return;

            __lockBuffer = true;
        }

        public void UnlockEntityBuffer() {
            if (__lockBuffer == false)
                return;
            __lockBuffer = false;

            if (__entityAddBuffer.Count > 0)
                foreach (var e in __entityAddBuffer)
                    Entities.Add(e);

            if (__entityRemoveBuffer.Count > 0)
                foreach (var e in __entityRemoveBuffer)
                    Entities.Remove(e);
        }

        /// <summary>
        /// Adds an existing entity to <see cref="GameplayManager.Entities"/>.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public MapEntity AddEntity(MapEntity entity) {
            if (__lockBuffer)
                __entityAddBuffer.Add(entity);
            else
                Entities.Add(entity);

            return entity;
        }

        /// <summary>
        /// Removes an existing entity from <see cref="GameplayManager.Entities"/>.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public MapEntity RemoveEntity(MapEntity entity) {
            if (__lockBuffer)
                __entityRemoveBuffer.Add(entity);
            else
                Entities.Remove(entity);

            return entity;
        }

        /// <summary>
        /// Creates an entity from a C# type and adds it to <see cref="GameplayManager.Entities"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8604 // Possible null reference argument.
        public T CreateEntity<T>() where T : MapEntity => (T)AddEntity((T)Activator.CreateInstance(typeof(T), this.Game));
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

        /// <summary>
        /// Creates an entity from an EntityType enumeration, and adds it to <see cref="GameplayManager.Entities"/>.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public MapEntity AddEntity(EntityType t) {
            return AddEntity(MapEntity.CreateFromType(this.Game, t));
        }

        /// <summary>
        /// Creates an event from an EventType enumeration, and adds it to <see cref="GameplayManager.Events"/>.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public MapEvent AddEvent(EventType t) {
            MapEvent e = MapEvent.CreateFromType(this.Game, t);
            Events.Add(e);
            return e;
        }

        /// <summary>
        /// Polling function which figures out the closest, potentially-hit entity and returns the result.
        /// </summary>
        /// <param name="pathway"></param>
        /// <returns>A <see cref="PollResult"/>, if it hit something, Hit is true, and vice versa.</returns>
        public PollResult Poll(PathwaySide pathway) {
            foreach (MapEntity entity in VisibleEntities) {
                // If the entity has no interactivity, ignore it in the poll
                if (!entity.Interactive)
                    continue;

                // If the entity says its dead, ignore it
                if (entity.Dead)
                    continue;

                switch (entity.Interactivity) {
                    case EntityInteractivity.Hit:
                    case EntityInteractivity.Sustain:
                        if (Pathway.ComparePathwayType(entity.Pathway, pathway)) {
                            double distance = entity.DistanceToHit;
                            double pregreat = -entity.PreGreatRange, postgreat = entity.PostGreatRange;
                            double preperfect = -entity.PrePerfectRange, postperfect = entity.PostPerfectRange;
                            if (DashMath.InRange(distance, pregreat, postgreat)) { // hit occured
                                var greatness = (DashMath.InRange(distance, preperfect, postperfect) ? "PERFECT" : "GREAT") + " " + Math.Round(distance * 1000, 1) + "ms";
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

            AddEntity(new TextEffect(this.Game, text, position, color.Value));
        }

        /// <summary>
        /// Loads an event from a <see cref="SheetEvent"/> representation, builds a <see cref="MapEvent"/> out of it, and adds it to  <see cref="GameplayManager.Events"/>.
        /// </summary>
        /// <param name="sheetEvent"></param>
        public void LoadEvent(SheetEvent sheetEvent) {
            var ev = MapEvent.CreateFromType(this.Game, sheetEvent.Type);

            ev.Time = sheetEvent.Time;
            ev.Length = sheetEvent.Length;

            ev.Score = sheetEvent.Score;
            ev.Fever = sheetEvent.Fever;
            ev.Damage = sheetEvent.Damage;

            ev.Build();

            Events.Add(ev);
        }

        /// <summary>
        /// Loads an entity from a <see cref="SheetEntity"/> representation, builds a <see cref="MapEntity"/> out of it, and adds it to <see cref="GameplayManager.Entities"/>.
        /// </summary>
        /// <param name="sheetEntity"></param>
        public void LoadEntity(SheetEntity sheetEntity) {
            var ent = MapEntity.CreateFromType(this.Game, sheetEntity.Type);

            ent.Pathway = sheetEntity.Pathway;
            ent.EnterDirection = sheetEntity.EnterDirection;

            ent.HitTime = sheetEntity.HitTime;
            ent.ShowTime = sheetEntity.ShowTime;
            ent.Length = sheetEntity.Length;

            ent.FeverGiven = sheetEntity.Fever;
            ent.DamageTaken = sheetEntity.Damage;
            ent.ScoreGiven = sheetEntity.Score;

            ent.RelatedToBoss = sheetEntity.RelatedToBoss;

            ent.Build();

            Entities.Add(ent);
        }
    }
}
