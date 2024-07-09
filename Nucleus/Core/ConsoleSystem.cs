using Nucleus.Core;

namespace Nucleus
{
    // to-do
    public struct ConsoleMessage {
        public DateTime Time;
        public LogLevel Level;
        public string Message;

        public double Age => (DateTime.Now - Time).TotalSeconds;
        public ConsoleMessage(string message, LogLevel level = LogLevel.Info) {
            Time = DateTime.Now;
            Level = level;
            Message = message;
        }
    }
    public static class ConsoleSystem
    {
        public static LogLevel LogLevel { get; set; } = LogLevel.Debug;
        private static List<ConsoleMessage> AllMessages = new();
        private static List<ConsoleMessage> ScreenMessages = new();

        public static ConsoleMessage[] GetMessages() => AllMessages.ToArray();
        public static int MaxConsoleMessages { get; set; } = 300;
        public static int MaxScreenMessages { get; set; } = 24;

        public static float DisappearTime { get; set; } = 0.93f;
        public static float MaxMessageTime { get; set; } = 10;
        public static void Initialize() {
            Logs.LogWrittenText += Logs_LogWrittenText;
        }

        private static void Logs_LogWrittenText(LogLevel level, string text) {
            AllMessages.Add(new(text, level));
            ScreenMessages.Add(new(text, level));
            if (AllMessages.Count > MaxConsoleMessages)
                AllMessages.RemoveAt(0);
            if (ScreenMessages.Count > MaxScreenMessages)
                ScreenMessages.RemoveAt(0);
            ConsoleMessageWrittenEvent?.Invoke(level, text);
        }
        public delegate void ConsoleMessageWritten(LogLevel level, string text);
        public static event ConsoleMessageWritten ConsoleMessageWrittenEvent;
        public static void Draw() {
            int i = 0;
            ScreenMessages.RemoveAll(x => x.Age > MaxMessageTime);

            while(i < ScreenMessages.Count){
                ConsoleMessage message = ScreenMessages[i];
                
                float fade = Math.Clamp((float)NMath.Remap(message.Age, MaxMessageTime*DisappearTime, MaxMessageTime, 1, 0), 0, 1);

                var text = $"[{Logs.LevelToConsoleString(message.Level)}] {message.Message}";
                var textSize = Graphics2D.GetTextSize(text, "Consolas", 15);
                Graphics2D.SetDrawColor(30, 30, 30, (int)(110 * fade));
                Graphics2D.DrawRectangle(2, 2 + (i * 14), textSize.W + 4, textSize.H + 4);
                Graphics2D.SetDrawColor(Logs.LevelToColor(message.Level), (int)(fade * 255));
                Graphics2D.DrawText(new(4, 4 + (i * 14)), text, "Consolas", 14);
                i++;
            }
        }
    }
}
