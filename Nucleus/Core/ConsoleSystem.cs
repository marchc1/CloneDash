using Nucleus.Commands;
using Nucleus.Engine;
using Nucleus.Extensions;
using Nucleus.Types;

using Raylib_cs;

namespace Nucleus.Core
{
	[MarkForStaticConstruction]
	public static class ConsoleSystem
	{
		public static LogLevel LogLevel { get; set; } = LogLevel.Debug;
		private static List<ConsoleMessage> AllMessages = new();
		private static List<ConsoleMessage> ScreenMessages = new();

		public static Span<ConsoleMessage> GetMessages() => AllMessages.AsSpan();
		public static int GetMessagesCount() => AllMessages.Count;
		public static int MaxConsoleMessages { get; set; } = 300;
		public static int MaxScreenMessages { get; set; } = 24;

		public static float DisappearTime { get; set; } = 0.93f;
		public static float MaxMessageTime { get; set; } = 10;
		public static void Initialize() {
			Logs.LogWrittenText += Logs_LogWrittenText;
		}
		public static void ParseSeveralCommands(string input) {
			// not implemented...
		}
		public static void ParseOneCommand(string input) {
			var whereIsSpace = input.IndexOf(' ');

			string ccname;
			string usargs;

			if (whereIsSpace == -1) {
				ccname = input;
				usargs = "";
			}
			else {
				ccname = input.Substring(0, whereIsSpace).Trim();
				usargs = input.Substring(whereIsSpace + 1).Trim();
			}

			ConCommandBase? baseC = ConCommandBase.Get(ccname);
			if (baseC == null) {
				Logs.Info($" '{ccname}' not found");
				return;
			}

			switch (baseC) {
				case ConVar cv:
					// Lets see if usargs is not set, which means a description is given
					if (usargs.Length == 0) {
						Logs.Info($"  {ccname} (default '{cv.DefaultValue}'{(cv.DefaultValue != cv.GetString() ? $", 'current {cv.GetString()})'" : ")")}");
						foreach (var line in cv.HelpString.Split("\n"))
							Logs.Info($"    {line}");
					}
					else {
						cv.SetValue(usargs);
					}
					break;
				case ConCommand cc:
					// Always run regardless of no args or not since that's how concommands work
					ConCommand.Execute(cc, usargs);
					break;
			}
		}
		private static void Logs_LogWrittenText(LogLevel level, string text) {
			ConsoleMessage message = new ConsoleMessage(text, level);
			ConsoleMessageWrittenEvent?.Invoke(ref message);

			AllMessages.Add(message);
			message.Message = message.Message.Replace("\r", "");
			ScreenMessages.Add(message);
			if (AllMessages.Count > MaxConsoleMessages)
				AllMessages.RemoveAt(0);
			if (ScreenMessages.Count > MaxScreenMessages)
				ScreenMessages.RemoveAt(0);
		}
		public delegate void ConsoleMessageWritten(ref readonly ConsoleMessage message);
		public static event ConsoleMessageWritten? ConsoleMessageWrittenEvent;
		public static void Draw() {
			if (!EngineCore.ShowConsoleLogsInCorner || IsScreenBlockerActive)
				return;

			RenderToScreen(6, 6);
		}
		public static bool IsScreenBlockerActive => scrblockers.Count > 0;
		public static int VisibleLines => ScreenMessages.Count;
		public static int TextSize { get; set; } = 13;
		public static void RenderToScreen(int x, int y) {
			int i = 0;
			ScreenMessages.RemoveAll(x => x.Age > MaxMessageTime);

			const int MAX_CHARS_PER_LINE = 1024;
			var currentMessages = ScreenMessages.AsSpan();
			Span<float> fades = stackalloc float[ScreenMessages.Count];
			Span<RectangleF> rectangles = stackalloc RectangleF[ScreenMessages.Count];
			Span<int> textLengths = stackalloc int[ScreenMessages.Count];
			Span<char> textMessages = stackalloc char[MAX_CHARS_PER_LINE * ScreenMessages.Count];
			Span<LogLevel> logLevels = stackalloc LogLevel[MAX_CHARS_PER_LINE * ScreenMessages.Count];
			const string START_BRACKET = "[";
			const string END_BRACKET = "] ";
			foreach (ref readonly ConsoleMessage message in currentMessages) {
				Span<char> textMessage = textMessages[(i * MAX_CHARS_PER_LINE)..];
				float fade = Math.Clamp((float)NMath.Remap(message.Age, MaxMessageTime * DisappearTime, MaxMessageTime, 1, 0), 0, 1);
				int len = 0;

				if (message.Message.Length > 950)
					// Excessive message; skipping
					continue;

				START_BRACKET.CopyTo(textMessage[len..]); len += START_BRACKET.Length;
				var messageLevel = Logs.LevelToConsoleString(message.Level);
				messageLevel.CopyTo(textMessage[len..]); len += messageLevel.Length;
				END_BRACKET.CopyTo(textMessage[len..]); len += END_BRACKET.Length;
				message.Message.CopyTo(textMessage[len..]); len += message.Message.Length;

				var text = $"[{Logs.LevelToConsoleString(message.Level)}] {message.Message}";
				var textSize = Graphics2D.GetTextSize(text, "Consolas", TextSize);
				rectangles[i] = RectangleF.XYWH(x, y + i * 15, textSize.W, textSize.H);
				fades[i] = fade;
				logLevels[i] = message.Level;
				textLengths[i] = len;

				i += 1 + text.Count(CountNewlines);
			}

			for (int j = 0; j < ScreenMessages.Count; j++) {
				RectangleF drawRectangle = rectangles[j];
				float fade = fades[j];
				Graphics2D.SetDrawColor(30, 30, 30, (int)(110 * fade));
				Graphics2D.DrawRectangle(drawRectangle.X, drawRectangle.Y + 2, drawRectangle.W + 4, drawRectangle.H + 4);
			}

			for (int j = 0; j < ScreenMessages.Count; j++) {
				Span<char> textMessage = textMessages[(j * MAX_CHARS_PER_LINE)..];
				RectangleF drawRectangle = rectangles[j];
				float fade = fades[j];
				LogLevel level = logLevels[j];
				Graphics2D.SetDrawColor(Logs.LevelToColor(level), (int)(fade * 255));
				Graphics2D.DrawText(new(drawRectangle.X - 1, drawRectangle.Y + 4 + 1), textMessage[..textLengths[j]], "Consolas", TextSize);
			}
		}
		static bool CountNewlines(char x) => x == '\n';
		private static List<object> scrblockers = [];

		public static void AddScreenBlocker(object blocker) {
			scrblockers.Add(blocker);
		}

		public static void RemoveScreenBlocker(object blocker) {
			scrblockers.Remove(blocker);
		}

		public static void ClearScreenBlockers() {
			scrblockers.Clear();
		}
	}
}
