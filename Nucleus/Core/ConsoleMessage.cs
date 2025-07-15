namespace Nucleus.Core
{
	public struct ConsoleMessage
	{
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
}
