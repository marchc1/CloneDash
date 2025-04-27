using Lua;
using Lua.Standard;
using Nucleus;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.ManagedMemory;
using Nucleus.Types;
using Raylib_cs;
using System.Text;

namespace CloneDash.Scripting;

public class CD_LuaEnv
{
	public LuaState State;
	private Level level;

	public ValueTask<int> print(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken) {
		string[] args = new string[context.ArgumentCount];
		for (int i = 0; i < context.ArgumentCount; i++) {
			args[i] = context.GetArgument(i).ToString();
		}

		Logs.Print(string.Join(' ', args));

		return new(0);
	}

	public void RegisterEnum<T>(string prefix) where T : Enum {
		var names = Enum.GetNames(typeof(T));
		var values = Enum.GetValuesAsUnderlyingType(typeof(T)).Cast<object>().ToArray();
		for (int i = 0; i < names.Length; i++) {
			var name = $"{prefix}_{names[i]}".ToUpper();
			switch (values[i]) {
				case byte cast: State.Environment[name] = cast; break;
				case sbyte cast: State.Environment[name] = cast; break;
				case ushort cast: State.Environment[name] = cast; break;
				case short cast: State.Environment[name] = cast; break;
				case uint cast: State.Environment[name] = cast; break;
				case int cast: State.Environment[name] = cast; break;
				case ulong cast: State.Environment[name] = cast; break;
				case long cast: State.Environment[name] = cast; break;
				case float cast: State.Environment[name] = cast; break;
				case double cast: State.Environment[name] = cast; break;
				default: throw new NotImplementedException(values[i].GetType().Name);
			}
		}
	}

	public CD_LuaEnv(Level level) {
		this.level = level;

		State = LuaState.Create();

		// These libraries *SHOULD* be safe...
		State.OpenBasicLibrary();
		State.OpenMathLibrary();
		State.OpenStringLibrary();
		State.OpenTableLibrary();

		// Types
		State.Environment["Color"] = new LuaColor();
		State.Environment["print"] = new LuaFunction("print", print);

		// Enums
		State.Environment["ANCHOR_TOP_LEFT"] = Anchor.TopLeft.Deconstruct();
		State.Environment["ANCHOR_TOP_CENTER"] = Anchor.TopCenter.Deconstruct();
		State.Environment["ANCHOR_TOP_RIGHT"] = Anchor.TopRight.Deconstruct();
		State.Environment["ANCHOR_CENTER_LEFT"] = Anchor.CenterLeft.Deconstruct();
		State.Environment["ANCHOR_CENTER"] = Anchor.Center.Deconstruct();
		State.Environment["ANCHOR_CENTER_RIGHT"] = Anchor.CenterRight.Deconstruct();
		State.Environment["ANCHOR_BOTTOM_LEFT"] = Anchor.BottomLeft.Deconstruct();
		State.Environment["ANCHOR_BOTTOM_CENTER"] = Anchor.BottomCenter.Deconstruct();
		State.Environment["ANCHOR_BOTTOM_RIGHT"] = Anchor.BottomRight.Deconstruct();

		State.Environment["TEXTURE_WRAP_REPEAT"] = (int)TextureWrap.TEXTURE_WRAP_REPEAT;
		State.Environment["TEXTURE_WRAP_CLAMP"] = (int)TextureWrap.TEXTURE_WRAP_CLAMP;
		State.Environment["TEXTURE_WRAP_MIRROR_REPEAT"] = (int)TextureWrap.TEXTURE_WRAP_MIRROR_REPEAT;
		State.Environment["TEXTURE_WRAP_MIRROR_CLAMP"] = (int)TextureWrap.TEXTURE_WRAP_MIRROR_CLAMP;

		// Libraries
		State.Environment["textures"] = new CD_LuaTextures(level, level.Textures);
		State.Environment["graphics"] = new CD_LuaGraphics(level);
	}

	public LuaValue[] DoFile(string pathID, string path) {
		var t = State.DoStringAsync(Filesystem.ReadAllText(pathID, path) ?? throw new FileNotFoundException(), IManagedMemory.MergePath(pathID, path)).AsTask();
		try {
			t.Wait();
			return t.Result;
		}
		catch (Exception ex) {
			Logs.Error(ex.Message);
			return [];
		}
	}

	public LuaValue[] Call(LuaFunction func, params LuaValue[] args) {
		var t = func.InvokeAsync(State, args).AsTask();
		t.Wait();
		return t.Result;
	}

	public LuaValue[] ProtectedCall(LuaFunction? func, params LuaValue[] args) {
		if (func == null)
			return [];

		Task<LuaValue[]> t;
		try {
			t = func.InvokeAsync(State, args).AsTask();
			t.Wait();

			return t.Result;
		}
		catch (Exception ex) {
			Logs.Error($"{ex.Message}");
			return [];
		}
	}
}


public interface ILuaWrappedObject<Around>
{
	public Around Unwrap();
}


[LuaObject]
public partial class CD_LuaGraphics(Level level)
{
	private Raylib_cs.Color drawColor = Raylib_cs.Color.White;
	private CD_LuaTexture? activeTexture;

	[LuaMember("setDrawColor")]
	public void SetDrawColor(double r, double g, double b, double a) {
		int rI = (int)(float)Math.Clamp(r, 0, 255);
		int gI = (int)(float)Math.Clamp(g, 0, 255);
		int bI = (int)(float)Math.Clamp(b, 0, 255);
		int aI = (int)(float)Math.Clamp(a, 0, 255);

		drawColor = new(rI, gI, bI, aI);
	}

	[LuaMember("setTexture")]
	public void SetTexture(CD_LuaTexture tex) {
		activeTexture = tex;
	}

	[LuaMember("drawRectangle")]
	public void DrawRectangle(float x, float y, float width, float height)
		=> Raylib.DrawRectanglePro(new(x, y, width, height), new(0, 0), 0, drawColor);


	[LuaMember("drawTextureUV")]
	public void DrawTextureUV(float x, float y, float width, float height, float startU, float startV, float endU, float endV) {
		var texture = activeTexture;
		if (texture == null) return;

		var texWidth = texture.Width;
		var texHeight = texture.Height;

		float u1 = startU * texWidth, v1 = startV * texHeight;
		float u2 = (endU * texWidth) - u1, v2 = (endV * texHeight) - v1;

		Raylib.DrawTexturePro(texture.Unwrap(), new(u1, v1, u2, v2), new(x, y, width, height), new(0, 0), 0, drawColor);
	}

	[LuaMember("drawTexture")]
	public void DrawTexture(float x, float y, float width, float height) {
		var texture = activeTexture;
		if (texture == null) return;

		Raylib.DrawTexturePro(texture.Unwrap(), new(0, 0, texture.Width, texture.Height), new(x, y, width, height), new(0, 0), 0, drawColor);
	}

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
	public static LuaColor __call(int r, int g, int b, int a = 255) {
		return new() { color = new(Math.Clamp(r, 0, 255), Math.Clamp(g, 0, 255), Math.Clamp(b, 0, 255), Math.Clamp(a, 0, 255)) };
	}
}