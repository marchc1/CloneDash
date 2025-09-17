using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using Raylib_cs;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.ExceptionServices;
using Nucleus.Rendering;
using Nucleus.Files;
using System.Numerics;
using SDL;
using Nucleus.Util;
using Nucleus.Commands;
using Nucleus.Input;
using Nucleus.Extensions;

namespace Nucleus;

[MarkForStaticConstruction]
public static class EngineCore
{
	[ConCommand(Help: "Performs an immediate GC collection of all generations")]
	static void gc_collect() {
		GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced | GCCollectionMode.Forced);
		GC.WaitForPendingFinalizers();
		var status = GC.WaitForFullGCComplete();
		switch (status) {
			case GCNotificationStatus.Succeeded: Logs.Info("GC: Collection succeeded."); break;
			case GCNotificationStatus.Failed: Logs.Error("GC: Collection errored."); break;
			case GCNotificationStatus.NotApplicable: Logs.Info("GC: Not applicable.."); break;
			case GCNotificationStatus.Timeout: Logs.Info("GC: Timed out."); break;
			case GCNotificationStatus.Canceled: Logs.Info("GC: Collection cancelled."); break;
		}
	}

	[ConCommand(Help: "Exits the engine via EngineCore.Close(forced: false)")] static void exit() => Close(false);
	[ConCommand(Help: "Exits the engine via EngineCore.Close(forced: true)")] static void quit() => Close(true);
	[ConCommand(Help: "Unloads the current level")] static void unload() => MainThread.RunASAP(UnloadLevel, ThreadExecutionTime.AfterFrame);
	[ConCommand(Help: "ries to create a new level with the first argument. Will not work if the level requires initialization parameters.")]
	static void level(ConCommandArguments args) {
		var level = args.Raw;
		var listOfLevels = (
			from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
			from type in domainAssembly.GetTypes()
			where typeof(Level).IsAssignableFrom(type) && type.Name != "Level"
			select type).ToArray();

		if (level == "") {
			Logs.Info($"Found {listOfLevels.Length} levels.");
			int i = 1;
			foreach (var lvl in listOfLevels) {
				Logs.Info($"    #{i}: {lvl.FullName}");
				i += 1;
			}
			return;
		}

		foreach (var lvl in listOfLevels) {
			if (lvl.FullName?.ToLower() == level.ToLower()) {
				Logs.Info($"Attempting to load {level}...");
				EngineCore.LoadLevel(Activator.CreateInstance(lvl) as Level, []);
				return;
			}
		}

		Logs.Error($"No level with the name '{level}'.");
	}

	public static ConVar engine_wireframe = ConVar.Register(nameof(engine_wireframe), "0", ConsoleFlags.None, "Enables wireframe rendering", 0, 1, (cv, _, _) => {
		// Queued so there's actually a GL context to work with
		MainThread.RunASAP(() => {
			if (cv.GetBool())
				Rlgl.EnableWireMode();
			else
				Rlgl.DisableWireMode();
		});
	});

	public static ConCommand engine_activetextures = ConCommand.Register(nameof(engine_activetextures), (_, _) => {
		var texs = new List<string>();
		foreach (var texIDPair in Raylib.GetLoadedTextures()) {
			texs.Add($"{texIDPair.Id} [{texIDPair.Width} x {texIDPair.Height} of format {texIDPair.Format}]");
		}
		Interrupt(() => { }, false, texs.ToArray());
	});

	// ------------------------------------------------------------------------------------------ //
	// Level storing & state
	// ------------------------------------------------------------------------------------------ //

	/// <summary>
	/// A special loading screen level. Always kept "loaded" in some fashion, although only runs its frame function when LoadingLevel == true.
	/// </summary>
	public static Level LoadingScreen { get; set; }
	/// <summary>
	/// The current level; if null, you'll get a big red complaint
	/// </summary>
	public static Level Level { get; private set; }
	/// <summary>
	/// Is the engine core currently loading a level. This overrides everything else; level frame's dont get called when this is turned on.
	/// </summary>
	public static bool LoadingLevel { get; private set; } = false;

	public static Element? KeyboardFocusedElement { get; internal set; } = null;
	public static bool DemandedFocus { get; private set; } = false;
	public static void RequestKeyboardFocus(Element self) {
		if (!IValidatable.IsValid(self))
			return;

		if (IValidatable.IsValid(KeyboardFocusedElement)) {
			if (DemandedFocus)
				return;

			KeyboardFocusedElement.KeyboardFocusLost(self, true);
		}

		KeyboardFocusedElement = self;
		self.KeyboardFocusGained(DemandedFocus);
		Window.StartTextInput();
	}
	public static void DemandKeyboardFocus(Element self) {
		DemandedFocus = false;      // have to reset it even if its true so the return doesnt occur in the request method
		RequestKeyboardFocus(self);
		DemandedFocus = true;       // set this flag to true so requestkeyboardfocus fails
	}
	internal static void KeyboardUnfocus(Element self, bool force = false) {
		if (!IValidatable.IsValid(KeyboardFocusedElement))
			return;
		if (!IValidatable.IsValid(self))
			return;
		if (KeyboardFocusedElement.IsIndirectChildOf(self)) {
			KeyboardFocusedElement.KeyboardFocusLost(self, false);
			KeyboardFocusedElement = null;

			return;
		}
		if (self != KeyboardFocusedElement && force == false)
			return;

		KeyboardFocusedElement.KeyboardFocusLost(self, false);
		KeyboardFocusedElement = null;
		Window.StopTextInput();
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
	private static unsafe void LogCustom(int logLevel, sbyte* text, sbyte* args) {
		var message = Logging.GetLogMessage(new IntPtr(text), new IntPtr(args));
		if (message == "FILEIO: [] Failed to open text file") return;
		Logs.Source = " raylib";

		switch ((TraceLogLevel)logLevel) {
			case TraceLogLevel.LOG_ALL:
			case TraceLogLevel.LOG_NONE:
				Logs.Print(message);
				break;
			case TraceLogLevel.LOG_TRACE:
			case TraceLogLevel.LOG_DEBUG:
				Logs.Debug(message);
				break;
			case TraceLogLevel.LOG_INFO:
				Logs.Info(message);
				break;
			case TraceLogLevel.LOG_WARNING:
				Logs.Warn(message);
				break;
			case TraceLogLevel.LOG_ERROR:
			case TraceLogLevel.LOG_FATAL:
				Logs.Error(message);
				break;
		}
		Logs.Source = "nucleus";
	}

	public static Window OpenProfiler() {
		Window window = Level.UI.Add<Window>();

		window.Title = "Nucleus Profiler";
		window.AddParent.PaintOverride += AddParent_PaintOverride;
		window.Titlebar.MinimizeButton.Enabled = false;
		window.Titlebar.MaximizeButton.Enabled = false;

		return window;
	}

	private static void AddParent_PaintOverride(Element self, float width, float height) {
		int y = 12;
		self.Parent.BorderSize = 0;

		DrawBar("Frame Time", new Color(225, 225, 225, 255), width, y, NProfiler.TotalFrameTime);
		y += 18 + 4;

		foreach (var profileResult in NProfiler.Results()) {
			DrawBar(profileResult.Name, profileResult.Color, width, y, profileResult.Elapsed.TotalMilliseconds);
			y += 18 + 4;
		}
	}
	private static void DrawBar(string text, Color color, float width, float y, double ms) {
		float ratio = (float)ms / NProfiler.TotalFrameTime;
		float rectPadding = 4;
		float textWidth = 288;
		float msWidth = 80;
		var fnWidth = (width - rectPadding - rectPadding - textWidth - msWidth);

		Graphics2D.SetDrawColor(color);

		Graphics2D.DrawRectangle(rectPadding + textWidth, rectPadding + y + 2, fnWidth * ratio, 14);
		Graphics2D.DrawRectangle(rectPadding + textWidth + fnWidth, rectPadding + y, 2, 18);
		Graphics2D.SetDrawColor(color.Adjust(0, -0.4f, 0));
		Graphics2D.DrawText(rectPadding + textWidth - 6, y + (12), text, "Noto Sans", 18, Anchor.CenterRight);
		Graphics2D.DrawText(rectPadding + textWidth + fnWidth + 8, y + (12), $"{ms:0.00} ms", "Noto Sans", 20, Anchor.CenterLeft);
	}

	public static MouseCursor MouseCursor_Frame { get; private set; }
	public static MouseCursor? MouseCursor_Persist { get; private set; }

	public static void SetMouseCursor(MouseCursor? mouseCursor, bool persist = false) {
		if (Level != null && Level.IsRendering)
			Logs.Warn("Trying to set the mouse cursor in a rendering context!");
		if (persist) {
			MouseCursor_Persist = mouseCursor;
		}
		else {
			if (mouseCursor.HasValue)
				MouseCursor_Frame = mouseCursor.Value;
		}
	}

	public static void ResetMouseCursor() {
		MouseCursor_Frame = MouseCursor.MOUSE_CURSOR_DEFAULT;
		MouseCursor_Persist = null;
	}

	public static Thread GameThread;
	private static object GameThread_GLLock = new();
	public static Action? GameThreadInitializationProcedure;
	public static void GameThreadProcedure() {
		// Initialize the window GL
		lock (GameThread_GLLock) {
			Window.SetupGL();

			if (prgIcon != null)
				Window.SetIcon(Filesystem.ReadImage("images", prgIcon));

			OpenGL.Import(Platform.OpenGL_GetProc);
			// English language
			Graphics2D.RegisterCodepoints(@"`1234567890-=qwertyuiop[]\asdfghjkl;'zxcvbnm,./~!@#$%^&*()_+QWERTYUIOP{}|ASDFGHJKL:""ZXCVBNM<>?");
			//Graphics2D.RegisterCodepoints(@"`1234567890");

			// Japanese (hiragana, katakana)
			Graphics2D.RegisterCodepoints(@"あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをんがぎぐげござじずぜぞだぢづでどばびぶべぼぱぴぷぺぽ");
			Graphics2D.RegisterCodepoints(@"アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲンガギグゲゴザジズゼゾダヂヅデドバビブベボパピプペポ");

			// Some korean
			Graphics2D.RegisterCodepoints(@"하고는을이다의에지게도한안가나의되사아그수과보있어서것같시으로와더는지기요내나또만주잘어서면때자게해이제여어야전라중좀거그래되것들이에게해요정말");

			foreach (var languageLine in ErrorMessageInAutoTranslatedLanguages)
				Graphics2D.RegisterCodepoints(languageLine);

			// Set GameThread_GLReady flag so the main thread can finish its work
		}

		// And then wait for the main thread to set GameThread_Playing
		GameThreadInitializationProcedure?.Invoke();
		lock (GameThread_GLLock) ;

		StartGameThread();
	}
	private static string? prgIcon;

	static ConVar borderless = ConVar.Register(nameof(borderless), "0", ConsoleFlags.Saved, "Hide window decorations", min: 0, max: 1, callback_first: true, callback: borderlessChange);
	private static void borderlessChange(ConVar self, CVValue old, CVValue now) {
		if (Window != null)
			Window.Undecorated = now.AsInt >= 1;
	}

	static ConVar fullscreen = ConVar.Register(nameof(fullscreen), "0", ConsoleFlags.Saved, "Fullscreen mode", min: 0, max: 1, callback_first: true, callback: fullscreenChange);
	private static void fullscreenChange(ConVar self, CVValue old, CVValue now) {
		if (Window != null)
			Window.Fullscreen = now.AsInt >= 1;
	}

	public static void Initialize(int windowWidth, int windowHeight, string windowName = "Nucleus Engine", string[]? args = null, string? icon = null, ConfigFlags[]? flags = null, Action? gameThreadInit = null) {
		if (!MainThread.ThreadSet)
			MainThread.Thread = Thread.CurrentThread;

		Filesystem.Initialize(GameInfo.AppName);
		Host.ReadConfig();
		CommandLine.FromArgs(args ?? []);
		ShowDebuggingInfo = CommandLine.IsParamTrue("debug");
		GameThreadInitializationProcedure = gameThreadInit;

		// check build number, 3rd part is days since jan 1st, 2000
		ConsoleSystem.Initialize();
		Console.Title = windowName;

		var isDebug = typeof(EngineCore).Assembly.IsAssemblyDebugBuild();
		var dt = typeof(EngineCore).Assembly.GetLinkerTime();
		Logs.Info($"Nucleus Engine, Build {(dt.HasValue ? dt.Value.ToString("yyyy-MM-dd") : "<NO LINKER TIME>")} {(isDebug ? "DEBUG" : "RELEASE")}.");
		Logs.Info("Initializing...");
		unsafe {
			Raylib.SetTraceLogCallback(&LogCustom);
		}

		Logs.Info($"    > Display server:     {Platform.DisplayServer}");

		ConfigFlags add = (ConfigFlags)0;

		if (flags != null) {
			foreach (var flag in flags) {
				add |= flag;
			}
		}


		Raylib.InitAudioDevice();
		// Initialize SDL. This has to be done on the main thread.
		OS.InitSDL(new(GameInfo.AppName, GameInfo.AppVersion ?? Assembly.GetCallingAssembly().GetName().Version?.ToString() ?? "0.1.0", GameInfo.AppIdentifier));
		if (borderless.GetBool())
			add |= ConfigFlags.FLAG_WINDOW_UNDECORATED;
		if (fullscreen.GetBool()) {
			add |= ConfigFlags.FLAG_FULLSCREEN_MODE;
			// Fix monitor sizing for fullscreens first frame
			// todo: is there a better way to do this? This interferes with a few things I think
			OSMonitor curMonitor = CommandLine.TryGetParam("monitor", out curMonitor) ? curMonitor : OS.GetPrimaryMonitor();
			var size = curMonitor.Size;
			windowWidth = (int)size.W;
			windowHeight = (int)size.H;
		}
		Window = OSWindow.Create(windowWidth, windowHeight, windowName, ConfigFlags.FLAG_MSAA_4X_HINT | ConfigFlags.FLAG_WINDOW_RESIZABLE | add);
		// We need to start the gane thread and allow it to initialize.
		prgIcon = icon;
		GameThread = new Thread(GameThreadProcedure);
		if (!MainThread.GameThreadSet) MainThread.GameThread = GameThread;
		GameThread.Start();
		lock (GameThread_GLLock) ;

		if (CommandLine.TryGetParam("monitor", out int monitorIdx)) {
			OSMonitor monitor = new OSMonitor(monitorIdx);
			var monitorPos = monitor.Position;
			var monitorSize = monitor.Size;
			var windowSize = new Vector2F(windowWidth, windowHeight);

			// monitor TL + (monitor size / 2) == centerMonitor
			// centerMonitor - (window size / 2) to center window to monitor
			var windowPos = (monitorPos + (monitorSize / 2)) - (windowSize / 2);

			Window.Position = new((int)windowPos.X, (int)windowPos.Y);
		}

		Raylib.SetTraceLogLevel(TraceLogLevel.LOG_WARNING);
	}

	private static void __loadLevel(Level level, object[] args) {
		if (level == null) {
			Level = null;
			LoadingLevel = false;
			return;
		}
		if (Level != null) {
			UnloadLevel();
		}

		Logs.Info($"Loading level {level.GetType().Name}...");
		Stopwatch s = new Stopwatch();
		s.Start();

		Level = level;
		LoadingLevel = true;
		level.PreInitialize();
		level.InitializeUI();
		Level.Initialize(args);
		Level.__isValid = true;
		InGameConsole.HookToLevel(Level);
		LoadingLevel = false;

		LoadingScreen?.Unload();
		__nextFrameLevel = null;
		__nextFrameArgs = null;
		if (EngineCore.ShowDebuggingInfo) {
			var CPUGraph = Level.UI.Add(new PerfGraph() {
				Anchor = Anchor.BottomRight,
				Origin = Anchor.BottomRight,
				Position = new(-8, -8 + -32 + -8),
				Size = new(400, 32),
				Mode = PerfGraphMode.CPU_Frametime
			});
			var MemGraph = Level.UI.Add(new PerfGraph() {
				Anchor = Anchor.BottomRight,
				Origin = Anchor.BottomRight,
				Position = new(-8, -8),
				Size = new(400, 32),
				Mode = PerfGraphMode.RAM_Usage
			});
		}

		s.Stop();

		GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true);
		GC.WaitForPendingFinalizers();
		GC.Collect();

		Logs.Info($"{level.GetType().Name} loaded in {s.Elapsed.TotalSeconds:0.####} seconds");
		//GC.Collect();
		//GC.WaitForPendingFinalizers();
	}

	private static Level? __nextFrameLevel;
	private static object[]? __nextFrameArgs;
	public static bool Started { get; private set; } = false;
	public static bool InLevelFrame { get; private set; } = false;
	public static void LoadLevel(Level level, params object[] args) {
		if (InLevelFrame || !Started) {
			__nextFrameLevel = level;
			__nextFrameArgs = args;
			LoadingScreen?.Initialize([]);
		}
		else
			__loadLevel(level, args);
	}

	public static void UnloadLevel() {
		LoadingLevel = true;
		KeyboardFocusedElement = null;

		if (Level != null) {
			Level.Unload();
			LoadingScreen?.Unload();
		}
		StopSound();
		Level = null;

		ConsoleSystem.ClearScreenBlockers();

		GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);

		LoadingLevel = false;
	}

	private static bool _running = true;
	private static bool _blockClosure = false;

	public static bool Running {
		get => _running;
	}

	public delegate void ShouldEngineCloseD();
	/// <summary>
	/// Returning true means that the engine close is blocked. False will not block the engine closure.
	/// </summary>
	public static event ShouldEngineCloseD? ShouldEngineClose;

	public static void BlockClosure() {
		_blockClosure = true;
	}

	public static void Close(bool forced = false) {
		if (!_running) return;

		if (forced) {
			_running = false;
			return;
		}

		_blockClosure = false;
		ShouldEngineClose?.Invoke();
		Level?.ShouldEngineClose();
		if (_blockClosure == false) {
			_running = false;
		}
	}

	public static Vector2F GetScreenSize() {
		Vector2F ret = Window.Size;
		if (IsUndecorated && !Maximized)
			ret -= new Vector2F(8);
		return ret;
	}

	public static RectangleF GetScreenBounds() => RectangleF.FromPosAndSize(new(0, 0), Window.Size);

	public static Vector2F GetGlobalScreenOffset() {
		if (!IsUndecorated || Maximized)
			return Vector2F.Zero;

		return new(4);
	}

	public static FrameState CurrentFrameState { get; set; }
	public static GameInfo GameInfo { get; set; }

	public static double TargetFrameTime { get; private set; } = 0;
	public static double CurrentAppTime { get; private set; } = 0;
	public static double PreviousAppTime { get; private set; } = 0;
	public static double UpdateTime { get; private set; } = 0;
	public static double DrawTime { get; private set; } = 0;
	public static double FrameTime { get; private set; } = 0;

	public static bool ShowConsoleLogsInCorner { get; set; } = true;
	public static bool ShowDebuggingInfo { get; set; } = false;
	public static string FrameCost { get; set; } = "";
	public static float FrameCostMS { get; set; }

	private const int FPS_CAPTURE_FRAMES_COUNT = 30;
	private const float FPS_AVERAGE_TIME_SECONDS = 0.5f;
	private const float FPS_STEP = FPS_AVERAGE_TIME_SECONDS / FPS_CAPTURE_FRAMES_COUNT;
	private static int fps_index = 0;
	private static float[] fps_history = new float[FPS_CAPTURE_FRAMES_COUNT];
	private static float fps_average = 0, fps_last = 0;

	public static OSWindow Window;

	public static float FPS {
		get {
			float fpsFrame = (float)FrameTime;

			if (fpsFrame == 0) return 0;

			var t = OS.GetTime();
			if ((t - fps_last) > FPS_STEP) {
				fps_last = (float)t;
				fps_index = (fps_index + 1) % FPS_CAPTURE_FRAMES_COUNT;
				fps_average -= fps_history[fps_index];
				fps_history[fps_index] = fpsFrame / FPS_CAPTURE_FRAMES_COUNT;
				fps_average += fps_history[fps_index];
			}

			return MathF.Round(1.0f / fps_average);
		}
	}
	public static ConVar fps_max = ConVar.Register("fps_max", "0", ConsoleFlags.Saved, "Default frames per second. By default, unlimited.", 0, 10000, (cv, _, _) => LimitFramerate(cv.GetInt()));
	public static ConVar r_renderat = ConVar.Register("renderrate", "60", ConsoleFlags.Saved, "Separate control over how often rendering functions in particular are ran.", 0, 10000);
	public static double RenderRate => r_renderat.GetDouble() == 0 ? 0 : 1d / r_renderat.GetDouble();

	private static string WorkConsole = "";
	public static void Frame() {
		NProfiler.Reset();
		NucleusSingleton.Spin();
		MouseCursor_Frame = MouseCursor.MOUSE_CURSOR_DEFAULT;

		CurrentAppTime = OS.GetTime();
		UpdateTime = CurrentAppTime - PreviousAppTime;
		PreviousAppTime = CurrentAppTime;
		OSWindow.PropagateEventBuffer();

		Rlgl.LoadIdentity();
		unsafe {
			var c = Raymath.MatrixToFloatV(Window.ScreenScale);
			Rlgl.MultMatrixf(c.v);
		}

		Graphics2D.SetOffset(GetGlobalScreenOffset());

		var screenBounds = GetScreenSize();

		InLevelFrame = true;
		if (LoadingLevel) {
			if (LoadingScreen == null) {
				Graphics2D.SetDrawColor(10, 15, 20);
				Graphics2D.DrawRectangle(0, 0, screenBounds.W, screenBounds.H);
				Graphics2D.SetDrawColor(240, 245, 255);
				Graphics2D.DrawText(screenBounds.W / 2, screenBounds.H / 2, "LOADING", "Noto Sans", 24, TextAlignment.Center, TextAlignment.Top);
			}
			else {
				LoadingScreen.Frame();
			}
		}


		if (IValidatable.IsValid(Level))
			Level.Frame();
		else {
			Graphics2D.SetDrawColor(30, 5, 0);
			Graphics2D.DrawRectangle(0, 0, Window.Size.W, Window.Size.H);
			Graphics2D.SetDrawColor(240, 70, 60);
			Graphics2D.DrawText(screenBounds.X / 2, screenBounds.Y / 2, "<No level loaded!>", "Noto Sans", 24, TextAlignment.Center, TextAlignment.Center);
			//Graphics2D.DrawText(screenBounds.X / 2, screenBounds.Y / 2, "Make sure you're changing EngineCore.Level.", "Noto Sans", 18, TextAlignment.Center, TextAlignment.Top);

			int y = 0;
			var msgs = ConsoleSystem.GetMessages();
			int txS = 12;
			foreach (var cmsg in msgs.Reverse()) {
				int c = 1;
				for (int ci = 0; ci < cmsg.Message.Length; ci++) {
					char curchar = cmsg.Message[ci];
					if (curchar == '\r' && ((ci < cmsg.Message.Length - 1 && cmsg.Message[ci + 1] == '\n') || ci == cmsg.Message.Length - 1)) {
						c++;
						ci++;
					}
					else if (curchar == '\n')
						c++;
				}
				y += c;
				Graphics2D.DrawText(4, screenBounds.Y - 24 - (y * txS), cmsg.Message.Replace('\r', ' '), "Consolas", txS, TextAlignment.Left, TextAlignment.Top);

			}
			// mini game loop
			KeyboardState keyboardState = new();
			Window.FlushKeyboardStateInto(ref keyboardState);

			int i = 0;
			while (keyboardState.KeyAvailable(ref i, out int k, out _)) {
				KeyAction action = KeyboardLayout.USA.GetKeyAction(keyboardState, KeyboardLayout.USA.FromInt(k));
				switch (action.Type) {
					case CharacterType.Enter:
						Logs.Info($"] {WorkConsole}");
						ConsoleSystem.ParseOneCommand(WorkConsole);
						WorkConsole = "";
						break;
					case CharacterType.DeleteBackwards:
						if (WorkConsole.Length > 0) {
							WorkConsole = WorkConsole.Substring(0, WorkConsole.Length - 1);
						}
						break;
					case CharacterType.VisibleCharacter:
						WorkConsole += action.Extra;
						break;
				}
			}

			Graphics2D.DrawText(4, screenBounds.Y - 16, $"user> {WorkConsole}", "Consolas", txS, TextAlignment.Left, TextAlignment.Top);
		}

		InLevelFrame = false;
		if (__nextFrameLevel != null) {
			__loadLevel(__nextFrameLevel, __nextFrameArgs);
			__nextFrameLevel = null;
		}

		Rlgl.DrawRenderBatchActive();
		if (Level == null || Level.RenderedFrame)
			Window.SwapScreenBuffer();

		CurrentAppTime = OS.GetTime();
		DrawTime = CurrentAppTime - PreviousAppTime;
		PreviousAppTime = CurrentAppTime;
		FrameTime = UpdateTime + DrawTime;

		if (FrameTime < TargetFrameTime) {
			double waitFor = TargetFrameTime - FrameTime;
			OS.Wait(waitFor);

			CurrentAppTime = OS.GetTime();
			double waitTime = CurrentAppTime - PreviousAppTime;
			PreviousAppTime = CurrentAppTime;

			FrameTime += waitTime;
		}

		Window.SetMouseCursor(MouseCursor_Persist ?? MouseCursor_Frame);
		Host.CheckDirty();

		if (Window.UserClosed()) {
			Close();
		}
	}

	public static bool Maximized => Window.Maximized;
	public static bool Minimized => Window.Minimized;
	public static bool Focused => Window.InputFocused;
	public static bool InFullscreen {
		get => Window.Fullscreen;
		set => Window.Fullscreen = value;
	}
	public static bool IsUndecorated => Window.Undecorated;

	public static void Maximize() {
		if (Maximized) {
			Unmaximize();
			return;
		}
		Window.Maximized = true;
	}
	public static void Unmaximize() {
		if (!Maximized)
			return;

		Window.Maximized = false;
		Window.Visible = true;
	}
	public static void Minimize() {
		if (Minimized) {
			Unminimize();
			return;
		}
		Window.Minimized = true;
	}
	public static void Unminimize() {
		if (!Minimized)
			return;

		Window.Minimized = false;
		Window.Visible = true;
	}

	public static unsafe Vector2F MousePos {
		get => EngineCore.Window.Mouse.CurrentMousePosition;
	}

	public static void LimitFramerate(int fps) {
		if (fps < 1)
			TargetFrameTime = 0;
		else
			TargetFrameTime = 1.0 / (double)fps;

		Logs.Info($"Target FPS: {fps}, milliseconds: {TargetFrameTime * 1000:0.00}");
	}


	private static bool shouldThrow = false;

	private static HashSet<Assembly> earlyJITAssemblies = [];

	public static void AddToEarlyJIT() {
		earlyJITAssemblies.Add(Assembly.GetCallingAssembly());
	}
	public static void AddToEarlyJIT(Assembly assembly) {
		earlyJITAssemblies.Add(assembly);
	}

	public static void StartGameThread() {
		ExceptionDispatchInfo edi;
		Started = true;
		if (Debugger.IsAttached) {
			// Skip panic routine.
			Logs.Info("PANIC: Disabled immediate thread panicking due to the presence of a debugger.");
			LoadingScreen?.Initialize([]);
			while (Running) {
				shouldThrow = false;
				Frame();
			}
		}
		else {
			try {
				Logs.Info("PANIC: Immediate thread panicking active.");
				LoadingScreen?.Initialize([]);
				while (Running) {
					shouldThrow = false;
					Frame();
				}
			}
			catch (Exception ex) {
				edi = ExceptionDispatchInfo.Capture(ex);
				if (!Panic(edi)) {
					edi.Throw();
				}
			}
		}
		Logs.Info("Nucleus Engine has halted peacefully.");
	}

	public static void StartMainThread() {
		lock (GameThread_GLLock) {
			var ea = Assembly.GetEntryAssembly();
			if (ea != null)
				earlyJITAssemblies.Add(ea);

			earlyJITAssemblies.Add(Assembly.GetExecutingAssembly());
			earlyJITAssemblies.Add(Assembly.GetCallingAssembly());

			Logs.Info("BOOT: Initializing static constructors...");
			foreach (var t in from a in AppDomain.CurrentDomain.GetAssemblies()
							  from t in a.GetTypes()
							  let attributes = t.GetCustomAttributes(typeof(MarkForStaticConstructionAttribute), true)
							  where attributes != null && attributes.Length > 0
							  select t) {
				RuntimeHelpers.RunClassConstructor(t.TypeHandle);
				foreach (var ccmd in ConCommandAttribute.GetAttributes(t))
					ConCommandAttribute.RegisterAttribute(t, ccmd.baseMethod, ccmd.attr);
			}

			Logs.Info("BOOT: Running JIT early where possible...");
			Parallel.ForEach(earlyJITAssemblies
				   .SelectMany(a => a.GetTypes())
				   .SelectMany(t => t.GetMethods()), (method) => {
					   if (method.ContainsGenericParameters) return;
					   if (method.IsAbstract) return;
					   if (method.Attributes.HasFlag(MethodAttributes.NewSlot)) return;
					   if (method.Attributes.HasFlag(MethodAttributes.PinvokeImpl)) return;
					   try {
						   RuntimeHelpers.PrepareMethod(method.MethodHandle);
					   }
					   catch {

					   }
				   });

		}

		// Fixes event delays on linux, but all operating systems should benefit
		Thread.CurrentThread.Priority = ThreadPriority.Highest;

		while (Running) {
			OSWindow.PumpOSEvents();
			if (!Running) {
				Window.Close();
				return;
			}
		}
	}

	public static ConCommand panic = ConCommand.Register("panic", (_, _) => {
		if (Debugger.IsAttached) {
			try {
				throw new Exception("force-panic (despite panic system deactivated due to presence of debugger)");
			}
			catch (Exception ex) {
				var edi = ExceptionDispatchInfo.Capture(ex);
				if (!Panic(edi)) {
					edi.Throw();
				}
			}
		}
		throw new Exception("panic concommand called");
	}, ConsoleFlags.DevelopmentOnly, "Tests the EngineCore.Panic method (NOTE: this *will* crash the engine!).");

	public static ConCommand interrupt = ConCommand.Register("interrupt", (_, a) => {
		EngineCore.Interrupt(() => {
			Graphics2D.SetDrawColor(255, 0, 0);
			Graphics2D.DrawRectangle(64, 64, 256, 256);
		}, (a.GetInt(0) ?? 0) > 0, "You should see a red square in the top-left!");
	}, ConsoleFlags.DevelopmentOnly, "Tests the EngineCore.Interrupt method");

	private const string PANIC_FONT = "Noto Sans";
	private const string PANIC_FONT_CONSOLE = "Noto Sans Mono";
	private const float PANIC_SIZE = 18;
	private const float PANIC_SIZE_CONSOLE = 16;
	private static void renderLine(ref int textY) => renderLine(null, ref textY);
	private static void renderLine(string? line, ref int textY) {
		if (line == null) {
			textY++;
			return;
		}
		Graphics2D.SetDrawColor(0, 0, 0, 220);
		var textSize = Graphics2D.GetTextSize(line, PANIC_FONT_CONSOLE, PANIC_SIZE_CONSOLE);
		Graphics2D.DrawRectangle(0, textY * PANIC_SIZE_CONSOLE, textSize.W + 8, PANIC_SIZE_CONSOLE);

		Graphics2D.SetDrawColor(255, 255, 255);
		Graphics2D.DrawText(new(4, textY * PANIC_SIZE_CONSOLE), line, PANIC_FONT_CONSOLE, PANIC_SIZE_CONSOLE);

		textY++;
	}
	// Commented out lines don't have font support yet. Can add them back when they work again
	private static readonly string[] ErrorMessageInAutoTranslatedLanguages = [
		"A fatal error has occured. Press any key to exit.",
		//"حدث خطأ فادح. اضغط على أي مفتاح للخروج.",
		"Възникнала е фатална грешка. Натиснете който и да е клавиш, за да излезете.",
		"出现致命错误。按任意键退出。              發生致命錯誤。按任意鍵退出。",
		"Došlo k fatální chybě. Stiskněte libovolnou klávesu pro ukončení.",
		"Der er opstået en fatal fejl. Tryk på en vilkårlig tast for at afslutte.",
		"Er is een fatale fout opgetreden. Druk op een willekeurige toets om af te sluiten.",
		"On ilmnenud fataalne viga. Väljumiseks vajutage suvalist klahvi.",
		"On tapahtunut kohtalokas virhe. Poistu painamalla mitä tahansa näppäintä.",
		"Une erreur fatale s'est produite. Appuyez sur n'importe quelle touche pour quitter.",
		"Es ist ein schwerwiegender Fehler aufgetreten. Drücken Sie eine beliebige Taste zum Beenden.",
		"Προέκυψε ένα μοιραίο σφάλμα. Πατήστε οποιοδήποτε πλήκτρο για έξοδο.",
		"Végzetes hiba történt. Nyomja meg bármelyik billentyűt a kilépéshez.",
		"Telah terjadi kesalahan fatal. Tekan sembarang tombol untuk keluar.",
		"Si è verificato un errore fatale. Premere un tasto qualsiasi per uscire.",
		"致命的なエラーが発生しました。いずれかのキーを押して終了してください。",
		//"치명적인 오류가 발생했습니다. 종료하려면 아무 키나 누르세요.",
		"Ir notikusi fatāla kļūda. Nospiediet jebkuru taustiņu, lai izietu.",
		"Įvyko lemtinga klaida. Paspauskite bet kurį klavišą, kad išeitumėte.",
		"Det har oppstått en alvorlig feil. Trykk på en hvilken som helst tast for å avslutte.",
		"Wystąpił błąd krytyczny. Naciśnij dowolny przycisk, aby wyjść.",
		"Ocorreu um erro fatal. Prima qualquer tecla para sair.",
		"Ocorreu um erro fatal. Pressione qualquer tecla para sair.",
		"A apărut o eroare fatală. Apăsați orice tastă pentru a ieși.",
		"Произошла фатальная ошибка. Нажмите любую клавишу, чтобы выйти.",
		"Vyskytla sa fatálna chyba. Stlačte ľubovoľné tlačidlo, aby ste ukončili prácu.",
		"Zgodila se je usodna napaka. Za izhod pritisnite katero koli tipko.",
		"Se ha producido un error fatal. Pulse cualquier tecla para salir.",
		"Ett allvarligt fel har inträffat. Tryck på valfri tangent för att avsluta.",
		"Ölümcül bir hata oluştu. Çıkmak için herhangi bir tuşa basın.",
		"Виникла фатальна помилка. Натисніть будь-яку клавішу для виходу."
	];

	public static bool Panic(ExceptionDispatchInfo ex) {
		if (shouldThrow)
			ex.Throw();

		var oldMaster = Raylib.GetMasterVolume();
		Raylib.SetMasterVolume(0);
		Window.Title = "Nucleus Engine - Panicked!";
		Window.MinSize = new((int)Window.Size.W, (int)Window.Size.H);
		Window.MaxSize = new((int)Window.Size.W, (int)Window.Size.H);
		shouldThrow = true;
		// Rudimentary frame loop for crashed state. Kinda emulates an older Mac kernel panic
		Stopwatch time = new();
		Graphics2D.ResetDrawingOffset();
		time.Start();
		int y = 0;
		var lastTime = 0d;

		var exLines = ex.SourceException.Message.Split('\n');
		var exStkLines = ex.SourceException.StackTrace?.Split('\n') ?? ["<No stack trace available>"];

		var innerEx = ex.SourceException.InnerException;
		var innerExLines = innerEx?.Message?.Split('\n') ?? ["<No inner exception>"];
		var innerExStkLines = innerEx?.StackTrace?.Split('\n') ?? ["<No stack trace available>"];
		bool hasRenderedOverlay = false;

		while (true) {
			var now = time.Elapsed.TotalSeconds;
			double elapsed = now - lastTime;
			lastTime = now;
			Rlgl.LoadIdentity();

			if (y < Window.Size.H) {
				int elapsedY = (int)((float)elapsed * 1150);
				for (int i = 0; i < 2; i++) { // Need to draw on both buffers
					Raylib.DrawRectangle(0, y, (int)Window.Size.W, elapsedY, new(90, 100, 120, 170));
					Rlgl.DrawRenderBatchActive();
					Window.SwapScreenBuffer();
				}
				y += elapsedY;
			}
			else if (!hasRenderedOverlay) {
				// Hopefully it wasnt the font manager that broke!
				Graphics2D.SetDrawColor(255, 255, 255);

				var box = new System.Numerics.Vector2(0, PANIC_SIZE * ErrorMessageInAutoTranslatedLanguages.Length);
				foreach (var languageLine in ErrorMessageInAutoTranslatedLanguages) {
					var size = Graphics2D.GetTextSize(languageLine, PANIC_FONT, PANIC_SIZE);
					if (size.X > box.X)
						box.X = size.X;
				}
				var padding = 32;
				var paddingDiv2 = padding / 2;
				var center = new System.Numerics.Vector2((Window.Size.W / 2) - (box.X / 2), (Window.Size.H / 2) - (box.Y / 2));
				Raylib.DrawRectangle((int)center.X - paddingDiv2, (int)center.Y - paddingDiv2, (int)box.X + padding, (int)box.Y + padding, new Color(10, 220));
				var langLineY = 0;
				foreach (var line in ErrorMessageInAutoTranslatedLanguages) {
					Graphics2D.DrawText(center.X + (box.X / 2), center.Y + (langLineY * PANIC_SIZE), line, PANIC_FONT, PANIC_SIZE, Anchor.TopCenter);

					langLineY++;
				}

				int textY = 0;
				renderLine("A fatal error has occured. Please restart the application.", ref textY);
				renderLine("Details:", ref textY);
				renderLine(null, ref textY);
				foreach (var line in exLines) renderLine(line, ref textY);
				renderLine(ref textY);
				foreach (var line in exStkLines) renderLine($"    {line}", ref textY);

				if (innerEx != null) {
					renderLine(ref textY);
					renderLine("Inner exception:", ref textY);

					foreach (var line in innerExLines) renderLine($"    {line}", ref textY);
					renderLine(ref textY);
					foreach (var line in innerExStkLines) renderLine($"        {line}", ref textY);
				}

				hasRenderedOverlay = true;
				Rlgl.DrawRenderBatchActive();
				Window.SwapScreenBuffer();
			}
			else {
				int i = 0;
				while (true) {
					OSWindow.PropagateEventBuffer();
					if (Window.KeyAvailable(ref i, out _, out _) || Window.UserClosed()) {
						Raylib.SetMasterVolume(oldMaster);
						return false;
					}
				}
			}

			Rlgl.DrawRenderBatchActive();
			OS.Wait(hasRenderedOverlay ? 0.2 : 0.005);
		}
	}

	private static bool interrupting = false;
	public static bool InInterrupt => interrupting;
	public static void Interrupt(Action draw, bool problematic, params string[] messages) {
		if (interrupting) return;
		interrupting = true;

		var oldMaster = Raylib.GetMasterVolume();
		Raylib.SetMasterVolume(0);

		Window.MinSize = new((int)Window.Size.W, (int)Window.Size.H);
		Window.MaxSize = new((int)Window.Size.W, (int)Window.Size.H);

		// Rudimentary frame loop for crashed state. Kinda emulates an older Mac kernel panic
		Stopwatch time = new();
		Graphics2D.ResetDrawingOffset();
		time.Start();
		int y = 0;
		var lastTime = 0d;

		bool hasRenderedOverlay = false;

		while (true) {
			var now = time.Elapsed.TotalSeconds;
			double elapsed = now - lastTime;
			lastTime = now;
			Rlgl.LoadIdentity();

			if (y < Window.Size.H) {
				int elapsedY = (int)((float)elapsed * 5000);
				for (int i = 0; i < 2; i++) { // Need to draw on both buffers
					Raylib.DrawRectangle(0, y, (int)Window.Size.W, elapsedY, new(90, 100, 120, 170));
					Rlgl.DrawRenderBatchActive();
					Window.SwapScreenBuffer();
				}
				y += elapsedY;
			}
			else if (!hasRenderedOverlay) {
				Graphics2D.SetDrawColor(255, 255, 255);

				// don't feel like making it static right now
				var lines = new string[messages.Length + 3];
				if (problematic) {
					lines[0] = "An interrupt has occured due to an issue, and the application has temporarily halted.";
				}
				else {
					lines[0] = "A debugging interrupt has occured and the application has temporarily halted.";
				}
				for (int i = 0; i < messages.Length; i++) lines[i + 1] = messages[i] ?? "<NULL STRING>";

				lines[lines.Length - 2] = "";
				lines[lines.Length - 1] = "Press any key to continue.";

				var box = new System.Numerics.Vector2(0, PANIC_SIZE * lines.Length);
				foreach (var languageLine in lines) {
					var size = Graphics2D.GetTextSize(languageLine, PANIC_FONT, PANIC_SIZE);
					if (size.X > box.X)
						box.X = size.X;
				}
				var padding = 32;
				var paddingDiv2 = padding / 2;
				var center = new System.Numerics.Vector2((Window.Size.W / 2) - (box.X / 2), padding);
				Raylib.DrawRectangle((int)center.X - paddingDiv2, (int)center.Y - paddingDiv2, (int)box.X + padding, (int)box.Y + padding, new Color(10, 220));
				var langLineY = 0;
				foreach (var line in lines) {
					Graphics2D.DrawText(center.X + (box.X / 2), center.Y + (langLineY * PANIC_SIZE), line, PANIC_FONT, PANIC_SIZE, Anchor.TopCenter);

					langLineY++;
				}

				draw();
				hasRenderedOverlay = true;
				Rlgl.DrawRenderBatchActive();
				Window.SwapScreenBuffer();
			}
			else {
				int i = 0;
				OSWindow.PropagateEventBuffer();
				if (Window.KeyAvailable(ref i, out _, out _)) {
					Raylib.SetMasterVolume(oldMaster);
					interrupting = false;
					return;
				}
			}

			OS.Wait(hasRenderedOverlay ? 0.2 : 1 / 60f);
		}
	}

	public static Vector2F GetWindowSize() => Window.Size;

	public static float GetWindowWidth() => Window.Size.W;
	public static float GetWindowHeight() => Window.Size.H;
	public static void SetWindowPosition(Vector2F pos) => Window.Position = pos;
	public static void SetWindowTitle(string title) => Window.Title = title;

	public static void StopSound() {
		var lvl = Level;

		if (lvl == null) return;
		if (lvl.Sounds == null) return;

		lvl.Sounds.Dispose();
	}
}
