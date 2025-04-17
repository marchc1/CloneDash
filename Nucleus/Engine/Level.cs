using Newtonsoft.Json.Linq;
using Nucleus.Audio;
using Nucleus.Core;
using Nucleus.ManagedMemory;
using Nucleus.Rendering;
using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MouseButton = Nucleus.Types.MouseButton;

namespace Nucleus.Engine
{
	/// <summary>
	/// Game level, powers *everything*, including menus.
	/// Which means while designing this, it needs to be kept in mind that menus are just game levels.
	/// <br></br>
	/// Remember: levels store LOGIC, and, when needed, game-level-specific data. But any data such as entities, UI panels, textures etc. should remain within the engine core
	/// </summary>
	public abstract class Level
	{
		// Managed memory
		public TextureManagement Textures { get; } = new();
		public SoundManagement Sounds { get; } = new();
		public TimerManagement Timers { get; }
		public ModelManagement Models { get; } = new();

		public Level() {
			Timers = new(this);
		}

		public Draw3DCoordinateStart Draw3DCoordinateStart { get; set; } = Draw3DCoordinateStart.Centered0_0;
		public T As<T>() where T : Level => (T)this;
		public T? AsNullable<T>() where T : Level => this is T ret ? ret : null;

		private RectangleF? __view = null;
		private bool __viewDirty = true;

		/// <summary>
		/// Specifies a viewing rectangle for this level. If null, scales to the screen. Currently not functional
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
		public void ResetUI() {
			UI = Element.Create<UserInterface>();
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
		public virtual void ShouldEngineClose() { }

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
			Textures.Dispose();
			Sounds.Dispose();

			Entity[] dead = EntityList.ToArray();
			foreach (Entity ent in dead) {
				ent.Remove();
			}

			EntityList.Clear();
			EntityHash.Clear();

			OnUnload();
			if (RenderTarget.HasValue) {
				Raylib.UnloadRenderTexture(RenderTarget.Value);
			}
		}
		public virtual void OnUnload() { }

		/// <summary>
		/// Called when the engine begins loading a level. If the level needs to do work for an extended period of time, return true
		/// <br>todo: proper async loading</br>
		/// </summary>
		public virtual void Initialize(params object[] args) {
			return;
		}

		// ------------------------------------------------------------------------------------------ //
		// Entity system
		// ------------------------------------------------------------------------------------------ //

		// These are separate for two reasons:
		// 1. IsValid checks on entities can be done with the HashSet
		// 3. Finding entities by their unique properties/for the sake of execution can be done with the List
		// Basically just trying to cover every possible

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

		public UserInterface UI { get; private set; }
		public void InitializeUI() {
			if (UI != null) return;
			UI = Element.Create<UserInterface>();
			UI.EngineLevel = this;
		}

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
			Logs.Debug($"Level.Add call bufferLock = {__lockedBuffer} ent {ent}");

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

		public DateTime Start { get; private set; } = DateTime.UtcNow;

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

		public struct DebugRecord(bool containsValue, string key, string? value = null)
		{
			public static implicit operator DebugRecord(string from) {
				bool containsValue = false;
				string key = "";
				string? value = null;

				var colon = from.IndexOf(':');
				if (colon == -1) {
					containsValue = false;
				}
				else {
					if (colon == from.Length - 1) {
						containsValue = false;
					}
					else
						containsValue = true;
				}

				if (containsValue) {
					key = from.Substring(0, colon);
					value = from.Substring(colon + 1).Trim();
				}
				else {
					key = from;
				}

				return new(containsValue, key, value);
			}

			public override string ToString() {
				return $"{key}{(containsValue ? $": {value}" : "")}";
			}
		}

		private void RunThreadExecutionTimeMethods(ThreadExecutionTime t) {
			MainThread.Run(t);
			Timers.Run(t);
		}

		Stopwatch profiler = new();
		Stopwatch timing = new();

		public void PreInitialize() {
			timing.Start();
		}
		/// <summary>
		/// Call this every frame.
		/// </summary>
		public void Frame() {
			profiler.Reset();
			profiler.Start();
			RunThreadExecutionTimeMethods(ThreadExecutionTime.BeforeFrame);

			LastFrameState = FrameState;

			LastRealtime = Realtime;
			Realtime = timing.Elapsed.TotalSeconds;
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
				width = EngineCore.GetScreenSize().X;
				height = EngineCore.GetScreenSize().Y;
			}

			frameState.WindowX = x;
			frameState.WindowY = y;
			frameState.WindowWidth = width;
			frameState.WindowHeight = height;

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

			mouseState.MousePos = EngineCore.MousePos;
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
				KeyboardState emulatedState = EngineCore.KeyboardFocusedElement.KeyboardInputMarshal.State(keyboardState);
				ranKeybinds = EngineCore.KeyboardFocusedElement.Keybinds.TestKeybinds(emulatedState);
				if (!ranKeybinds) {
					ranKeybinds = UI.Keybinds.TestKeybinds(emulatedState);
					if (!ranKeybinds) {
						foreach (var keyPress in emulatedState.KeysPressed) {
							if (IValidatable.IsValid(EngineCore.KeyboardFocusedElement))
								EngineCore.KeyboardFocusedElement.KeyPressedOccur(emulatedState, KeyboardLayout.USA.FromInt(keyPress));
						}
						foreach (var keyRelease in emulatedState.KeysReleased) {
							if (IValidatable.IsValid(EngineCore.KeyboardFocusedElement))
								EngineCore.KeyboardFocusedElement.KeyReleasedOccur(emulatedState, KeyboardLayout.USA.FromInt(keyRelease));
						}
					}
				}
			}
			frameState.KeyboardState = keyboardState;

			if (!Paused) RunEventPreThink(ref frameState);

			// UI thinking should happen here because if a popup UI element exists, we need to block input to the game. Don't just block Think though
			int rebuilds = Element.LayoutRecursive(UI, frameState);

			Element? hoveredElement = Element.ResolveElementHoveringState(UI, frameState, EngineCore.GetGlobalScreenOffset(), EngineCore.GetScreenBounds());
			frameState.HoveredUIElement = hoveredElement;
			UI.Hovered = hoveredElement;

			EngineCore.CurrentFrameState = frameState;
			if (frameState.MouseState.MouseClicked) {
				if (UI.Hovered != null) {
					UI.Depressed = frameState.HoveredUIElement;

					if (frameState.MouseState.Mouse1Clicked) UI.Hovered.MouseClickOccur(frameState, MouseButton.Mouse1);
					if (frameState.MouseState.Mouse2Clicked) UI.Hovered.MouseClickOccur(frameState, MouseButton.Mouse2);
					if (frameState.MouseState.Mouse3Clicked) UI.Hovered.MouseClickOccur(frameState, MouseButton.Mouse3);
					if (frameState.MouseState.Mouse4Clicked) UI.Hovered.MouseClickOccur(frameState, MouseButton.Mouse4);
					if (frameState.MouseState.Mouse5Clicked) UI.Hovered.MouseClickOccur(frameState, MouseButton.Mouse5);
				}
				else {
					if (frameState.MouseState.Mouse1Clicked) UI.TriggerElementClicked(null, frameState, MouseButton.Mouse1);
					if (frameState.MouseState.Mouse2Clicked) UI.TriggerElementClicked(null, frameState, MouseButton.Mouse2);
					if (frameState.MouseState.Mouse3Clicked) UI.TriggerElementClicked(null, frameState, MouseButton.Mouse3);
					if (frameState.MouseState.Mouse4Clicked) UI.TriggerElementClicked(null, frameState, MouseButton.Mouse4);
					if (frameState.MouseState.Mouse5Clicked) UI.TriggerElementClicked(null, frameState, MouseButton.Mouse5);
				}
			}

			EngineCore.CurrentFrameState = frameState;
			if (frameState.MouseState.MouseReleased) {
				if (UI.Depressed != null) {
					if (UI.Hovered == UI.Depressed) {
						if (frameState.MouseState.Mouse1Released)
							UI.Hovered.MouseReleaseOccur(frameState, MouseButton.Mouse1);
						if (frameState.MouseState.Mouse2Released)
							UI.Hovered.MouseReleaseOccur(frameState, MouseButton.Mouse2);
						if (frameState.MouseState.Mouse3Released)
							UI.Hovered.MouseReleaseOccur(frameState, MouseButton.Mouse3);
						if (frameState.MouseState.Mouse4Released)
							UI.Hovered.MouseReleaseOccur(frameState, MouseButton.Mouse4);
						if (frameState.MouseState.Mouse5Released)
							UI.Hovered.MouseReleaseOccur(frameState, MouseButton.Mouse5);
					}
					else {
						if (frameState.MouseState.Mouse1Released)
							UI.Depressed.MouseLostOccur(frameState, MouseButton.Mouse1);
						if (frameState.MouseState.Mouse2Released)
							UI.Depressed.MouseLostOccur(frameState, MouseButton.Mouse2);
						if (frameState.MouseState.Mouse3Released)
							UI.Depressed.MouseLostOccur(frameState, MouseButton.Mouse3);
						if (frameState.MouseState.Mouse4Released)
							UI.Depressed.MouseLostOccur(frameState, MouseButton.Mouse4);
						if (frameState.MouseState.Mouse5Released)
							UI.Depressed.MouseLostOccur(frameState, MouseButton.Mouse5);
					}
					if (UI.Depressed != null) {
						UI.Depressed.Depressed = false;
						UI.Depressed = null;
					}
				}
			}

			if (!mouseState.MouseScroll.IsZero()) {
				if (IValidatable.IsValid(UI.Hovered) && UI.Hovered.InputDisabled == false) {
					Element e = UI.Hovered;
					for (int i = 0; i < 1000; i++) {
						if (!IValidatable.IsValid(e))
							break;

						e.ConsumedScrollEvent = false;
						e.MouseScrollOccur(frameState, mouseState.MouseScroll);
						if (e.ConsumedScrollEvent)
							break;

						e = e.Parent;
					}
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
			RunThreadExecutionTimeMethods(ThreadExecutionTime.AfterFrameStateConstructed);

			if (!ranKeybinds)
				ranKeybinds = Keybinds.TestKeybinds(frameState.KeyboardState);

			if (!Paused) RunEventThink(frameState);
			if (!Paused) RunEventPostThink(frameState);

			RunThreadExecutionTimeMethods(ThreadExecutionTime.AfterThink);
			/*if (false) {
				var size = 16;
				for (int gx = 0; gx < 100; gx++) {
					for (int gy = 0; gy < 100; gy++) {
						var g = ((gy % 2) + gx) % 2 == 0 ? 45 : 15;
						Graphics2D.SetDrawColor(g, g, g, 255);
						Graphics2D.DrawRectangle(gx * size, gy * size, size, size);
					}
				}
			}*/

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

			DebugOverlay.Render();

			if (ui_hoverresult.GetBool() && UI.Hovered != null) {
				Graphics2D.SetDrawColor(255, 255, 255);
				Graphics2D.DrawRectangleOutline(RectangleF.FromPosAndSize(UI.Hovered.GetGlobalPosition(), UI.Hovered.RenderBounds.Size), 1);
				Graphics2D.DrawText(UI.Hovered.GetGlobalPosition() + new Vector2F(0, UI.Hovered.RenderBounds.H), $"Element: {UI.Hovered}", "Consolas", 14, Anchor.BottomLeft);
			}

			Graphics2D.ResetDrawingOffset();
			if (ui_showupdates.GetBool()) RenderShowUpdates();
			if (ui_visrenderbounds.GetBool()) VisRenderBounds(UI);
			

			UnlockEntityBuffer();
			var FPS = Raylib.GetFPS();
			List<DebugRecord> fields;
			if (EngineCore.ShowDebuggingInfo) {
				Graphics2D.ResetDrawingOffset();
				fields = [
					$"Nucleus Level / {EngineCore.GameInfo} - DebugContext",
					"",
					$"Engine",
					$"    [CPU]  Sound Memory   : {IManagedMemory.NiceBytes(Sounds.UsedBits / 8)}",
					$"    [GPU]  Texture Memory : {IManagedMemory.NiceBytes(Textures.UsedBits)}",
					$"Window",
					$"    Resolution            : {frameState.WindowWidth}x{frameState.WindowHeight}",
					$"    Graphics2D.Offset     : {Graphics2D.Offset}",
					$"    FPS                   : {FPS} ({Raylib.GetFrameTime() * 1000:0.##}ms render time)",
					$"Level",
					$"    Level Classname       : {this.GetType().Name}",
					$"    Level Entities        : {EntityList.Count}",
					$"User Interface",
					$"    UI Elements           : {UI.Elements.Count}",
					$"    UI Rebuilds           : {rebuilds}",
					$"    Hovered Element       : {(UI.Hovered == null ? "<null>" : UI.Hovered)}",
					$"    Depressed Element     : {(UI.Depressed == null ? "<null>" : UI.Depressed)}",
					$"State",
					$"    Mouse State           : {frameState.MouseState}",
					$"    Keyboard State        : {frameState.KeyboardState}",
				];
			}
			else
				fields = [
                    //$"FPS : {FPS} ({Math.Round(1000f / FPS, 2)}ms render time)"
                ];

			if (EngineCore.ShowDebuggingInfo && FrameDebuggingStrings.Count > 0) {
				fields.Add("");
				fields.Add("Game-specific Debug Fields:");
				foreach (string s in FrameDebuggingStrings)
					fields.Add("    " + s);
			}

			int maxKey = 0;
			int maxValue = 0;

			for (int i = 0; i < fields.Count; i++) {
				var tx = 12;
				var ty = (frameState.WindowHeight - 16) - ((fields.Count - i) * 14);

				var t = fields[i].ToString();
				Graphics2D.SetDrawColor(new(255, 255, 255, 255));
				Graphics2D.DrawText(tx, ty, t, "Consolas", 12, Anchor.TopLeft);
			}

			if (EngineCore.ShowDebuggingInfo)
				ConsoleSystem.Draw();

			RunThreadExecutionTimeMethods(ThreadExecutionTime.AfterFrame);
			profiler.Stop();
			EngineCore.FrameCostMS = (float)profiler.Elapsed.TotalMilliseconds;
			var nanoseconds = (float)profiler.Elapsed.TotalNanoseconds;

			if (nanoseconds < 1000) // less than 1 microsecond
				EngineCore.FrameCost = $"{nanoseconds} ns";
			else if (nanoseconds < 2000) // between 1 and 2 microseconds
				EngineCore.FrameCost = $"{nanoseconds / 1000.0} us";
			else if (nanoseconds < 100_000) // less than 0.1 milliseconds
				EngineCore.FrameCost = $"{nanoseconds / 1000.0} us";
			else if (nanoseconds < 2_000_000) // between 0.1 and 2 milliseconds
				EngineCore.FrameCost = $"{nanoseconds / 1_000_000.0} ms";
			else // greater than or equal to 2 milliseconds
				EngineCore.FrameCost = $"{nanoseconds / 1_000_000_000.0} s";

		}

		private void RenderShowUpdates() {
			var now = Curtime;
			foreach (var element in UI.Elements) {
				var lastLayout = element.LastLayoutTime;
				var delta = 1 - (Math.Min(now - lastLayout, 0.5) * 2);
				if (delta > 1) continue;

				Graphics2D.SetDrawColor(255, 50, 50, (int)(150f * delta));
				Graphics2D.DrawRectangle(element.GetGlobalPosition(), element.RenderBounds.Size);
			}
		}

		private void VisRenderBounds(Element e) {
			foreach (var element in e.Children) {
				if (!element.Visible || element.EngineInvisible) continue;
				Graphics2D.SetDrawColor(255, 255, 255);
				Graphics2D.DrawRectangleOutline(element.GetGlobalPosition(), element.RenderBounds.Size);
				VisRenderBounds(element);
			}
		}

		public static ConVar ui_hoverresult 
			= ConVar.Register("ui_hoverresult", "0", ConsoleFlags.None, "Highlights the currently hovered element", 0, 1);
		public static ConVar ui_visrenderbounds
			= ConVar.Register("ui_visrenderbounds", "0", ConsoleFlags.None, "Visualizes each elements render bounds as a outlined rectangle.", 0, 1);
		public static ConVar ui_showupdates
			= ConVar.Register("ui_showupdates", "0", ConsoleFlags.None, "Visualize layout updates.", 0, 1);
		public static ConCommand ui_elementcount
			= ConCommand.Register("ui_elementcount", (_, _) => Logs.Print($"UI Elements: {EngineCore.Level.UI.Elements.Count}"), ConsoleFlags.None, "Highlights the currently hovered element");

		public bool HasEntity(Entity entity) => EntityHash.Contains(entity);
	}
}
