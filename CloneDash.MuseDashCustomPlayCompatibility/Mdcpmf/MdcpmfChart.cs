using System.Globalization;

namespace CloneDash.MuseDashCustomPlayCompatibility.Mdcpmf;

public enum MdcpmfLane
{
	None,
	Road,
	Air,
	Middle
}

public class MdcpmfNote
{
	public string Kind = "";
	public string BaseKind = "";
	public MdcpmfLane Lane;

	public double Time;
	public string InType = "";
	public double OutTime = -1;
	public string? Extra;
	public int Speed = 2;
	public string? Modifiers;

	public int Layer;

	public bool HasOutTime => OutTime >= 0 && OutTime > Time;
	public bool IsCurve => InType == "curve";
	public bool IsReverseTrack => Extra == "revtrack";

	public override string ToString() => $"note({Kind},{Time},{InType},{OutTime},{Extra ?? "NULL"},{Speed})";
}

public struct MdcpmfTiming
{
	public double Time;
	public double Value;
	public int Layer;
}

public struct MdcpmfBpmBlock
{
	public double Time;
	public double BPM;
	public int Numerator;
	public int Denominator;
}

public class MdcpmfChart
{
	public string Header = "";

	public string ID = "";
	public string Title = "";
	public string Artist = "";
	public string Designer = "";
	public string Difficulty = "";
	public string Level = "";
	public double BPM;
	public double EndTime;
	public double DemoStart;
	public double DemoEnd;
	public string Scene = "";
	public int ObjectCount;

	public readonly List<MdcpmfBpmBlock> BpmBlocks = [];
	public readonly List<MdcpmfNote> Notes = [];
	public readonly List<MdcpmfTiming> Timings = [];
	public int LayerCount;

	public static MdcpmfChart FromFile(string filepath) => FromBytes(File.ReadAllBytes(filepath));
	public static MdcpmfChart FromBytes(ReadOnlySpan<byte> file) => Parse(MdcpmfCodec.DecodeToString(file));

	static double ParseDouble(ReadOnlySpan<char> s, double fallback = 0)
		=> double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : fallback;
	static int ParseInt(ReadOnlySpan<char> s, int fallback = 0)
		=> int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : fallback;

	static List<string> SplitArgs(ReadOnlySpan<char> args) {
		List<string> ret = [];
		int depth = 0, start = 0;
		for (int i = 0; i < args.Length; i++) {
			switch (args[i]) {
				case '(': depth++; break;
				case ')': if (depth > 0) depth--; break;
				case ',':
					if (depth == 0) {
						ret.Add(args[start..i].ToString());
						start = i + 1;
					}
					break;
			}
		}
		ret.Add(args[start..].ToString());
		return ret;
	}

	static bool TryParseStatement(ReadOnlySpan<char> line, out ReadOnlySpan<char> name, out ReadOnlySpan<char> args) {
		name = args = default;
		int open = line.IndexOf('(');
		int close = line.LastIndexOf(')');
		if (open <= 0 || close < open)
			return false;

		name = line[..open].Trim();
		args = line[(open + 1)..close];
		return true;
	}

	public static MdcpmfChart Parse(string text) {
		MdcpmfChart chart = new();
		string? block = null;
		int layer = -1;

		foreach (var rawLine in text.Split('\n')) {
			var line = rawLine.AsSpan().Trim();
			if (line.IsEmpty) continue;

			if (line.StartsWith("//")) {
				if (chart.Header.Length == 0)
					chart.Header = line[2..].Trim().ToString();
				continue;
			}

			if (line.EndsWith("{")) {
				block = line[..^1].Trim().ToString();
				if (block == "maplayer")
					layer++;
				continue;
			}
			if (line.StartsWith("}")) {
				block = null;
				continue;
			}

			if (!TryParseStatement(line, out var name, out var argSpan))
				continue;

			var args = SplitArgs(argSpan);

			switch (name) {
				case "mapinfo": chart.ReadMapInfo(args); break;
				case "bpmbeatblock" when args.Count >= 2:
					chart.BpmBlocks.Add(new() {
						Time = ParseDouble(args[0]),
						BPM = ParseDouble(args[1]),
						Numerator = args.Count > 2 ? ParseInt(args[2], 4) : 4,
						Denominator = args.Count > 3 ? ParseInt(args[3], 4) : 4,
					});
					break;
				case "timing" when args.Count >= 2:
					chart.Timings.Add(new() { Time = ParseDouble(args[0]), Value = ParseDouble(args[1], 1), Layer = Math.Max(layer, 0) });
					break;
				case "note" when args.Count >= 2:
					chart.Notes.Add(ReadNote(args, Math.Max(layer, 0)));
					break;
			}
		}

		chart.LayerCount = layer + 1;
		chart.Notes.Sort(static (a, b) => a.Time.CompareTo(b.Time));
		chart.Timings.Sort(static (a, b) => a.Time.CompareTo(b.Time));
		chart.BpmBlocks.Sort(static (a, b) => a.Time.CompareTo(b.Time));
		return chart;
	}

	void ReadMapInfo(List<string> args) {
		if (args.Count < 12) {
			while (args.Count < 12) args.Add("");
		}
		else if (args.Count > 12) {
			int extra = args.Count - 12;
			args[1] = string.Join(",", args.GetRange(1, extra + 1));
			args.RemoveRange(2, extra);
		}

		ID = args[0];
		Title = args[1];
		Artist = args[2];
		Designer = args[3];
		Difficulty = args[4].Trim().ToLowerInvariant();
		Level = args[5].Trim();
		BPM = ParseDouble(args[6]);
		EndTime = ParseDouble(args[7]);
		DemoStart = ParseDouble(args[8]);
		DemoEnd = ParseDouble(args[9]);
		Scene = args[10].Trim();
		ObjectCount = ParseInt(args[11]);
	}

	static MdcpmfNote ReadNote(List<string> args, int layer) {
		MdcpmfNote note = new() {
			Kind = args[0].Trim(),
			Time = ParseDouble(args[1]),
			InType = args.Count > 2 ? args[2].Trim() : "normal",
			OutTime = args.Count > 3 ? ParseDouble(args[3], -1) : -1,
			Extra = args.Count > 4 && args[4] != "NULL" ? args[4] : null,
			Speed = args.Count > 5 ? ParseInt(args[5], 2) : 2,
			Modifiers = args.Count > 6 ? string.Join(",", args.Skip(6)) : null,
			Layer = layer
		};

		note.BaseKind = note.Kind;
		note.Lane = MdcpmfLane.None;
		if (note.Kind.EndsWith("_road")) { note.Lane = MdcpmfLane.Road; note.BaseKind = note.Kind[..^5]; }
		else if (note.Kind.EndsWith("_air")) { note.Lane = MdcpmfLane.Air; note.BaseKind = note.Kind[..^4]; }
		else if (note.Kind.EndsWith("_middle")) { note.Lane = MdcpmfLane.Middle; note.BaseKind = note.Kind[..^7]; }

		return note;
	}
}
