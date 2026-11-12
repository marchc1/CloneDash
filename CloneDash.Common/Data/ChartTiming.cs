namespace CloneDash.Common.Data;

public readonly record struct ChartTiming(IReadOnlyList<TempoChange> TempoChanges, IReadOnlyList<TimeSignatureChange> TimeSignatureChanges);
