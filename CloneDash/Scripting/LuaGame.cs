using Lua;

namespace CloneDash.Game;

[LuaObject]
public partial class LuaGame(DashGameLevel game)
{
	[LuaMember("inFever")] public bool InFever { get => game.InFever; set { } }
	[LuaMember("feverTimeMax")] public double FeverTimeMax { get => game.FeverTime; set { } }
	[LuaMember("feverTimeLeft")] public double FeverTimeLeft { get => game.FeverTimeLeft; set { } }
}
