using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Game;
using CloneDash.Unbeatable.Internal;
using Nucleus.Types;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public sealed record UnbeatableWhiteLabelNoteComponents(Conductor Conductor, UnbeatableWhiteLabelJudgeComponent Judge, INoteRules<UnbeatableWhiteLabelNote> NoteRules, UnbeatableWhiteLabelPlayerComponent Player, ScrollComponent Scroll);

public abstract class UnbeatableWhiteLabelNote : RhythmNote
{
	protected UnbeatableWhiteLabelNote(NoteInfo info, UnbeatableWhiteLabelNoteComponents components) : base(components.Conductor, components.Scroll) {
		Components = components;
		Info = info;
		Side = info.side;
		InitialPathway = UnbeatableWhiteLabelPathways.FromHeight(info.height);
		CurrentPathway = InitialPathway;
		Pathway = InitialPathway.AsSet;
		HitTime = info.time / 1000d;
		TargetTime = HitTime;
		Interactivity = EntityInteractivity.Hit;
	}

	public PathwayId CurrentPathway { get; protected set; }
	public double DeathTime { get; private set; }
	public bool Destroyed { get; set; }
	public NoteInfo Info { get; }
	public PathwayId InitialPathway { get; }
	public bool IsPast { get; protected set; }
	public UnbeatableWhiteLabelJudgement Judgement { get; set; }
	public virtual bool Killable => true;
	public Side Side { get; }
	public bool Spawned { get; set; }
	public double TargetTime { get; protected set; }
	public virtual bool Travelling => true;

	protected UnbeatableWhiteLabelNoteComponents Components { get; }

	public virtual double? GetAutoplayTime() => Dead ? null : TargetTime;

	public abstract void Judge();

	public void MarkDead() => DeathTime = Components.Judge.Position;

	public override void OnReset() {
		CurrentPathway = InitialPathway;
		DeathTime = 0;
		Destroyed = false;
		IsPast = false;
		Judgement = default;
		Spawned = false;
		TargetTime = HitTime;
	}

	public override bool VisTest(float gamewidth, float gameheight, float xPosition) => Spawned && !Destroyed;

	protected void CountHit(UnbeatableWhiteLabelJudgement judgement) => CountHit(judgement, CurrentPathway);

	protected void CountHit(UnbeatableWhiteLabelJudgement judgement, PathwayId pathway) {
		Components.Judge.Hit(pathway);
		Components.Judge.CountHit(this, judgement);
	}

	protected void CountMiss(UnbeatableWhiteLabelJudgement judgement) => Components.Judge.CountMiss(this, judgement);

	protected void Hit() => Components.Judge.Hit(CurrentPathway);

	protected bool IsSwungAt() => Components.Judge.IsUpcoming(this) && PlayerCanHit() && PlayerSwung();

	protected UnbeatableWhiteLabelJudgement Judged(bool miss) => new(Components.Judge.Position, HitTime, miss);

	protected bool PastHitRange() => Components.Judge.PastHitRange(TargetTime);

	protected bool PastHitRange(double time) => Components.Judge.PastHitRange(time);

	protected bool PlayerCanHit() {
		var judge = Components.Judge;
		if (CurrentPathway == UnbeatableWhiteLabelPathways.Mid)
			return true;
		if (judge.PressedLow && CurrentPathway == UnbeatableWhiteLabelPathways.Low)
			return true;
		return judge.PressedTop && CurrentPathway == UnbeatableWhiteLabelPathways.Top;
	}

	protected bool PlayerSwung() => Components.Judge.PressedAny && !Components.Player.DidHitAt(CurrentPathway);

	protected bool WithinHitRange() => Components.Judge.WithinHitRange(TargetTime);

	protected bool WithinHitRange(double time) => Components.Judge.WithinHitRange(time);

	protected override bool CanAffectPlayer() => Components.NoteRules.CanAffectPlayer(this);
	protected override Vector2F GetAnchorPosition() => new(Side.Pick(-UnbeatableWhiteLabelPathways.TARGET_X, UnbeatableWhiteLabelPathways.TARGET_X), UnbeatableWhiteLabelPathways.GetPathwayY(InitialPathway));
	protected override void OnDamagePlayer() => Components.NoteRules.Damage(this);
	protected override void OnKill() => Components.NoteRules.Kill(this);
	protected override void OnPunishment() => Components.NoteRules.Punish(this);
	protected override void OnRewardPlayer() => Components.NoteRules.Reward(this);
}
