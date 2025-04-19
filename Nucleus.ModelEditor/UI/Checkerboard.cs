using Nucleus.Core;
using Nucleus.Types;
using Raylib_cs;
using System.Numerics;

namespace Nucleus.ModelEditor
{
	public static class Checkerboard
	{
		private static Shader shader = Filesystem.ReadFragmentShader("shaders", "checkerboard.fshader");
		private static Color defaultLight => new Color(60, 60, 63);
		private static Color defaultDark => new Color(46, 46, 49);
		public static void Draw(float gridSize = 50, float quadSize = 4096, Color? light = null, Color? dark = null) {
			Color c = light ?? defaultLight, d = dark ?? defaultDark;

			shader.SetShaderValue("scale", quadSize / gridSize);
			shader.SetShaderValue("lightColor", new Vector3(c.R / 255f, c.G / 255f, c.B / 255f));
			shader.SetShaderValue("darkColor", new Vector3(d.R / 255f, d.G / 255f, d.B / 255f));
			Raylib.BeginShaderMode(shader);
			Rlgl.Begin(DrawMode.QUADS);

			var z = -1;

			Rlgl.Color4f(1, 1, 1, 1);
			Rlgl.TexCoord2f(-1, -1);
			Rlgl.Vertex3f(-quadSize, quadSize, z);

			Rlgl.Color4f(1, 1, 1, 1);
			Rlgl.TexCoord2f(1, -1);
			Rlgl.Vertex3f(quadSize, quadSize, z);

			Rlgl.Color4f(1, 1, 1, 1);
			Rlgl.TexCoord2f(1, 1);
			Rlgl.Vertex3f(quadSize, -quadSize, z);

			Rlgl.Color4f(1, 1, 1, 1);
			Rlgl.TexCoord2f(-1, 1);
			Rlgl.Vertex3f(-quadSize, -quadSize, z);

			Rlgl.End();
			Raylib.EndShaderMode();
		}
	}
}
