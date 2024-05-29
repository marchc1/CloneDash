using CloneDash.Systems;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using Raylib_cs;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Nucleus
{
    public static class EngineCore
    {
        // ------------------------------------------------------------------------------------------ //
        // Level storing & state
        // ------------------------------------------------------------------------------------------ //

        /// <summary>
        /// A special loading screen level. Always kept "loaded" in some fashion, although only runs its frame function when LoadingLevel == true.
        /// </summary>
        public static Level LoadingScreen { get; private set; }
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

                KeyboardFocusedElement.KeyboardFocusLost(self);
            }

            KeyboardFocusedElement = self;
            self.KeyboardFocusGained();
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
                KeyboardFocusedElement = null;
                self.KeyboardFocusLost(self);

                return;
            }
            if (self != KeyboardFocusedElement && force == false)
                return;

            KeyboardFocusedElement = null;
            self.KeyboardFocusLost(self);
        }
        public static bool IsAssemblyDebugBuild(Assembly assembly) {
            return assembly.GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(da => da.IsJITTrackingEnabled);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        private static unsafe void LogCustom(int logLevel, sbyte* text, sbyte* args) {
            var message = Logging.GetLogMessage(new IntPtr(text), new IntPtr(args));
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

        public static void Initialize(int windowWidth, int windowHeight, string windowName = "Nucleus Engine") {
            ConsoleSystem.Initialize();

            var isDebug = IsAssemblyDebugBuild(typeof(EngineCore).Assembly);
            Logs.Info($"Nucleus Engine, Build {(isDebug ? "[DEBUG]" : "[RELEASE]")}.");
            Logs.Info("Initializing...");
            unsafe {
                Raylib.SetTraceLogCallback(&LogCustom);
            }
            Raylib.InitAudioDevice();
            Raylib.SetTraceLogLevel(TraceLogLevel.LOG_WARNING);
            Raylib.SetConfigFlags(ConfigFlags.FLAG_MSAA_4X_HINT | ConfigFlags.FLAG_WINDOW_RESIZABLE);
            Raylib.InitWindow(windowWidth, windowHeight, windowName);
            Raylib.SetExitKey(Raylib_cs.KeyboardKey.KEY_NULL);
        }
        private static Window console;
        private static Textbox lines;
        private static void OpenConsole() {
            if (IValidatable.IsValid(console)) {
                console.Remove();
                return;
            }
            console = Level.UI.Add<Window>();
            console.Title = "Developer Console";
            console.Size = new(500, 700);
            console.Position = new(1000, 100);
            console.DockPadding = RectangleF.TLRB(4);

            var input = console.Add<Textbox>();
            input.Dock = Dock.Bottom;
            input.Font = "Consolas";
            input.TextAlignment = Anchor.MiddleLeft;
            input.HelperText = "";

            lines = console.Add<Textbox>();
            lines.ReadOnly = true;
            lines.Dock = Dock.Fill;
            lines.Font = "Consolas";
            lines.TextAlignment = Anchor.TopLeft;
            lines.HelperText = "";
            lines.ReadOnly = true;
            lines.MultiLine = true;
            lines.AutoSize = false;

            ConsoleSystem.ConsoleMessageWrittenEvent += ConsoleSystem_ConsoleMessageWrittenEvent;

            lines.Text = "";
            foreach(ConsoleMessage message in ConsoleSystem.GetMessages()) {
                lines.Text += message.Message + "\n";
            }

            console.Removed += delegate (Element self) {
                ConsoleSystem.ConsoleMessageWrittenEvent -= ConsoleSystem_ConsoleMessageWrittenEvent;
            };
        }

        private static void ConsoleSystem_ConsoleMessageWrittenEvent(LogLevel level, string text) {
            if (!IValidatable.IsValid(console))
                return;

            lines.Text += text + "\n";
        }

        private static void __loadLevel(Level level, object[] args) {
            if (level == null) {
                Level = null;
                LoadingLevel = false;
                return;
            }

            Model3AnimationChannel.GlobalPause = false;
            AudioSystem.Reset();

            Level = level;
            LoadingLevel = true;
            Level.Initialize(args);
            Level.Keybinds.AddKeybind([KeyboardLayout.USA.Tilda], new Action(() => {
                OpenConsole();
            }));
            LoadingLevel = false;

            __nextFrameLevel = null;
            __nextFrameArgs = null;
            GC.Collect();
        }

        private static Level __nextFrameLevel;
        private static object[] __nextFrameArgs;
        public static bool InLevelFrame { get; private set; } = false;
        public static void LoadLevel(Level level, params object[] args) {
            if (InLevelFrame) {
                __nextFrameLevel = level;
                __nextFrameArgs = args;
            }
            else
                __loadLevel(level, args);
        }

        public static void UnloadLevel() {
            LoadingLevel = true;

            if (Level != null) {
                Level.Unload();
                Level.UI.Remove();
            }

            TextureSystem.Unload();

            LoadingLevel = false;
        }

        public static bool Closing => Raylib.WindowShouldClose();

        public static FrameState CurrentFrameState { get; set; }
        public static GameInfo GameInfo { get; set; }

        public static float FrameTime => Raylib.GetFrameTime();

        public static void Frame() {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(0, 0, 0, 0));
            InLevelFrame = true;
            if (LoadingLevel) {
                if (LoadingScreen == null) {
                    Graphics2D.SetDrawColor(10, 15, 20);
                    Graphics2D.DrawRectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
                    Graphics2D.SetDrawColor(240, 245, 255);
                    Graphics2D.DrawText(Raylib.GetScreenWidth() / 2, Raylib.GetScreenHeight() / 2, "LOADING", "Arial", 24, TextAlignment.Center, TextAlignment.Top);
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
                Graphics2D.DrawText(Raylib.GetScreenWidth() / 2, Raylib.GetScreenHeight() / 2, "No level loaded or in the process of loading!", "Arial", 24, TextAlignment.Center, TextAlignment.Bottom);
                Graphics2D.DrawText(Raylib.GetScreenWidth() / 2, Raylib.GetScreenHeight() / 2, "Make sure you're changing EngineCore.Level.", "Arial", 18, TextAlignment.Center, TextAlignment.Top);
            }
            InLevelFrame = false;
            if (__nextFrameLevel != null) {
                __loadLevel(__nextFrameLevel, __nextFrameArgs);
                __nextFrameLevel = null;
            }
            Raylib.EndDrawing();
        }
    }
}