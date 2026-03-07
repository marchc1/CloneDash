using CloneDash.Game;

namespace CloneDash.Data
{
	public class ChartSheet
	{
		public ChartSong Song { get; private set; }
		public ChartSheet(ChartSong song) => Song = song;

		public double StartOffset { get; set; }
		public string Rating { get; set; }

		public readonly List<ChartEntity> Entities = [];
		public readonly List<ChartEvent> Events = [];
		public readonly List<TempoChange> TempoChanges = [];
		public readonly List<TimeSignatureChange> TimeSignatureChanges = [];
	}
}
