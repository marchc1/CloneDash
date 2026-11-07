using System.Numerics;

namespace Nucleus.Particles;

public sealed class ParticleSystemManager
{
	public const int PARALLEL_PARTICLE_THRESHOLD = 2048;

	readonly List<ParticleCollection> collections = [];
	int nextRandomSeed;

	public IReadOnlyList<ParticleCollection> Collections => collections;

	public void Clear() => collections.Clear();

	public ParticleCollection Create(ParticleSystemDefinition definition, in Matrix4x4 transform, float delay = 0) {
		var collection = new ParticleCollection(definition, transform, nextRandomSeed, delay);
		nextRandomSeed += 7919;
		collections.Add(collection);
		return collection;
	}

	public void Remove(ParticleCollection collection) => collections.Remove(collection);

	public void Render(in Matrix4x4 transform) {
		foreach (var collection in collections)
			collection.Render(transform);
	}

	public void Simulate(float dt) {
		if (dt <= 0 || collections.Count == 0)
			return;

		int particles = 0;
		foreach (var collection in collections)
			particles += collection.TotalActiveCount;

		if (collections.Count > 1 && particles >= PARALLEL_PARTICLE_THRESHOLD)
			Parallel.ForEach(collections, collection => collection.Simulate(dt));
		else
			foreach (var collection in collections)
				collection.Simulate(dt);

		collections.RemoveAll(collection => collection.ReleaseWhenFinished && collection.IsFinished());
	}
}
