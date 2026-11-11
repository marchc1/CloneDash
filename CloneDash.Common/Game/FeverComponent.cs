using Nucleus.Types;

namespace CloneDash.Common.Game;

public class FeverComponent(IConductor conductor, GameEventManager events) : IRhythmGameComponent, IRhythmGameThinker
{
	public double DismissalTime;
	public double Duration = 6;
	public bool Manual;
	public float Max;

	double LastExitTime = -1000000d;
	double windowStart = -1000000d;

	public bool Dismissed => (conductor.GetTime() - LastExitTime) < DismissalTime;

	/// <summary>
	/// Is the player currently in fever?
	/// </summary>
	public bool InFever { get; private set; } = false;

	/// <summary>
	/// Returns the fever time left as a value of 0-1, where 0 is the end and 1 is the start. Good for animation.
	/// </summary>
	public double Ratio => 1f - ((conductor.GetTime() - WhenDidFeverStart) / Duration);

	/// <summary>
	/// Should the player exit fever?
	/// </summary>
	public bool ShouldExit => (conductor.GetTime() - WhenDidFeverStart) >= Duration;

	/// <summary>
	/// How much fever time is left?
	/// </summary>
	public double TimeLeft => Duration - (conductor.GetTime() - WhenDidFeverStart);

	/// <summary>
	/// Current fever bar.<br></br>
	/// Default: 0
	/// </summary>
	public float Value { get; private set; } = 0;

	/// <summary>
	/// When did the fever start?
	/// </summary>
	public double WhenDidFeverStart { get; private set; } = -1000000d;

	/// <summary>
	/// Adds to the players fever value, and automatically enters fever when the player has maxed out the fever bar.
	/// </summary>
	/// <param name="fever"></param>
	public void Add(float fever) => Add(fever, conductor.GetTime());

	public void Add(float fever, double time) {
		if (IsActiveAt(time)) return;
		if (InFever) {
			if (time < windowStart) return;
			Exit();
		}
		if (Dismissed || IsFull) return;

		Value = Math.Clamp(Value + fever, 0, Max);
		events.Fire(new FeverChangedGameEvent(Value, Max));
		if (IsFull && !Manual)
			Enter(time);
	}

	public bool IsActiveAt(double time) => time >= windowStart && time <= windowStart + Duration;

	public bool IsFull => Value >= Max;

	public bool TryEnter() {
		if (InFever || !IsFull) return false;

		Enter();
		return true;
	}

	public float Consume(float amount) {
		if (InFever) return 0;

		var consumed = Math.Min(Value, amount);
		Value -= consumed;
		events.Fire(new FeverChangedGameEvent(Value, Max));
		return consumed;
	}

	/// <summary>
	/// Enters fever.
	/// </summary>
	public void Enter() => Enter(conductor.GetTime());

	public void Enter(double time) {
		InFever = true;
		WhenDidFeverStart = time;
		windowStart = time;
		events.Fire(new FeverStartGameEvent(Duration));
	}

	/// <summary>
	/// Exits fever.
	/// </summary>
	public void Exit() {
		InFever = false;
		Value = 0;
		WhenDidFeverStart = -1000000d;
		LastExitTime = conductor.GetTime();
		events.Fire(new FeverEndGameEvent());
	}

	public void Reset() {
		InFever = false;
		WhenDidFeverStart = -1000000d;
		windowStart = -1000000d;
		Value = 0;
		LastExitTime = -1000000d;
	}

	public void Think(FrameState frameState) {
		if (ShouldExit && InFever)
			Exit();
	}
}
