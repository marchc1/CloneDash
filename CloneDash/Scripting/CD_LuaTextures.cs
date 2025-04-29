using Lua;
using Nucleus.Engine;
using Nucleus.ManagedMemory;

namespace CloneDash.Scripting;

[LuaObject]
public partial class CD_LuaTextures(Level level, TextureManagement textures)
{
	[LuaMember("loadTextureFromFile")]
	public CD_LuaTexture LoadTextureFromFile(string pathID, string path) {
		return new(level, textures, textures.LoadTextureFromFile(pathID, path));
	}
}
