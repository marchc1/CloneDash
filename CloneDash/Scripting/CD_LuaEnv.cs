using CloneDash.Game;
using Lua;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.ManagedMemory;
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
		State.Environment["textures"] = new CD_LuaTextures(level, level.Textures);
		State.Environment["graphics"] = new CD_Graphics(level);
	}

	public LuaValue[] DoFile(string pathID, string path) {
		var t = State.DoStringAsync(Filesystem.ReadAllText(pathID, path) ?? throw new FileNotFoundException(), IManagedMemory.MergePath(pathID, path)).AsTask();
		t.Wait();
		return t.Result;
	}
}


public interface ILuaWrappedObject<Around> {
	public Around Unwrap();
}


[LuaObject]
public partial class CD_Graphics(Level level)
{
	private Color drawColor = Color.White;

	[LuaMember("setDrawColor")]
	public void SetDrawColor(double r, double g, double b, double a) {
		int rI = (int)(float)Math.Clamp(r, 0, 255);
		int gI = (int)(float)Math.Clamp(g, 0, 255);
		int bI = (int)(float)Math.Clamp(b, 0, 255);
		int aI = (int)(float)Math.Clamp(a, 0, 255);

		drawColor = new(rI, gI, bI, aI);
	}

	[LuaMember("drawTexture")]
	public void DrawTexture(float x, float y, float width, float height, CD_LuaTexture texture)
		=> Raylib.DrawTexturePro(texture.Unwrap(), new(0, 0, texture.Width, texture.Height), new(x, y, width, height), new(0, 0), 0, drawColor);
}



[LuaObject]
public partial class CD_LuaTexture(Level level, TextureManagement textures, Texture texture) : ILuaWrappedObject<Texture>
{
	public Texture Unwrap() => texture;

	[LuaMember("hardwareID")] public float HardwareID => texture.HardwareID;
	[LuaMember("width")] public float Width => texture.Width;
	[LuaMember("height")] public float Height => texture.Height;
}



[LuaObject]
public partial class CD_LuaTextures(Level level, TextureManagement textures)
{
	[LuaMember("loadTextureFromFile")]
	public CD_LuaTexture LoadTextureFromFile(string pathID, string path) {
		return new(level, textures, textures.LoadTextureFromFile(pathID, path));
	}
}