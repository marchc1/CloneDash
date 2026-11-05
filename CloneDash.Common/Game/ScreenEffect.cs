using Nucleus.Engine;
using Nucleus.ManagedMemory;
using Nucleus.Types;

namespace CloneDash.Common.Game;

public abstract class ScreenEffect
{
	IConductor conductor = null!;
	double CurrentValue;
	Func<double, double>? Easing;
	double LastValue;
	double Length;
	double Time;

	public double Target => CurrentValue;

	public double Value {
		get {
			if (Length <= 0) return CurrentValue;

			double t = Math.Clamp((Conductor.GetTime() - Time) / Length, 0.0, 1.0);
			if (Easing != null) t = Easing(t);
			return double.Lerp(LastValue, CurrentValue, t);
		}
	}

	protected IConductor Conductor => conductor;

	protected ScreenEffectsComponent Effects { get; private set; } = null!;

	protected Level Level { get; private set; } = null!;

	public virtual void Initialize() { }

	public virtual bool Present(FrameState frameState, RenderTexture read) => false;

	public virtual void Render(FrameState frameState, ref RenderTexture read, ref RenderTexture write) { }

	public virtual void RenderOverlay(FrameState frameState) { }

	public void Reset() {
		CurrentValue = 0;
		Easing = null;
		LastValue = 0;
		Length = 0;
		Time = 0;
		OnReset();
	}

	public void Trigger(double target, double length, Func<double, double>? easing = null) {
		LastValue = Value;
		CurrentValue = target;
		Length = length;
		Time = Conductor.GetTime();
		Easing = easing;
	}

	internal void Attach(Level level, IConductor conductor, ScreenEffectsComponent effects) {
		Level = level;
		this.conductor = conductor;
		Effects = effects;
	}

	protected virtual void OnReset() { }
}
