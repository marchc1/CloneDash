namespace CloneDash;

public struct GameVersion
{
	public static readonly GameVersion Current = new GameVersion("2025", "05", "07", "alpha");

	public string Year;
	public string Month;
	public string Day;
	public string? Extra;

	public GameVersion(string year, string month, string day, string? extra = null) {
		this.Year = year;
		this.Month = month;
		this.Day = day;
		this.Extra = extra;
	}

	public override string ToString() => $"{Year}.{Month}.{Day}" + (Extra == null ? "" : $" {Extra}");
}
