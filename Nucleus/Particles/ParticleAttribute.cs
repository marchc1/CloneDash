namespace Nucleus.Particles;

public enum ParticleAttribute
{
	Position,
	Velocity,
	Lifetime,
	CreationTime,
	Radius,
	Rotation,
	Tint,
	Alpha,
	Sequence,
	AnimatedVelocity,

	Count
}

[Flags]
public enum ParticleAttributeMask : uint
{
	None = 0,
	Position = 1u << (int)ParticleAttribute.Position,
	Velocity = 1u << (int)ParticleAttribute.Velocity,
	Lifetime = 1u << (int)ParticleAttribute.Lifetime,
	CreationTime = 1u << (int)ParticleAttribute.CreationTime,
	Radius = 1u << (int)ParticleAttribute.Radius,
	Rotation = 1u << (int)ParticleAttribute.Rotation,
	Tint = 1u << (int)ParticleAttribute.Tint,
	Alpha = 1u << (int)ParticleAttribute.Alpha,
	Sequence = 1u << (int)ParticleAttribute.Sequence,
	AnimatedVelocity = 1u << (int)ParticleAttribute.AnimatedVelocity,

	Vectors = Position | Velocity | Tint | AnimatedVelocity,
	Required = Position | Lifetime | CreationTime
}

public static class ParticleAttributes
{
	public static ParticleAttributeMask ToMask(this ParticleAttribute attribute) => (ParticleAttributeMask)(1u << (int)attribute);

	public static bool IsVector(this ParticleAttribute attribute) => (ParticleAttributeMask.Vectors & attribute.ToMask()) != 0;

	public static int ComponentCount(this ParticleAttribute attribute) => attribute.IsVector() ? 3 : 1;
}
