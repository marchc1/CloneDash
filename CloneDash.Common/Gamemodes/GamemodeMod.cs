using CloneDash.Common.Compatibility;
using Nucleus;
using Nucleus.Commands;
using Nucleus.Common.Commands;
using Nucleus.Util;
using System.Runtime.CompilerServices;

namespace CloneDash.Common.Gamemodes;

[MarkForStaticConstruction]
public class GamemodeMod() : ModStore<IGamemodeProvider, IGamemodeDescriptor>("Gamemodes", "gamemode")
{
	[ConCommand(Name: "gamemodes", Help: "Prints all available gamemodes")]
	public static void gamemodesCmd(ConCommand cmd, in TokenizedCommand args) {
		var gamemodes = ModStores.Get<GamemodeMod>().GetAllDescriptors();
		foreach (var gamemode in gamemodes)
			Logs.Print($"    {gamemode}");
	}
}