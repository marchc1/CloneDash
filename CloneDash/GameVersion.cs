using Nucleus.Util;

using System.Reflection;

namespace CloneDash;

public struct GameVersion
{
	public static GameVersion FromAssembly(Assembly assembly, string? extra = null) {
		if (assembly.TryGetLinkerTime(out var dt)) {
			return new($"{dt.Year}", $"{dt.Month:00}", $"{dt.Day:00}", extra);
		}

		return default;
	}

	public static readonly GameVersion Current = FromAssembly(Assembly.GetExecutingAssembly(), "alpha");

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
