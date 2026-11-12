using System.Numerics;

namespace Nucleus.Particles;

public sealed class ColorInitializer : ParticleInitializer
{
	public ParticleColorRange Color = ParticleColorRange.Constant(Vector4.One);

	public override ParticleAttributeMask WrittenAttributes => ParticleAttributeMask.Tint | ParticleAttributeMask.Alpha;

	public override void InitNewParticles(ParticleCollection particles, int first, int count, object? context) {
		particles.GetVector(ParticleAttribute.Tint, out var r, out var g, out var b);
		var alpha = particles.GetFloat(ParticleAttribute.Alpha);
		float time = particles.NormalizedSystemTime;
		for (int i = first; i < first + count; i++) {
			var color = Color.Evaluate(time, particles.RandomFloat(i, 21));
			r[i] = color.X;
			g[i] = color.Y;
			b[i] = color.Z;
			alpha[i] = color.W;
		}
	}
}

public sealed class LifetimeInitializer : ParticleInitializer
{
	public ParticleRange Lifetime = ParticleRange.Constant(1);

	public override ParticleAttributeMask WrittenAttributes => ParticleAttributeMask.Lifetime;

	public override void InitNewParticles(ParticleCollection particles, int first, int count, object? context) {
		var lifetime = particles.GetFloat(ParticleAttribute.Lifetime);
		float time = particles.NormalizedSystemTime;
		for (int i = first; i < first + count; i++)
			lifetime[i] = MathF.Max(Lifetime.Evaluate(time, particles.RandomFloat(i, 11)), 0.0001f);
	}
}

public sealed class RadiusInitializer : ParticleInitializer
{
	public ParticleRange Radius = ParticleRange.Constant(0.5f);

	public override ParticleAttributeMask WrittenAttributes => ParticleAttributeMask.Radius;

	public override void InitNewParticles(ParticleCollection particles, int first, int count, object? context) {
		var radius = particles.GetFloat(ParticleAttribute.Radius);
		float time = particles.NormalizedSystemTime;
		for (int i = first; i < first + count; i++)
			radius[i] = Radius.Evaluate(time, particles.RandomFloat(i, 31));
	}
}

public sealed class RotationInitializer : ParticleInitializer
{
	public ParticleRange Rotation = ParticleRange.Constant(0);

	public override ParticleAttributeMask WrittenAttributes => ParticleAttributeMask.Rotation;

	public override void InitNewParticles(ParticleCollection particles, int first, int count, object? context) {
		var rotation = particles.GetFloat(ParticleAttribute.Rotation);
		float time = particles.NormalizedSystemTime;
		for (int i = first; i < first + count; i++)
			rotation[i] = Rotation.Evaluate(time, particles.RandomFloat(i, 41));
	}
}

public enum ParticleShape
{
	Point,
	Sphere,
	Hemisphere,
	Cone,
	Box,
	Circle,
	Edge,
	Rectangle
}

public sealed class ShapeInitializer : ParticleInitializer
{
	public ParticleShape Shape = ParticleShape.Point;
	public float Radius = 1;
	public float RadiusThickness = 1;
	public float Angle = 25 * MathF.PI / 180;
	public float Arc = MathF.Tau;
	public float Length = 5;
	public bool EmitFromVolume;
	public float RandomDirection;
	public float SphericalDirection;
	public float RandomPosition;
	public Matrix4x4 Transform = Matrix4x4.Identity;
	public ParticleRange Speed = ParticleRange.Constant(5);

	public override ParticleAttributeMask WrittenAttributes => ParticleAttributeMask.Position | ParticleAttributeMask.Velocity;

	public override void InitNewParticles(ParticleCollection particles, int first, int count, object? context) {
		particles.GetVector(ParticleAttribute.Position, out var px, out var py, out var pz);
		particles.GetVector(ParticleAttribute.Velocity, out var vx, out var vy, out var vz);
		var matrix = Transform * particles.GetControlPoint(0);
		float time = particles.NormalizedSystemTime;

		for (int i = first; i < first + count; i++) {
			Sample(particles, i, out var position, out var direction);

			if (RandomDirection > 0)
				direction = Vector3.Lerp(direction, particles.RandomVector(i, 61), RandomDirection);
			if (SphericalDirection > 0 && position.LengthSquared() > 0)
				direction = Vector3.Lerp(direction, Vector3.Normalize(position), SphericalDirection);
			if (RandomPosition > 0)
				position += particles.RandomVector(i, 63) * RandomPosition * particles.RandomFloat(i, 65);

			position = Vector3.Transform(position, matrix);
			direction = Vector3.TransformNormal(direction, matrix);
			if (direction.LengthSquared() > 0)
				direction = Vector3.Normalize(direction);

			var velocity = direction * Speed.Evaluate(time, particles.RandomFloat(i, 67));
			px[i] = position.X;
			py[i] = position.Y;
			pz[i] = position.Z;
			vx[i] = velocity.X;
			vy[i] = velocity.Y;
			vz[i] = velocity.Z;
		}
	}

	void Sample(ParticleCollection particles, int i, out Vector3 position, out Vector3 direction) {
		float a = particles.RandomFloat(i, 51), b = particles.RandomFloat(i, 53), c = particles.RandomFloat(i, 55);
		switch (Shape) {
			case ParticleShape.Sphere:
			case ParticleShape.Hemisphere: {
					direction = particles.RandomVector(i, 57);
					if (Shape == ParticleShape.Hemisphere)
						direction.Z = MathF.Abs(direction.Z);
					position = direction * Radius * MathF.Cbrt(1 - RadiusThickness * a);
					break;
				}
			case ParticleShape.Cone: {
					float theta = b * Arc;
					float distance = MathF.Sqrt(1 - RadiusThickness * a);
					var disc = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * distance;
					float spread = MathF.Sin(Angle);
					direction = Vector3.Normalize(new(disc.X * spread, disc.Y * spread, MathF.Cos(Angle)));
					position = new Vector3(disc * Radius, 0);
					if (EmitFromVolume)
						position += direction * Length * c;
					break;
				}
			case ParticleShape.Box:
				position = new(a - 0.5f, b - 0.5f, c - 0.5f);
				direction = Vector3.UnitZ;
				break;
			case ParticleShape.Circle: {
					float theta = b * Arc;
					direction = new(MathF.Cos(theta), MathF.Sin(theta), 0);
					position = direction * Radius * MathF.Sqrt(1 - RadiusThickness * a);
					break;
				}
			case ParticleShape.Edge:
				position = new(float.Lerp(-Radius, Radius, a), 0, 0);
				direction = Vector3.UnitY;
				break;
			case ParticleShape.Rectangle:
				position = new(a - 0.5f, b - 0.5f, 0);
				direction = Vector3.UnitZ;
				break;
			default:
				position = Vector3.Zero;
				direction = Vector3.UnitZ;
				break;
		}
	}
}
