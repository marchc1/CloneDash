namespace Nucleus.Particles;

public sealed class BurstEmitter : ParticleEmitter
{
	public float Time;
	public ParticleRange Count = ParticleRange.Constant(30);
	public int Cycles = 1;
	public float Interval = 0.01f;
	public float Probability = 1;

	public override void Emit(ParticleCollection particles, object? context) {
		if (particles.EmissionStopped)
			return;

		var definition = particles.Definition;
		float duration = definition.Duration;
		float from = particles.PreviousTime, to = particles.CurrentTime;

		int firstLoop = 0, lastLoop = 0;
		if (definition.Looping && duration > 0) {
			firstLoop = (int)MathF.Floor(from / duration);
			lastLoop = (int)MathF.Floor(to / duration);
		}

		for (int loop = firstLoop; loop <= lastLoop; loop++) {
			float loopStart = loop * duration;
			for (int cycle = 0; Cycles <= 0 || cycle < Cycles; cycle++) {
				float local = Time + cycle * Interval;
				if (duration > 0 && local >= duration)
					break;

				float time = loopStart + local;
				if (time >= to)
					break;

				if (time >= from && particles.RandomFloat() <= Probability) {
					int count = (int)MathF.Round(Count.Evaluate(particles.NormalizedSystemTime, particles.RandomFloat()));
					int first = particles.EmitParticles(count);
					particles.GetFloat(ParticleAttribute.CreationTime)[first..particles.ActiveCount].Fill(time);
				}

				if (Interval <= 0)
					break;
			}
		}
	}

	public override bool MayCreateMoreParticles(ParticleCollection particles, object? context) {
		var definition = particles.Definition;
		if (definition.Looping)
			return true;
		if (Cycles <= 0)
			return particles.CurrentTime < definition.Duration;
		return particles.CurrentTime <= Time + (Cycles - 1) * MathF.Max(Interval, 0);
	}
}

public sealed class ContinuousEmitter : ParticleEmitter
{
	public ParticleRange Rate = ParticleRange.Constant(10);

	sealed class EmitterState
	{
		public float Accumulator;
	}

	public override object? CreateContext() => new EmitterState();

	public override void Emit(ParticleCollection particles, object? context) {
		var definition = particles.Definition;
		if (particles.EmissionStopped || (!definition.Looping && particles.PreviousTime >= definition.Duration))
			return;

		var state = (EmitterState)context!;
		state.Accumulator += Rate.Evaluate(particles.NormalizedSystemTime, particles.RandomFloat()) * particles.Dt;
		int count = (int)state.Accumulator;
		if (count <= 0)
			return;
		state.Accumulator -= count;

		int first = particles.EmitParticles(count);
		var creation = particles.GetFloat(ParticleAttribute.CreationTime);
		int emitted = particles.ActiveCount - first;
		for (int i = 0; i < emitted; i++)
			creation[first + i] = particles.PreviousTime + particles.Dt * (i + 1) / emitted;
	}

	public override bool MayCreateMoreParticles(ParticleCollection particles, object? context) {
		var definition = particles.Definition;
		return definition.Looping || particles.CurrentTime < definition.Duration;
	}
}
