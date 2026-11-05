using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Game;
using CloneDash.Settings;
using CloneDash.Unbeatable.Internal;
using Nucleus.Types;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public enum UnbeatableWhiteLabelTwinKind
{
	None,
	Twin,
	Hold
}

public class UnbeatableWhiteLabelJudgeComponent(UnbeatableWhiteLabelChartComponent chart, Conductor conductor, HealthComponent health, InputComponent input, UnbeatableWhiteLabelPlayerComponent player, RhythmGameLevel level, UnbeatableWhiteLabelNoteRules rules, UnbeatableWhiteLabelScoreComponent score) : IRhythmGameComponent, IRhythmGameThinker
{
	readonly Dictionary<(PathwayId Pathway, Side Side), UnbeatableWhiteLabelNote> upcoming = [];
	double lastPosition = double.NaN;

	public double DeltaTime { get; private set; }
	public UnbeatableWhiteLabelJudgement? LastJudgement { get; private set; }
	public double LastJudgementTime { get; private set; }
	public double LastMissTime { get; private set; } = double.NegativeInfinity;
	public RhythmGameThinkPhase Phase => RhythmGameThinkPhase.Judge;
	public double Position => conductor.Time - InputSettings.JudgementOffset;
	public bool PressedAny => PressedTop || PressedLow;
	public bool PressedLow => input.State.ClickCount(InputAction.GroundAttack) > 0;
	public bool PressedTop => input.State.ClickCount(InputAction.AirAttack) > 0;
	public IReadOnlyDictionary<(PathwayId Pathway, Side Side), UnbeatableWhiteLabelNote> Upcoming => upcoming;
	public UnbeatableWhiteLabelDodgeNote? UpcomingDodgeLow { get; private set; }
	public UnbeatableWhiteLabelDodgeNote? UpcomingDodgeTop { get; private set; }

	public void CountHit(UnbeatableWhiteLabelNote note, UnbeatableWhiteLabelJudgement judgement) {
		Count(note, judgement);
		note.Kill();
		note.MarkDead();
	}

	public void CountMiss(UnbeatableWhiteLabelNote note, UnbeatableWhiteLabelJudgement judgement) {
		Count(note, judgement);
		LastMissTime = Position;
		note.DamagePlayer();
	}

	public void Dodge(PathwayId pathway) => player.MarkHit(UnbeatableWhiteLabelPathways.Opposite(pathway));

	public void Hit(PathwayId pathway) {
		player.MarkHit(pathway);
		rules.Swing();
	}

	public bool IsHeldAt(PathwayId pathway) {
		bool top = input.State.IsHeld(InputAction.AirAttack);
		bool low = input.State.IsHeld(InputAction.GroundAttack);
		if (pathway == UnbeatableWhiteLabelPathways.Top)
			return top;
		if (pathway == UnbeatableWhiteLabelPathways.Low)
			return low;
		return top || low;
	}

	public bool IsUpcoming(UnbeatableWhiteLabelNote note) => upcoming.TryGetValue((note.CurrentPathway, note.Side), out var current) && current == note;

	public bool PastHitRange(double time) => Position >= time + UnbeatableWhiteLabelJudgement.LENIENCY;

	public void Reset() {
		upcoming.Clear();
		lastPosition = double.NaN;
		DeltaTime = 0;
		LastJudgement = null;
		LastJudgementTime = 0;
		LastMissTime = double.NegativeInfinity;
		UpcomingDodgeLow = null;
		UpcomingDodgeTop = null;
	}

	public void Think(FrameState frameState) {
		double position = Position;
		DeltaTime = double.IsNaN(lastPosition) ? 0 : Math.Max(0, position - lastPosition);
		lastPosition = position;

		if (health.Dead)
			return;

		UpdateUpcoming();
		player.BeginFrame(this);

		var active = chart.Active;
		for (int i = 0; i < active.Count; i++) {
			var note = active[i];
			if (!note.Destroyed)
				note.Judge();
		}

		player.UpdateFrame(this, (float)DeltaTime);
		player.EndFrame();
	}

	public UnbeatableWhiteLabelTwinKind UpcomingTwins() {
		if (UpcomingDodgeTop != null || UpcomingDodgeLow != null)
			return UnbeatableWhiteLabelTwinKind.None;
		if (upcoming.ContainsKey((UnbeatableWhiteLabelPathways.Mid, player.Side)))
			return UnbeatableWhiteLabelTwinKind.None;
		if (!upcoming.TryGetValue((UnbeatableWhiteLabelPathways.Top, player.Side), out var top) || !upcoming.TryGetValue((UnbeatableWhiteLabelPathways.Low, player.Side), out var low))
			return UnbeatableWhiteLabelTwinKind.None;

		bool topHold = top is UnbeatableWhiteLabelHoldNote;
		bool lowHold = low is UnbeatableWhiteLabelHoldNote;
		if (topHold && lowHold)
			return top.HitTime == low.HitTime ? UnbeatableWhiteLabelTwinKind.Hold : UnbeatableWhiteLabelTwinKind.None;
		if (!topHold && !lowHold && top.TargetTime == low.TargetTime)
			return UnbeatableWhiteLabelTwinKind.Twin;
		return UnbeatableWhiteLabelTwinKind.None;
	}

	public bool WithinHitRange(double time) => !PastHitRange(time) && Position >= time - UnbeatableWhiteLabelJudgement.LENIENCY;

	void Count(UnbeatableWhiteLabelNote note, UnbeatableWhiteLabelJudgement judgement) {
		note.Judgement = judgement;
		score.Add(judgement);
		LastJudgement = judgement;
		LastJudgementTime = level.Realtime;
	}

	void UpdateUpcoming() {
		upcoming.Clear();
		UpcomingDodgeLow = null;
		UpcomingDodgeTop = null;
		UnbeatableWhiteLabelSpamNote? spam = null;

		foreach (var note in chart.Active) {
			if (note.Destroyed)
				continue;

			if (note is UnbeatableWhiteLabelDodgeNote dodge) {
				if (dodge.IsPast)
					continue;
				if (dodge.CurrentPathway == UnbeatableWhiteLabelPathways.Top)
					UpcomingDodgeTop = dodge;
				else if (dodge.CurrentPathway == UnbeatableWhiteLabelPathways.Low)
					UpcomingDodgeLow = dodge;
				continue;
			}

			if (note is UnbeatableWhiteLabelSpamNote spamNote)
				spam = spamNote;

			if (!note.Killable || note.Dead)
				continue;

			var key = (note.CurrentPathway, note.Side);
			if (!upcoming.TryGetValue(key, out var current))
				upcoming[key] = note;
			else if (note.TargetTime < current.TargetTime && !note.IsPast && !PastHitRange(note.TargetTime))
				upcoming[key] = note;
		}

		var topKey = (UnbeatableWhiteLabelPathways.Top, player.Side);
		var lowKey = (UnbeatableWhiteLabelPathways.Low, player.Side);
		if (UpcomingDodgeTop != null && upcoming.TryGetValue(topKey, out var upcomingTop) && UpcomingDodgeTop.TargetTime < upcomingTop.TargetTime)
			upcoming.Remove(topKey);
		if (UpcomingDodgeLow != null && upcoming.TryGetValue(lowKey, out var upcomingLow) && UpcomingDodgeLow.TargetTime < upcomingLow.TargetTime)
			upcoming.Remove(lowKey);

		if (spam == null)
			return;

		var spamKey = (spam.CurrentPathway, spam.Side);
		if (PressedTop && upcoming.TryGetValue(topKey, out var top) && top.TargetTime < spam.TargetTime)
			upcoming.Remove(spamKey);
		if (PressedLow && upcoming.TryGetValue(lowKey, out var low) && low.TargetTime < spam.TargetTime)
			upcoming.Remove(spamKey);
	}
}
