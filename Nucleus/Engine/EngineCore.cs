using Nucleus.Commands;
using Nucleus.Common.Commands;
using Nucleus.Common.Engine;
using Nucleus.Common.Input;
using Nucleus.Common.OS;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Extensions;
using Nucleus.Files;
using Nucleus.Input;
using Nucleus.Rendering;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using Nucleus.Util;
using Raylib_cs;
using SDL;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

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
	[ConCommand(Help: "Exits the engine via EngineCore.Close(forced: false)")] static void exit() => Close(false);
	[ConCommand(Help: "Exits the engine via EngineCore.Close(forced: true)")] static void quit() => Close(true);
	[ConCommand(Help: "Unloads the current level")] static void unload() => MainThread.RunASAP(UnloadLevel, ThreadExecutionTime.AfterFrame);
	[ConCommand(Help: "Tries to create a new level with the first argument. Will not work if the level requires initialization parameters.")]
	static void level(in TokenizedCommand args) {
		var level = args[1];
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
			if (lvl.FullName.Equals(level, StringComparison.OrdinalIgnoreCase)) {
				Logs.Info($"Attempting to load {level}...");
				EngineCore.LoadLevel((Activator.CreateInstance(lvl) as Level)!, []);
				return;
			}
		}

		Logs.Error($"No level with the name '{level}'.");
	}

	public static ConVar engine_wireframe = new(nameof(engine_wireframe), "0", FCvar.None, "Enables wireframe rendering", 0, 1, (cv, _, _) => {
		// Queued so there's actually a GL context to work with
		MainThread.RunASAP(() => {
			if (cv.GetBool())
				Rlgl.EnableWireMode();
			else
				Rlgl.DisableWireMode();
		});
	});

	public static ConCommand engine_activetextures = new(nameof(engine_activetextures), (_, in _) => {
		var texs = new List<string>();
		foreach (var texIDPair in Raylib.GetLoadedTextures())
			texs.Add($"{texIDPair.Id} [{texIDPair.Width} x {texIDPair.Height} of format {texIDPair.Format}]");

		PanicSystem.Interrupt(() => { }, false, texs.ToArray());
	});

	// ------------------------------------------------------------------------------------------ //
	// Level storing & state
	// ------------------------------------------------------------------------------------------ //

	static Level? NextFrameLevel;
	static object[]? NextFrameArgs;
	static TimeSpan LastTimeToUpdate;
	static TimeSpan LastTimeToRender;

	public static ConVar snd_volume = new("snd_volume", "1.0", FCvar.Saved, "Overall sound volume.", 0, 2f, (cv, o, n) => audiosystem.SetMasterVolume(cv.GetFloat()));

	public static Level LoadingScreen { get; set; }

	/// <summary>
	/// The current level; if null, you'll get a big red complaint
	/// </summary>
	public static Level Level { get; private set; } = null!;
	/// <summary>
	/// Is the engine core currently loading a level. This overrides everything else; level frame's dont get called when this is turned on.
	/// </summary>
	public static bool LoadingLevel { get; private set; } = false;

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
	private static unsafe void LogCustom(int logLevel, sbyte* text, sbyte* args) {
		var message = Logging.GetLogMessage(new IntPtr(text), new IntPtr(args));
		if (message == "FILEIO: [] Failed to open text file") return;
		using (Logs.SourceScope("raylib")) {
			if (DetectAnnoyingRaylibMessages(message))
				return;

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
		}
	}

	private static bool DetectAnnoyingRaylibMessages(ReadOnlySpan<char> message) {
		const string FONT_START = "FONT: ";
		if (message.StartsWith(FONT_START)) {
			// Raylib 6 introduced a message for when glyph heights are bigger than requested font size.
			// Not a bad thing in particular but way too annoying. We intentionally spray and pray how many
			// codepoints the font can load for us as well which triggers another message about how Raylib
			// didn't produce all requested codepoints. So this catches both.
			ReadOnlySpan<char> submessage = message[FONT_START.Length..];
			if (submessage.StartsWith('[')) {
				// skip by (lbrack) 1 + (hex) 6 + (rbrack) 1 + (space) 1 to skip past the hex
				const int SKIP_BY = 1 + 6 + 1 + 1;
				if (submessage.Length < SKIP_BY)
					return false;
				submessage = submessage[SKIP_BY..];
				if (submessage.StartsWith("Glyph height is bigger than"))
					return true;
			}
			else if (submessage.StartsWith("Requested codepoints glyphs"))
				return true;
		}

		return false;
	}

	public static Window OpenProfiler() {
		Window window = new Window(Level.RootPanel);

		window.Title = "Nucleus Profiler";
		window.GetAddParent().SetPaintBackgroundEnabled(false);
		window.Titlebar.MinimizeButton.SetMouseInputEnabled(false);
		window.Titlebar.MaximizeButton.SetMouseInputEnabled(false);

		return window;
	}

	private static void AddParent_PaintOverride(Element self, float width, float height) {
		int y = 12;
		self.GetParent()?.BorderSize = 0;

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
		const float rectPadding = 4;
		const float textWidth = 288;
		const float msWidth = 80;
		float fnWidth = (width - rectPadding - rectPadding - textWidth - msWidth);

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

	static void MakeWindowCurrent(OSWindow window) {
		Rlgl.SetFramebufferWidth((int)window.Size.W);
		Rlgl.SetFramebufferHeight((int)window.Size.H);
		window.ActivateGL();
		window.SetupViewport(window.Size.W, window.Size.H);
		Window = window;
	}

	public static void GameThreadProcedure() {
		// Initialize the window GL
		lock (GameThread_GLLock) {
			Window.SetupGL();
			MakeWindowCurrent(Window);

			if (prgIcon != null)
				Window.SetIcon(Filesystem.ReadImage("images", prgIcon));

			OpenGL.Import(Platform.OpenGL_GetProc);
			gfxHardwareConfig.ConfirmCapabilities();

			// English language
			Graphics2D.RegisterCodepoints(@"`1234567890-=qwertyuiop[]\asdfghjkl;'zxcvbnm,./~!@#$%^&*()_+QWERTYUIOP{}|ASDFGHJKL:""ZXCVBNM<>?");
			//Graphics2D.RegisterCodepoints(@"`1234567890");

			// Japanese (hiragana, katakana)
			Graphics2D.RegisterCodepoints(@"あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをんがぎぐげござじずぜぞだぢづでどばびぶべぼぱぴぷぺぽ");
			Graphics2D.RegisterCodepoints(@"アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲンガギグゲゴザジズゼゾダヂヅデドバビブベボパピプペポ");

			// Some korean
			Graphics2D.RegisterCodepoints(@"하고는을이다의에지게도한안가나의되사아그수과보있어서것같시으로와더는지기요내나또만주잘어서면때자게해이제여어야전라중좀거그래되것들이에게해요정말");

			Graphics2D.RegisterCodepoints(string.Join("", PanicSystem.ErrorMessages.Keys));

			// Set GameThread_GLReady flag so the main thread can finish its work
		}

		// And then wait for the main thread to set GameThread_Playing
		GameThreadInitializationProcedure?.Invoke();
		lock (GameThread_GLLock) ;

		StartGameThread();
	}
	private static string? prgIcon;

	static ConVar borderless = new(nameof(borderless), "0", FCvar.Saved, "Hide window decorations", min: 0, max: 1, callback: borderlessChange);
	private static void borderlessChange(ConVar self, ReadOnlySpan<char> old, double oldD) {
		if (Window != null)
			Window.Undecorated = self.GetBool();
	}

	static ConVar fullscreen = new(nameof(fullscreen), "0", FCvar.Saved, "Fullscreen mode", min: 0, max: 1, callback: fullscreenChange);
	private static void fullscreenChange(ConVar self, ReadOnlySpan<char> old, double oldD) {
		if (Window != null)
			Window.Fullscreen = self.GetBool();
	}

	public static void Initialize(int windowWidth, int windowHeight, in StartupInfo startupInfo, string? windowName = null, string? icon = null, ConfigFlags flags = 0, Action? gameThreadInit = null) {
		windowName ??= startupInfo.AppName;

		if (!MainThread.ThreadSet)
			MainThread.Thread = Thread.CurrentThread;

		filesystem.Initialize(startupInfo.AppName);
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
		Logs.Info($"    > OpenGL version:     {Rlgl.GetVersion()}");

		audiosystem.Initialize();
		// Initialize SDL. This has to be done on the main thread.
		OS.InitSDL(in startupInfo);
		if (borderless.GetBool())
			flags |= ConfigFlags.WindowUndecorated;
		if (fullscreen.GetBool()) {
			flags |= ConfigFlags.FullScreenMode;
			// Fix monitor sizing for fullscreens first frame
			// todo: is there a better way to do this? This interferes with a few things I think
			OSMonitor curMonitor = CommandLine().ParmValue("monitor", OS.GetPrimaryMonitor().DisplayID);
			var size = curMonitor.Size;
			windowWidth = (int)size.W;
			windowHeight = (int)size.H;
		}
		Window = OSWindow.Create(windowWidth, windowHeight, windowName, ConfigFlags.WindowMSAA4XHint | ConfigFlags.WindowResizable | flags);

		// Run engine initialization
		// this sets up early JIT assemblies and static constructors
		Assembly? ea = Assembly.GetEntryAssembly();
		if (ea != null)
			EarlyJITAssemblies.Add(ea);

		EarlyJITAssemblies.Add(Assembly.GetExecutingAssembly());
		EarlyJITAssemblies.Add(Assembly.GetCallingAssembly());

		gameDLL.PreStaticInitialize();

		Logs.Info("BOOT: Initializing static constructors...");
		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
			if (!assembly.IsDefined(typeof(NucleusAssemblyAttribute)))
				continue;

			foreach (Type type in assembly.GetTypes()) {
				object[] attributes = type.GetCustomAttributes(typeof(MarkForStaticConstructionAttribute), true);
				if (attributes is { Length: > 0 })
					RuntimeHelpers.RunClassConstructor(type.TypeHandle);

				foreach ((MethodInfo baseMethod, ConCommandAttribute attr) ccmd in ConCommandAttribute.GetAttributes(type))
					ConCommandAttribute.RegisterAttribute(type, ccmd.baseMethod, ccmd.attr);
			}
		}

		ConVar.Register();
		Host.ReadConfiguration();

		Cbuf.AddText("stuffcmds");

		Host.Initialized = true;

		Logs.Info("BOOT: Running JIT early where possible...");
		Parallel.ForEach(EarlyJITAssemblies
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
					// ignored
				}
			});

		// We need to start the game thread and allow it to initialize.
		prgIcon = icon;
		GameThread = new Thread(GameThreadProcedure);
		if (!MainThread.GameThreadSet) MainThread.GameThread = GameThread;
		GameThread.Start();
		lock (GameThread_GLLock) ;

		if (CommandLine().HasParm("-monitor")) {
			OSMonitor monitor = new OSMonitor(CommandLine().ParmValue("-monitor", 0));
			Window.Center(monitor);
		}

		Raylib.SetTraceLogLevel(TraceLogLevel.LOG_WARNING);
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

		Level = level;
		ResetWindowLevelSpecificEnv(window);
		LoadingLevel = true;
		level.PreInitialize();
		level.InitializeUI();
		Level.Initialize(args);
		Level.__isValid = true;
		InGameConsole.HookToLevel(Level);
		LoadingLevel = false;

		NextFrameLevel = null;
		NextFrameArgs = null;

		LoadingScreen?.Unload();
		Level.DeveloperOverlay.SetUpDebugOverlays();

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
			NextFrameLevel = level;
			NextFrameArgs = args;
			LoadingScreen?.Initialize([]);
		}
		else
			__loadLevel(window, level, args);
	}
	public static void LoadLevel(Level level, params object[] args) => LoadLevel(Window, level, args);
	public static void UnloadLevel() {
		LoadingLevel = true;

		if (Level != null) {
			Level.Unload();
			LoadingScreen?.Unload();
		}
		StopSound();
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
		Window.Close();
		_running = false;
	}

	public static int BorderlessScreenPadding = 2;

	public static Vector2F GetScreenSize() {
		Vector2F ret = Window.Size;
		if (IsUndecorated && !Maximized)
			ret -= new Vector2F(BorderlessScreenPadding * 2);
		return ret;
	}

	public static RectangleF GetScreenBounds() => RectangleF.FromPosAndSize(new(0, 0), Window.Size);

	public static Vector2F GetGlobalScreenOffset() {
		if (!IsUndecorated || Maximized)
			return Vector2F.Zero;

		return new(BorderlessScreenPadding);
	}
	public static double TargetFrameTime { get; set; }
	public static double CurrentAppTime { get; set; }
	public static double PreviousAppTime { get; set; }
	public static double UpdateTime { get; set; }
	public static double DrawTime { get; set; }
	public static double FrameTime { get; set; }

	public static readonly ConVar developer = new("developer", "0", FCvar.None, "Enables/disables developer prints and overlays.", null);

	static bool? devoverlay_override;
	public static void SetDeveloperOverlayOverride(bool? ovr) {
		devoverlay_override = ovr;
	}
	public static bool ShouldShowDeveloperOverlays() {
		return devoverlay_override ?? developer.GetInt() >= 1;
	}


	/// <summary>
	/// How long did the last update-frame take?
	/// </summary>
	/// <returns></returns>
	public static TimeSpan GetTimeToUpdate() => LastTimeToUpdate;
	/// <summary>
	/// How long did the last render-frame take?
	/// </summary>
	/// <returns></returns>
	public static TimeSpan GetTimeToRender() => LastTimeToRender;
	internal static void SetTimeToUpdate(TimeSpan value) => LastTimeToUpdate = value;
	internal static void SetTimeToRender(TimeSpan value) => LastTimeToRender = value;

	private const int FPS_CAPTURE_FRAMES_COUNT = 30;
	private const float FPS_AVERAGE_TIME_SECONDS = 0.5f;
	private const float FPS_STEP = FPS_AVERAGE_TIME_SECONDS / FPS_CAPTURE_FRAMES_COUNT;
	private static int fps_index = 0;
	private static float[] fps_history = new float[FPS_CAPTURE_FRAMES_COUNT];
	private static float fps_average = 0, fps_last = 0;

	public static OSWindow Window { get; private set; }

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
	public static ConVar fps_max = new("fps_max", "300", FCvar.Saved, "Default frames per second.", 0, 10000, (cv, _, _) => LimitFramerate(cv.GetInt()));
	public static ConVar r_renderat = new("renderrate", "60", FCvar.Saved, "Separate control over how often rendering functions in particular are ran.", 0, 10000);
	public static ConVar gc_collectperframe = new("gc_collectperframe", "0", FCvar.Saved, "If set to 1, Nucleus will perform a forced gen-0 garbage collection after every frame. This is an experiment, mileage may vary.", 0, 1);
	public static double RenderRate => r_renderat.GetDouble() == 0 ? 0 : 1d / r_renderat.GetDouble();

	private static string WorkConsole = "";
	public static void Frame() {
		WaitForGameThread();
		NucleusSingleton.Spin();
		OSWindow.PropagateEventBuffer();
		Host.CheckForResave();

		CurrentAppTime = OS.GetTime();
		UpdateTime = CurrentAppTime - PreviousAppTime;
		PreviousAppTime = CurrentAppTime;

		Graphics2D.FontManager.CleanUpFontsMarkedForDeath();
		MainThread.Run(ThreadExecutionTime.BeforeFrame);
		Cbuf.Execute();

		audiosystem.Update();

		MakeWindowCurrent(Window);
		PerWindowFrame();

		ReleaseGameThread();
		MainThread.Run(ThreadExecutionTime.AfterFrame);

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
	}
	static void PerWindowFrame() {
		NProfiler.Reset();

		MouseCursor_Frame = MouseCursor.MOUSE_CURSOR_DEFAULT;

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
			int txS = 12;
			var msgList = ConsoleSystem.GetAllMessagesList();
			msgList.BeginRead();
			int msgCount = msgList.ComputeCount();
			Span<int> offsets = stackalloc int[msgCount];
			int found = msgList.GetMessages(offsets, out _);

			Span<char> formatted = stackalloc char[512];

			for (int j = found - 1; j >= 0; j--) {
				if (!msgList.GetMessageAt(offsets, j, out var cmsg, out var msgText))
					continue;

				int lineCount = 1;
				for (int ci = 0; ci < msgText.Length; ci++) {
					if (msgText[ci] == '\r' && ci + 1 < msgText.Length && msgText[ci + 1] == '\n') {
						lineCount++;
						ci++;
					}
					else if (msgText[ci] == '\n')
						lineCount++;
				}

				int pos = 0;
				formatted[pos++] = '[';
				var levelStr = Logs.LevelToConsoleString(cmsg.Level);
				levelStr.CopyTo(formatted[pos..]);
				pos += levelStr.Length;
				formatted[pos++] = ']';
				formatted[pos++] = ' ';
				msgText.CopyTo(formatted[pos..]);
				pos += msgText.Length;

				y += lineCount;
				Graphics2D.DrawText(4, screenBounds.Y - 24 - (y * txS),
					formatted[..pos], "Consolas", txS, TextAlignment.Left, TextAlignment.Top);
			}
			msgList.EndRead();
			// mini game loop
			KeyboardState keyboardState = new();
			Window.FlushKeyboardStateInto(ref keyboardState);

			int i = 0;
			while (keyboardState.KeyAvailable(ref i, out ButtonCode k, out _)) {
				ButtonAction action = k.GetAction(ctrl: keyboardState.ControlDown, alt: keyboardState.AltDown, shift: keyboardState.ShiftDown);
				switch (action.Type) {
					case CharacterType.Enter:
						Logs.Info($"] {WorkConsole}");
						Cbuf.AddText(WorkConsole);
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
		Level? nextFrameLevel = NextFrameLevel;
		if (nextFrameLevel != null) {
			__loadLevel(Window, nextFrameLevel, NextFrameArgs ?? []);
			NextFrameLevel = null;
		}

		Rlgl.DrawRenderBatchActive();
		if (Level == null || Level.RenderedFrame)
			Window.SwapScreenBuffer();

		// EXPERIMENT: Before any timing checks, perform a GC generation 0 collection
		if (gc_collectperframe.GetBool())
			GC.Collect(0, GCCollectionMode.Forced, true);

		if (Window.MouseFocused)
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

	internal static bool ShouldThrowExceptions = false;
	private static readonly HashSet<Assembly> EarlyJITAssemblies = [];

	public static void StartGameThread() {
		Started = true;
		if (Debugger.IsAttached) {
			// Skip panic routine.
			Logs.Info("PANIC: Disabled immediate thread panicking due to the presence of a debugger.");
			LoadingScreen?.Initialize([]);
			while (Running) {
				ShouldThrowExceptions = false;
				Frame();
			}
		}
		else {
			try {
				Logs.Info("PANIC: Immediate thread panicking active.");
				LoadingScreen?.Initialize([]);
				while (Running) {
					ShouldThrowExceptions = false;
					Frame();
				}
			}
			catch (Exception ex) {
				ExceptionDispatchInfo edi = ExceptionDispatchInfo.Capture(ex);
				if (!PanicSystem.Panic(edi)) {
					edi.Throw();
				}
			}
		}

		Host.WriteConfiguration();
		Logs.Info("Nucleus Engine has halted peacefully.");
	}

	private static readonly Mutex GameThreadMutex = new();
	public static void WaitForGameThread() {
		GameThreadMutex.WaitOne();
	}
	public static void ReleaseGameThread() {
		GameThreadMutex.ReleaseMutex();
	}

	public static void StartMainThread() {
		// Fixes event delays on linux, but all operating systems should benefit
		Thread.CurrentThread.Priority = ThreadPriority.Highest;

		while (Running) {
			OSWindow.PumpOSEvents();
			if (Running)
				continue;

			if (Window.IsValid())
				Window.Close();
			return;
		}
	}

	public static Vector2F GetWindowSize() => Window.Size;

	public static float GetWindowWidth() => Window.Size.W;
	public static float GetWindowHeight() => Window.Size.H;
	public static void SetWindowPosition(Vector2F pos) => Window.Position = pos;
	public static void SetWindowTitle(string title) => Window.Title = title;

	public static void StopSound() => audiosystem.StopAllSounds();
}
