using Lua;

using Nucleus.Engine;
using Nucleus.ManagedMemory;

using Raylib_cs;

namespace CloneDash.Scripting;

[LuaObject]
public partial class CD_LuaTexture(Level level, TextureManagement textures, Texture texture) : ILuaWrappedObject<Texture>
{
	public Texture Unwrap() => texture;

	[LuaMember("hardwareID")] public int HardwareID => (int)texture.HardwareID;
	[LuaMember("width")]
	public float Width {
		get => texture.Width;
		set { }
	}
	[LuaMember("height")]
	public float Height {
		get => texture.Height;
		set { }
	}

	[LuaMember("wrap")]
	public int Wrap {
		get => (int)texture.GetWrap();
		set => texture.SetWrap((TextureWrap)value);
	}
}
