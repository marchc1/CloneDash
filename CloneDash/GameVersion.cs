namespace CloneDash;

public struct GameVersion
{
	public static readonly GameVersion Current = new GameVersion(2025, 05, 07);

	public int Year;
	public int Month;
	public int Day;

	public GameVersion(int year, int month, int day) {
		this.Year = year;
		this.Month = month;
		this.Day = day;
	}

	public override string ToString() => $"{Year}.{Month}.{Day}";
}
