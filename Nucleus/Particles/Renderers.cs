using Nucleus.Common.Graphics;
using Nucleus.Rendering;
using Raylib_cs;
using System.Numerics;

namespace Nucleus.Particles;

public sealed class ParticleSpriteRenderer : ParticleRenderer
{
	public override ParticleAttributeMask ReadAttributes => ParticleAttributeMask.Position | ParticleAttributeMask.Radius | ParticleAttributeMask.Rotation | ParticleAttributeMask.Tint | ParticleAttributeMask.Alpha | ParticleAttributeMask.Sequence;

	public override void Render(ParticleCollection particles, in Matrix4x4 transform, object? context) {
		particles.GetVector(ParticleAttribute.Position, out var px, out var py, out _);
		var radius = particles.GetFloat(ParticleAttribute.Radius);
		var rotation = particles.GetFloat(ParticleAttribute.Rotation);
		var sequence = particles.GetFloat(ParticleAttribute.Sequence);

		ParticleQuads.Begin(Material);
		for (int i = 0; i < particles.ActiveCount; i++) {
			float r = radius[i];
			if (r <= 0)
				continue;

			float cos = MathF.Cos(rotation[i]) * r, sin = MathF.Sin(rotation[i]) * r;
			var right = new Vector2(cos, sin);
			var up = new Vector2(-sin, cos);
			var center = new Vector2(px[i], py[i]);
			ParticleQuads.Quad(Material, transform, center - right + up, center - right - up, center + right - up, center + right + up, sequence[i], ParticleQuads.Color(Material, particles, i));
		}
		ParticleQuads.End();
	}
}

public sealed class ParticleStretchedRenderer : ParticleRenderer
{
	public float LengthScale = 2;
	public float VelocityScale;

	public override ParticleAttributeMask ReadAttributes => ParticleAttributeMask.Position | ParticleAttributeMask.Velocity | ParticleAttributeMask.Radius | ParticleAttributeMask.Tint | ParticleAttributeMask.Alpha | ParticleAttributeMask.Sequence;

	public override void Render(ParticleCollection particles, in Matrix4x4 transform, object? context) {
		particles.GetVector(ParticleAttribute.Position, out var px, out var py, out _);
		particles.GetVector(ParticleAttribute.Velocity, out var vx, out var vy, out _);
		bool animated = particles.HasAttribute(ParticleAttribute.AnimatedVelocity);
		Span<float> ax = default, ay = default;
		if (animated)
			particles.GetVector(ParticleAttribute.AnimatedVelocity, out ax, out ay, out _);
		var radius = particles.GetFloat(ParticleAttribute.Radius);
		var sequence = particles.GetFloat(ParticleAttribute.Sequence);

		ParticleQuads.Begin(Material);
		for (int i = 0; i < particles.ActiveCount; i++) {
			float r = radius[i];
			if (r <= 0)
				continue;

			var velocity = new Vector2(vx[i], vy[i]);
			if (animated)
				velocity += new Vector2(ax[i], ay[i]);
			float speed = velocity.Length();
			var forward = speed > 0.0001f ? velocity / speed : Vector2.UnitY;
			var side = new Vector2(forward.Y, -forward.X) * r;
			float length = r * 2 * LengthScale + speed * VelocityScale;
			var head = new Vector2(px[i], py[i]);
			var tail = head - forward * length;
			ParticleQuads.Quad(Material, transform, head - side, tail - side, tail + side, head + side, sequence[i], ParticleQuads.Color(Material, particles, i));
		}
		ParticleQuads.End();
	}
}

static class ParticleQuads
{
	static bool culling;
	static bool flipV;

	public static void Begin(ParticleMaterial material) {
		Rlgl.DrawRenderBatchActive();
		culling = OpenGL.IsEnabled(OpenGL.CULL_FACE);
		Rlgl.DisableBackfaceCulling();
		Rlgl.SetBlendMode(material.BlendMode switch {
			ParticleBlendMode.Premultiplied => BlendMode.AlphaPremultiply,
			ParticleBlendMode.Additive => BlendMode.Additive,
			ParticleBlendMode.Multiply => BlendMode.Multiplied,
			_ => BlendMode.Alpha
		});
		var texture = material.Texture;
		flipV = texture?.HasPublicFlags(PublicTextureFlags.RequiresFlippedV) ?? false;
		Rlgl.SetTexture(texture?.GetTextureHandle() ?? Rlgl.GetTextureIdDefault());
		Rlgl.Begin(DrawMode.QUADS);
	}

	public static Vector4 Color(ParticleMaterial material, ParticleCollection particles, int i) {
		particles.GetVector(ParticleAttribute.Tint, out var r, out var g, out var b);
		var color = new Vector4(r[i], g[i], b[i], particles.GetFloat(ParticleAttribute.Alpha)[i]) * material.Color;
		if (material.BlendMode == ParticleBlendMode.Premultiplied)
			color = new(color.X * color.W, color.Y * color.W, color.Z * color.W, color.W);
		return color;
	}

	public static void End() {
		Rlgl.End();
		Rlgl.DrawRenderBatchActive();
		Rlgl.SetTexture(0);
		Rlgl.SetBlendMode(BlendMode.Alpha);
		if (culling)
			Rlgl.EnableBackfaceCulling();
	}

	public static void Quad(ParticleMaterial material, in Matrix4x4 transform, Vector2 topLeft, Vector2 bottomLeft, Vector2 bottomRight, Vector2 topRight, float sequence, Vector4 color) {
		int columns = Math.Max(material.SheetColumns, 1), rows = Math.Max(material.SheetRows, 1);
		int frame = Math.Clamp((int)sequence, 0, columns * rows - 1);
		float u0 = (float)(frame % columns) / columns, v0 = (float)(frame / columns) / rows;
		float u1 = u0 + 1f / columns, v1 = v0 + 1f / rows;

		Rlgl.Color4f(color.X, color.Y, color.Z, color.W);
		Vertex(material, transform, topLeft, u0, v0);
		Vertex(material, transform, bottomLeft, u0, v1);
		Vertex(material, transform, bottomRight, u1, v1);
		Vertex(material, transform, topRight, u1, v0);
	}

	static void Vertex(ParticleMaterial material, in Matrix4x4 transform, Vector2 position, float u, float v) {
		u = u * material.TextureScale.X + material.TextureOffset.X;
		v = v * material.TextureScale.Y + material.TextureOffset.Y;
		if (flipV)
			v = 1 - v;
		var world = Vector2.Transform(position, transform);
		Rlgl.TexCoord2f(u, v);
		Rlgl.Vertex2f(world.X, world.Y);
	}
}
