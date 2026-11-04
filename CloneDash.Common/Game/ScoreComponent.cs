namespace CloneDash.Common.Game;

public class ScoreComponent : IGameComponent
{
	/// <summary>
	/// Current score of the player.
	/// </summary>
	public int Value { get; private set; } = 0;

	public void Add(int score) {
		Value += score;
	}

	public void Reset() {
		Value = 0;
	}
}
