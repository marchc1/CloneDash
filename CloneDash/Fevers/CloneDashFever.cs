using CloneDash.Game;
using CloneDash.Modding;
using CloneDash.Scripting;

using Lua;

using Newtonsoft.Json;

using Nucleus.Files;

namespace CloneDash.Fevers;

public class CloneDashFever : CloneDashDescriptor, IFeverDescriptor
{
	public CloneDashFever() : base(CloneDashDescriptorType.Fever, "fevers", "fever", "fever", "2025-05-06-01") { }

	public static CloneDashFever? ParseFever(string filename) => Filesystem.ReadAllText("fevers", filename, out var text) ? ParseFile<CloneDashFever>(text, filename) : null;

	LuaFunction? startFever;
	LuaFunction? thinkFever;
	LuaFunction? renderFever;

	private void SetupLua(DashGameLevel game, LuaEnv lua, bool first = true) {
		if (first) {
			lua.State.Environment["fever"] = new LuaTable();

			lua.DoFile("fever", PathToBackgroundController);
		}

		var scene = lua.State.Environment["fever"].Read<LuaTable>();
		{
			scene["start"].TryRead(out startFever);
			scene["render"].TryRead(out renderFever);
			scene["think"].TryRead(out thinkFever);
		}
	}

	public void Initialize(DashGameLevel game) {
		SetupLua(game, game.Lua);
	}

	public void Start(DashGameLevel game) {
		game.Lua.ProtectedCall(startFever);
	}

	public void Think(DashGameLevel game) {
		game.Lua.ProtectedCall(thinkFever);
	}

	public void Render(DashGameLevel game) {
		game.Lua.Graphics.StartRenderingLuaContext();
		game.Lua.ProtectedCall(renderFever);
		game.Lua.Graphics.EndRenderingLuaContext();
	}

#nullable disable
	[JsonRequired][JsonProperty("name")] public string Name;
	[JsonRequired][JsonProperty("author")] public string Author;
	[JsonRequired][JsonProperty("background_controller")] public string PathToBackgroundController;
#nullable enable
}
