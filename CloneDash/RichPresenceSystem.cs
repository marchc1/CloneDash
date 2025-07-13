using DiscordRPC;
using DiscordRPC.Logging;

using Nucleus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloneDash;

public struct RichPresenceState {
	public string Details;
	public string State;
}

internal class NucleusDiscordLogger : ILogger
{
	public DiscordRPC.Logging.LogLevel Level { get; set; } = DiscordRPC.Logging.LogLevel.Warning;

	public void Error(string message, params object[] args) {
		if(Level <= DiscordRPC.Logging.LogLevel.Error)
			Logs.Error(string.Format(message, args));
	}
	public void Info(string message, params object[] args) {
		if (Level <= DiscordRPC.Logging.LogLevel.Info)
			Logs.Info(string.Format(message, args));
	}
	public void Trace(string message, params object[] args) {
		if (Level <= DiscordRPC.Logging.LogLevel.Trace)
			Logs.Debug(string.Format(message, args));
	}
	public void Warning(string message, params object[] args) {
		if (Level <= DiscordRPC.Logging.LogLevel.Warning)
			Logs.Warn(string.Format(message, args));
	}
}
public static class RichPresenceSystem
{
	static DiscordRpcClient DiscordClient;

	public static void Initialize() {
		DiscordClient = new DiscordRpcClient("1372433185115476018");
		DiscordClient.Logger = new NucleusDiscordLogger();
		DiscordClient.OnReady += (_, e) => Logs.Info($"Received Ready from user {e.User.Username}");
		DiscordClient.OnPresenceUpdate += (_, e) => 
			Logs.Info($"Received Update! {e.Presence}");
		DiscordClient.Initialize();
	}
	public static void Shutdown() {
		DiscordClient.Dispose();
	}

	public static void SetPresence(in RichPresenceState state) {
		DiscordClient.SetPresence(new() {
			Details = state.Details,
			State = state.State,
			Assets = new Assets() {
				LargeImageKey = "clonedashguy512wip", LargeImageText = "Clone Dash"
			}
		});
	}
}