using Raylib_cs;

namespace CloneDash
{
    // to-do

    public enum LogLevel {
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4
    }
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
        public static List<ConsoleMessage> Messages = new();
        public static int MaxMessages { get; set; } = 30;
        public static void Print(string message) {
            Messages.Add(new(message));
        }
        public static Color GetColorFromLevel(LogLevel level) {
            switch(level) {
                case LogLevel.Debug: return new Color(255, 255, 255, 255);
                case LogLevel.Info: return new Color(200, 230, 255, 255);
                case LogLevel.Warning: return new Color(240, 200, 70, 255);
                case LogLevel.Error: return new Color(255, 55, 15, 255);
            }

            return Color.WHITE;
        }
        public static float DisappearTime { get; set; } = 0.93f;
        public static float MaxMessageTime { get; set; } = 10;
        public static void Draw() {
            int i = 0;
            Messages.RemoveAll(x => x.Age > MaxMessageTime);

            foreach(ConsoleMessage message in Messages) {
                float fade = Math.Clamp((float)DashMath.Remap(message.Age, MaxMessageTime*DisappearTime, MaxMessageTime, 1, 0), 0, 1);
                Graphics.SetDrawColor(GetColorFromLevel(message.Level), (int)(fade * 255));
                Graphics.DrawText(new(4, 4 + (i * 12)), message.Message, "Consolas", 14);
                i++;
            }
        }
    }
}
