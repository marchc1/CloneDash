using Lua;

using Nucleus.Engine;

namespace CloneDash.Game;

[LuaObject]
public partial class CD_LuaLevel(Level game)
{
	[LuaMember("curtime")] public double Curtime { get => game.Curtime; set { } }
	[LuaMember("curtimeDelta")] public double CurtimeDelta { get => game.CurtimeDelta; set { } }
	[LuaMember("realtime")] public double Realtime { get => game.Realtime; set { } }
	[LuaMember("realtimeDelta")] public double RealtimeDelta { get => game.RealtimeDelta; set { } }
	[LuaMember("curtimeLast")] public double LastCurtime { get => game.LastCurtime; set { } }
}
