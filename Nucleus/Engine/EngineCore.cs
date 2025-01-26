using Nucleus.Core;
using Nucleus.CrossPlatform;
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
    public static class EngineCore
    {
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

            CommandLineArguments.FromArgs(args ?? []);
            ShowDebuggingInfo = CommandLineArguments.IsParamTrue("debug");

            Packages.ErrorIfLinuxAndPackageNotInstalled("libx11-dev", "sudo apt-get install libx11-dev");

            // check build number, 3rd part is days since jan 1st, 2000
            ConsoleSystem.Initialize();
            Console.Title = windowName;

            var version = new DateTime(2000, 1, 1) + TimeSpan.FromDays(typeof(EngineCore).Assembly.GetName().Version.Build);
            var isDebug = IsAssemblyDebugBuild(typeof(EngineCore).Assembly);
            Logs.Info($"Nucleus Engine, Build {(isDebug ? "[DEBUG]" : "[RELEASE]")} {version.Month}-{version.Day}-{version.Year}.");
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
            Raylib.SetTraceLogLevel(TraceLogLevel.LOG_WARNING);
            Raylib.SetConfigFlags(ConfigFlags.FLAG_MSAA_4X_HINT | ConfigFlags.FLAG_WINDOW_RESIZABLE | add);
            Raylib.InitWindow(windowWidth, windowHeight, windowName);
            if (icon != null) Raylib.SetWindowIcon(Raylib.LoadImage(Filesystem.Resolve(icon, "images")));
            OpenGL.Import(OpenGLAddressRetriever.GetProc);
            Raylib.SetExitKey(Raylib_cs.KeyboardKey.KEY_NULL);

            // English language
            Graphics2D.RegisterCodepoints(@"`1234567890-=qwertyuiop[]\asdfghjkl;'zxcvbnm,./~!@#$%^&*()_+QWERTYUIOP{}|ASDFGHJKL:""ZXCVBNM<>?");
            //Graphics2D.RegisterCodepoints(@"`1234567890");

            // Japanese (hiragana, katakana)
            Graphics2D.RegisterCodepoints(@"あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをんがぎぐげござじずぜぞだぢづでどばびぶべぼぱぴぷぺぽ");
            Graphics2D.RegisterCodepoints(@"アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲンガギグゲゴザジズゼゾダヂヅデドバビブベボパピプペポ");
        }

        private static Window console;
        private static Textbox lines;

        private static void __loadLevel(Level level, object[] args) {
            if (level == null) {
                Level = null;
                LoadingLevel = false;
                return;
            }
            if (Level != null) {
                UnloadLevel();
            }

            Model3AnimationChannel.GlobalPause = false;
            Logs.Info($"Loading level {level.GetType().Name}...");
            Stopwatch s = new Stopwatch();
            s.Start();

            Level = level;
            LoadingLevel = true;
            level.InitializeUI();
            Level.Initialize(args);
            LoadingLevel = false;

            LoadingScreen?.Unload();
            __nextFrameLevel = null;
            __nextFrameArgs = null;
            s.Stop();

            Logs.Info($"{level.GetType().Name} loaded in {s.Elapsed.TotalSeconds:0.####} seconds");
            //GC.Collect();
            //GC.WaitForPendingFinalizers();
        }

        private static Level __nextFrameLevel;
        private static object[] __nextFrameArgs;
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
        public static string FrameCost { get; set; }
        public static float FrameCostMS { get; set; }
        public static float FPS => Raylib.GetFPS();

        public static void Frame() {
            MouseCursor_Frame = MouseCursor.MOUSE_CURSOR_DEFAULT;
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(0, 0, 0, 255));
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

        public static void Start() {
			foreach (var t in from a in AppDomain.CurrentDomain.GetAssemblies()
							  from t in a.GetTypes()
							  let attributes = t.GetCustomAttributes(typeof(MarkForStaticConstructionAttribute), true)
							  where attributes != null && attributes.Length > 0
							  select t) {
				RuntimeHelpers.RunClassConstructor(t.TypeHandle);
			}

			Started = true;
            LoadingScreen?.Initialize([]);
            while (Running) {
                Frame();
            }
            Logs.Info("Nucleus Engine has halted peacefully.");
        }

        public static Vector2F GetWindowSize() {
            return new(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
        }

        public static void SetWindowPosition(Vector2F mposFinal) {
            Raylib.SetWindowPosition((int)mposFinal.X, (int)mposFinal.Y);
        }
    }
}