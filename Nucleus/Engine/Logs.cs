using Raylib_cs;

namespace Nucleus
{
	public enum LogLevel
	{
		Debug = 0,
		Info = 1,
		Print = 2,
		Success = 3,
		Warn = 4,
		Error = 5
	}

	public static class Logs
	{
		// Settings for the logger
		public static bool LogTime { get; set; } = true;
		public static bool PrintColor { get; set; } = true;
		public static bool Newline { get; set; } = true;
		public static bool ShowLevel { get; set; } = true;

		public static string TimeFormat { get; set; } = "M-dd-yyyy h:mm:ss tt";
		public static LogLevel LogLevel { get; set; } = LogLevel.Debug;

		private static bool _initializedColorConsole = false;

		private static Color _defaultBackground = new Color(15, 20, 25, 255);
		private static Color _defaultForeground = new Color(245, 245, 245, 255);

		public static LogLevel ConsoleStringToLevel(string logString) {
			switch (logString) {
				case "PRINT":
					return LogLevel.Print;
				case "DEBUG":
					return LogLevel.Debug;
				case "INFO ":
					return LogLevel.Info;
				case "GOOD ":
					return LogLevel.Success;
				case "WARN ":
					return LogLevel.Warn;
				case "ERROR":
					return LogLevel.Error;
				default:
					return LogLevel.Info; 
			}

		}
		public static Color LevelToColor(LogLevel l) {
			switch (l) {
				case LogLevel.Print:
					return new Color(210, 235, 255, 255);
				case LogLevel.Debug:
					return new Color(210, 245, 255, 255);
				case LogLevel.Info:
					return new Color(210, 245, 255, 255);
				case LogLevel.Success:
					return new Color(100, 255, 145, 255);
				case LogLevel.Warn:
					return new Color(245, 185, 25, 255);
				case LogLevel.Error:
					return new Color(245, 65, 25, 255);
				default:
					return new Color(0, 255, 255, 255); // obnoxious color will never be shown
			}
		}
		public static string LevelToConsoleString(LogLevel l) {
			switch (l) {
				case LogLevel.Print:
					return "PRINT";
				case LogLevel.Debug:
					return "DEBUG";
				case LogLevel.Info:
					return "INFO ";
				case LogLevel.Success:
					return "GOOD ";
				case LogLevel.Warn:
					return "WARN ";
				case LogLevel.Error:
					return "ERROR";
				default:
					return " WTF ";
			}
		}
		private static System.Drawing.Color RLCToSDC(Raylib_cs.Color c) => System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
		public delegate void LogWrittenTextDelegate(LogLevel level, string text);
		public static event LogWrittenTextDelegate LogWrittenText;

		private static void __writeLog(LogLevel level, bool printColor = true, bool newlineAfter = true, params object?[] items) {
			if (!_initializedColorConsole) {
				Platform.ConsoleInitialize(RLCToSDC(_defaultBackground), RLCToSDC(_defaultForeground));
				_initializedColorConsole = true;
			}

			if (level < LogLevel && level != LogLevel.Print)
				return;

			if (LogTime)
				Platform.ConsoleWrite($"[{DateTime.Now.ToString(TimeFormat)}] ", RLCToSDC(_defaultForeground));

			if (ShowLevel) {
				System.Drawing.Color toShow = RLCToSDC(_defaultForeground);
				if (PrintColor)
					toShow = RLCToSDC(LevelToColor(level));

				Platform.ConsoleWrite($"[{Source}/{LevelToConsoleString(level)}] ", toShow);
			}

			var current = RLCToSDC(_defaultForeground);
			for (int i = 0; i < items.Length; i++) {
				object item = items[i];

				// if printing with colors, and item is color, and this isn't the last item in the array, use it for vtconsole
				if (printColor && item is Color && i != items.Length - 1)
					current = RLCToSDC((Color)item);
				else
					Platform.ConsoleWrite(item == null ? "<null>" : item.ToString() ?? "<null-str>", current);
			}

			List<string> data = [];
			foreach (object? o in items)
				if (o == null)
					data.Add("<null>");
				else
					data.Add(o.ToString() ?? "<null to-string>");

			LogWrittenText?.Invoke(level, string.Join("", data));

			if (newlineAfter)
				Platform.ConsoleWriteLine();
		}

		public static void Log(LogLevel level, bool printColor = true, bool newlineAfter = true, params object?[] items) {
			if (MainThread.GameThreadSet ? (MainThread.GameThread == Thread.CurrentThread) : (MainThread.Thread == Thread.CurrentThread)) __writeLog(level, printColor, newlineAfter, items);
			else MainThread.RunASAP(() => __writeLog(level, printColor, newlineAfter, items));
		}

		public static string Source { get; internal set; } = "nucleus";

		public static void Print(params object?[] items) => Log(LogLevel.Print, PrintColor, Newline, items);
		public static void Debug(params object?[] items) => Log(LogLevel.Debug, PrintColor, Newline, items);
		public static void Info(params object?[] items) => Log(LogLevel.Info, PrintColor, Newline, items);
		public static void Success(params object?[] items) => Log(LogLevel.Success, PrintColor, Newline, items);
		public static void Warn(params object?[] items) => Log(LogLevel.Warn, PrintColor, Newline, items);
		public static void Error(params object?[] items) => Log(LogLevel.Error, PrintColor, Newline, items);
	}
}
