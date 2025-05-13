using CloneDash.Game;
using CloneDash.Modding;
using CloneDash.Scripting;

using Lua;

using Newtonsoft.Json;

using Nucleus.Files;

namespace CloneDash.Fevers;

public class CD_FeverDescriptor : CloneDashDescriptor, IFeverDescriptor
{
	public CD_FeverDescriptor() : base(CloneDashDescriptorType.Fever, "fevers", "fever", "fever", "2025-05-06-01") { }

	public static CD_FeverDescriptor? ParseFever(string filename) => Filesystem.ReadAllText("fevers", filename, out var text) ? ParseFile<CD_FeverDescriptor>(text, filename) : null;

	LuaFunction? startFever;
	LuaFunction? thinkFever;
	LuaFunction? renderFever;

	private void SetupLua(CD_GameLevel game, CD_LuaEnv lua, bool first = true) {
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

	public void Initialize(CD_GameLevel game) {
		SetupLua(game, game.Lua);
	}

	public void Start(CD_GameLevel game) {
		game.Lua.ProtectedCall(startFever);
	}

	public void Think(CD_GameLevel game) {
		game.Lua.ProtectedCall(thinkFever);
	}

	public void Render(CD_GameLevel game) {
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
