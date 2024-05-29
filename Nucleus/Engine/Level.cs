using Nucleus.Core;
using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;
using MouseButton = Nucleus.Types.MouseButton;

namespace Nucleus.Engine
{
    public abstract class Level
    {
        public Draw3DCoordinateStart Draw3DCoordinateStart { get; set; } = Draw3DCoordinateStart.Centered0_0;
        public T As<T>() where T : Level => (T)this;

        private RectangleF? __view = null;
        private bool __viewDirty = true;

        /// <summary>
        /// Specifies a viewing rectangle for this level. If null, scales to the screen. Currently not functional...
        /// </summary>
        public RectangleF? View {
            get {
                return __view;
            }
            set {
                __view = value;
                __viewDirty = true;
            }
        }

        public virtual void PreThink(ref FrameState frameState) { }
        public virtual void ModifyMouseState(ref MouseState mouseState) { }
        public virtual void ModifyKeyboardState(ref KeyboardState keyboardState) { }
        public virtual void Think(FrameState frameState) { }
        public virtual void PostThink(FrameState frameState) { }
        public virtual void CalcView(FrameState frameState, ref Camera3D cam) { }
        public virtual void PreRenderBackground(FrameState frameState) { }
        public virtual void PreRender(FrameState frameState) { }
        public virtual void Render(FrameState frameState) { }
        public virtual void Render2D(FrameState frameState) { }
        public virtual void PostRender(FrameState frameState) { }

        public void RunEventPreThink(ref FrameState frameState) {
            PreThink(ref frameState);
            foreach (Entity entity in EntityList)
                if (entity.Enabled && entity.ThinksForItself)
                    entity.PreThink(ref frameState);
        }
        public void RunEventModifyMouseState(ref MouseState mouseState) {
            ModifyMouseState(ref mouseState);
            foreach (Entity entity in EntityList)
                if (entity.Enabled)
                    entity.ModifyMouseState(ref mouseState);
        }
        public void RunEventModifyKeyboardState(ref KeyboardState keyboardState) {
            ModifyKeyboardState(ref keyboardState);
            foreach (Entity entity in EntityList)
                if (entity.Enabled)
                    entity.ModifyKeyboardState(ref keyboardState);
        }
        public void RunEventThink(FrameState frameState) {
            Think(frameState);
            foreach (Entity entity in EntityList)
                if (entity.Enabled && entity.ThinksForItself)
                    entity.Think(frameState);
        }
        public void RunEventPostThink(FrameState frameState) {
            PostThink(frameState);
            foreach (Entity entity in EntityList)
                if (entity.Enabled && entity.ThinksForItself)
                    entity.PostThink(frameState);
        }
        public void RunEventPreRenderBackground(FrameState frameState) {
            PreRenderBackground(frameState);
        }
        public void RunEventPreRender(FrameState frameState) {
            PreRender(frameState);
            foreach (Entity entity in EntityList)
                if (entity.Enabled && entity.RendersItself)
                    entity.PreRender(frameState);
        }
        public void RunEventRender(FrameState frameState) {
            Render(frameState);
            foreach (Entity entity in EntityList)
                if (entity.Enabled && entity.RendersItself)
                    entity.Render(frameState);
        }
        public void RunEventRender2D(FrameState frameState) {
            Render2D(frameState);
            foreach (Entity entity in EntityList)
                if (entity.Enabled && entity.RendersItself)
                    entity.Render2D(frameState);
        }
        public void RunEventPostRender(FrameState frameState) {
            PostRender(frameState);
            foreach (Entity entity in EntityList)
                if (entity.Enabled && entity.RendersItself)
                    entity.PostRender(frameState);
        }

        public void Unload() {
            foreach (Entity ent in EntityList) {
                ent.Remove();
            }

            EntityList.Clear();
            EntityHash.Clear();

            OnUnload();
        }
        public virtual void OnUnload() { }

        /// <summary>
        /// Called when the engine begins loading a level.
        /// <br>todo: proper async loading</br>
        /// </summary>
        public virtual void Initialize(params object[] args) {
            return;
        }

        // ------------------------------------------------------------------------------------------ //
        // Entity system
        // ------------------------------------------------------------------------------------------ //

        /// <summary>
        /// A hashset of all currently available entities.
        /// </summary>
        private HashSet<Entity> EntityHash { get; } = new();

        /// <summary>
        /// A list of all currently available entities.
        /// </summary>
        private List<Entity> EntityList { get; } = new();

        public List<string> FrameDebuggingStrings { get; set; }

        public Entity[] Entities => EntityList.ToArray();

        public UserInterface UI { get; } = Element.Create<UserInterface>();

        private void __addEntity(Entity ent) {
            EntityHash.Add(ent);
            EntityList.Add(ent);
        }
        private void __initializeEntity<T>(T ent) where T : Entity {
            if (__lockedBuffer)
                __addBuffer.Add(ent);
            else
                __addEntity(ent);

            ent.Level = this;
            ent.Initialize();
        }

        private bool __lockedBuffer = false;
        private List<Entity> __addBuffer = [];
        private List<Entity> __removeBuffer = [];

        public void LockEntityBuffer() {
            __addBuffer.Clear();
            __removeBuffer.Clear();
            __lockedBuffer = true;
        }
        public void UnlockEntityBuffer() {
            foreach (Entity ent in __removeBuffer) {
                EntityList.Remove(ent);
                EntityHash.Remove(ent);
                ent.Remove();
            }

            foreach (Entity ent in __addBuffer)
                __addEntity(ent);

            __addBuffer.Clear();
            __removeBuffer.Clear();
            __lockedBuffer = false;
        }

        public T Add<T>(params object[] args) where T : Entity {
            object? instance = Activator.CreateInstance(typeof(T), args);
            if (instance == null)
                throw new Exception("instance == null");

            T ent = (T)instance;
            //Logs.Debug($"Level.Add call bufferLock = {__lockedBuffer} ent {ent}");

            __initializeEntity(ent);
            return ent;
        }

        public T Add<T>(T ent) where T : Entity {
            //Logs.Debug($"Level.Add call bufferLock = {__lockedBuffer} ent {ent}");
            if (!IValidatable.IsValid(ent))
                throw new ArgumentNullException("ent");

            __initializeEntity(ent);
            return ent;
        }

        public void Remove(Entity ent) {
            if (!IValidatable.IsValid(ent))
                return;
            //Logs.Debug($"Level.Remove call bufferLock = {__lockedBuffer} ent {ent}");

            if (__lockedBuffer) {
                __removeBuffer.Add(ent);
            }
            else {
                EntityHash.Remove(ent);
                EntityList.Remove(ent);
            }
        }

        public DateTime Start { get; private set; } = DateTime.Now;

        public double LastRealtime { get; private set; } = 0;
        public double Realtime { get; private set; } = 0;
        public double RealtimeDelta { get; private set; } = 0;

        public float LastRealtimeF => (float)LastRealtime;
        public float RealtimeF => (float)Realtime;
        public float RealtimeDeltaF => (float)RealtimeDelta;

        public double LastCurtime { get; private set; } = 0;
        public double Curtime { get; private set; } = 0;
        public double CurtimeDelta { get; private set; } = 0;

        public float LastCurtimeF => (float)LastCurtime;
        public float CurtimeF => (float)Curtime;
        public float CurtimeDeltaF => (float)CurtimeDelta;

        private RenderTexture2D? RenderTarget = null;

        public bool Paused { get; set; } = false;

        public FrameState LastFrameState { get; private set; }
        public FrameState FrameState { get; private set; }

        public KeybindSystem Keybinds { get; private set; } = new();

        public bool DrawDebuggingGrid { get; private set; } = false;
        /// <summary>
        /// Call this every frame.
        /// </summary>
        public void Frame() {
            LastFrameState = FrameState;

            LastRealtime = Realtime;
            Realtime = (DateTime.Now - Start).TotalSeconds;
            RealtimeDelta = Realtime - LastRealtime;

            if (!Paused) {
                LastCurtime = Curtime;
                Curtime += RealtimeDelta;
                CurtimeDelta = Curtime - LastCurtime;
            }

            // Construct a FrameState from inputs
            UnlockEntityBuffer(); LockEntityBuffer();

            FrameDebuggingStrings = [];
            EngineCore.CurrentFrameState = new();
            FrameState frameState = new();

            float x, y, width, height;

            if (__view != null) {
                x = __view.Value.X;
                y = __view.Value.Y;
                width = __view.Value.W;
                height = __view.Value.H;
            }
            else {
                x = 0;
                y = 0;
                width = Raylib.GetScreenWidth();
                height = Raylib.GetScreenHeight();
            }

            frameState.WindowX = x;
            frameState.WindowY = y;
            frameState.WindowWidth = width;
            frameState.WindowHeight = height;

            RunEventPreThink(ref frameState);

            if (frameState.WindowWidth != LastFrameState.WindowWidth || frameState.WindowHeight != LastFrameState.WindowHeight) {
                __viewDirty = true;
            }

            if (__viewDirty) {
                if (RenderTarget.HasValue) {
                    Raylib.UnloadRenderTexture(RenderTarget.Value);
                }

                RenderTarget = Raylib.LoadRenderTexture((int)width, (int)height);
                __viewDirty = false;
            }

            // Mouse processing
            MouseState mouseState = new();

            if (Raylib.IsMouseButtonDown(Raylib_cs.MouseButton.MOUSE_BUTTON_LEFT)) mouseState.Mouse1Held = true;
            if (Raylib.IsMouseButtonDown(Raylib_cs.MouseButton.MOUSE_BUTTON_RIGHT)) mouseState.Mouse2Held = true;
            if (Raylib.IsMouseButtonDown(Raylib_cs.MouseButton.MOUSE_BUTTON_MIDDLE)) mouseState.Mouse3Held = true;
            if (Raylib.IsMouseButtonDown(Raylib_cs.MouseButton.MOUSE_BUTTON_BACK)) mouseState.Mouse4Held = true;
            if (Raylib.IsMouseButtonDown(Raylib_cs.MouseButton.MOUSE_BUTTON_FORWARD)) mouseState.Mouse5Held = true;

            if (Raylib.IsMouseButtonPressed(Raylib_cs.MouseButton.MOUSE_BUTTON_LEFT)) mouseState.Mouse1Clicked = true;
            if (Raylib.IsMouseButtonPressed(Raylib_cs.MouseButton.MOUSE_BUTTON_RIGHT)) mouseState.Mouse2Clicked = true;
            if (Raylib.IsMouseButtonPressed(Raylib_cs.MouseButton.MOUSE_BUTTON_MIDDLE)) mouseState.Mouse3Clicked = true;
            if (Raylib.IsMouseButtonPressed(Raylib_cs.MouseButton.MOUSE_BUTTON_BACK)) mouseState.Mouse4Clicked = true;
            if (Raylib.IsMouseButtonPressed(Raylib_cs.MouseButton.MOUSE_BUTTON_FORWARD)) mouseState.Mouse5Clicked = true;

            if (Raylib.IsMouseButtonReleased(Raylib_cs.MouseButton.MOUSE_BUTTON_LEFT)) mouseState.Mouse1Released = true;
            if (Raylib.IsMouseButtonReleased(Raylib_cs.MouseButton.MOUSE_BUTTON_RIGHT)) mouseState.Mouse2Released = true;
            if (Raylib.IsMouseButtonReleased(Raylib_cs.MouseButton.MOUSE_BUTTON_MIDDLE)) mouseState.Mouse3Released = true;
            if (Raylib.IsMouseButtonReleased(Raylib_cs.MouseButton.MOUSE_BUTTON_BACK)) mouseState.Mouse4Released = true;
            if (Raylib.IsMouseButtonReleased(Raylib_cs.MouseButton.MOUSE_BUTTON_FORWARD)) mouseState.Mouse5Released = true;

            mouseState.MousePos = Raylib.GetMousePosition().ToNucleus();
            mouseState.MouseDelta = Raylib.GetMouseDelta().ToNucleus();
            mouseState.MouseScroll = Raylib.GetMouseWheelMoveV().ToNucleus();

            RunEventModifyMouseState(ref mouseState);
            frameState.MouseState = mouseState;

            // Keyboard processing
            KeyboardState keyboardState = new();

            while (true) {
                int keyPressed = Raylib.GetKeyPressed();

                if (keyPressed == 0)
                    break;

                keyboardState.KeysPressed.Add(keyPressed);
                keyboardState.KeyOrder.Add(keyPressed);

                if (!keyboardState.KeyPressCounts.ContainsKey(keyPressed))
                    keyboardState.KeyPressCounts[keyPressed] = 1;
                else
                    keyboardState.KeyPressCounts[keyPressed] += 1;
            }

            foreach (int key in KeyboardLayout.USA.Keys) {
                if (Raylib.IsKeyDown((Raylib_cs.KeyboardKey)key))
                    keyboardState.KeysHeld.Add(key);
                if (Raylib.IsKeyPressed((Raylib_cs.KeyboardKey)key))
                    keyboardState.KeysReleased.Add(key);
            }

            RunEventModifyKeyboardState(ref keyboardState);

            bool ranKeybinds = false;
            if (EngineCore.KeyboardFocusedElement != null) {
                ranKeybinds = EngineCore.KeyboardFocusedElement.Keybinds.TestKeybinds(keyboardState);
                if (!ranKeybinds) {
                    ranKeybinds = UI.Keybinds.TestKeybinds(keyboardState);
                    if (!ranKeybinds) {
                        foreach (var keyPress in keyboardState.KeysPressed)
                            EngineCore.KeyboardFocusedElement.KeyPressed(keyboardState, KeyboardLayout.USA.FromInt(keyPress));
                        foreach (var keyRelease in keyboardState.KeysReleased)
                            EngineCore.KeyboardFocusedElement.KeyReleased(keyboardState, KeyboardLayout.USA.FromInt(keyRelease));
                    }
                }
            }
            frameState.KeyboardState = keyboardState;

            // UI thinking should happen here because if a popup UI element exists, we need to block input to the game. Don't just block Think though
            int rebuilds = Element.LayoutRecursive(UI, frameState);

            Element? hoveredElement = Element.ResolveElementHoveringState(UI, frameState, new(0));
            frameState.HoveredUIElement = hoveredElement;
            UI.Hovered = hoveredElement;

            if (frameState.MouseState.MouseClicked) {
                if (UI.Hovered != null) {
                    UI.Depressed = frameState.HoveredUIElement;

                    if (frameState.MouseState.Mouse1Clicked) UI.Hovered.MouseClickOccur(frameState, MouseButton.Mouse1);
                    if (frameState.MouseState.Mouse2Clicked) UI.Hovered.MouseClickOccur(frameState, MouseButton.Mouse2);
                    if (frameState.MouseState.Mouse3Clicked) UI.Hovered.MouseClickOccur(frameState, MouseButton.Mouse3);
                    if (frameState.MouseState.Mouse4Clicked) UI.Hovered.MouseClickOccur(frameState, MouseButton.Mouse4);
                    if (frameState.MouseState.Mouse5Clicked) UI.Hovered.MouseClickOccur(frameState, MouseButton.Mouse5);
                }
            }
            if (frameState.MouseState.MouseReleased) {
                if (UI.Depressed != null) {
                    if (UI.Hovered == UI.Depressed) {
                        if (frameState.MouseState.Mouse1Released) UI.Hovered.MouseReleaseOccur(frameState, MouseButton.Mouse1);
                        if (frameState.MouseState.Mouse2Released) UI.Hovered.MouseReleaseOccur(frameState, MouseButton.Mouse2);
                        if (frameState.MouseState.Mouse3Released) UI.Hovered.MouseReleaseOccur(frameState, MouseButton.Mouse3);
                        if (frameState.MouseState.Mouse4Released) UI.Hovered.MouseReleaseOccur(frameState, MouseButton.Mouse4);
                        if (frameState.MouseState.Mouse5Released) UI.Hovered.MouseReleaseOccur(frameState, MouseButton.Mouse5);
                    }
                    UI.Depressed.Depressed = false;
                    UI.Depressed = null;
                }
            }
            if (!mouseState.MouseScroll.IsZero()) {
                if (IValidatable.IsValid(UI.Hovered) && UI.Hovered.InputDisabled == false) {
                    UI.Hovered.MouseScrollOccur(frameState, mouseState.MouseScroll);
                }
            }

            if (LastFrameState.MouseState.MouseHeld && FrameState.MouseState.MouseHeld && !FrameState.MouseState.MouseDelta.IsZero() && IValidatable.IsValid(UI.Depressed) && UI.Depressed.InputDisabled == false)
                UI.Depressed.MouseDragOccur(frameState, FrameState.MouseState.MouseDelta);

            EngineCore.CurrentFrameState = frameState; FrameState = frameState;
            Element.ThinkRecursive(UI, frameState);

            // If an element has keyboard focus, wipe the keyboard state because the game shouldnt get that information
            if (EngineCore.KeyboardFocusedElement != null)
                frameState.KeyboardState = new();

            // The frame state is basically complete after PreThink and UI layout/hover resolving, so it should be stored
            // Last change will be after element thinking
            EngineCore.CurrentFrameState = frameState; FrameState = frameState;

            if (!ranKeybinds)
                ranKeybinds = Keybinds.TestKeybinds(frameState.KeyboardState);

            RunEventThink(frameState);
            RunEventPostThink(frameState);

            if (false) {
                var size = 16;
                for (int gx = 0; gx < 100; gx++) {
                    for (int gy = 0; gy < 100; gy++) {
                        var g = ((gy % 2) + gx) % 2 == 0 ? 45 : 15;
                        Graphics2D.SetDrawColor(g, g, g, 255);
                        Graphics2D.DrawRectangle(gx * size, gy * size, size, size);
                    }
                }
            }

            System.Numerics.Vector3 offset = Draw3DCoordinateStart == Draw3DCoordinateStart.Centered0_0 ? new(0, 0, 0) : new(frameState.WindowWidth / 2, frameState.WindowHeight / 2, 0);

            var cam = new Camera3D() {
                Projection = CameraProjection.CAMERA_ORTHOGRAPHIC,
                FovY = frameState.WindowHeight * 1,
                Position = offset + new System.Numerics.Vector3(0, 0, -500),
                Target = offset + new System.Numerics.Vector3(0, 0, 0),
                Up = new(0, -1, 0)
            };

            CalcView(frameState, ref cam);
            RunEventPreRenderBackground(frameState);
            frameState.Camera = cam;

            //Graphics.ScissorRect(RectangleF.XYWH(frameState.WindowX, frameState.WindowY, frameState.WindowWidth, frameState.WindowHeight));
            Raylib.BeginMode3D(cam);

            //Raylib.DrawLine3D(new(0, 0, 0), new(256, 0, 0), new Color(255, 70, 60, 200));
            //Raylib.DrawLine3D(new(0, 0, 0), new(0, 256, 0), new Color(80, 255, 70, 200));

            RunEventPreRender(frameState);

            RunEventRender(frameState);

            Raylib.EndMode3D();
            //Graphics.ScissorRect();

            RunEventRender2D(frameState);
            Element.DrawRecursive(UI);
            //Raylib.EndTextureMode();

            /*Raylib.DrawTexturePro(RenderTarget.Value.Texture,
                new Rectangle(0, 0, RenderTarget.Value.Texture.Width, -RenderTarget.Value.Texture.Height),
                new Rectangle(0, 0, RenderTarget.Value.Texture.Width, RenderTarget.Value.Texture.Height),
                new System.Numerics.Vector2(0, 0),
                0,
                Color.WHITE);*/

            // Only really exists for REALLY late rendering
            RunEventPostRender(frameState);
            UnlockEntityBuffer();

            var FPS = Raylib.GetFPS();
            List<string> fields = [
                $"Nucleus Level / {EngineCore.GameInfo} - Debugger",
                "",
                $"Window",
                $"    Resolution        : {frameState.WindowWidth}x{frameState.WindowHeight}",
                $"    FPS               : {FPS} ({1000f / FPS}ms render time)",
                $"Level",
                $"    Level Classname   : {this.GetType().Name}",
                $"    Level Entities    : {EntityList.Count}",
                $"User Interface",
                $"    UI Elements       : {UI.Elements.Count}",
                $"    UI Rebuilds       : {rebuilds}",
                $"    Hovered Element   : {(UI.Hovered == null ? "<null>" : UI.Hovered)}",
                $"    Depressed Element : {(UI.Depressed == null ? "<null>" : UI.Depressed)}",
                $"State",
                $"    Mouse State       : {frameState.MouseState}",
                $"    Keyboard State    : {frameState.KeyboardState}",
            ];

            if (FrameDebuggingStrings.Count > 0) {
                fields.Add("");
                fields.Add("Game-specific Debug Fields:");
                foreach (string s in FrameDebuggingStrings)
                    fields.Add("    " + s);
            }

            for (int i = 0; i < fields.Count; i++) {
                var tx = 12;
                var ty = (frameState.WindowHeight - 12) - ((fields.Count - i) * 16);

                Graphics2D.SetDrawColor(new(255, 255, 255, 255));
                Graphics2D.DrawText(tx, ty, fields[i], "Consolas", 14);
            }

            ConsoleSystem.Draw();
        }

        public bool HasEntity(Entity entity) => EntityHash.Contains(entity);
    }
}
