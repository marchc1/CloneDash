using DiscordRPC;
using Nucleus;
using Nucleus.Commands;

namespace CloneDash.Common.Systems.Discord;

[MarkForStaticConstruction]
public static class RichPresenceSystem
{
	public const string LargeImageKey = "clonedashguy512wip";
	public const string LargeImageText = "Clone Dash";
	
	private static DiscordRpcClient? _discordClient;
	private static bool _initialized;
	private static readonly Lock DoubleInitializationLock = new();
	
	public static readonly ConVar RichPresence = new(nameof(RichPresence), "1", FCvar.Saved, "Enables/disables rich presence systems", 0, 1, (cv, _, _) => {
		if (cv.GetBool()) {
			if (!_initialized)
				Initialize();
		}
		else {
			Shutdown();
		}
	});

	public static void Initialize() {
		lock (DoubleInitializationLock) {
			if (!RichPresence.GetBool()) return;
			if (_initialized) return;

			_discordClient = new DiscordRpcClient("1372433185115476018");
			_discordClient.Logger = new NucleusDiscordLogger();
			_discordClient.OnReady += (_, e) => {
				Logs.Info($"Received Ready from user {e.User.Username}");
				if (_hasPrevPresence)
					SetPresence(in _lastPresence);
			};
			
			_discordClient.OnPresenceUpdate += (_, e) =>
				Logs.Info($"Received Update! {e.Presence}");
			
			_discordClient.Initialize();
			_initialized = true;
		}
	}

	public static void Shutdown() {
		_discordClient?.Dispose();
		_discordClient = null;
		_initialized = false;
	}

	private static bool _hasPrevPresence;
	private static RichPresenceState _lastPresence;

	public static void SetPresence(in RichPresenceState state) {
		_lastPresence = state;
		_hasPrevPresence = true;
		_discordClient?.SetPresence(new RichPresence {
			Details = state.Details,
			State = state.State,
			Assets = new Assets {
				LargeImageKey = LargeImageKey,
				LargeImageText = LargeImageText
			}
		});
	}
}