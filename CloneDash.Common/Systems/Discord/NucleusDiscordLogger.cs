using DiscordRPC.Logging;
using Nucleus;
using LogLevel = DiscordRPC.Logging.LogLevel;

namespace CloneDash.Common.Systems.Discord;

internal class NucleusDiscordLogger : ILogger
{
	public LogLevel Level { get; set; } = LogLevel.Warning;

	public void Error(string message, params object[] args) {
		if (Level <= LogLevel.Error)
			Logs.Error(string.Format(message, args));
	}

	public void Info(string message, params object[] args) {
		if (Level <= LogLevel.Info)
			Logs.Info(string.Format(message, args));
	}

	public void Trace(string message, params object[] args) {
		if (Level <= LogLevel.Trace)
			Logs.Debug(string.Format(message, args));
	}

	public void Warning(string message, params object[] args) {
		if (Level <= LogLevel.Warning)
			Logs.Warn(string.Format(message, args));
	}
}