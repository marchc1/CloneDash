using CloneDash.Game;
using FMOD;
using Lua;
using Nucleus;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.ManagedMemory;
using Nucleus.UI;
using Raylib_cs;
using System.Numerics;

namespace CloneDash.Scripting;


public class CD_LuaEnv
{
	public LuaState State;
	private Level level;
	public CD_LuaEnv(Level level) {
		this.level = level;

		State = LuaState.Create();
		// Types
		State.Environment["Color"] = new LuaColor();

		// Libraries
		State.Environment["textures"] = new CD_LuaTextures(level, level.Textures);
		State.Environment["graphics"] = new CD_Graphics(level);
	}

	public LuaValue[] DoFile(string pathID, string path) {
		var t = State.DoStringAsync(Filesystem.ReadAllText(pathID, path) ?? throw new FileNotFoundException(), IManagedMemory.MergePath(pathID, path)).AsTask();
		t.Wait();
		return t.Result;
	}

	public LuaValue[] Call(LuaFunction func, params LuaValue[] args) {
		var t = func.InvokeAsync(State, args).AsTask();
		t.Wait();
		return t.Result;
	}

	public LuaValue[] ProtectedCall(LuaFunction func, params LuaValue[] args) {
		Task<LuaValue[]> t;
		try {
			t = func.InvokeAsync(State, args).AsTask();
			t.Wait();

			return t.Result;
		}
		catch(Exception ex) {
			Logs.Error($"{ex.Message}");
			return [];
		}
	}
}



public interface ILuaWrappedObject<Around> {
	public Around Unwrap();
}


[LuaObject]
public partial class CD_Graphics(Level level)
{
	private Raylib_cs.Color drawColor = Raylib_cs.Color.White;

	[LuaMember("setDrawColor")]
	public void SetDrawColor(double r, double g, double b, double a) {
		int rI = (int)(float)Math.Clamp(r, 0, 255);
		int gI = (int)(float)Math.Clamp(g, 0, 255);
		int bI = (int)(float)Math.Clamp(b, 0, 255);
		int aI = (int)(float)Math.Clamp(a, 0, 255);

		drawColor = new(rI, gI, bI, aI);
	}

	[LuaMember("drawRectangle")]
	public void DrawRectangle(float x, float y, float width, float height)
		=> Raylib.DrawRectanglePro(new(x, y, width, height), new(0, 0), 0, drawColor);

	[LuaMember("drawTexture")]
	public void DrawTexture(float x, float y, float width, float height, CD_LuaTexture texture)
		=> Raylib.DrawTexturePro(texture.Unwrap(), new(0, 0, texture.Width, texture.Height), new(x, y, width, height), new(0, 0), 0, drawColor);

	[LuaMember("drawGradientH")]
	public void DrawGradientH(float x, float y, float width, float height, LuaColor color1, LuaColor color2)
		=> Raylib.DrawRectangleGradientH((int)x, (int)y, (int)width, (int)height, color1.Unwrap(), color2.Unwrap());

	[LuaMember("drawGradientV")]
	public void DrawGradientV(float x, float y, float width, float height, LuaColor color1, LuaColor color2)
		=> Raylib.DrawRectangleGradientV((int)x, (int)y, (int)width, (int)height, color1.Unwrap(), color2.Unwrap());
}



[LuaObject]
public partial class CD_LuaTexture(Level level, TextureManagement textures, Texture texture) : ILuaWrappedObject<Texture>
{
	public Texture Unwrap() => texture;

	[LuaMember("hardwareID")] public int HardwareID => (int)texture.HardwareID;
	[LuaMember("width")] public float Width {
		get => texture.Width;
		set { }
	}
	[LuaMember("height")] public float Height {
		get => texture.Height;
		set { }
	}
}



[LuaObject]
public partial class CD_LuaTextures(Level level, TextureManagement textures)
{
	[LuaMember("loadTextureFromFile")]
	public CD_LuaTexture LoadTextureFromFile(string pathID, string path) {
		return new(level, textures, textures.LoadTextureFromFile(pathID, path));
	}
}

[LuaObject]
public partial class LuaColor : ILuaWrappedObject<Raylib_cs.Color>
{
	Raylib_cs.Color color;

	[LuaMember("r")]
	public float R {
		get => color.R;
		set => color = color with { R = (byte)(int)Math.Clamp(value, 0, 255) };
	}

	[LuaMember("g")]
	public float G{
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
	public static LuaColor __call(int r, int g, int b, int a = 255) {
		return new() { color = new(Math.Clamp(r, 0, 255), Math.Clamp(g, 0, 255), Math.Clamp(b, 0, 255), Math.Clamp(a, 0, 255)) };
	}
}