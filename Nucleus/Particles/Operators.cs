using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Nucleus.Particles;

public sealed class ColorOverLifetimeOperator : ParticleOperator
{
	public ParticleColorRange Color = ParticleColorRange.Constant(Vector4.One);

	public override ParticleAttributeMask ReadAttributes => ParticleAttributeMask.Lifetime | ParticleAttributeMask.CreationTime;
	public override ParticleAttributeMask ReadInitialAttributes => ParticleAttributeMask.Tint | ParticleAttributeMask.Alpha;
	public override ParticleAttributeMask WrittenAttributes => ParticleAttributeMask.Tint | ParticleAttributeMask.Alpha;

	public override void Operate(ParticleCollection particles, object? context) {
		particles.GetVector(ParticleAttribute.Tint, out var r, out var g, out var b);
		particles.GetInitialVector(ParticleAttribute.Tint, out var r0, out var g0, out var b0);
		var alpha = particles.GetFloat(ParticleAttribute.Alpha);
		var alpha0 = particles.GetInitialFloat(ParticleAttribute.Alpha);
		for (int i = 0; i < particles.ActiveCount; i++) {
			var color = Color.Evaluate(particles.GetAge(i), particles.RandomFloat(i, 81));
			r[i] = r0[i] * color.X;
			g[i] = g0[i] * color.Y;
			b[i] = b0[i] * color.Z;
			alpha[i] = alpha0[i] * color.W;
		}
	}
}

public sealed class ControlPointLockOperator : ParticleOperator
{
	sealed class LockState
	{
		public bool Initialized;
		public Matrix4x4 Previous;
	}

	public override bool RunsBeforeEmitters => true;
	public override ParticleAttributeMask ReadAttributes => ParticleAttributeMask.Position | ParticleAttributeMask.Velocity;
	public override ParticleAttributeMask WrittenAttributes => ParticleAttributeMask.Position | ParticleAttributeMask.Velocity;

	public override object? CreateContext() => new LockState();

	public override void Operate(ParticleCollection particles, object? context) {
		var state = (LockState)context!;
		var current = particles.GetControlPoint(0);
		if (!state.Initialized || state.Previous == current || !Matrix4x4.Invert(state.Previous, out var inverse)) {
			state.Previous = current;
			state.Initialized = true;
			return;
		}

		var delta = inverse * current;
		state.Previous = current;
		particles.GetVector(ParticleAttribute.Position, out var px, out var py, out var pz);
		particles.GetVector(ParticleAttribute.Velocity, out var vx, out var vy, out var vz);
		for (int i = 0; i < particles.ActiveCount; i++) {
			var position = Vector3.Transform(new(px[i], py[i], pz[i]), delta);
			var velocity = Vector3.TransformNormal(new(vx[i], vy[i], vz[i]), delta);
			px[i] = position.X;
			py[i] = position.Y;
			pz[i] = position.Z;
			vx[i] = velocity.X;
			vy[i] = velocity.Y;
			vz[i] = velocity.Z;
		}
	}
}

public sealed class DecayOperator : ParticleOperator
{
	public override ParticleAttributeMask ReadAttributes => ParticleAttributeMask.Lifetime | ParticleAttributeMask.CreationTime;

	public override void Operate(ParticleCollection particles, object? context) {
		ref float creation = ref MemoryMarshal.GetReference(particles.GetFloat(ParticleAttribute.CreationTime));
		ref float lifetime = ref MemoryMarshal.GetReference(particles.GetFloat(ParticleAttribute.Lifetime));
		var now = Vector128.Create(particles.CurrentTime);
		int padded = particles.PaddedCount;
		for (int i = 0; i < padded; i += ParticleSimd.WIDTH) {
			var age = now - Vector128.LoadUnsafe(ref creation, (nuint)i);
			uint dead = Vector128.GreaterThanOrEqual(age, Vector128.LoadUnsafe(ref lifetime, (nuint)i)).ExtractMostSignificantBits();
			while (dead != 0) {
				particles.KillParticle(i + BitOperations.TrailingZeroCount(dead));
				dead &= dead - 1;
			}
		}
	}
}

public sealed class LimitVelocityOperator : ParticleOperator
{
	public ParticleRange Speed = ParticleRange.Constant(1);
	public bool SeparateAxes;
	public ParticleRange X = ParticleRange.Constant(1);
	public ParticleRange Y = ParticleRange.Constant(1);
	public ParticleRange Z = ParticleRange.Constant(1);
	public float Dampen = 1;
	public ParticleRange Drag = ParticleRange.Constant(0);
	public bool DragBySize;
	public bool DragBySpeed;

	public override ParticleAttributeMask ReadAttributes => ParticleAttributeMask.Velocity | ParticleAttributeMask.Lifetime | ParticleAttributeMask.CreationTime | (DragBySize ? ParticleAttributeMask.Radius : 0);
	public override ParticleAttributeMask WrittenAttributes => ParticleAttributeMask.Velocity;

	public override void Operate(ParticleCollection particles, object? context) {
		particles.GetVector(ParticleAttribute.Velocity, out var vx, out var vy, out var vz);
		var radius = DragBySize ? particles.GetFloat(ParticleAttribute.Radius) : default;
		float dt = particles.Dt;
		float dampen = 1 - MathF.Pow(1 - Math.Clamp(Dampen, 0, 1), dt * 30);
		bool drag = !Drag.IsZero;

		for (int i = 0; i < particles.ActiveCount; i++) {
			float age = particles.GetAge(i);
			if (SeparateAxes) {
				vx[i] = LimitAxis(vx[i], X.Evaluate(age, particles.RandomFloat(i, 91)), dampen);
				vy[i] = LimitAxis(vy[i], Y.Evaluate(age, particles.RandomFloat(i, 92)), dampen);
				vz[i] = LimitAxis(vz[i], Z.Evaluate(age, particles.RandomFloat(i, 93)), dampen);
			}
			else {
				float speed = MathF.Sqrt(vx[i] * vx[i] + vy[i] * vy[i] + vz[i] * vz[i]);
				float limit = Speed.Evaluate(age, particles.RandomFloat(i, 94));
				if (speed > limit && speed > 0) {
					float scale = (speed - (speed - limit) * dampen) / speed;
					vx[i] *= scale;
					vy[i] *= scale;
					vz[i] *= scale;
				}
			}

			if (drag) {
				float amount = Drag.Evaluate(age, particles.RandomFloat(i, 95));
				if (DragBySize)
					amount *= radius[i] * 2;
				if (DragBySpeed)
					amount *= MathF.Sqrt(vx[i] * vx[i] + vy[i] * vy[i] + vz[i] * vz[i]);
				float scale = MathF.Max(0, 1 - amount * dt);
				vx[i] *= scale;
				vy[i] *= scale;
				vz[i] *= scale;
			}
		}
	}

	static float LimitAxis(float value, float limit, float dampen) {
		float magnitude = MathF.Abs(value);
		if (magnitude <= limit)
			return value;
		return MathF.CopySign(magnitude - (magnitude - limit) * dampen, value);
	}
}

public sealed class MovementOperator : ParticleOperator
{
	public Vector3 Gravity = new(0, -9.81f, 0);
	public ParticleRange GravityScale = ParticleRange.Constant(0);

	public override ParticleAttributeMask ReadAttributes => ParticleAttributeMask.Position | ParticleAttributeMask.Velocity;
	public override ParticleAttributeMask WrittenAttributes => ParticleAttributeMask.Position | ParticleAttributeMask.Velocity;

	public override void Operate(ParticleCollection particles, object? context) {
		int padded = particles.PaddedCount;
		float dt = particles.Dt;

		particles.ClearScratch(out var ax, out var ay, out var az);
		if (!GravityScale.IsZero) {
			if (GravityScale.Mode == ParticleRangeMode.Constant) {
				ParticleSimd.Fill(ax, Gravity.X * GravityScale.Max, padded);
				ParticleSimd.Fill(ay, Gravity.Y * GravityScale.Max, padded);
				ParticleSimd.Fill(az, Gravity.Z * GravityScale.Max, padded);
			}
			else {
				float time = particles.NormalizedSystemTime;
				for (int i = 0; i < particles.ActiveCount; i++) {
					float scale = GravityScale.Evaluate(time, particles.RandomFloat(i, 101));
					ax[i] = Gravity.X * scale;
					ay[i] = Gravity.Y * scale;
					az[i] = Gravity.Z * scale;
				}
			}
		}
		particles.AccumulateForces(ax, ay, az);

		particles.GetVector(ParticleAttribute.Velocity, out var vx, out var vy, out var vz);
		particles.GetVector(ParticleAttribute.Position, out var px, out var py, out var pz);
		ParticleSimd.MultiplyAdd(vx, ax, dt, padded);
		ParticleSimd.MultiplyAdd(vy, ay, dt, padded);
		ParticleSimd.MultiplyAdd(vz, az, dt, padded);
		ParticleSimd.MultiplyAdd(px, vx, dt, padded);
		ParticleSimd.MultiplyAdd(py, vy, dt, padded);
		ParticleSimd.MultiplyAdd(pz, vz, dt, padded);

		particles.EnforceConstraints();
	}
}

public sealed class RadiusOverLifetimeOperator : ParticleOperator
{
	public ParticleRange Scale = ParticleRange.Constant(1);

	public override ParticleAttributeMask ReadAttributes => ParticleAttributeMask.Lifetime | ParticleAttributeMask.CreationTime;
	public override ParticleAttributeMask ReadInitialAttributes => ParticleAttributeMask.Radius;
	public override ParticleAttributeMask WrittenAttributes => ParticleAttributeMask.Radius;

	public override void Operate(ParticleCollection particles, object? context) {
		var radius = particles.GetFloat(ParticleAttribute.Radius);
		var radius0 = particles.GetInitialFloat(ParticleAttribute.Radius);
		for (int i = 0; i < particles.ActiveCount; i++)
			radius[i] = radius0[i] * Scale.Evaluate(particles.GetAge(i), particles.RandomFloat(i, 111));
	}
}

public sealed class SequenceOverLifetimeOperator : ParticleOperator
{
	public ParticleRange FrameOverLifetime = ParticleRange.Constant(0);
	public ParticleRange StartFrame = ParticleRange.Constant(0);
	public float Cycles = 1;
	public int FrameCount = 1;
	public int Row;
	public int RowCount = 1;
	public bool RandomRow;

	public override ParticleAttributeMask ReadAttributes => ParticleAttributeMask.Lifetime | ParticleAttributeMask.CreationTime;
	public override ParticleAttributeMask WrittenAttributes => ParticleAttributeMask.Sequence;

	public override void Operate(ParticleCollection particles, object? context) {
		var sequence = particles.GetFloat(ParticleAttribute.Sequence);
		int frames = Math.Max(FrameCount, 1);
		for (int i = 0; i < particles.ActiveCount; i++) {
			float progress = FrameOverLifetime.Evaluate(particles.GetAge(i), particles.RandomFloat(i, 121)) * Cycles;
			progress -= MathF.Floor(progress);
			float start = StartFrame.Evaluate(particles.GetAge(i), particles.RandomFloat(i, 122));
			int frame = (int)((progress + start) * frames) % frames;
			int row = RandomRow ? (int)(particles.RandomFloat(i, 123) * RowCount) % Math.Max(RowCount, 1) : Row;
			sequence[i] = row * frames + frame;
		}
	}
}

public sealed class SpinOverLifetimeOperator : ParticleOperator
{
	public ParticleRange Speed = ParticleRange.Constant(0);

	public override ParticleAttributeMask ReadAttributes => ParticleAttributeMask.Rotation | ParticleAttributeMask.Lifetime | ParticleAttributeMask.CreationTime;
	public override ParticleAttributeMask WrittenAttributes => ParticleAttributeMask.Rotation;

	public override void Operate(ParticleCollection particles, object? context) {
		var rotation = particles.GetFloat(ParticleAttribute.Rotation);
		float dt = particles.Dt;
		for (int i = 0; i < particles.ActiveCount; i++)
			rotation[i] += Speed.Evaluate(particles.GetAge(i), particles.RandomFloat(i, 131)) * dt;
	}
}

public sealed class VelocityOverLifetimeOperator : ParticleOperator
{
	public ParticleRange X = ParticleRange.Constant(0);
	public ParticleRange Y = ParticleRange.Constant(0);
	public ParticleRange Z = ParticleRange.Constant(0);
	public bool WorldSpace;

	public override ParticleAttributeMask ReadAttributes => ParticleAttributeMask.Position | ParticleAttributeMask.Lifetime | ParticleAttributeMask.CreationTime;
	public override ParticleAttributeMask WrittenAttributes => ParticleAttributeMask.Position;

	public override void Operate(ParticleCollection particles, object? context) {
		particles.GetVector(ParticleAttribute.Position, out var px, out var py, out var pz);
		ref readonly var origin = ref particles.GetControlPoint(0);
		float dt = particles.Dt;
		for (int i = 0; i < particles.ActiveCount; i++) {
			float age = particles.GetAge(i);
			var velocity = new Vector3(X.Evaluate(age, particles.RandomFloat(i, 141)), Y.Evaluate(age, particles.RandomFloat(i, 142)), Z.Evaluate(age, particles.RandomFloat(i, 143)));
			if (!WorldSpace)
				velocity = Vector3.TransformNormal(velocity, origin);
			px[i] += velocity.X * dt;
			py[i] += velocity.Y * dt;
			pz[i] += velocity.Z * dt;
		}
	}
}
