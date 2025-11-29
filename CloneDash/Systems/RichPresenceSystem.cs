using DiscordRPC;
using DiscordRPC.Logging;

using Nucleus;
using Nucleus.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloneDash.Systems;

public struct RichPresenceState
{
	public string Details;
	public string State;
}

internal class NucleusDiscordLogger : ILogger
{
	public DiscordRPC.Logging.LogLevel Level { get; set; } = DiscordRPC.Logging.LogLevel.Warning;

	public void Error(string message, params object[] args) {
		if (Level <= DiscordRPC.Logging.LogLevel.Error)
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
[MarkForStaticConstruction]
public static class RichPresenceSystem
{
	static DiscordRpcClient? DiscordClient;
	static bool initialized;
	public static ConVar richpresence = ConVar.Register(nameof(richpresence), "1", ConsoleFlags.Saved, "Enables/disables rich presence systems", 0, 1, (_, _, cv) => {
		if ((cv.AsInt ?? 0) >= 1) {
			if (!initialized) 
				Initialize();
		}
		else {
			Shutdown();
		}

	}, callback_first: false);

	public static void Initialize() {
		if (!richpresence.GetBool()) return;
		DiscordClient = new DiscordRpcClient("1372433185115476018");
		DiscordClient.Logger = new NucleusDiscordLogger();
		DiscordClient.OnReady += (_, e) => {
			Logs.Info($"Received Ready from user {e.User.Username}");
			if (hasPrevPresence)
				SetPresence(in lastPresence);
		};
		DiscordClient.OnPresenceUpdate += (_, e) =>
			Logs.Info($"Received Update! {e.Presence}");
		DiscordClient.Initialize();
		initialized = true;
	}
	public static void Shutdown() {
		DiscordClient?.Dispose();
		DiscordClient = null;
		initialized = false;
	}
	static bool hasPrevPresence;
	static RichPresenceState lastPresence;
	public static void SetPresence(in RichPresenceState state) {
		lastPresence = state;
		hasPrevPresence = true;
		DiscordClient?.SetPresence(new() {
			Details = state.Details,
			State = state.State,
			Assets = new Assets() {
				LargeImageKey = "clonedashguy512wip",
				LargeImageText = "Clone Dash"
			}
		});
	}
}