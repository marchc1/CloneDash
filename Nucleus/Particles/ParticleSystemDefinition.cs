using System.Numerics;

namespace Nucleus.Particles;

public sealed record ParticleChild(ParticleSystemDefinition Definition, float Delay, Matrix4x4 Transform);

public sealed class ParticleSystemDefinition(string name)
{
	public string Name = name;
	public int MaxParticles = 1000;
	public float Duration = 5;
	public bool Looping = true;
	public float TimeScale = 1;
	public float MaximumTimeStep = 0.1f;

	public float ConstantLifetime = 1;
	public float ConstantRadius = 0.5f;
	public float ConstantRotation;
	public float ConstantSequence;
	public Vector4 ConstantColor = Vector4.One;

	public readonly List<ParticleEmitter> Emitters = [];
	public readonly List<ParticleInitializer> Initializers = [];
	public readonly List<ParticleOperator> Operators = [];
	public readonly List<ParticleForce> Forces = [];
	public readonly List<ParticleConstraint> Constraints = [];
	public readonly List<ParticleRenderer> Renderers = [];
	public readonly List<ParticleChild> Children = [];

	public IEnumerable<ParticleFunction> Functions => Emitters.Cast<ParticleFunction>().Concat(Initializers).Concat(Operators).Concat(Forces).Concat(Constraints).Concat(Renderers);

	public ParticleAttributeMask GetAllocatedAttributes() {
		var mask = ParticleAttributeMask.Required;
		foreach (var function in Functions)
			mask |= function.ReadAttributes | function.ReadInitialAttributes | function.WrittenAttributes;
		return mask;
	}

	public ParticleAttributeMask GetInitialAttributes() {
		var mask = ParticleAttributeMask.None;
		foreach (var function in Functions)
			mask |= function.ReadInitialAttributes;
		return mask;
	}

	public ParticleAttributeMask GetInitializedAttributes() {
		var mask = ParticleAttributeMask.None;
		foreach (var emitter in Emitters)
			mask |= emitter.WrittenAttributes;
		foreach (var initializer in Initializers)
			mask |= initializer.WrittenAttributes;
		return mask;
	}
}
