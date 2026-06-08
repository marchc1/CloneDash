using Nucleus.Commands;
using Nucleus.Engine;
using Nucleus.Extensions;
using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;

namespace Nucleus.Core;

public struct ConsoleOverlaySettings
{
	public bool DoNotRender;
	public int TextSize;
	public Vector2F Position;
	public Anchor Anchor;
	public Element? Parent;
}

[MarkForStaticConstruction]
public static class ConsoleSystem
{
	public static LogLevel LogLevel { get; set; } = LogLevel.Debug;
	private static readonly ConsoleMessageList AllMessages = new();
	private static readonly ConsoleMessageList ScreenMessages = new();

	public static int ComputeMessagesCount() => AllMessages.ComputeCount();
	internal static ConsoleMessageList GetAllMessagesList() => AllMessages;
	public static int MaxConsoleMessages { get; set; } = 300;
	public static int MaxScreenMessages { get; set; } = 24;

	public static float DisappearTime { get; set; } = 0.93f;
	public static float MaxMessageTime { get; set; } = 10;
	public static void Initialize() {
		Logs.LogWrittenText += Logs_LogWrittenText;
	}

	private static void Logs_LogWrittenText(LogLevel level, ReadOnlySpan<char> text) {
		var time = DateTime.Now;
		var message = AllMessages.AddToEnd(level, time, text);
		ConsoleMessageWrittenEvent?.Invoke(ref message);

		Span<char> clean = stackalloc char[text.Length];
		int cleanLen = 0;
		for (int i = 0; i < text.Length; i++)
			if (text[i] != '\r')
				clean[cleanLen++] = text[i];

		ScreenMessages.AddToEnd(level, time, clean[..cleanLen]);

		if (AllMessages.ComputeCount() > MaxConsoleMessages)
			AllMessages.RemoveFromStart();
		if (ScreenMessages.ComputeCount() > MaxScreenMessages)
			ScreenMessages.RemoveFromStart();
	}
	public delegate void ConsoleMessageWritten(ref readonly LiveConsoleMessage message);
	public static event ConsoleMessageWritten? ConsoleMessageWrittenEvent;
	public static void Draw(in ConsoleOverlaySettings settings) {
		if (!EngineCore.ShouldShowDeveloperOverlays() || IsScreenBlockerActive)
			return;

		RenderToScreen(in settings);
	}
	public static bool IsScreenBlockerActive => scrblockers.Count > 0;

	public static void RenderToScreen(in ConsoleOverlaySettings settings) {
		int i = 0;

		float x = settings.Position.X;
		float y = settings.Position.Y;
		int textSize = settings.TextSize;
		if (textSize < 6)
			return;

		ScreenMessages.RemoveExpired(MaxMessageTime);

		// Snapshot the count ONCE after pruning
		int messageCount = ScreenMessages.ComputeCount();
		if (messageCount <= 0)
			return;

		ScreenMessages.BeginRead();
		const int MAX_CHARS_PER_LINE = 1024;
		Span<int> offsets = stackalloc int[messageCount];
		ScreenMessages.GetMessages(offsets, out int maxMessageLength);
		Span<float> fades = stackalloc float[messageCount];
		Span<RectangleF> rectangles = stackalloc RectangleF[messageCount];
		Span<int> textLengths = stackalloc int[messageCount];
		Span<char> textMessages = stackalloc char[MAX_CHARS_PER_LINE * messageCount];
		Span<LogLevel> logLevels = stackalloc LogLevel[MAX_CHARS_PER_LINE * messageCount];
		Span<char> temporaryTextBuffer = stackalloc char[3 + maxMessageLength + 40];

		const string START_BRACKET = "[";
		const string END_BRACKET = "] ";
		int lines = 0;
		int idx = 0;
		for (bool hasMessage = ScreenMessages.GetMessageAt(offsets, idx, out ConsoleMessage message, out ReadOnlySpan<char> msgtext); hasMessage; idx++, hasMessage = ScreenMessages.GetMessageAt(offsets, idx, out message, out msgtext)) {
			if (i >= messageCount)
				break;

			Span<char> textMessage = textMessages[(i * MAX_CHARS_PER_LINE)..];
			float fade = Math.Clamp((float)NMath.Remap(message.GetAge(), MaxMessageTime * DisappearTime, MaxMessageTime, 1, 0), 0, 1);
			int len = 0;

			if (textMessage.Length <= 0)
				break;

			if (msgtext.Length > 950)
				continue;

			START_BRACKET.CopyTo(textMessage[len..]); len += START_BRACKET.Length;
			var messageLevel = Logs.LevelToConsoleString(message.Level);
			messageLevel.CopyTo(textMessage[len..]); len += messageLevel.Length;
			END_BRACKET.CopyTo(textMessage[len..]); len += END_BRACKET.Length;
			msgtext.CopyTo(textMessage[len..]); len += msgtext.Length;

			Span<char> text = temporaryTextBuffer;
			int pos = 0;
			text[pos++] = '[';
			var consoleString = Logs.LevelToConsoleString(message.Level);
			consoleString.CopyTo(text[pos..]);
			pos += consoleString.Length;
			text[pos++] = ']';
			text[pos++] = ' ';
			msgtext.CopyTo(text[pos..]);
			pos += msgtext.Length;
			text = text[..pos];

			var thisTextSize = Graphics2D.GetTextSize(text, "Consolas", textSize);
			rectangles[i] = RectangleF.XYWH(x, y + lines * 15, thisTextSize.W, thisTextSize.H);
			fades[i] = fade;
			logLevels[i] = message.Level;
			textLengths[i] = len;

			i += 1;
			lines += 1 + CountNewlines(text);
		}

		// Use 'i' (actual items written) not ScreenMessages.Count
		for (int j = 0; j < i; j++) {
			RectangleF drawRectangle = rectangles[j];
			float fade = fades[j];
			Graphics2D.SetDrawColor(30, 30, 30, (int)(110 * fade));
			Graphics2D.DrawRectangle(drawRectangle.X, drawRectangle.Y + 2, drawRectangle.W + 4, drawRectangle.H + 4);
		}

		for (int j = 0; j < i; j++) {
			Span<char> textMessage = textMessages[(j * MAX_CHARS_PER_LINE)..];
			RectangleF drawRectangle = rectangles[j];
			float fade = fades[j];
			LogLevel level = logLevels[j];
			Graphics2D.SetDrawColor(Logs.LevelToColor(level), (int)(fade * 255));
			Graphics2D.DrawText(new(drawRectangle.X - 1, drawRectangle.Y + 4 + 1), textMessage[..textLengths[j]], "Consolas", textSize);
		}

		ScreenMessages.EndRead();
	}

	static int CountNewlines(ReadOnlySpan<char> x) {
		int ret = 0;
		for (int i = 0; i < x.Length; i++)
			if (x[i] == '\n')
				ret++;

		return ret;
	}
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
