using Nucleus.Commands;
using Nucleus.Core;
using Nucleus.Debugging;
using Nucleus.Entities;
using Nucleus.Input;
using Nucleus.ManagedMemory;
using Nucleus.Rendering;
using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;

using System.Diagnostics;

namespace Nucleus.Engine;

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

/// <summary>
/// Game level, powers *everything*, including menus.
/// Which means while designing this, it needs to be kept in mind that menus are just game levels.
/// <br></br>
/// Remember: levels store LOGIC, and, when needed, game-level-specific data. But any data such as entities, UI panels, textures etc. should remain within the engine core
/// </summary>
[MarkForStaticConstruction]
public abstract class Level : IValidatable
{
	// Managed memory
	public TextureManagement Textures { get; } = new();
	public TimerManagement Timers { get; }
	public ModelManagement Models { get; } = new();
	public ShaderManagement Shaders { get; } = new();

	internal bool __isValid = false;
	public bool IsValid() => __isValid;

	public readonly DeveloperOverlay DeveloperOverlay;
	
	/// <summary>
	/// When true, this will block some <see cref="ConVar"/>'s from being modified by the user.
	/// </summary>
	public virtual bool IsInGame => false;
	public virtual ConsoleOverlaySettings GetConsoleOverlaySettings() => new() {
		Position = new(6, 6),
		Anchor = Anchor.TopLeft,
		DoNotRender = false,
		TextSize = 13,
		Parent = RootPanel
	};

	public Level() {
		Timers = new(this);
		DeveloperOverlay = new DeveloperOverlay(this);
	}

	private List<Action<Level>> finalizers = [];
	public void AddFinalizer(Action<Level> finalizer) => finalizers.Add(finalizer);
	private void runFinalizers() {
		foreach (var finalizer in finalizers)
			finalizer(this);
		finalizers.Clear();
	}

	public T As<T>() where T : Level => (T)this;
	public T? AsNullable<T>() where T : Level => this is T ret ? ret : null;

	public void ResetUI() {
		RootPanel = new();
		RootPanel.Window = EngineCore.Window;
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
	public virtual void PostRenderEntities(FrameState frameState) { }
	public virtual void PostRender(FrameState frameState) { }
	public virtual void PostRenderUI(FrameState frameState) { }
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
		PostRenderEntities(frameState);
	}
	public void RunEventPostRender(FrameState frameState) {
		PostRender(frameState);
		foreach (Entity entity in EntityList)
			if (entity.Enabled && entity.RendersItself)
				entity.PostRender(frameState);
	}

	public void RunEventPostRenderUI(FrameState frameState) {
		PostRenderUI(frameState);
	}
	public void Unload() {
		runFinalizers();

		// restructure-tests @ 2-12-2026: These have been commented out for now. The general ownership of textures is way too confusing.
		// Future works on restructuring the engine will resolve this. For starters, the level shouldn't even be the owner 
		// of these resources. There should be independent subsystems (IAudioSystem, IMaterialSystem, IModelSystem, etc) which
		// contain weak references to resources. 

		// Textures.Dispose();
		// Sounds.Dispose();
		// Models.Dispose();
		// Shaders.Dispose();

		RootPanel.Dispose();

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


	public List<Entity> Entities => EntityList;

	public UserInterface RootPanel { get; private set; }
	public void InitializeUI() {
		if (RootPanel != null) return;
		RootPanel = new UserInterface();
		RootPanel.Window = EngineCore.Window;
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


	readonly Stopwatch timing = new();

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

	double lastRenderTime = -10;
	public bool RenderedFrame { get; set; } = false;
	public bool IsRendering { get; set; } = false;

	readonly Stopwatch updateTrack = new();
	readonly Stopwatch renderTrack = new();

	public virtual bool OnFileDropped(string filepath, Vector2F pos) => false;
	public virtual bool OnTextDropped(string text, Vector2F pos) => false;

	readonly Queue<DragNDropItem> DragNDropFileEvents_ForNextMouseHover = [];
	readonly Queue<DragNDropItem> DragNDropTextEvents_ForNextMouseHover = [];

	public void FileDropped(DragNDropItem item, bool isWindowFocused) {
		if (!isWindowFocused) {
			// Due to position not being available at this; enqueue this event for later. SDL doesn't give us positioning so we need to wait
			// until the window focuses again.
			DragNDropFileEvents_ForNextMouseHover.Enqueue(item);
		}
		else {
			if (OnFileDropped(item, FrameState.Mouse.MousePos)) return;

			// Try sending it to the UI element we last hovered over, iterating through parents
			Element? e = RootPanel.GetHoveredElement();

			while (e != null) {
				if (e.FileDropped(item, FrameState.Mouse.MousePos))
					break;
				e = e.GetParent();
			}
		}
	}
	public void TextDropped(DragNDropItem item, bool isWindowFocused) {
		if (!isWindowFocused) {
			// Due to position not being available at this; enqueue this event for later. SDL doesn't give us positioning so we need to wait
			// until the window focuses again.
			DragNDropTextEvents_ForNextMouseHover.Enqueue(item);
		}
		else {
			if (OnTextDropped(item, FrameState.Mouse.MousePos)) return;

			// Try sending it to the UI element we last hovered over, iterating through parents
			Element? e = RootPanel.GetHoveredElement(); 
			while (e != null) {
				if (e.TextDropped(item, FrameState.Mouse.MousePos))
					break;
				e = e.GetParent();
			}
		}
	}

	
	/// <summary>
	/// Call this every frame.
	/// </summary>
	public void Frame() {
		RenderedFrame = false;

		updateTrack.Reset();
		renderTrack.Reset();

		updateTrack.Start();
		Timers.Run(ThreadExecutionTime.BeforeFrame);
		SwapFrameStates();

		LastRealtime = Realtime;
		Realtime = timing.Elapsed.TotalSeconds;
		RealtimeDelta = Realtime - LastRealtime;

		if (!Paused) {
			LastCurtime = Curtime;
			Curtime += Math.Clamp(RealtimeDelta, 0, 0.1);
			CurtimeDelta = Curtime - LastCurtime;
		}

		// Temporary: we need to redo this entire frame loop system.
		globals.CurTime = Curtime;
		globals.CurTimeDelta = CurtimeDelta;

		// Construct a FrameState from inputs
		UnlockEntityBuffer(); LockEntityBuffer();
		DeveloperOverlay.EvaluatePerfGraphVisibility();

		DeveloperOverlay.UserDefinedDebugRecords.Reset();
		DeveloperOverlay.UserDefinedDebugRecords.EnterScope();
		
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

		frameState.Mouse.Focused = EngineCore.Window.MouseFocused;
		frameState.Keyboard.Focused = EngineCore.Window.InputFocused;
		if (frameState.Mouse.Focused) EngineCore.Window.FlushMouseStateInto(ref frameState.Mouse);
		if (frameState.Keyboard.Focused) EngineCore.Window.FlushKeyboardStateInto(ref frameState.Keyboard);

		RootPanel.SetPos(new(0, 0));
		RootPanel.SetSize(new(frameState.WindowWidth,frameState.WindowHeight));

		ref ElementSolveState solveState = ref RootPanel.ProduceSolveState();
		RootPanel.Scheme.ApplyScheme(RootPanel, ref solveState);

		EngineCore.Window.FlushDragNDropStateInto(ref frameState.DragNDrop);

		for (int i = 0; i < frameState.DragNDrop.Files; i++) {
			DragNDropItem item;
			if ((item = frameState.DragNDrop.File[i]).Text != null)
				FileDropped(item, false);
		}
		for (int i = 0; i < frameState.DragNDrop.Texts; i++) {
			DragNDropItem item;
			if ((item = frameState.DragNDrop.Text[i]).Text != null)
				TextDropped(item, false);
		}

		if (!Paused) RunEventPreThink(ref frameState);

		if (EngineCore.Window.MouseFocused) {
			while (DragNDropFileEvents_ForNextMouseHover.TryDequeue(out DragNDropItem item))
				FileDropped(item, true);
			while (DragNDropTextEvents_ForNextMouseHover.TryDequeue(out DragNDropItem item))
				TextDropped(item, true);
		}

		RootPanel.Thinking.Think(RootPanel, ref solveState);
		RootPanel.Input.SolveHovered(RootPanel, ref solveState, frameState);
		RootPanel.Input.DispatchEvents(ref solveState, frameState);
		Keybinds.TestKeybinds(ref frameState.Keyboard, out _);

		if (!Paused) RunEventThink(frameState);
		if (!Paused) RunEventPostThink(frameState);

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

			// Temporary: we need to redo this entire frame loop system.
			globals.CurTime = Rendertime;
			globals.CurTimeDelta = RendertimeDelta;

			System.Numerics.Vector3 offset = new(0, 0, 0);

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

			//Raylib.EndTextureMode();

			/*Raylib.DrawTexturePro(RenderTarget.Value.Texture,
				new Rectangle(0, 0, RenderTarget.Value.Texture.Width, -RenderTarget.Value.Texture.Height),
				new Rectangle(0, 0, RenderTarget.Value.Texture.Width, RenderTarget.Value.Texture.Height),
				new System.Numerics.Vector2(0, 0),
				0,
				Color.WHITE);*/

			// Only really exists for REALLY late rendering
			RunEventPostRender(frameState);

			RootPanel.Painting.Paint(RootPanel, ref solveState, ElementPaintPopupMode.NoPopups);
			RootPanel.Painting.Paint(RootPanel, ref solveState, ElementPaintPopupMode.OnlyPopups);

			RunEventPostRenderUI(frameState);

			DebugOverlay.Render();

			if (ui_hoverresult.GetBool() && RootPanel.GetHoveredElement() != null) {
				var uiPosition = RootPanel.GetHoveredElement()!.GetGlobalPosition();
				var uiSize = RootPanel.GetHoveredElement()!.GetRenderBounds().Size;
				Graphics2D.SetDrawColor(255, 255, 255);
				Graphics2D.DrawRectangleOutline(RectangleF.FromPosAndSize(uiPosition, uiSize), 1);

				Vector2F drawpos = uiPosition + new Vector2F(0, uiSize.H);
				drawpos.Y = Math.Clamp(drawpos.Y, 0, frameState.WindowHeight);

				Graphics2D.DrawText(drawpos, $"Element: {RootPanel.GetHoveredElement()}", "Consolas", 14, Anchor.BottomLeft);
			}

			Graphics2D.ResetDrawingOffset();
			if (ui_showupdates.GetBool()) RenderShowUpdates();
			if (ui_visrenderbounds.GetBool()) VisRenderBounds(RootPanel);

			DeveloperOverlay.Draw(frameState);

			IsRendering = false;
			renderTrack.Stop();
			EngineCore.SetTimeToRender(renderTrack.Elapsed);
		}

		updateTrack.Start();
		UnlockEntityBuffer();

		updateTrack.Stop();
		EngineCore.SetTimeToUpdate(updateTrack.Elapsed);
	}

	private void RenderShowUpdates() {
		var now = Curtime;
		foreach (var element in RootPanel.GetAllElements()) {
			var lastLayout = element.GetLastLayoutTime();
			var delta = 1 - (Math.Min(now - lastLayout, 0.5) * 2);
			if (delta > 1) continue;

			Graphics2D.SetDrawColor(255, 50, 50, (int)(150f * delta));
			Graphics2D.DrawRectangle(element.GetGlobalPosition(), element.GetRenderBounds().Size);
		}
	}

	private void VisRenderBounds(Element e) {
		foreach (var element in e.Children) {
			if (!element.IsVisible()) continue;
			Graphics2D.SetDrawColor(255, 255, 255);
			Graphics2D.DrawRectangleOutline(element.GetGlobalPosition() + EngineCore.GetGlobalScreenOffset(), element.GetRenderBounds().Size);
			VisRenderBounds(element);
		}
	}

	public static ConVar ui_hoverresult
		= new("ui_hoverresult", "0", FCvar.None, "Highlights the currently hovered element", 0, 1);
	public static ConVar ui_visrenderbounds
		= new("ui_visrenderbounds", "0", FCvar.None, "Visualizes each elements render bounds as a outlined rectangle.", 0, 1);
	public static ConVar ui_showupdates
		= new("ui_showupdates", "0", FCvar.None, "Visualize layout updates.", 0, 1);
	public static ConCommand ui_elementcount
		= new("ui_elementcount", (_, in _) => Logs.Print($"UI Elements: {EngineCore.Level.RootPanel.GetAllElements().Length}"), FCvar.None, "Highlights the currently hovered element");

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


	static readonly char[] formatconvs = new char[256];
	public void AddDebugString(ReadOnlySpan<char> text) => DeveloperOverlay.UserDefinedDebugRecords.Write(text);
	public void AddDebugString(ReadOnlySpan<char> key, ReadOnlySpan<char> value) => DeveloperOverlay.UserDefinedDebugRecords.Write(key, value);
	public void AddDebugString<T>(ReadOnlySpan<char> key, T value) where T : ISpanFormattable {
		value.TryFormat(formatconvs, out int chars, default, null);
		DeveloperOverlay.UserDefinedDebugRecords.Write(key, formatconvs.AsSpan()[..chars]);
	}
}
