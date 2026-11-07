using Nucleus.Common.Graphics;
using System.Numerics;

namespace Nucleus.Particles;

public enum ParticleBlendMode
{
	Alpha,
	Premultiplied,
	Additive,
	Multiply
}

public sealed class ParticleMaterial
{
	public ITexture? Texture;
	public ParticleBlendMode BlendMode = ParticleBlendMode.Alpha;
	public Vector4 Color = Vector4.One;
	public int SheetColumns = 1;
	public int SheetRows = 1;
	public Vector2 TextureScale = Vector2.One;
	public Vector2 TextureOffset = Vector2.Zero;
}
