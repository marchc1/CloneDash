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
using System.Globalization;

namespace Nucleus;

[MarkForStaticConstruction]
public static class EngineCore
{
	[ConCommand(Help: "Performs an immediate GC collection of all generations")]
	static void gc_collect() {
		GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
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
	[ConCommand(Help: "Creates a new null subwindow")]
	static void nullwindow(ConCommandArguments args) {
		EngineCore.SubWindow(args.GetInt(0, out int x) ? x : 640, args.GetInt(1, out int y) ? y : 480, args.GetString(2) ?? "Nucleus Subwindow");
	}

	[ConCommand(Help: "Exits the engine via EngineCore.Close(forced: false)")] static void exit() => Close(false);
	[ConCommand(Help: "Exits the engine via EngineCore.Close(forced: true)")] static void quit() => Close(true);
	[ConCommand(Help: "Unloads the current level")] static void unload() => MainThread.RunASAP(UnloadLevel, ThreadExecutionTime.AfterFrame);
	[ConCommand(Help: "Tries to create a new level with the first argument. Will not work if the level requires initialization parameters.")]
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

	class OSWindowCtx
	{
		public Level? Level;
		public Level? NextFrameLevel;
		public object[]? NextFrameArgs;

		public TimeSpan LastTimeToUpdate;
		public TimeSpan LastTimeToRender;

		public double TargetFrameTime;
		public double CurrentAppTime;
		public double PreviousAppTime;
		public double UpdateTime;
		public double DrawTime;
		public double FrameTime;
	}

	// This really shouldnt get used but there are REALLY dumb places some of the timing stuff gets called
	static readonly OSWindowCtx StartCache = new();
	static readonly OSWindowCtx DUMMY = new();
	static OSWindowCtx GetWindowCtx(OSWindow window) {
		if (!Started)
			return StartCache;

		if (window == null)
			return DUMMY;
		if (WindowContexts.TryGetValue(window, out OSWindowCtx? value))
			return value;
		value = new();
		WindowContexts.Add(window, value);
		return value;
	}

	static readonly Dictionary<OSWindow, OSWindowCtx> WindowContexts = [];
	public static Level LoadingScreen { get; set; }
	/// <summary>
	/// The current level; if null, you'll get a big red complaint
	/// </summary>
	public static Level Level { get; private set; }
	/// <summary>
	/// Is the engine core currently loading a level. This overrides everything else; level frame's dont get called when this is turned on.
	/// </summary>
	public static bool LoadingLevel { get; private set; } = false;

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

		DrawBar("Time to Update", new Color(225, 225, 225, 255), width, y, GetTimeToUpdate().TotalSeconds);
		DrawBar("Time to Render", new Color(225, 225, 225, 255), width, y, GetTimeToRender().TotalSeconds);
		y += 18 + 4;

		foreach (var profileResult in NProfiler.Results()) {
			DrawBar(profileResult.Name, profileResult.Color, width, y, profileResult.Elapsed.TotalMilliseconds);
			y += 18 + 4;
		}
	}
	private const float BAR_BASELINE = 1000f / 60f; // 60 fps/ups
	private static void DrawBar(string text, Color color, float width, float y, double ms) {
		float ratio = (float)ms / BAR_BASELINE;
		float rectPadding = 4;
		float textWidth = 288;
		float msWidth = 80;
		var fnWidth = (width - rectPadding - rectPadding - textWidth - msWidth);

		Graphics2D.SetDrawColor(color);

		Graphics2D.DrawRectangle(rectPadding + textWidth, rectPadding + y + 2, fnWidth * ratio, 14);
		Graphics2D.DrawRectangle(rectPadding + textWidth + fnWidth, rectPadding + y, 2, 18);
		Graphics2D.SetDrawColor(color.Adjust(0, -0.4f, 0));
		Graphics2D.DrawText(rectPadding + textWidth - 6, y + (12), text, Graphics2D.UI_FONT_NAME, 18, Anchor.CenterRight);
		Graphics2D.DrawText(rectPadding + textWidth + fnWidth + 8, y + (12), $"{ms:0.00} ms", Graphics2D.UI_FONT_NAME, 20, Anchor.CenterLeft);
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
			MainWindow.SetupGL();
			WindowContexts[MainWindow] = StartCache;

			MakeWindowCurrent(MainWindow);

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

			Graphics2D.RegisterCodepoints(string.Join("", ErrorMessages.Keys));

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
		OS.InitSDL(ref GameInfo);
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
		MainWindow = OSWindow.Create(windowWidth, windowHeight, windowName, ConfigFlags.FLAG_MSAA_4X_HINT | ConfigFlags.FLAG_WINDOW_RESIZABLE | add);
		GetWindowCtx(MainWindow).Level = null;
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

			MainWindow.Position = new((int)windowPos.X, (int)windowPos.Y);
		}

		Raylib.SetTraceLogLevel(TraceLogLevel.LOG_WARNING);
	}

	public static void MakeWindowCurrent(OSWindow window) {
		Rlgl.SetFramebufferWidth((int)window.Size.W);
		Rlgl.SetFramebufferHeight((int)window.Size.H);
		window.ActivateGL();
		window.SetupViewport(window.Size.W, window.Size.H);
		Window = window;
		Level = GetWindowCtx(Window).Level!;
	}

	// Specific things that need to get called (because a level usually calls these like hittesting)
	static void ResetWindowLevelSpecificEnv(OSWindow window) {
		window.DisableHitTest();
	}

	private static void __loadLevel(OSWindow window, Level level, object[] args) {
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

		GetWindowCtx(window).Level = level;
		MakeWindowCurrent(window);
		ResetWindowLevelSpecificEnv(window);
		LoadingLevel = true;
		level.PreInitialize();
		level.InitializeUI();
		Level.Initialize(args);
		Level.__isValid = true;
		InGameConsole.HookToLevel(Level);
		LoadingLevel = false;

		LoadingScreen?.Unload();
		GetWindowCtx(Window).NextFrameLevel = null;
		GetWindowCtx(Window).NextFrameArgs = null;
		if (EngineCore.ShowDebuggingInfo) {
			var UpdateGraph = Level.UI.Add(new PerfGraph() {
				Anchor = Anchor.BottomRight,
				Origin = Anchor.BottomRight,
				Position = new(-8, -8 + -52 + -16),
				Size = new(400, 26),
				Mode = PerfGraphMode.CPU_UpdateTime
			});
			var RenderGraph = Level.UI.Add(new PerfGraph() {
				Anchor = Anchor.BottomRight,
				Origin = Anchor.BottomRight,
				Position = new(-8, -8 + -26 + -8),
				Size = new(400, 26),
				Mode = PerfGraphMode.CPU_RenderTime
			});
			var MemGraph = Level.UI.Add(new PerfGraph() {
				Anchor = Anchor.BottomRight,
				Origin = Anchor.BottomRight,
				Position = new(-8, -8),
				Size = new(400, 26),
				Mode = PerfGraphMode.RAM_Usage
			});
		}

		s.Stop();

		GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced | GCCollectionMode.Aggressive, true);
		GC.WaitForPendingFinalizers();

		Logs.Info($"{level.GetType().Name} loaded in {s.Elapsed.TotalSeconds:0.####} seconds");
		//GC.Collect();
		//GC.WaitForPendingFinalizers();
	}

	public static bool Started { get; private set; } = false;
	public static bool InLevelFrame { get; private set; } = false;
	public static void LoadLevel(OSWindow window, Level level, params object[] args) {
		if (InLevelFrame || !Started) {
			GetWindowCtx(Window).NextFrameLevel = level;
			GetWindowCtx(Window).NextFrameArgs = args;
			LoadingScreen?.Initialize([]);
		}
		else
			__loadLevel(window, level, args);
	}
	public static void LoadLevel(Level level, params object[] args) => LoadLevel(Window, level, args);
	public static void SubWindow(int width, int height, string title, ConfigFlags flags = 0) {
		OSWindow.CreateSubwindow((window) => {
			window.SetupGL();
			WindowContexts[window] = new();
		}, width, height, title, flags);
	}
	public static void SubWindow(Action<OSWindow> callback, int width, int height, string title, ConfigFlags flags = 0) {
		OSWindow.CreateSubwindow((window) => {
			window.SetupGL();
			WindowContexts[window] = new();
			callback(window);
		}, width, height, title, flags);
	}
	public static void LoadLevelSubWindow<T>(T level, int width, int height, string title, ConfigFlags flags = 0, params object[] args) where T : Level {
		SubWindow((window) => {
			OSWindow lastWindow = Window;
			MakeWindowCurrent(window);
			{
				__loadLevel(window, level, args);
			}
			MakeWindowCurrent(lastWindow);
		}, width, height, title, flags);
	}

	public static void UnloadLevel() {
		LoadingLevel = true;

		if (Level != null) {
			Level.Unload();
			LoadingScreen?.Unload();
		}
		StopSound();
		GetWindowCtx(Window).Level = null;
		Level = null!;
		ResetWindowLevelSpecificEnv(Window);

		ConsoleSystem.ClearScreenBlockers();

		GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced | GCCollectionMode.Aggressive, true);

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
			ExitWindow();
			return;
		}

		_blockClosure = false;
		ShouldEngineClose?.Invoke();
		Level?.PreWindowClose();
		if (_blockClosure == false)
			ExitWindow();
	}

	public static void ExitWindow() {
		if (WindowContexts.Count == 1) {
			// just exit
			_running = false;
			return;
		}

		WindowContexts.Remove(Window);
		Window.Close();

		if (Window == MainWindow) {
			// Uh oh! We just deleted the main window! Try to choose a new window?
			MainWindow = WindowContexts.Keys.First();
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

	public static GameInfo GameInfo;

	public static double TargetFrameTime {
		get => GetWindowCtx(Window).TargetFrameTime;
		set => GetWindowCtx(Window).TargetFrameTime = value;
	}

	public static double CurrentAppTime {
		get => GetWindowCtx(Window).CurrentAppTime;
		set => GetWindowCtx(Window).CurrentAppTime = value;
	}

	public static double PreviousAppTime {
		get => GetWindowCtx(Window).PreviousAppTime;
		set => GetWindowCtx(Window).PreviousAppTime = value;
	}

	public static double UpdateTime {
		get => GetWindowCtx(Window).UpdateTime;
		set => GetWindowCtx(Window).UpdateTime = value;
	}

	public static double DrawTime {
		get => GetWindowCtx(Window).DrawTime;
		set => GetWindowCtx(Window).DrawTime = value;
	}

	public static double FrameTime {
		get => GetWindowCtx(Window).FrameTime;
		set => GetWindowCtx(Window).FrameTime = value;
	}

	public static bool ShowConsoleLogsInCorner { get; set; } = true;
	public static bool ShowDebuggingInfo { get; set; } = false;


	/// <summary>
	/// How long did the last update-frame take?
	/// </summary>
	/// <returns></returns>
	public static TimeSpan GetTimeToUpdate() => GetWindowCtx(Window).LastTimeToUpdate;
	/// <summary>
	/// How long did the last render-frame take?
	/// </summary>
	/// <returns></returns>
	public static TimeSpan GetTimeToRender() => GetWindowCtx(Window).LastTimeToRender;
	internal static void SetTimeToUpdate(TimeSpan value) => GetWindowCtx(Window).LastTimeToUpdate = value;
	internal static void SetTimeToRender(TimeSpan value) => GetWindowCtx(Window).LastTimeToRender = value;

	private const int FPS_CAPTURE_FRAMES_COUNT = 30;
	private const float FPS_AVERAGE_TIME_SECONDS = 0.5f;
	private const float FPS_STEP = FPS_AVERAGE_TIME_SECONDS / FPS_CAPTURE_FRAMES_COUNT;
	private static int fps_index = 0;
	private static float[] fps_history = new float[FPS_CAPTURE_FRAMES_COUNT];
	private static float fps_average = 0, fps_last = 0;

	public static OSWindow Window { get; private set; }
	public static OSWindow MainWindow;

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
	public static ConVar gc_collectperframe = ConVar.Register("gc_collectperframe", "0", ConsoleFlags.Saved, "If set to 1, Nucleus will perform a forced gen-0 garbage collection after every frame. This is an experiment, mileage may vary.", 0, 1);
	public static double RenderRate => r_renderat.GetDouble() == 0 ? 0 : 1d / r_renderat.GetDouble();

	private static string WorkConsole = "";
	static readonly List<OSWindow> windowsThisFrame = [];
	public static void Frame() {
		WaitForGameThread();
		NucleusSingleton.Spin();
		OSWindow.PropagateEventBuffer();

		MainThread.Run(ThreadExecutionTime.BeforeFrame);

		windowsThisFrame.Clear();
		foreach (var window in WindowContexts)
			windowsThisFrame.Add(window.Key);

		foreach (var window in windowsThisFrame) {
			MakeWindowCurrent(window);
			PerWindowFrame();
		}
		ReleaseGameThread();
		Host.CheckDirty();
		MainThread.Run(ThreadExecutionTime.AfterFrame);
	}
	static void PerWindowFrame() {
		NProfiler.Reset();

		if (Window.MouseFocused)
			MouseCursor_Frame = MouseCursor.MOUSE_CURSOR_DEFAULT;

		CurrentAppTime = OS.GetTime();
		UpdateTime = CurrentAppTime - PreviousAppTime;
		PreviousAppTime = CurrentAppTime;

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
				Graphics2D.DrawText(screenBounds.W / 2, screenBounds.H / 2, "LOADING", Graphics2D.UI_FONT_NAME, 24, TextAlignment.Center, TextAlignment.Top);
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
			Graphics2D.DrawText(screenBounds.X / 2, screenBounds.Y / 2, "<No level loaded!>", Graphics2D.UI_FONT_NAME, 24, TextAlignment.Center, TextAlignment.Center);
			//Graphics2D.DrawText(screenBounds.X / 2, screenBounds.Y / 2, "Make sure you're changing EngineCore.Level.", Graphics2D.UI_FONT_NAME, 18, TextAlignment.Center, TextAlignment.Top);

			int y = 0;
			var msgs = ConsoleSystem.GetMessages();
			int txS = 12;
			for (int j = ConsoleSystem.GetMessagesCount() - 1; j >= 0; j--) {
				ref readonly ConsoleMessage cmsg = ref msgs[j];

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
		Level? nextFrameLevel = GetWindowCtx(Window).NextFrameLevel;
		if (nextFrameLevel != null) {
			__loadLevel(Window, nextFrameLevel, GetWindowCtx(Window).NextFrameArgs ?? []);
			GetWindowCtx(Window).NextFrameLevel = null;
		}

		Rlgl.DrawRenderBatchActive();
		if (Level == null || Level.RenderedFrame)
			Window.SwapScreenBuffer();

		// EXPERIMENT: Before any timing checks, perform a GC generation 0 collection
		if (gc_collectperframe.GetBool())
			GC.Collect(0, GCCollectionMode.Forced, true);

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

	internal static Level? GetWindowLevel(OSWindow window) => GetWindowCtx(window).Level;

	static readonly Mutex GameThreadMutex = new();
	public static void WaitForGameThread() {
		GameThreadMutex.WaitOne();
	}
	public static void ReleaseGameThread() {
		GameThreadMutex.ReleaseMutex();
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
	private const string PANIC_FONT_ARABIC = "Noto Sans Arabic";
	private static readonly string PANIC_FONT_TC = CultureInfo.CurrentCulture.Name switch {
		"zh-HK" => "Noto Sans HK",
		"zh-MO" => "Noto Sans HK",
		_ => "Noto Sans TC",
	};
	private const string PANIC_FONT_SC = "Noto Sans SC";
	private const string PANIC_FONT_KR = "Noto Sans KR";
	private const string PANIC_FONT_JP = "Noto Sans JP";
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
	private static readonly Dictionary<string, string> ErrorMessages = new(){
		{"A fatal error has occured. Press any key to exit.", PANIC_FONT},
		{"حدث خطأ فادح. اضغط على أي مفتاح للخروج.", PANIC_FONT},
		{"Възникнала е фатална грешка. Натиснете който и да е клавиш, за да излезете.", PANIC_FONT_ARABIC},
		{"出现致命错误。按任意键退出。", PANIC_FONT_SC},
		{"發生致命錯誤。按任意鍵退出。", PANIC_FONT_TC},
		{"Došlo k fatální chybě. Stiskněte libovolnou klávesu pro ukončení.", PANIC_FONT},
		{"Der er opstået en fatal fejl. Tryk på en vilkårlig tast for at afslutte.", PANIC_FONT},
		{"Er is een fatale fout opgetreden. Druk op een willekeurige toets om af te sluiten.", PANIC_FONT},
		{"On ilmnenud fataalne viga. Väljumiseks vajutage suvalist klahvi.", PANIC_FONT},
		{"On tapahtunut kohtalokas virhe. Poistu painamalla mitä tahansa näppäintä.", PANIC_FONT},
		{"Une erreur fatale s'est produite. Appuyez sur n'importe quelle touche pour quitter.", PANIC_FONT},
		{"Es ist ein schwerwiegender Fehler aufgetreten. Drücken Sie eine beliebige Taste zum Beenden.", PANIC_FONT},
		{"Προέκυψε ένα μοιραίο σφάλμα. Πατήστε οποιοδήποτε πλήκτρο για έξοδο.", PANIC_FONT},
		{"Végzetes hiba történt. Nyomja meg bármelyik billentyűt a kilépéshez.", PANIC_FONT},
		{"Telah terjadi kesalahan fatal. Tekan sembarang tombol untuk keluar.", PANIC_FONT},
		{"Si è verificato un errore fatale. Premere un tasto qualsiasi per uscire.", PANIC_FONT},
		{"致命的なエラーが発生しました。いずれかのキーを押して終了してください。", PANIC_FONT_JP},
		{"치명적인 오류가 발생했습니다. 종료하려면 아무 키나 누르세요.", PANIC_FONT_KR},
		{"Ir notikusi fatāla kļūda. Nospiediet jebkuru taustiņu, lai izietu.", PANIC_FONT},
		{"Įvyko lemtinga klaida. Paspauskite bet kurį klavišą, kad išeitumėte.", PANIC_FONT},
		{"Det har oppstått en alvorlig feil. Trykk på en hvilken som helst tast for å avslutte.", PANIC_FONT},
		{"Wystąpił błąd krytyczny. Naciśnij dowolny przycisk, aby wyjść.", PANIC_FONT},
		{"Ocorreu um erro fatal. Prima qualquer tecla para sair.", PANIC_FONT},
		{"Ocorreu um erro fatal. Pressione qualquer tecla para sair.", PANIC_FONT},
		{"A apărut o eroare fatală. Apăsați orice tastă pentru a ieși.", PANIC_FONT},
		{"Произошла фатальная ошибка. Нажмите любую клавишу, чтобы выйти.", PANIC_FONT},
		{"Vyskytla sa fatálna chyba. Stlačte ľubovoľné tlačidlo, aby ste ukončili prácu.", PANIC_FONT},
		{"Zgodila se je usodna napaka. Za izhod pritisnite katero koli tipko.", PANIC_FONT},
		{"Se ha producido un error fatal. Pulse cualquier tecla para salir.", PANIC_FONT},
		{"Ett allvarligt fel har inträffat. Tryck på valfri tangent för att avsluta.", PANIC_FONT},
		{"Ölümcül bir hata oluştu. Çıkmak için herhangi bir tuşa basın.", PANIC_FONT},
		{"Виникла фатальна помилка. Натисніть будь-яку клавішу для виходу.", PANIC_FONT},
	};

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

				var box = new System.Numerics.Vector2(0, PANIC_SIZE * ErrorMessages.Count);
				foreach ((var languageLine, var languageFont) in ErrorMessages) {
					var size = Graphics2D.GetTextSize(languageLine, languageFont, PANIC_SIZE);
					if (size.X > box.X)
						box.X = size.X;
				}
				var padding = 32;
				var paddingDiv2 = padding / 2;
				var center = new System.Numerics.Vector2((Window.Size.W / 2) - (box.X / 2), (Window.Size.H / 2) - (box.Y / 2));
				Raylib.DrawRectangle((int)center.X - paddingDiv2, (int)center.Y - paddingDiv2, (int)box.X + padding, (int)box.Y + padding, new Color(10, 220));
				var langLineY = 0;
				foreach ((var line, var font) in ErrorMessages) {
					Graphics2D.DrawText(center.X + (box.X / 2), center.Y + (langLineY * PANIC_SIZE), line, font, PANIC_SIZE, Anchor.TopCenter);

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
					if (Window.KeyAvailable(out _, out _) || Window.UserClosed()) {
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
				if (Window.KeyAvailable(out _, out _)) {
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
