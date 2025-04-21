using Nucleus.Core;
using Nucleus.Platform;
using Nucleus.Engine;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using Raylib_cs;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Nucleus.EngineCore;
using System.Runtime.ExceptionServices;
using System.Buffers.Text;
using static System.Net.Mime.MediaTypeNames;

namespace Nucleus
{
	/// <summary>
	/// Marks the class as needing to be statically constructed during the engine initialization. Will fix most convar/concommand issues.
	/// </summary>
	public class MarkForStaticConstructionAttribute : Attribute;

	public enum ThreadExecutionTime
	{
		/// <summary>
		/// The function will run before any frame-code.
		/// </summary>
		BeforeFrame,
		/// <summary>
		/// The function will run after a FrameState is constructed, and before <see cref="Level.Think"/> happens.
		/// </summary>
		AfterFrameStateConstructed,
		/// <summary>
		/// The function will run after <see cref="Level.Think"/> executes.
		/// </summary>
		AfterThink,
		/// <summary>
		/// The function will run after frame-code is complete.
		/// </summary>
		AfterFrame
	}
	public record MainThreadExecutionTask(Action Action, ThreadExecutionTime When);

	/// <summary>
	/// A way to run parameterless and returnless methods/anonymous delegates on the main thread.
	/// </summary>
	/// 
	public static class MainThread
	{
		private static Thread? _thread = null;

		public static Thread Thread {
			get => _thread ?? throw new Exception("For some reason, the MainThread.Thread property was accessed before it was set. EngineCore initialization sets this variable.");
			set {
				if (_thread != null)
					throw new Exception("The MainThread.Thread property can not be set again.");
				_thread = value;
			}
		}
		private static ConcurrentQueue<MainThreadExecutionTask> Actions { get; } = [];

		public static void RunASAP(Action a, ThreadExecutionTime when = ThreadExecutionTime.BeforeFrame) => Actions.Enqueue(new(a, when));

		public static void Run(ThreadExecutionTime when) {
			lock (Actions) {
				List<MainThreadExecutionTask> putBack = [];

				while (Actions.TryDequeue(out var task)) {
					if (task.When == when)
						task.Action();
					else
						putBack.Add(task);
				}

				foreach (var task in putBack)
					Actions.Enqueue(task);
			}
		}
	}
	[MarkForStaticConstruction]
	public static class EngineCore
	{
		public static ConCommand gc_collect = ConCommand.Register("gc_collect", (_, _) => {
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
		}, "Performs an immediate GC collection of all generations");

		public static ConVar engine_wireframe = ConVar.Register("engine_wireframe", "0", ConsoleFlags.None, "Enables wireframe rendering", 0, 1, (cv, _, _) => {
			// Queued so there's actually a GL context to work with
			MainThread.RunASAP(() => {
				if (cv.GetBool())
					Rlgl.EnableWireMode();
				else
					Rlgl.DisableWireMode();
			});
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
		}
		public static bool IsAssemblyDebugBuild(Assembly assembly) {
			return assembly.GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(da => da.IsJITTrackingEnabled);
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

		public static MouseCursor MouseCursor_Frame { get; private set; }
		public static MouseCursor? MouseCursor_Persist { get; private set; }

		public static void SetMouseCursor(MouseCursor? mouseCursor, bool persist = false) {
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
		public static void Initialize(int windowWidth, int windowHeight, string windowName = "Nucleus Engine", string[]? args = null, string? icon = null, ConfigFlags[]? flags = null) {
			MainThread.Thread = Thread.CurrentThread;

			Host.ReadConfig();
			CommandLineArguments.FromArgs(args ?? []);
			ShowDebuggingInfo = CommandLineArguments.IsParamTrue("debug");


			Packages.ErrorIfLinuxAndPackageNotInstalled("libx11-dev", "sudo apt-get install libx11-dev");

			// check build number, 3rd part is days since jan 1st, 2000
			ConsoleSystem.Initialize();
			Console.Title = windowName;

			var isDebug = IsAssemblyDebugBuild(typeof(EngineCore).Assembly);
			Logs.Info($"Nucleus Engine, BuildConfig {(isDebug ? "DEBUG" : "RELEASE")}.");
			Logs.Info("Initializing...");
			unsafe {
				Raylib.SetTraceLogCallback(&LogCustom);
			}

			ConfigFlags add = (ConfigFlags)0;
			bool undecorated = false;

			if (flags != null) {
				foreach (var flag in flags) {
					add |= flag;
					if (flag == ConfigFlags.FLAG_WINDOW_UNDECORATED) {
						undecorated = true;
						//add |= ConfigFlags.FLAG_WINDOW_TRANSPARENT;
					}
				}
			}

			Raylib.InitAudioDevice();
			Raylib.SetConfigFlags(ConfigFlags.FLAG_MSAA_4X_HINT | ConfigFlags.FLAG_WINDOW_RESIZABLE | add);
			Raylib.InitWindow(windowWidth, windowHeight, windowName);
			if (CommandLineArguments.TryGetParam("monitor", out int monitor)) {
				var monitorPos = Raylib.GetMonitorPosition(monitor);
				var monitorW = Raylib.GetMonitorWidth(monitor);
				var monitorH = Raylib.GetMonitorHeight(monitor);
				var monitorSize = new System.Numerics.Vector2(monitorW, monitorH);
				var windowSize = new System.Numerics.Vector2(windowWidth, windowHeight);

				// monitor TL + (monitor size / 2) == centerMonitor
				// centerMonitor - (window size / 2) to center window to monitor
				var windowPos = (monitorPos + (monitorSize / 2)) - (windowSize / 2);

				Raylib.SetWindowPosition((int)windowPos.X, (int)windowPos.Y);
			}
			Raylib.SetTraceLogLevel(TraceLogLevel.LOG_WARNING);
			if (icon != null) Raylib.SetWindowIcon(Filesystem.ReadImage("images", icon));
			OpenGL.Import(OpenGLAddressRetriever.GetProc);
			Raylib.SetExitKey(Raylib_cs.KeyboardKey.KEY_NULL);

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
		}

		public static PerfGraph CPUGraph;
		public static PerfGraph MemGraph;
		public delegate void LevelRemovedDelegate(Level level);
		public static event LevelRemovedDelegate? LevelRemoved;

		private static void __loadLevel(Level level, object[] args) {
			if (level == null) {
				LevelRemoved?.Invoke(Level);
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
			InGameConsole.HookToLevel(Level);
			LoadingLevel = false;

			LoadingScreen?.Unload();
			__nextFrameLevel = null;
			__nextFrameArgs = null;
			if (EngineCore.ShowDebuggingInfo) {
				CPUGraph = Level.UI.Add(new PerfGraph() {
					Anchor = Anchor.BottomRight,
					Origin = Anchor.BottomRight,
					Position = new(-8, -8 + -32 + -8),
					Size = new(400, 32),
					Mode = PerfGraphMode.CPU_Frametime
				});
				MemGraph = Level.UI.Add(new PerfGraph() {
					Anchor = Anchor.BottomRight,
					Origin = Anchor.BottomRight,
					Position = new(-8, -8),
					Size = new(400, 32),
					Mode = PerfGraphMode.RAM_Usage
				});
			}

			s.Stop();

			GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true);
			GC.WaitForFullGCComplete();

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

			if (Level != null) {
				Level.Unload();
				Level.UI.Remove();
				LoadingScreen?.Unload();
			}
			LevelRemoved?.Invoke(Level);
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced | GCCollectionMode.Forced);
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
			Vector2F ret = new Vector2F(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
			if (IsUndecorated && !Maximized)
				ret -= new Vector2F(8);
			return ret;
		}

		public static RectangleF GetScreenBounds() => RectangleF.FromPosAndSize(new(0, 0), GetScreenSize());

		public static Vector2F GetGlobalScreenOffset() {
			if (!IsUndecorated || Maximized)
				return Vector2F.Zero;

			return new(4);
		}

		public static FrameState CurrentFrameState { get; set; }
		public static GameInfo GameInfo { get; set; }

		public static float FrameTime => Raylib.GetFrameTime();

		public static bool ShowConsoleLogsInCorner { get; set; } = true;
		public static bool ShowDebuggingInfo { get; set; } = false;
		public static string FrameCost { get; set; } = "";
		public static float FrameCostMS { get; set; }
		public static float FPS => Raylib.GetFPS();
		public static ConVar fps_max = ConVar.Register("fps_max", "0", ConsoleFlags.Saved, "Default FPS. By default, unlimited.", 0, 10000, (cv, _, _) => LimitFramerate(cv.GetInt()));

		public static void Frame() {
			MouseCursor_Frame = MouseCursor.MOUSE_CURSOR_DEFAULT;
			Raylib.BeginDrawing();
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

			if (Level != null)
				Level.Frame();
			else {
				Graphics2D.SetDrawColor(30, 5, 0);
				Graphics2D.DrawRectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
				Graphics2D.SetDrawColor(240, 70, 60);
				Graphics2D.DrawText(screenBounds.X / 2, screenBounds.Y / 2, "No level loaded or in the process of loading!", "Noto Sans", 24, TextAlignment.Center, TextAlignment.Bottom);
				Graphics2D.DrawText(screenBounds.X / 2, screenBounds.Y / 2, "Make sure you're changing EngineCore.Level.", "Noto Sans", 18, TextAlignment.Center, TextAlignment.Top);
			}
			if (!EngineCore.Running) {
				Raylib.CloseWindow();
				return;
			}

			InLevelFrame = false;
			if (__nextFrameLevel != null) {
				__loadLevel(__nextFrameLevel, __nextFrameArgs);
				__nextFrameLevel = null;
			}
			Raylib.EndDrawing();
			Raylib.SetMouseCursor(MouseCursor_Persist ?? MouseCursor_Frame);

			Host.CheckDirty();

			if (Raylib.WindowShouldClose()) {
				Close();
			}
		}

		public static bool Maximized => Raylib.IsWindowMaximized();
		public static bool Minimized => Raylib.IsWindowMinimized();
		public static bool Focused => Raylib.IsWindowFocused();
		public static bool InFullscreen => Raylib.IsWindowFullscreen();
		public static bool IsUndecorated => Raylib.IsWindowState(ConfigFlags.FLAG_WINDOW_UNDECORATED);

		public static void Maximize() {
			if (Maximized) {
				Unmaximize();
				return;
			}
			Raylib.MaximizeWindow();
		}
		public static void Unmaximize() {
			if (!Maximized)
				return;
			Raylib.RestoreWindow();
		}
		public static void Minimize() {
			if (Minimized) {
				Unminimize();
				return;
			}
			Raylib.MinimizeWindow();
		}
		public static void Unminimize() {
			if (!Minimized)
				return;
			Raylib.RestoreWindow();
		}

		public static unsafe Vector2F MousePos {
			get => GlobalMousePointerRetriever.GetMousePos();
		}

		public static void LimitFramerate(int v) {
			Raylib.SetTargetFPS(v);
		}
		private static bool shouldThrow = false;
		public static void Start() {
			foreach (var t in from a in AppDomain.CurrentDomain.GetAssemblies()
							  from t in a.GetTypes()
							  let attributes = t.GetCustomAttributes(typeof(MarkForStaticConstructionAttribute), true)
							  where attributes != null && attributes.Length > 0
							  select t) {
				RuntimeHelpers.RunClassConstructor(t.TypeHandle);
			}

			ExceptionDispatchInfo? edi = null;
			Started = true;
			if (Debugger.IsAttached) {
				// Skip panic routine.
				Logs.Info("PANIC: Disabled due to the presence of a debugger.");
				LoadingScreen?.Initialize([]);
				while (Running) {
					shouldThrow = false;
					Frame();
				}
				Logs.Info("Nucleus Engine has halted peacefully.");
			}
			try {
				Logs.Info("PANIC: Active.");
				LoadingScreen?.Initialize([]);
				while (Running) {
					shouldThrow = false;
					Frame();
				}
				Logs.Info("Nucleus Engine has halted peacefully.");
			}
			catch (Exception ex) {
				edi = ExceptionDispatchInfo.Capture(ex);
				if (!Panic(edi)) {
					edi.Throw();
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
				Graphics2D.SetDrawColor(255, 255, 255);
				Graphics2D.DrawRectangle(64, 64, 256, 256);
			}, (a.GetInt(0) ?? 0) > 0);
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
		private static readonly string[] ErrorMessageInAutoTranslatedLanguages = [
			"A fatal error has occured. Press any key to exit.",
			"حدث خطأ فادح. اضغط على أي مفتاح للخروج.",
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
			"치명적인 오류가 발생했습니다. 종료하려면 아무 키나 누르세요.",
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
			Raylib.SetWindowTitle("Nucleus Engine - Panicked!");
			Raylib.SetWindowMinSize(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
			Raylib.SetWindowMaxSize(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
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

				if (y < Raylib.GetScreenHeight()) {
					int elapsedY = (int)((float)elapsed * 1150);
					Raylib.DrawRectangle(0, y, Raylib.GetScreenWidth(), elapsedY, new(70, 170));
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
					var center = new System.Numerics.Vector2((Raylib.GetScreenWidth() / 2) - (box.X / 2), (Raylib.GetScreenHeight() / 2) - (box.Y / 2));
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
				}
				else {
					Raylib.PollInputEvents();
					if (Raylib.GetKeyPressed() != 0) {
						Raylib.SetMasterVolume(oldMaster);
						return false;
					}
				}

				Rlgl.DrawRenderBatchActive();
				Raylib.SwapScreenBuffer();
				Raylib.WaitTime(hasRenderedOverlay ? 0.2 : 0.005);
			}
		}

		private static bool interrupting = false;
		public static bool InInterrupt => interrupting;
		public static void Interrupt(Action draw, bool problematic, params string[] messages) {
			if (interrupting) return;
			interrupting = true;

			var oldMaster = Raylib.GetMasterVolume();
			Raylib.SetMasterVolume(0);

			Raylib.SetWindowMinSize(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
			Raylib.SetWindowMaxSize(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());

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

				if (y < Raylib.GetScreenHeight()) {
					int elapsedY = (int)((float)elapsed * 1150);
					Raylib.DrawRectangle(0, y, Raylib.GetScreenWidth(), elapsedY, new(90, 100, 120, 170));
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
					lines[1] = "Press any key to continue";
					lines[2] = "";
					for (int i = 0; i < messages.Length; i++) lines[i + 3] = messages[i];


					var box = new System.Numerics.Vector2(0, PANIC_SIZE * ErrorMessageInAutoTranslatedLanguages.Length);
					foreach (var languageLine in lines) {
						var size = Graphics2D.GetTextSize(languageLine, PANIC_FONT, PANIC_SIZE);
						if (size.X > box.X)
							box.X = size.X;
					}
					var padding = 32;
					var paddingDiv2 = padding / 2;
					var center = new System.Numerics.Vector2((Raylib.GetScreenWidth() / 2) - (box.X / 2), (box.Y / 2) + padding);
					Raylib.DrawRectangle((int)center.X - paddingDiv2, (int)center.Y - paddingDiv2, (int)box.X + padding, (int)box.Y + padding, new Color(10, 220));
					var langLineY = 0;
					foreach (var line in ErrorMessageInAutoTranslatedLanguages) {
						Graphics2D.DrawText(center.X + (box.X / 2), center.Y + (langLineY * PANIC_SIZE), line, PANIC_FONT, PANIC_SIZE, Anchor.TopCenter);

						langLineY++;
					}

					draw();
					hasRenderedOverlay = true;
				}
				else {
					Raylib.PollInputEvents();
					if (Raylib.GetKeyPressed() != 0) {
						Raylib.SetMasterVolume(oldMaster);
						interrupting = false;
						return;
					}
				}

				Rlgl.DrawRenderBatchActive();
				Raylib.SwapScreenBuffer();
				Raylib.WaitTime(hasRenderedOverlay ? 0.2 : 0.005);
			}
		}

		public static Vector2F GetWindowSize() {
			return new(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
		}

		public static float GetWindowWidth() => Raylib.GetScreenWidth();
		public static float GetWindowHeight() => Raylib.GetScreenHeight();

		public static void SetWindowPosition(Vector2F mposFinal) {
			Raylib.SetWindowPosition((int)mposFinal.X, (int)mposFinal.Y);
		}

		public static void SetWindowTitle(string title) {
			Raylib.SetWindowTitle(title);
		}

		public static void StopSound() {
			var lvl = Level;

			if (lvl == null) return;
			if (lvl.Sounds == null) return;

			lvl.Sounds.Dispose();
		}
	}
}