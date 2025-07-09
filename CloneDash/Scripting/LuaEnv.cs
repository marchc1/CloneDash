using CloneDash.Game;

using Lua;
using Lua.Standard;

using Nucleus;
using Nucleus.Engine;
using Nucleus.Files;
using Nucleus.ManagedMemory;
using Nucleus.Types;

using Raylib_cs;

namespace CloneDash.Scripting;

public class LuaEnv
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

	public LuaEnv(Level level) {
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
		State.Environment["ANCHOR_TOP_LEFT"] = (int)Anchor.TopLeft;
		State.Environment["ANCHOR_TOP_CENTER"] = (int)Anchor.TopCenter;
		State.Environment["ANCHOR_TOP_RIGHT"] = (int)Anchor.TopRight;
		State.Environment["ANCHOR_CENTER_LEFT"] = (int)Anchor.CenterLeft;
		State.Environment["ANCHOR_CENTER"] = (int)Anchor.Center;
		State.Environment["ANCHOR_CENTER_RIGHT"] = (int)Anchor.CenterRight;
		State.Environment["ANCHOR_BOTTOM_LEFT"] = (int)Anchor.BottomLeft;
		State.Environment["ANCHOR_BOTTOM_CENTER"] = (int)Anchor.BottomCenter;
		State.Environment["ANCHOR_BOTTOM_RIGHT"] = (int)Anchor.BottomRight;

		State.Environment["TEXTURE_WRAP_REPEAT"] = (int)TextureWrap.TEXTURE_WRAP_REPEAT;
		State.Environment["TEXTURE_WRAP_CLAMP"] = (int)TextureWrap.TEXTURE_WRAP_CLAMP;
		State.Environment["TEXTURE_WRAP_MIRROR_REPEAT"] = (int)TextureWrap.TEXTURE_WRAP_MIRROR_REPEAT;
		State.Environment["TEXTURE_WRAP_MIRROR_CLAMP"] = (int)TextureWrap.TEXTURE_WRAP_MIRROR_CLAMP;

		// Libraries
		// TODO: CD_LuaAudio
		State.Environment["graphics"] = Graphics = new(level);
		State.Environment["level"] = new LuaLevel(level);
		// TODO: CD_LuaModels
		State.Environment["textures"] = new LuaTextures(level, level.Textures);
	}

	public LuaGraphics Graphics;

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
	public LuaValue[] DoString(string code, string? id = null) {
		var t = State.DoStringAsync(code, id ?? "<anonymous code>").AsTask();
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
