using Nucleus.Core;
using System.Globalization;

namespace Nucleus
{
    public enum ConsoleFlags : ulong
    {
        None = 0,
        Saved = 1
    }
    public struct CVValue
    {
        public string String;
        public int Length => String.Length;
        public double? AsDouble;
        public int? AsInt;
        public static bool Update(ref CVValue cv, string input) {
            var different = cv.String != input;
            cv.String = input;
            if (double.TryParse(input, out double d)) {
                cv.AsDouble = d;
                if (int.TryParse(input, out int i))
                    cv.AsInt = i;
                else
                    cv.AsInt = Convert.ToInt32(Math.Round(d));
            }
            else {
                cv.AsDouble = null;
                cv.AsInt = null;
            }

            return different;
        }
        public static bool Update(ref CVValue cv, string input, double? min, double? max) {
            var updated = Update(ref cv, input);
            var clampChanged = Clamp(ref cv, min, max);
            return updated || clampChanged;
        }
        public static bool Clamp(ref CVValue cv, double? min, double? max) {
            if (cv.AsDouble == null) return false;

            var oldD = cv.AsDouble;
            var oldI = cv.AsInt;

            if (min.HasValue) cv.AsDouble = Math.Max(cv.AsDouble.Value, min.Value);
            if (max.HasValue) cv.AsDouble = Math.Min(cv.AsDouble.Value, max.Value);

            cv.AsInt = Convert.ToInt32(Math.Round(cv.AsDouble.Value));

            return cv.AsDouble != oldD || cv.AsInt != oldI;
        }
    }

    public abstract class ConCommandBase
    {
        protected static Dictionary<string, ConCommandBase> lookup { get; } = [];

        public delegate void ChangeCallback(ConVar self, CVValue old, CVValue now);

        public string Name { get; private set; } = "";
        public string HelpString { get; private set; } = "";
        public ConsoleFlags Flags { get; private set; }

        protected ConCommandBase() {

        }
        public ConCommandBase(string name, string helpString = "", ConsoleFlags flags = ConsoleFlags.None) {
            if (IsRegistered(name))
                throw new Exception($"ConCommandBase: {name} already exists");

            Name = name;
            HelpString = helpString;
            Flags = flags;

            Register(name, this);
        }

        public static bool IsRegistered(string name) => lookup.ContainsKey(name);
        public static ConCommandBase? Get(string name) {
            if (IsRegistered(name))
                return lookup[name];

            return null;
        }

        public static void Register(string name, ConCommandBase cmd) {
            if (IsRegistered(name))
                return;
            lookup.Add(name, cmd);
        }

        public abstract bool IsCommand { get; }
        public virtual bool IsFlagSet(ConsoleFlags flag) => (flag & Flags) == flag;
        public virtual void AddFlags(ConsoleFlags flags) => Flags = Flags | flags;
        public virtual void RemoveFlags(ConsoleFlags flags) => Flags = Flags & ~flags;
    }
    public class ConCommand : ConCommandBase
    {
        public override bool IsCommand => true;
    }
    public class ConVar : ConCommandBase
    {
        public override bool IsCommand => false;

        public delegate void OnConvarChangeDelegate(ConVar self, CVValue old, CVValue now);
        public event ChangeCallback? OnChange;

        public string DefaultValue { get; set; }

        private double? minimum = null;
        private double? maximum = null;
        private CVValue value = new();

        public double? Minimum {
            get => minimum;
            set {
                minimum = value;
                Clamp();
            }
        }

        public double? Maximum {
            get => maximum;
            set {
                maximum = value;
                Clamp();
            }
        }

        public ConVar(string name, string defaultValue, ConsoleFlags flags, string helpString) : base(name, helpString, flags) {
            DefaultValue = defaultValue;
            Update(DefaultValue);
        }

        public ConVar(string name, string defaultValue, ConsoleFlags flags) : this(name, defaultValue, flags, "") { }
        public ConVar(string name, string defaultValue, ConsoleFlags flags, string helpString, double? min = null, double? max = null) : this(name, defaultValue, flags, helpString) {
            Minimum = min;
            Maximum = max;
        }

        private void Update(string input) {
            var old = value;
            var changed = CVValue.Update(ref value, input, Minimum, Maximum);
            if (changed)
                OnChange?.Invoke(this, old, value);
        }

        private void Clamp() {
            var old = value;
            var changed = CVValue.Clamp(ref value, Minimum, Maximum);
            if (changed)
                OnChange?.Invoke(this, old, value);
        }

        public void Revert() {
            Update(DefaultValue);
        }

        public double GetDouble() => value.AsDouble ?? 0;
        public int GetInt() => value.AsInt ?? 0;
        public string GetString() => value.String ?? "";
        public bool GetBool() => (value.AsDouble ?? 0) >= 1;

        public void SetValue(string str) => Update(str);
        public void SetValue(int i) => Update(Convert.ToString(i, CultureInfo.InvariantCulture));
        public void SetValue(double d) => Update(Convert.ToString(d, CultureInfo.InvariantCulture));
        public void SetValue(bool b) => Update(Convert.ToString(b, CultureInfo.InvariantCulture));

        public static ConVar Register(
            string name,
            string defaultValue,
            ConsoleFlags flags,
            string helpString,
            double? min = null,
            double? max = null
        ) {
            if (IsRegistered(name)) {
                var t = lookup[name];
                if (t is ConVar cv)
                    return cv;

                throw new Exception($"ConCommandBase '{name}' already existed and was not a ConVar");
            }

            ConVar? cmd = (ConVar?)Activator.CreateInstance(typeof(ConVar), [name, defaultValue, flags, helpString, min, max]);
            if (cmd == null) throw new Exception("ConVar: null?");

            lookup[name] = cmd;

            return cmd;
        }
    }

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
