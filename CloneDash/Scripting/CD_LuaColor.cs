using Lua;

namespace CloneDash.Scripting;

[LuaObject]
public partial class CD_LuaColor : ILuaWrappedObject<Raylib_cs.Color>
{
	Raylib_cs.Color color;

	[LuaMember("r")]
	public float R {
		get => color.R;
		set => color = color with { R = (byte)(int)Math.Clamp(value, 0, 255) };
	}

	[LuaMember("g")]
	public float G {
		get => color.G;
		set => color = color with { G = (byte)(int)Math.Clamp(value, 0, 255) };
	}

	[LuaMember("b")]
	public float B {
		get => color.B;
		set => color = color with { B = (byte)(int)Math.Clamp(value, 0, 255) };
	}
	[LuaMember("a")]
	public float A {
		get => color.A;
		set => color = color with { A = (byte)(int)Math.Clamp(value, 0, 255) };
	}

	public Raylib_cs.Color Unwrap() => color;

	[LuaMetamethod(LuaObjectMetamethod.Call)]
	public static CD_LuaColor __call(int r, int g, int b, int a = 255) {
		return new() { color = new(Math.Clamp(r, 0, 255), Math.Clamp(g, 0, 255), Math.Clamp(b, 0, 255), Math.Clamp(a, 0, 255)) };
	}
}