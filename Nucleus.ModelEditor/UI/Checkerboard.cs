using Nucleus.Common.Types;
using Nucleus.Engine;
using Nucleus.Extensions;
using Nucleus.Files;
using Nucleus.ManagedMemory;
using Raylib_cs;
using System.Numerics;

namespace Nucleus.ModelEditor;

public class Checkerboard(Level lvl)
{
	private IShader shader = lvl.Shaders.LoadFragmentShaderFromFile("shaders", "checkerboard.fshader");
	private Color defaultLight => new Color(60, 60, 63);
	private Color defaultDark => new Color(46, 46, 49);
	public void Draw(float gridSize = 50, float quadSize = 4096, Color? light = null, Color? dark = null) {
		Color c = light ?? defaultLight, d = dark ?? defaultDark;

		shader.SetUniform("scale", quadSize / gridSize);
		shader.SetUniform("lightColor", new Vector3(c.R / 255f, c.G / 255f, c.B / 255f));
		shader.SetUniform("darkColor", new Vector3(d.R / 255f, d.G / 255f, d.B / 255f));
		shader.Activate();
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
		shader.Deactivate();
	}
}