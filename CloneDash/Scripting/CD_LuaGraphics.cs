using Lua;

using Nucleus;
using Nucleus.Engine;

using Raylib_cs;

namespace CloneDash.Scripting;

[LuaObject]
public partial class CD_LuaGraphics
{
	private Level level;

	private static int getRGBAPiece(ref LuaFunctionExecutionContext ctx, int index)
		=> (byte)(float)Math.Clamp(ctx.GetArgument<double>(index), 0, 255);
	private static bool tryGetRGBAPiece(LuaValue arg, out int rgbaPiece) {
		if (arg.TryRead(out double dV)) {
			rgbaPiece = (byte)(float)Math.Clamp(dV, 0, 255);
			return true;
		}
		else {
			rgbaPiece = 0;
			return false;
		}
	}

	public CD_LuaGraphics(Level level) {
		this.level = level;

		this.setDrawColor = new LuaFunction(async (ctx, buffer, ct) => {
			switch (ctx.ArgumentCount - 1) {
				case 0:
					Logs.Warn("No arguments passed to setDrawColor.");
					return 0;
				case 1:
					var arg0 = ctx.GetArgument(1);
					if (tryGetRGBAPiece(arg0, out int rgbaPiece))
						drawColor = new(rgbaPiece, rgbaPiece, rgbaPiece, 255);
					else if (arg0.TryRead(out CD_LuaColor luaColor)) {
						drawColor = luaColor.Unwrap();
					}
					else {
						throw new ArgumentException("No behavior for non-number or non-color single argument");
					}

					return 0;
				case 2: {
						var rgb = getRGBAPiece(ref ctx, 1);
						var a = getRGBAPiece(ref ctx, 2);
						drawColor = new(rgb, rgb, rgb, a);
					}
					return 0;
				case 3: {
						var r = getRGBAPiece(ref ctx, 1);
						var g = getRGBAPiece(ref ctx, 2);
						var b = getRGBAPiece(ref ctx, 3);
						drawColor = new(r, g, b, 255);
					}
					return 0;
				default: {
						var r = getRGBAPiece(ref ctx, 1);
						var g = getRGBAPiece(ref ctx, 2);
						var b = getRGBAPiece(ref ctx, 3);
						var a = getRGBAPiece(ref ctx, 4);
						drawColor = new(r, g, b, a);
					}
					return 0;
			}
		});
	}

	private Raylib_cs.Color drawColor = Raylib_cs.Color.White;
	private CD_LuaTexture? activeTexture;
	private int matricesCreated = -1;

	public void StartRenderingLuaContext() {
		matricesCreated = 0;
	}
	public void EndRenderingLuaContext() {
		for (int i = 0; i < matricesCreated; i++) {
			Rlgl.PopMatrix();
		}
		matricesCreated = -1;
	}

	[LuaMember("pushMatrix")]
	public void PushMatrix() {
		if (matricesCreated <= -1) {
			Logs.Error("Can't push a matrix; not in Lua rendering context");
			return;
		}

		Rlgl.PushMatrix();
		matricesCreated++;
	}
	[LuaMember("popMatrix")]
	public void PopMatrix() {
		if (matricesCreated <= 0) {
			Logs.Error("Tried to pop a matrix the Lua context doesn't have control over!");
			return;
		}

		Rlgl.PopMatrix();
		matricesCreated--;
	}
	[LuaMember("translate")]
	public void Translatef(float x, float y, float z) {
		if (matricesCreated <= 0) {
			Logs.Error("Tried to translate a matrix the Lua context doesn't have control over!");
			return;
		}
		Rlgl.Translatef(x, y, z);
	}
	[LuaMember("rotate")]
	public void Rotatef(float angle, float x, float y, float z) {
		if (matricesCreated <= 0) {
			Logs.Error("Tried to rotate a matrix the Lua context doesn't have control over!");
			return;
		}

		Rlgl.Rotatef(angle, x, y, z);
	}
	[LuaMember("scale")]
	public void Scalef(float x, float y, float z) {
		if (matricesCreated <= 0) {
			Logs.Error("Tried to scale a matrix the Lua context doesn't have control over!");
			return;
		}
		Rlgl.Scalef(x, y, z);
	}

	[LuaMember("setDrawColor")] public LuaFunction setDrawColor;


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

	[LuaMember("drawTextureUVRotated")]
	public void DrawTextureUVRotated(float x, float y, float width, float height, float startU, float startV, float endU, float endV, float rotation) {
		var texture = activeTexture;
		if (texture == null) return;

		var texWidth = texture.Width;
		var texHeight = texture.Height;

		float u1 = startU * texWidth, v1 = startV * texHeight;
		float u2 = (endU * texWidth) - u1, v2 = (endV * texHeight) - v1;

		Raylib.DrawTexturePro(texture.Unwrap(), new(u1, v1, u2, v2), new(x, y, width, height), new(width / 2, height / 2), rotation, drawColor);
	}

	[LuaMember("drawTexture")]
	public void DrawTexture(float x, float y, float width, float height) {
		var texture = activeTexture;
		if (texture == null) return;

		Raylib.DrawTexturePro(texture.Unwrap(), new(0, 0, texture.Width, texture.Height), new(x, y, width, height), new(0, 0), 0, drawColor);
	}
	[LuaMember("drawTextureRotated")]
	public void DrawTextureRotated(float x, float y, float width, float height, float rotation) {
		var texture = activeTexture;
		if (texture == null) return;

		Raylib.DrawTexturePro(texture.Unwrap(), new(0, 0, texture.Width, texture.Height), new(x, y, width, height), new(width / 2, height / 2), rotation, drawColor);
	}

	[LuaMember("drawGradientH")]
	public void DrawGradientH(float x, float y, float width, float height, CD_LuaColor color1, CD_LuaColor color2)
		=> Raylib.DrawRectangleGradientH((int)x, (int)y, (int)width, (int)height, color1.Unwrap(), color2.Unwrap());

	[LuaMember("drawGradientV")]
	public void DrawGradientV(float x, float y, float width, float height, CD_LuaColor color1, CD_LuaColor color2)
		=> Raylib.DrawRectangleGradientV((int)x, (int)y, (int)width, (int)height, color1.Unwrap(), color2.Unwrap());
}
