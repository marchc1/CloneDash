using System.Numerics;

namespace Nucleus.Particles;

public sealed class ParticleCollection
{
	public const int MAX_CONTROL_POINTS = 8;

	readonly float[]?[] attributes = new float[(int)ParticleAttribute.Count * 3][];
	readonly float[]?[] initialAttributes = new float[(int)ParticleAttribute.Count * 3][];
	readonly List<ParticleCollection> children = [];
	readonly object?[] constraintContexts;
	readonly Matrix4x4[] controlPoints = new Matrix4x4[MAX_CONTROL_POINTS];
	readonly object?[] emitterContexts;
	readonly object?[] forceContexts;
	readonly object?[] initializerContexts;
	readonly List<int> killList = [];
	readonly object?[] operatorContexts;
	readonly object?[] rendererContexts;
	readonly List<ParticleRenderItem> renderQueue = [];
	readonly ParticleAttributeMask uninitializedAttributes;

	int capacity;
	int nextParticleId;
	int[] particleIds = [];
	int randomQueryCount;
	float[] scratchX = [], scratchY = [], scratchZ = [];
	object? tag;

	public ParticleCollection(ParticleSystemDefinition definition, in Matrix4x4 transform, int randomSeed = 0, float delay = 0) {
		Definition = definition;
		RandomSeed = randomSeed;
		CurrentTime = -delay;
		PreviousTime = CurrentTime;

		AllocatedAttributes = definition.GetAllocatedAttributes();
		InitialAttributes = definition.GetInitialAttributes() & AllocatedAttributes;
		uninitializedAttributes = AllocatedAttributes & ~definition.GetInitializedAttributes();

		for (int i = 0; i < (int)ParticleAttribute.Count; i++) {
			var attribute = (ParticleAttribute)i;
			for (int c = 0; c < attribute.ComponentCount(); c++) {
				if ((AllocatedAttributes & attribute.ToMask()) != 0)
					attributes[i * 3 + c] = [];
				if ((InitialAttributes & attribute.ToMask()) != 0)
					initialAttributes[i * 3 + c] = [];
			}
		}

		emitterContexts = [.. definition.Emitters.Select(x => x.CreateContext())];
		initializerContexts = [.. definition.Initializers.Select(x => x.CreateContext())];
		operatorContexts = [.. definition.Operators.Select(x => x.CreateContext())];
		forceContexts = [.. definition.Forces.Select(x => x.CreateContext())];
		constraintContexts = [.. definition.Constraints.Select(x => x.CreateContext())];
		rendererContexts = [.. definition.Renderers.Select(x => x.CreateContext())];

		for (int i = 0; i < controlPoints.Length; i++)
			controlPoints[i] = transform;

		for (int i = 0; i < definition.Children.Count; i++) {
			var child = definition.Children[i];
			children.Add(new ParticleCollection(child.Definition, child.Transform * transform, randomSeed + (i + 1) * 131, delay + child.Delay));
		}
	}

	public int ActiveCount { get; private set; }

	public ParticleAttributeMask AllocatedAttributes { get; }

	public IReadOnlyList<ParticleCollection> Children => children;

	public float CurrentTime { get; private set; }

	public ParticleSystemDefinition Definition { get; }

	public float Dt { get; private set; }

	public bool EmissionStopped { get; private set; }

	public ParticleAttributeMask InitialAttributes { get; }

	public float NormalizedSystemTime {
		get {
			float duration = Definition.Duration;
			if (duration <= 0 || CurrentTime <= 0)
				return 0;
			if (Definition.Looping)
				return CurrentTime % duration / duration;
			return MathF.Min(CurrentTime / duration, 1);
		}
	}

	public int PaddedCount => ParticleSimd.Pad(ActiveCount);

	public ReadOnlySpan<int> ParticleIds => particleIds.AsSpan(0, ActiveCount);

	public float PreviousTime { get; private set; }

	public int RandomSeed { get; }

	public bool ReleaseWhenFinished { get; set; } = true;

	public object? Tag {
		get => tag;
		set {
			tag = value;
			foreach (var child in children)
				child.Tag = value;
		}
	}

	public int TotalActiveCount {
		get {
			int count = ActiveCount;
			foreach (var child in children)
				count += child.TotalActiveCount;
			return count;
		}
	}

	public void AccumulateForces(Span<float> x, Span<float> y, Span<float> z) {
		var forces = Definition.Forces;
		for (int i = 0; i < forces.Count; i++)
			forces[i].AddForces(this, x, y, z, forceContexts[i]);
	}

	public void ClearScratch(out Span<float> x, out Span<float> y, out Span<float> z) {
		int padded = PaddedCount;
		x = scratchX.AsSpan(0, padded);
		y = scratchY.AsSpan(0, padded);
		z = scratchZ.AsSpan(0, padded);
		x.Clear();
		y.Clear();
		z.Clear();
	}

	public int EmitParticles(int count) {
		int first = ActiveCount;
		count = Math.Min(count, Definition.MaxParticles - ActiveCount);
		if (count <= 0)
			return first;

		EnsureCapacity(ActiveCount + count);
		for (int i = 0; i < count; i++)
			particleIds[first + i] = nextParticleId++;
		ActiveCount += count;
		return first;
	}

	public void EnforceConstraints() {
		var constraints = Definition.Constraints;
		for (int i = 0; i < constraints.Count; i++)
			constraints[i].EnforceConstraint(this, constraintContexts[i]);
	}

	public float GetAge(int particle) {
		var creation = attributes[Slot(ParticleAttribute.CreationTime)]!;
		var lifetime = attributes[Slot(ParticleAttribute.Lifetime)]!;
		float life = lifetime[particle];
		return life <= 0 ? 1 : Math.Clamp((CurrentTime - creation[particle]) / life, 0, 1);
	}

	public ref readonly Matrix4x4 GetControlPoint(int index) => ref controlPoints[index];

	public Span<float> GetFloat(ParticleAttribute attribute) => attributes[Slot(attribute)] ?? throw MissingAttribute(attribute);

	public Span<float> GetInitialFloat(ParticleAttribute attribute) => initialAttributes[Slot(attribute)] ?? throw MissingAttribute(attribute);

	public void GetInitialVector(ParticleAttribute attribute, out Span<float> x, out Span<float> y, out Span<float> z) {
		int slot = Slot(attribute);
		x = initialAttributes[slot] ?? throw MissingAttribute(attribute);
		y = initialAttributes[slot + 1]!;
		z = initialAttributes[slot + 2]!;
	}

	public void GetVector(ParticleAttribute attribute, out Span<float> x, out Span<float> y, out Span<float> z) {
		int slot = Slot(attribute);
		x = attributes[slot] ?? throw MissingAttribute(attribute);
		y = attributes[slot + 1]!;
		z = attributes[slot + 2]!;
	}

	public bool HasAttribute(ParticleAttribute attribute) => (AllocatedAttributes & attribute.ToMask()) != 0;

	public bool IsFinished() {
		if (CurrentTime < 0 || ActiveCount > 0)
			return false;

		if (!EmissionStopped) {
			var emitters = Definition.Emitters;
			for (int i = 0; i < emitters.Count; i++)
				if (emitters[i].MayCreateMoreParticles(this, emitterContexts[i]))
					return false;
		}

		foreach (var child in children)
			if (!child.IsFinished())
				return false;

		return true;
	}

	public void KillParticle(int particle) {
		if (particle < ActiveCount)
			killList.Add(particle);
	}

	public float RandomFloat() => ParticleRandom.Sample(RandomSeed + randomQueryCount++);

	public float RandomFloat(int particle, int offset) => ParticleRandom.Sample(RandomSeed, particleIds[particle], offset);

	public Vector3 RandomVector(int particle, int offset) => ParticleRandom.UnitVector(RandomSeed, particleIds[particle], offset);

	public void CollectRenderers(List<ParticleRenderItem> items, int minSortOrder = int.MinValue, int maxSortOrder = int.MaxValue) {
		var renderers = Definition.Renderers;
		for (int i = 0; i < renderers.Count; i++)
			if (renderers[i].SortOrder >= minSortOrder && renderers[i].SortOrder <= maxSortOrder)
				items.Add(new(renderers[i], this, rendererContexts[i], items.Count));

		foreach (var child in children)
			child.CollectRenderers(items, minSortOrder, maxSortOrder);
	}

	public void Render(in Matrix4x4 transform) {
		renderQueue.Clear();
		CollectRenderers(renderQueue);
		renderQueue.Sort(ParticleRenderItem.Compare);
		foreach (var item in renderQueue)
			item.Render(transform);
	}

	public void SetControlPoint(int index, in Matrix4x4 transform) {
		controlPoints[index] = transform;
		if (index != 0)
			return;

		for (int i = 0; i < children.Count; i++)
			children[i].SetControlPoint(0, Definition.Children[i].Transform * transform);
	}

	public void Simulate(float dt) {
		if (dt <= 0)
			return;

		SimulateSelf(dt * Definition.TimeScale);

		foreach (var child in children)
			child.Simulate(dt);
	}

	public void StartEmission() {
		EmissionStopped = false;
		foreach (var child in children)
			child.StartEmission();
	}

	public void StopEmission(bool removeAllParticles = false) {
		EmissionStopped = true;
		if (removeAllParticles) {
			ActiveCount = 0;
			killList.Clear();
		}

		foreach (var child in children)
			child.StopEmission(removeAllParticles);
	}

	void ApplyKillList() {
		if (killList.Count == 0)
			return;

		killList.Sort();
		int unique = 1;
		for (int i = 1; i < killList.Count; i++)
			if (killList[i] != killList[unique - 1])
				killList[unique++] = killList[i];
		killList.RemoveRange(unique, killList.Count - unique);

		int left = killList.Count;
		int active = ActiveCount;
		int slot = 0;

		while (left > 0 && killList[left - 1] >= active)
			left--;

		while (left > 0) {
			int kill = killList[slot++];
			left--;

			while (left > 0 && killList[slot + left - 1] == active - 1) {
				left--;
				active--;
			}

			if (kill == active - 1) {
				active--;
				break;
			}

			MoveParticle(active - 1, kill);
			active--;
		}

		ActiveCount = active;
		killList.Clear();
	}

	void CopyInitialAttributeValues(int first, int count) {
		for (int slot = 0; slot < initialAttributes.Length; slot++) {
			var initial = initialAttributes[slot];
			if (initial != null)
				Array.Copy(attributes[slot]!, first, initial, first, count);
		}
	}

	void EnsureCapacity(int needed) {
		if (needed <= capacity)
			return;

		int newCapacity = Math.Max(ParticleSimd.WIDTH, capacity);
		while (newCapacity < needed)
			newCapacity *= 2;
		newCapacity = ParticleSimd.Pad(Math.Min(newCapacity, Math.Max(needed, Definition.MaxParticles)));

		for (int i = 0; i < (int)ParticleAttribute.Count; i++) {
			var attribute = (ParticleAttribute)i;
			if ((AllocatedAttributes & attribute.ToMask()) == 0)
				continue;

			bool initial = (InitialAttributes & attribute.ToMask()) != 0;
			for (int c = 0; c < attribute.ComponentCount(); c++) {
				int slot = i * 3 + c;
				Array.Resize(ref attributes[slot], newCapacity);
				if (initial)
					Array.Resize(ref initialAttributes[slot], newCapacity);
			}
		}

		Array.Resize(ref particleIds, newCapacity);
		Array.Resize(ref scratchX, newCapacity);
		Array.Resize(ref scratchY, newCapacity);
		Array.Resize(ref scratchZ, newCapacity);
		capacity = newCapacity;
	}

	void InitializeNewParticles(int first, int count) {
		var initializers = Definition.Initializers;
		for (int i = 0; i < initializers.Count; i++)
			initializers[i].InitNewParticles(this, first, count, initializerContexts[i]);

		InitParticleAttributes(first, count);
		CopyInitialAttributeValues(first, count);
	}

	void InitParticleAttributes(int first, int count) {
		if (uninitializedAttributes == ParticleAttributeMask.None)
			return;

		var definition = Definition;
		ref readonly var origin = ref controlPoints[0];
		var color = definition.ConstantColor;
		for (int i = 0; i < (int)ParticleAttribute.Count; i++) {
			var attribute = (ParticleAttribute)i;
			if ((uninitializedAttributes & attribute.ToMask()) == 0)
				continue;

			switch (attribute) {
				case ParticleAttribute.Position: FillVector(attribute, first, count, origin.Translation); break;
				case ParticleAttribute.Velocity: FillVector(attribute, first, count, Vector3.Zero); break;
				case ParticleAttribute.Tint: FillVector(attribute, first, count, new(color.X, color.Y, color.Z)); break;
				case ParticleAttribute.Alpha: GetFloat(attribute).Slice(first, count).Fill(color.W); break;
				case ParticleAttribute.Lifetime: GetFloat(attribute).Slice(first, count).Fill(definition.ConstantLifetime); break;
				case ParticleAttribute.CreationTime: GetFloat(attribute).Slice(first, count).Fill(CurrentTime); break;
				case ParticleAttribute.Radius: GetFloat(attribute).Slice(first, count).Fill(definition.ConstantRadius); break;
				case ParticleAttribute.Rotation: GetFloat(attribute).Slice(first, count).Fill(definition.ConstantRotation); break;
				case ParticleAttribute.Sequence: GetFloat(attribute).Slice(first, count).Fill(definition.ConstantSequence); break;
				case ParticleAttribute.AnimatedVelocity: FillVector(attribute, first, count, Vector3.Zero); break;
			}
		}
	}

	void FillVector(ParticleAttribute attribute, int first, int count, Vector3 value) {
		GetVector(attribute, out var x, out var y, out var z);
		x.Slice(first, count).Fill(value.X);
		y.Slice(first, count).Fill(value.Y);
		z.Slice(first, count).Fill(value.Z);
	}

	void MoveParticle(int from, int to) {
		for (int slot = 0; slot < attributes.Length; slot++) {
			var data = attributes[slot];
			if (data != null)
				data[to] = data[from];
			var initial = initialAttributes[slot];
			if (initial != null)
				initial[to] = initial[from];
		}
		particleIds[to] = particleIds[from];
	}

	void RunEmitters() {
		var emitters = Definition.Emitters;
		for (int i = 0; i < emitters.Count; i++) {
			int before = ActiveCount;
			emitters[i].Emit(this, emitterContexts[i]);
			if (ActiveCount != before)
				InitializeNewParticles(before, ActiveCount - before);
		}
	}

	void RunOperators(bool beforeEmitters) {
		var operators = Definition.Operators;
		for (int i = 0; i < operators.Count; i++) {
			var op = operators[i];
			if (op.RunsBeforeEmitters != beforeEmitters)
				continue;
			if (!beforeEmitters && ActiveCount == 0)
				return;

			op.Operate(this, operatorContexts[i]);
			ApplyKillList();
		}
	}

	void SimulateSelf(float dt) {
		if (CurrentTime < 0) {
			CurrentTime += dt;
			PreviousTime = CurrentTime;
			if (CurrentTime < 0)
				return;
			dt = CurrentTime;
			CurrentTime = 0;
			PreviousTime = 0;
		}

		float maxStep = Definition.MaximumTimeStep > 0 ? Definition.MaximumTimeStep : 0.1f;
		float remaining = MathF.Min(dt, maxStep * 10);
		while (remaining > 0) {
			float step = MathF.Min(remaining, maxStep);
			remaining -= step;
			PreviousTime = CurrentTime;
			Dt = step;
			CurrentTime += step;

			RunOperators(true);
			RunEmitters();
			RunOperators(false);
		}
	}

	static int Slot(ParticleAttribute attribute) => (int)attribute * 3;

	static InvalidOperationException MissingAttribute(ParticleAttribute attribute) => new($"Particle attribute {attribute} is not allocated in this collection.");
}
