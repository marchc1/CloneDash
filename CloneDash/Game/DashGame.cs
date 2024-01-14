using CloneDash.Animation;
using CloneDash.Game.Components;
using CloneDash.Game.Entities;
using CloneDash.Game.Input;
using CloneDash.Game.Sheets;
using CloneDash.Systems;
using Raylib_cs;
using System.Diagnostics;
using System.Numerics;

namespace CloneDash
{
    public class DashGame
    {
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
        public MapEntity? MashingEntity;

        /// <summary>
        /// Enters the mash state, which causes all attacks to be redirected into this entity.
        /// </summary>
        /// <param name="ent"></param>
        public void EnterMashState(MapEntity ent) {
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

        /// <summary>
        /// The boss entity.
        /// </summary>
        public Boss Boss { get; private set; }

        /// <summary>
        /// The auto player, used for debugging & watching how a stage is played perfectly
        /// </summary>
        public AutoPlayer AutoPlayer { get; private set; }

        /// <summary>
        /// Timing system.
        /// </summary>
        public Conductor Conductor { get; private set; }

        /// <summary>
        /// Entity manager.
        /// </summary>
        public GameplayManager GameplayManager { get; private set; }

        /// <summary>
        /// Top pathway.
        /// </summary>
        public Pathway TopPathway { get; private set; }
        /// <summary>
        /// Top pathway.
        /// </summary>
        public Pathway BottomPathway { get; private set; }

        /// <summary>
        /// Player controller. Manages the players HP, score, combo, fever, position, drawing, etc...
        /// </summary>
        public PlayerController PlayerController { get; private set; }

        /// <summary>
        /// Screen manager. Auto updates the screenspace for use in rendering.
        /// </summary>
        public ScreenManager ScreenManager { get; private set; }
        /// <summary>
        /// Statistics manager, keeps track of perfect/greats, early/lates, full combo, etc....
        /// </summary>
        public Statistics Statistics { get; private set; }
        /// <summary>
        /// Music track object.
        /// </summary>
        public MusicTrack Music { get; private set; }
        /// <summary>
        /// Some debugging drawing functions will run when this is turned on.
        /// </summary>
        public bool Debug { get; set; } = false;


        // Player input system
        private List<IPlayerInput> PlayerInputs = [];
        public InputState InputState { get; private set; }

        /// <summary>
        /// Is the game currently paused
        /// </summary>
        public bool Paused { get; private set; } = false;
        public double UnpauseTime { get; private set; } = 0;
        public double DeltaUnpauseTime => DashVars.Curtime - UnpauseTime;

        public DashGame() {
            ScreenManager = new(this);
            AutoPlayer = new(this);
            AutoPlayer.Enabled = false;
            Conductor = new(this);
            GameplayManager = new(this);
            TopPathway = new(this, PathwaySide.Top);
            BottomPathway = new(this, PathwaySide.Bottom);
            PlayerController = new(this);
            Statistics = new(this);

            var inputInterface = typeof(IPlayerInput);
            var inputs = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => inputInterface.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
                .Select(x => Activator.CreateInstance(x)).ToList();

            foreach (object input in inputs)
                PlayerInputs.Add((IPlayerInput)input);

            Boss = GameplayManager.CreateEntity<Boss>();
            OnGameReady();
        }

        public void OnGameReady() {
            
        }
        public void OnGameComplete() {
            
        }


        /// <summary>
        /// How many ticks have passed, meant for debugging
        /// </summary>
        public int Ticks { get; private set; } = 0;

        // WIP pausing
        private void startPause() {
            Music.Paused = true;
            Paused = true;
            UnpauseTime = 0;
        }
        private void startUnpause() {
            UnpauseTime = DashVars.Curtime;
        }
        private void fullUnpause() {
            Music.Paused = false;
            Paused = false;
            UnpauseTime = 0;
        }

        /// <summary>
        /// RUN-ORDER FOR TICK:<br></br><br></br>
        /// 
        ///     - ScreenManager<br></br>
        ///     - Conductor<br></br>
        ///     - All IPlayerInput's<br></br>
        ///     - AutoPlayer<br></br>
        ///     - PlayerController<br></br>
        ///     - Top/Bottom Pathway<br></br>
        ///     - EntityManager<br></br>
        /// 
        /// </summary>
        public void Tick() {
            // Update the screen info
            ScreenManager.Tick();

            // Start polling for inputs from all currently available input interfaces
            InputState state = new InputState();
            foreach (IPlayerInput playerInput in PlayerInputs)
                playerInput.Poll(ref state);
            InputState = state;

            if (InputState.PauseButton)
                if (!Paused)
                    startPause();
                else
                    startUnpause();

            if (Paused) {
                if (UnpauseTime != 0)
                    if (DeltaUnpauseTime > 3)
                        fullUnpause();

                return;
            }

            IsWarning = false;
            // Enter the game loop, by setting this variable and ticking the Conductor to get an accurate time
            ScreenManager.Tick();
            Conductor.Tick();

            // Run the auto-player, if it's enabled, otherwise it won't touch the inputstate
            var input = InputState;
            AutoPlayer.Play(ref input);
            InputState = input;

            // Run the player controller tick
            PlayerController.Tick();

            // Run the pathways tick methods
            TopPathway.Tick();
            BottomPathway.Tick();

            // Start doing entity-specific logic now that the inputs and time are settled.
            // This is the main part that processes game events
            GameplayManager.Tick();

            Ticks++;
        }

        private SecondOrderSystem cameraZoom = new(1.5f, 0.68f, 1, 0);
        public void Draw() {
            // Enter game-space
            var updatedValue = cameraZoom.Update(InMashState ? 1 : 0);

            Camera2D Camera = new();
            Camera.Offset = new Vector2(0, ScreenManager.ScrHeight) / 2;
            Camera.Offset += new Vector2((float)DashMath.Remap(updatedValue, 0, 1, 0, 150), 0);
            Camera.Zoom = (float)DashMath.Remap(updatedValue, 0, 1, 1, 1.05f);

            Raylib.BeginMode2D(Camera);

            var x = (float)Math.Round(ScreenManager.ScrWidth * DashVars.PATHWAY_XDISTANCE);
            var y = ScreenManager.ScrHeight / 2;

            Graphics.SetDrawColor(220, 230, 255, 40);
            Graphics.DrawLine(x, -y, x, y, 5);
            Graphics.SetDrawColor(220, 230, 255, 170);
            Graphics.DrawLine(x, -y, x, y, 3);
            Graphics.SetDrawColor(220, 230, 255, 225);
            Graphics.DrawLine(x, -y, x, y, 1);

            TopPathway.DrawGameSpace();
            BottomPathway.DrawGameSpace();
            PlayerController.DrawGameSpace();
            GameplayManager.DrawGameSpace();

            Raylib.EndMode2D();

            //Begin screen-space drawing

            AutoPlayer.DrawScreenSpace();
            Conductor.DrawScreenSpace();
            GameplayManager.DrawScreenSpace();
            PlayerController.DrawScreenSpace();
            ScreenManager.DrawScreenSpace();
        }
        public float FrameTime => Raylib.GetFrameTime();

        public void DrawWarning(Vector2F? pos = null) {
            Vector2F warningSize = new(150, 150);
            Vector2F real = pos.HasValue ? pos.Value : new(ScreenManager.ScrWidth * 0.04f, 0);
            var A = Math.Clamp((int)(255f - (Conductor.NoteDivisorRealtime(8) * 255f)), 0, 255); //flash every 8th note
            Graphics.SetDrawColor(A, A, A, A);
            Graphics.DrawImage(TextureSystem.texture_warning, RectangleF.FromPosAndSize(real, warningSize), warningSize / 2, 0, hsvTransform: new(0, (float)A / 255f, 1));
        }

        public static DashGame LoadSheet(DashSheet dashSheet) {
            Stopwatch measureFunctionTime = Stopwatch.StartNew();
            DashGame game = new DashGame();

            foreach (var tempoChange in dashSheet.Header.TempoChanges)
                game.Conductor.TempoChanges.Add(tempoChange);

            GameplayManager gameplayManager = game.GameplayManager;

            switch (dashSheet.Music.StoredAs) {
                case MusicType.FromByteArray:
                    game.Music = MusicTrack.LoadFromMemory(dashSheet.Music.Data, true);
                    break;
                case MusicType.FromFile:
                    game.Music = MusicTrack.LoadFromFile(dashSheet.Music.Filepath, true);
                    break;
            }

            game.Music.Loops = false;

            foreach (SheetEvent sheetEvent in dashSheet.Events)
                gameplayManager.LoadEvent(sheetEvent);

            foreach (SheetEntity sheetEntity in dashSheet.Entities)
                gameplayManager.LoadEntity(sheetEntity);

            Console.WriteLine($"Took {measureFunctionTime.Elapsed.TotalSeconds} seconds to build DashGame from DashSheet with {dashSheet.Events.Count} events and {dashSheet.Entities.Count} entities");
            return game;
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

        public Pathway GetPathway(MapEntity ent) => GetPathway(ent.Pathway);
    }
}
