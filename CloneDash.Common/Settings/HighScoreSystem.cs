using CloneDash.Common.Songs;

namespace CloneDash.Common.Settings;

public class HighScore
{
	public DateTime Time;
	public int Score;
	public int PreviousHighScore;
}

public class LocalHighScoreSystem
{
	readonly Dictionary<string, List<HighScore>> highScores = [];
	public static int GetHighScore(ISongChart chart) { return 0; } // todo
	public static void SetHighScore(ISongChart chart, int score) { } // todo
}
