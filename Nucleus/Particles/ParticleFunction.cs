using System.Numerics;

namespace Nucleus.Particles;

public abstract class ParticleFunction
{
	public virtual ParticleAttributeMask ReadAttributes => ParticleAttributeMask.None;
	public virtual ParticleAttributeMask ReadInitialAttributes => ParticleAttributeMask.None;
	public virtual ParticleAttributeMask WrittenAttributes => ParticleAttributeMask.None;

	public virtual object? CreateContext() => null;
}

public abstract class ParticleEmitter : ParticleFunction
{
	public override ParticleAttributeMask WrittenAttributes => ParticleAttributeMask.CreationTime;

	public abstract void Emit(ParticleCollection particles, object? context);
	public virtual bool MayCreateMoreParticles(ParticleCollection particles, object? context) => false;
}

public abstract class ParticleInitializer : ParticleFunction
{
	public abstract void InitNewParticles(ParticleCollection particles, int first, int count, object? context);
}

public abstract class ParticleOperator : ParticleFunction
{
	public virtual bool RunsBeforeEmitters => false;

	public abstract void Operate(ParticleCollection particles, object? context);
}

public abstract class ParticleForce : ParticleFunction
{
	public abstract void AddForces(ParticleCollection particles, Span<float> x, Span<float> y, Span<float> z, object? context);
}

public abstract class ParticleConstraint : ParticleFunction
{
	public abstract void EnforceConstraint(ParticleCollection particles, object? context);
}

public abstract class ParticleRenderer : ParticleFunction
{
	public ParticleMaterial Material = new();
	public int SortOrder;

	public abstract void Render(ParticleCollection particles, in Matrix4x4 transform, object? context);
}
