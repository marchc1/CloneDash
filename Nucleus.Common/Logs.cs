using Nucleus.Common.Types;
using System.Diagnostics;

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
		public static bool LogTime { get; set; } = true;
		public static bool PrintColor { get; set; } = true;
		public static bool Newline { get; set; } = true;
		public static bool ShowLevel { get; set; } = true;

		// 2025-09-30 23:59:59
		public static string TimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
		public static LogLevel LogLevel { get; set; } = LogLevel.Debug;

		private static bool _initializedColorConsole = false;
		private static readonly Lock _writeLock = new();

		private static readonly Color _defaultBackground = new(15, 20, 25, 255);
		private static readonly Color _defaultForeground = new(245, 245, 245, 255);

		public class SourceInfoScope : IDisposable {
			readonly SourceInfo source;
			internal SourceInfoScope(SourceInfo source){
				this.source = source;
			}

			public SourceInfoScope PushSource(ReadOnlySpan<char> scope) {
				source.PushSource(scope);
				return this;
			}

			public void Dispose() => source.PopSource();
		}
		internal class SourceInfo
		{
			public readonly Stack<Range> Sources = new();
			public readonly char[] BackingMemory = new char[2048];
			public readonly SourceInfoScope Scope;

			public SourceInfo(){
				Scope = new(this);
			}

			public ReadOnlySpan<char> GetSource() {
				if (Sources.Count == 0)
					return "nucleus";
				return BackingMemory.AsSpan()[Sources.Peek()];
			}

			public bool PushSource(ReadOnlySpan<char> text) {
				int offset = Sources.Count == 0 ? 0 : Sources.Peek().End.Value;
				if (offset + text.Length > BackingMemory.Length)
					return false;
				text.CopyTo(BackingMemory.AsSpan(offset));
				Sources.Push(new Range(offset, offset + text.Length));
				return true;
			}

			public bool PopSource() {
				if (Sources.Count == 0)
					return false;
				Sources.Pop();
				return true;
			}
		}

		private static readonly ThreadLocal<SourceInfo> _source = new(() => new());

		// Per-thread scratch buffer for formatting timestamps/prefixes without allocating
		[ThreadStatic] private static char[]? _scratchBuffer;

		private static Span<char> GetScratch() {
			_scratchBuffer ??= new char[512];
			return _scratchBuffer;
		}

		public static ReadOnlySpan<char> Source => _source.Value!.GetSource();
		public static bool PushSource(ReadOnlySpan<char> source) => _source.Value!.PushSource(source);
		public static bool PopSource() => _source.Value!.PopSource();
		public static SourceInfoScope SourceScope(ReadOnlySpan<char> source) => _source.Value!.Scope.PushSource(source);


		public static LogLevel ConsoleStringToLevel(string logString) =>
			logString switch {
				"PRINT" => LogLevel.Print,
				"DEBUG" => LogLevel.Debug,
				"INFO " => LogLevel.Info,
				"GOOD " => LogLevel.Success,
				"WARN " => LogLevel.Warn,
				"ERROR" => LogLevel.Error,
				_ => LogLevel.Info
			};

		public static Color LevelToColor(LogLevel l) =>
			l switch {
				LogLevel.Print => new(210, 235, 255, 255),
				LogLevel.Debug => new(210, 245, 255, 255),
				LogLevel.Info => new(210, 245, 255, 255),
				LogLevel.Success => new(100, 255, 145, 255),
				LogLevel.Warn => new(245, 185, 25, 255),
				LogLevel.Error => new(245, 65, 25, 255),
				_ => new(0, 255, 255, 255)
			};

		public static ReadOnlySpan<char> LevelToConsoleString(LogLevel l) =>
			l switch {
				LogLevel.Print => "PRINT",
				LogLevel.Debug => "DEBUG",
				LogLevel.Info => "INFO ",
				LogLevel.Success => "GOOD ",
				LogLevel.Warn => "WARN ",
				LogLevel.Error => "ERROR",
				_ => " WTF "
			};

		private static System.Drawing.Color RLCToSDC(Color c) =>
			System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);

		public delegate void LogWrittenTextDelegate(LogLevel level, ReadOnlySpan<char> text);
		public static event LogWrittenTextDelegate? LogWrittenText;

		private static void WriteLog(LogLevel level, ReadOnlySpan<char> text,
			bool printColor = true, bool newlineAfter = true) {
			if (level < LogLevel && level != LogLevel.Print)
				return;

			// Read source on the calling thread (ThreadLocal)
			var source = Source;
			var levelStr = LevelToConsoleString(level);
			var fgColor = RLCToSDC(_defaultForeground);
			var levelColor = printColor ? RLCToSDC(LevelToColor(level)) : fgColor;

			// Build the prefix into the scratch buffer on this thread, no allocations
			var scratch = GetScratch();
			int prefixLen = 0;

			if (LogTime) {
				scratch[prefixLen++] = '[';
				DateTime.Now.TryFormat(scratch[prefixLen..], out int written, TimeFormat);
				prefixLen += written;
				scratch[prefixLen++] = ']';
				scratch[prefixLen++] = ' ';
			}

			int levelPrefixStart = prefixLen;
			if (ShowLevel) {
				scratch[prefixLen++] = '[';
				source.CopyTo(scratch[prefixLen..]);
				prefixLen += source.Length;
				scratch[prefixLen++] = '/';
				levelStr.CopyTo(scratch[prefixLen..]);
				prefixLen += levelStr.Length;
				scratch[prefixLen++] = ']';
				scratch[prefixLen++] = ' ';
			}

			int levelPrefixEnd = prefixLen;

			// Single lock — any thread can write, lines don't interleave
			lock (_writeLock) {
				if (!_initializedColorConsole) {
					Platform.ConsoleInitialize(RLCToSDC(_defaultBackground), fgColor);
					_initializedColorConsole = true;
				}

				// Timestamp portion
				if (LogTime)
					Platform.ConsoleWrite(scratch[..levelPrefixStart], fgColor);

				// [source/LEVEL] portion
				if (ShowLevel)
					Platform.ConsoleWrite(scratch[levelPrefixStart..levelPrefixEnd], levelColor);

				// The actual message
				if (text.Length > 0)
					Platform.ConsoleWrite(text, fgColor);

				if (newlineAfter)
					Platform.ConsoleWriteLine();
			}

			LogWrittenText?.Invoke(level, text);
		}

		public static void Print(ReadOnlySpan<char> text) => WriteLog(LogLevel.Print, text, PrintColor, Newline);
		[Conditional("DEBUG")]
		public static void Debug(ReadOnlySpan<char> text) => WriteLog(LogLevel.Debug, text, PrintColor, Newline);
		public static void Info(ReadOnlySpan<char> text) => WriteLog(LogLevel.Info, text, PrintColor, Newline);
		public static void Success(ReadOnlySpan<char> text) => WriteLog(LogLevel.Success, text, PrintColor, Newline);
		public static void Warn(ReadOnlySpan<char> text) => WriteLog(LogLevel.Warn, text, PrintColor, Newline);
		public static void WarnIf(bool cond, ReadOnlySpan<char> text) {
			if (cond) WriteLog(LogLevel.Warn, text, PrintColor, Newline);
		}
		public static void Error(ReadOnlySpan<char> text) => WriteLog(LogLevel.Error, text, PrintColor, Newline);

		public static void Assert(bool condition, ReadOnlySpan<char> message) {
			if (!condition) {
				System.Diagnostics.Debug.Assert(condition, new string(message));
				Warn(message);
			}
		}
	}
}