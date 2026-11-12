namespace CloneDash.Common.Game;

public class ComboComponent(IConductor conductor) : IRhythmGameComponent
{
	public int MissForgiveness;

	double LastCombo = -2000;

	/// <summary>
	/// Current combo of the player (how many successful hits/avoids in a row)
	/// </summary>
	public int Value { get; private set; } = 0;

	public int Max { get; private set; } = 0;

	/// <summary>
	/// Adds 1 to the players combo.
	/// </summary>
	public void Add() {
		Value++;
		Max = Math.Max(Max, Value);
		LastCombo = conductor.GetTime();
	}

	public void Break() {
		if (Value < MissForgiveness)
			return;

		Value = 0;
	}

	/// <summary>
	/// Resets the players combo.
	/// </summary>
	public void Reset() {
		Value = 0;
		Max = 0;
	}
}
