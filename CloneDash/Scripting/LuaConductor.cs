
using Lua;

using Nucleus;

namespace CloneDash.Game;

[LuaObject]
public partial class LuaConductor(Conductor conductor)
{
	[LuaMember("time")]
	public double Time {
		get => conductor.Time;
		set { Logs.Warn(("Lua: Please do not try setting Conductor's time.")); }
	}
}
