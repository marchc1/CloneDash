using Nucleus;
using Nucleus.Commands;
using Nucleus.Common.Commands;
using Nucleus.Util;

namespace CloneDash.Common.Gamemodes;

[MarkForStaticConstruction]
public static class GamemodeMod
{
	static IGamemodeDescriptor[]? gamemodes;

	[ConCommand(Name: "gamemodes", Help: "Prints all available gamemodes")]
	public static void gamemodesCmd(ConCommand cmd, in TokenizedCommand args) {
		var gamemodes = GetAvailableGamemodes();
		foreach (var gamemode in gamemodes)
			Logs.Print($"    {gamemode}");
	}

	public static IEnumerable<string> GetAvailableGamemodes() {
		gamemodes ??= ReflectionTools.InstantiateAllInheritorsOfInterface<IGamemodeDescriptor>();
		foreach (var gamemode in gamemodes) {
			var str = new UtlSymbol(gamemode.GetUUID()).String();
			if (str != null)
				yield return str;
		}
	}

	public static IGamemodeDescriptor? GetGamemode(string? uuid = null) {
		gamemodes ??= ReflectionTools.InstantiateAllInheritorsOfInterface<IGamemodeDescriptor>();

		if (string.IsNullOrWhiteSpace(uuid))
			return null;

		foreach (var gamemode in gamemodes) {
			if (gamemode.UUIDEquals(uuid))
				return gamemode;
		}

		return null;
	}
}