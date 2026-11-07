using System.Numerics;

namespace Nucleus.Particles;

public sealed class LifetimeForce : ParticleForce
{
	public ParticleRange X = ParticleRange.Constant(0);
	public ParticleRange Y = ParticleRange.Constant(0);
	public ParticleRange Z = ParticleRange.Constant(0);
	public bool WorldSpace;

	public override ParticleAttributeMask ReadAttributes => ParticleAttributeMask.Lifetime | ParticleAttributeMask.CreationTime;

	public override void AddForces(ParticleCollection particles, Span<float> x, Span<float> y, Span<float> z, object? context) {
		ref readonly var origin = ref particles.GetControlPoint(0);
		for (int i = 0; i < particles.ActiveCount; i++) {
			float age = particles.GetAge(i);
			var force = new Vector3(X.Evaluate(age, particles.RandomFloat(i, 151)), Y.Evaluate(age, particles.RandomFloat(i, 152)), Z.Evaluate(age, particles.RandomFloat(i, 153)));
			if (!WorldSpace)
				force = Vector3.TransformNormal(force, origin);
			x[i] += force.X;
			y[i] += force.Y;
			z[i] += force.Z;
		}
	}
}
