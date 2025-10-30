using Newtonsoft.Json.Linq;

using Nucleus.Audio;
using Nucleus.Commands;
using Nucleus.Core;
using Nucleus.Entities;
using Nucleus.Input;
using Nucleus.ManagedMemory;
using Nucleus.Rendering;
using Nucleus.Types;
using Nucleus.UI;

using Raylib_cs;

using SDL;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MouseButton = Nucleus.Input.MouseButton;

public enum HitTestResult : byte
{
	Normal,
	Draggable,
	ResizeTopLeft,
	ResizeTop,
	ResizeTopRight,
	ResizeRight,
	ResizeBottomRight,
	ResizeBottom,
	ResizeBottomLeft,
	ResizeLeft
}

namespace Nucleus.Engine
{
	/// <summary>
	/// Game level, powers *everything*, including menus.
	/// Which means while designing this, it needs to be kept in mind that menus are just game levels.
	/// <br></br>
	/// Remember: levels store LOGIC, and, when needed, game-level-specific data. But any data such as entities, UI panels, textures etc. should remain within the engine core
	/// </summary>
	public abstract class Level : IValidatable
	{
		// Managed memory
		public TextureManagement Textures { get; } = new();
		public SoundManagement Sounds { get; } = new();
		public TimerManagement Timers { get; }
		public ModelManagement Models { get; } = new();
		public ShaderManagement Shaders { get; } = new();

		internal bool __isValid = false;
		public bool IsValid() => __isValid;

		public Level() {
			Timers = new(this);
		}

		private List<Action<Level>> finalizers = [];
		public void AddFinalizer(Action<Level> finalizer) => finalizers.Add(finalizer);
		private void runFinalizers() {
			foreach (var finalizer in finalizers)
				finalizer(this);
			finalizers.Clear();
		}

		public Draw3DCoordinateStart Draw3DCoordinateStart { get; set; } = Draw3DCoordinateStart.Centered0_0;
		public T As<T>() where T : Level => (T)this;
		public T? AsNullable<T>() where T : Level => this is T ret ? ret : null;

		public void ResetUI() {
			UI = Element.Create<UserInterface>();
			UI.EngineLevel = this;
			UI.Window = EngineCore.Window;
		}
		public virtual void PreThink(ref FrameState frameState) { }
		public virtual void ModifyMouseState(ref MouseState mouseState) { }
		public virtual void ModifyKeyboardState(ref KeyboardState keyboardState) { }
		public virtual void Think(FrameState frameState) { }
		public virtual void PostThink(FrameState frameState) { }
		public virtual void CalcView2D(FrameState frameState, ref Camera2D cam) { }
		public virtual void CalcView3D(FrameState frameState, ref Camera3D cam) { }
		public virtual void PreRenderBackground(FrameState frameState) { }
		public virtual void PreRender(FrameState frameState) { }
		public virtual void Render(FrameState frameState) { }
		public virtual void Render2D(FrameState frameState) { }
		public virtual void PostRender(FrameState frameState) { }
		public virtual void PreWindowClose() { }
		public virtual HitTestResult WindowHitTest(Vector2F point) => HitTestResult.Normal;

		public void RunEventPreThink(ref FrameState frameState) {
			PreThink(ref frameState);
			foreach (Entity entity in EntityList)
				if (entity.Enabled && entity.ThinksForItself)
					entity.PreThink(ref frameState);
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
			runFinalizers();

			Textures.Dispose();
			Sounds.Dispose();
			Models.Dispose();
			Shaders.Dispose();

			UI.Dispose();

			Entity[] dead = EntityList.ToArray();
			foreach (Entity ent in dead) {
				ent.Remove();
			}

			EntityList.Clear();
			EntityHash.Clear();
			__isValid = false;

			OnUnload();
			Unloaded?.Invoke();
		}
		public delegate void UnloadDelegate();
		public event UnloadDelegate? Unloaded;

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

		public List<string> FrameDebuggingStrings { get; set; } = [];

		public List<Entity> Entities => EntityList;

		public UserInterface UI { get; private set; }
		public void InitializeUI() {
			if (UI != null) return;
			UI = Element.Create<UserInterface>();
			UI.EngineLevel = this;
			UI.Window = EngineCore.Window;
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

		public double LastRendertime { get; private set; } = 0;
		public double Rendertime { get; private set; } = 0;
		public double RendertimeDelta { get; private set; } = 0;

		public bool Paused { get; set; } = false;

		public FrameState LastFrameState { get; private set; } = FrameState.Default;
		public FrameState FrameState { get; private set; } = FrameState.Default;

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

		Stopwatch timing = new();

		public bool Render3D { get; set; } = true;

		public void PreInitialize() {
			timing.Start();
		}

		private void SwapFrameStates() {
			var last = LastFrameState;
			var curr = FrameState;

			LastFrameState = curr;
			FrameState = last;

			FrameState.Reset();
		}

		private void test() {
			Vector2F screenBounds = new(1600, 900);
			Graphics2D.SetDrawColor(30, 5, 0);
			Graphics2D.DrawRectangle(0, 0, screenBounds.W, screenBounds.H);
			Graphics2D.SetDrawColor(240, 70, 60);
			Graphics2D.DrawText(screenBounds.X / 2, screenBounds.Y / 2, "No level loaded or in the process of loading!", Graphics2D.UI_FONT_NAME, 24, TextAlignment.Center, TextAlignment.Bottom);
			Graphics2D.DrawText(screenBounds.X / 2, screenBounds.Y / 2, "Make sure you're changing EngineCore.Level.", Graphics2D.UI_FONT_NAME, 18, TextAlignment.Center, TextAlignment.Top);
		}
		double lastRenderTime = -10;
		public bool RenderedFrame { get; set; } = false;
		public bool IsRendering { get; set; } = false;

		readonly Stopwatch updateTrack = new();
		readonly Stopwatch renderTrack = new();

		/// <summary>
		/// Call this every frame.
		/// </summary>
		public void Frame() {
			RenderedFrame = false;

			updateTrack.Reset();
			renderTrack.Reset();

			updateTrack.Start();
			RunThreadExecutionTimeMethods(ThreadExecutionTime.BeforeFrame);

			SwapFrameStates();

			LastRealtime = Realtime;
			Realtime = timing.Elapsed.TotalSeconds;
			RealtimeDelta = Realtime - LastRealtime;

			if (!Paused) {
				LastCurtime = Curtime;
				Curtime += Math.Clamp(RealtimeDelta, 0, 0.1);
				CurtimeDelta = Curtime - LastCurtime;
			}

			// Construct a FrameState from inputs
			UnlockEntityBuffer(); LockEntityBuffer();
			FrameDebuggingStrings.Clear();
			FrameState frameState = FrameState;

			float x, y, width, height;

			// TODO: reconsider the "view" system. Need a good way to set the viewport of a level, multiple levels, etc
			// unfortunately engine infrastructure wasnt built for this like it should have been...
			x = 0;
			y = 0;
			var size = EngineCore.GetScreenSize();
			width = size.X;
			height = size.Y;

			frameState.WindowX = x;
			frameState.WindowY = y;
			frameState.WindowWidth = width;
			frameState.WindowHeight = height;

			frameState.Keyboard.Clear();
			if (EngineCore.Window.MouseFocused)
				EngineCore.Window.FlushMouseStateInto(ref frameState.Mouse);
			if (EngineCore.Window.InputFocused)
				EngineCore.Window.FlushKeyboardStateInto(ref frameState.Keyboard);

			UI.HandleInput();

			if (!Paused) RunEventPreThink(ref frameState);

			UI.HandleThinking();

			// If an element has keyboard focus, wipe the keyboard state because the game shouldnt get that information
			if (IValidatable.IsValid(UI.KeyboardFocusedElement))
				frameState.Keyboard = new();

			// The frame state is basically complete after PreThink and UI layout/hover resolving, so it should be stored
			// Last change will be after element thinking
			RunThreadExecutionTimeMethods(ThreadExecutionTime.AfterFrameStateConstructed);

			if (!Paused) RunEventThink(frameState);
			if (!Paused) RunEventPostThink(frameState);

			RunThreadExecutionTimeMethods(ThreadExecutionTime.AfterThink);

			if ((Realtime - lastRenderTime) >= EngineCore.RenderRate) {
				updateTrack.Stop();
				renderTrack.Start();
				lastRenderTime = Realtime;
				RenderedFrame = true;
				IsRendering = true;

				if (!Paused) {
					LastRendertime = Rendertime;
					Rendertime = Curtime;
					RendertimeDelta = Rendertime - LastRendertime;
				}

				System.Numerics.Vector3 offset = Draw3DCoordinateStart == Draw3DCoordinateStart.Centered0_0 ? new(0, 0, 0) : new(frameState.WindowWidth / 2, frameState.WindowHeight / 2, 0);

				Surface.Clear(0, 0, 0, 255);

				// TODO: Separate rendering logic entirely to the level responsible...
				bool render3D = Render3D; // Store state in case a mid frame update happens to that variable (which would almost certainly break state?)
				if (render3D) {
					var cam3d = new Camera3D() {
						Projection = CameraProjection.Orthographic,
						FovY = frameState.WindowHeight * 1,
						Position = offset + new System.Numerics.Vector3(0, 0, -500),
						Target = offset + new System.Numerics.Vector3(0, 0, 0),
						Up = new(0, -1, 0)
					};

					CalcView3D(frameState, ref cam3d);
					RunEventPreRenderBackground(frameState);

					EngineCore.Window.BeginMode3D(cam3d);
				}
				else {
					var cam2d = new Camera2D() { };

					CalcView2D(frameState, ref cam2d);
					RunEventPreRenderBackground(frameState);

					EngineCore.Window.BeginMode2D(cam2d);
				}
				//Raylib.DrawLine3D(new(0, 0, 0), new(256, 0, 0), new Color(255, 70, 60, 200));
				//Raylib.DrawLine3D(new(0, 0, 0), new(0, 256, 0), new Color(80, 255, 70, 200));

				RunEventPreRender(frameState);

				RunEventRender(frameState);
				if (render3D)
					EngineCore.Window.EndMode3D();
				else
					EngineCore.Window.EndMode2D();
				//Graphics.ScissorRect();

				RunEventRender2D(frameState);
				UI.Render();
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

				var FPS = EngineCore.FPS;
				List<DebugRecord> fields;
				if (EngineCore.ShowDebuggingInfo && !IValidatable.IsValid(InGameConsole.Instance)) {
					Graphics2D.ResetDrawingOffset();
					fields = [
						$"Nucleus Level / {EngineCore.GameInfo} - DebugContext",
					"",
					// $"Engine",
					// $"    [CPU]  Sound Memory   : {IManagedMemory.NiceBytes(Sounds.UsedBits / 8)}",
					// $"    [GPU]  Texture Memory : {IManagedMemory.NiceBytes(Textures.UsedBits)}",
					// $"Engine - Window",
					// $"    Resolution            : {frameState.WindowWidth}x{frameState.WindowHeight}",
					// $"    FPS                   : {FPS} ({EngineCore.FrameTime * 1000:0.##}ms render time)",
					// $"Engine - Current Level",
					// $"    Level Classname       : {this.GetType().Name}",
					// $"    Level Entities        : {EntityList.Count}",
					// $"Engine - User Interface",
					// $"    UI Elements           : {UI.Elements.Count}",
					// $"    UI Rebuilds           : {rebuilds}",
					// $"    UI State:             : hovered {UI.Hovered?.ToString() ?? "<null>"}, depressed {UI.Depressed?.ToString() ?? "<null>"}, focused {UI.Focused?.ToString() ?? "<null>"}",
					// $"Engine - State",
					// $"    Mouse State           : {frameState.Mouse}",
					// $"    Keyboard State        : {frameState.Keyboard}",
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
					var ty = (frameState.WindowHeight - 16) - ((fields.Count - i) * 12);

					var t = fields[i].ToString();
					Graphics2D.SetDrawColor(new(255, 255, 255, 255));
					Graphics2D.DrawText(tx, ty, t, "Consolas", 11, Anchor.TopLeft);
				}

				if (EngineCore.ShowDebuggingInfo)
					ConsoleSystem.Draw();

				IsRendering = false;
				renderTrack.Stop();
				EngineCore.SetTimeToRender(renderTrack.Elapsed);
			}
			updateTrack.Start();
			UnlockEntityBuffer();

			RunThreadExecutionTimeMethods(ThreadExecutionTime.AfterFrame);

			updateTrack.Stop();
			EngineCore.SetTimeToUpdate(updateTrack.Elapsed);
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
		public T GetEntity<T>(Predicate<Entity> predicate) where T : Entity {
			foreach (var entity in Entities) {
				if (predicate(entity))
					return entity as T ?? throw new Exception("The entity was found, but could not be cast to generic type");
			}

			throw new Exception("Predicate failed in GetEntity.");
		}
		public bool TryGetEntity<T>(Predicate<Entity> predicate, out T? found) where T : Entity {
			foreach (var entity in Entities) {
				if (predicate(entity)) {
					if (entity is not T) {
						found = default;
						return false;
					}
					found = entity as T;
					return true;
				}
			}

			found = default;
			return false;
		}

		internal void RunKeybinds() {
			Keybinds.TestKeybinds(FrameState.Keyboard);
		}
	}
}
