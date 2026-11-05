using CloneDash.Common.Game;
using CloneDash.Compatibility.Unity;
using CloneDash.Game;
using CloneDash.Unbeatable.Internal;
using Nucleus.Types;
using System.Numerics;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public class UnbeatableWhiteLabelPlayfieldComponent(UnbeatableWhiteLabelChartComponent chart, Conductor conductor, UnbeatableWhiteLabelJudgeComponent judge, UnbeatableWhiteLabelPlayerComponent player, UnbeatableWhiteLabelStageComponent stage) : IRhythmGameComponent, IRhythmGameRenderer
{
	sealed class NoteVisuals
	{
		public NoteVisuals(SceneObject root) {
			Root = root;
			RootScale = root.Transform.LocalScaleX;
			Sprite = UnbeatableWhiteLabelStageComponent.Find(root, "Sprite");
			SpriteRenderer = Sprite?.GetComponent<SceneSpriteRenderer>();
			SpriteColor = SpriteRenderer?.Color ?? Vector4.One;
			Animator = new(Sprite?.GetComponent<SceneAnimator>());
			StaticCircle = UnbeatableWhiteLabelStageComponent.Find(root, "Static Circle");
			ShrinkingCircle = UnbeatableWhiteLabelStageComponent.Find(root, "Shrinking Circle");
			StaticScale = StaticCircle?.Transform.LocalScaleX ?? 1;
			ShrinkScale = ShrinkingCircle?.Transform.LocalScaleX ?? 1;
			Trail = UnbeatableWhiteLabelStageComponent.Find(root, "Trail")?.GetComponent<SceneSpriteRenderer>();
			End = UnbeatableWhiteLabelStageComponent.Find(root, "End");
			BackStar = UnbeatableWhiteLabelStageComponent.Find(root, "End (1)");
			FrontStar = UnbeatableWhiteLabelStageComponent.Find(root, "End (2)");
		}

		public readonly UnbeatableWhiteLabelAnimatorState Animator;
		public readonly SceneObject? BackStar;
		public readonly SceneObject? End;
		public readonly SceneObject? FrontStar;
		public readonly SceneObject Root;
		public readonly float RootScale;
		public readonly SceneObject? ShrinkingCircle;
		public readonly float ShrinkScale;
		public readonly SceneObject? Sprite;
		public readonly Vector4 SpriteColor;
		public readonly SceneSpriteRenderer? SpriteRenderer;
		public readonly SceneObject? StaticCircle;
		public readonly float StaticScale;
		public readonly SceneSpriteRenderer? Trail;
		public bool Dead;
		public int Hits;
	}

	const float CIRCLE_PROGRESS_START = 0.6f;
	const float DEATH_GRAVITY = 65f;
	const float DEATH_HIT_PAUSE = 0.05f;
	const float DEATH_LIFETIME = 1f;
	const float DODGE_FADE_TIME = 0.25f;
	const float FLIP_WARNING_BEATS = 2;
	const float FLIP_WARNING_FADE = 0.2f;
	const float HIDE_DISTANCE = 0.5f;
	const float STAR_SPEED = 90f;

	static readonly Vector4 CircleColor = new(1, 0.23f, 0.48f, 1);
	static readonly Vector4 HitRangeColor = new(1, 73 / 255f, 114 / 255f, 1);

	readonly Dictionary<UnbeatableWhiteLabelNote, NoteVisuals> noteVisuals = [];
	UnbeatableWhiteLabelAnimatorState? clef;
	UnbeatableWhiteLabelAnimatorState? treble;
	FlipInfo? warnedFlip;
	double warningStart;

	public void Render(FrameState frameState) {
		double time = conductor.Time;

		RenderBand(time);
		stage.Render(stage.LeftGuide);
		stage.Render(stage.RightGuide);
		RenderFlipWarning(time);
		RenderQuaver();

		var active = chart.Active;
		for (int i = active.Count - 1; i >= 0; i--)
			RenderNote(active[i], time);
		RemoveFinishedNotes();

		RenderBeat();
	}

	public void Reset() {
		foreach (var visuals in noteVisuals.Values)
			SceneObject.Destroy(visuals.Root);
		noteVisuals.Clear();
		clef?.Reset();
		treble?.Reset();
		warnedFlip = null;
	}

	static Vector4 Fade(Vector4 color, float alpha) => color with { W = color.W * alpha };

	static string GetHitState(UnbeatableWhiteLabelNote note) {
		bool first = note.HitTime % 2 < 1;
		return note switch {
			UnbeatableWhiteLabelDoubleNote => "DefaultDoubleEnemyScream",
			UnbeatableWhiteLabelFreestyleNote => "DefaultFreestyleEnemyHit1",
			UnbeatableWhiteLabelHoldNote => first ? "DefaultHoldEnemyFrontHit1" : "DefaultHoldEnemyFrontHit2",
			UnbeatableWhiteLabelSpamNote => "DefaultSpamEnemyHit1",
			_ => note.InitialPathway == UnbeatableWhiteLabelPathways.Top
				? (first ? "DefaultBaseEnemyAirHit1" : "DefaultBaseEnemyAirHit2")
				: (first ? "DefaultBaseEnemyGroundHit1" : "DefaultBaseEnemyGroundHit2")
		};
	}

	static float GetTargetX(Side side) => side.Pick(-UnbeatableWhiteLabelPathways.TARGET_X, UnbeatableWhiteLabelPathways.TARGET_X);

	static void Place(SceneObject? obj, float x, float y, float rotation = 0) {
		if (obj == null)
			return;

		var transform = obj.Transform;
		transform.LocalX = x;
		transform.LocalY = y;
		transform.LocalZ = 0;
		float angle = rotation * MathF.PI / 360;
		transform.LocalRotationX = 0;
		transform.LocalRotationY = 0;
		transform.LocalRotationZ = MathF.Sin(angle);
		transform.LocalRotationW = MathF.Cos(angle);
	}

	static void SetColor(SceneObject? obj, Vector4 color) {
		var renderer = obj?.GetComponent<SceneSpriteRenderer>();
		if (renderer != null)
			renderer.Color = color;
	}

	static void SetScale(SceneObject? obj, float scale) {
		if (obj == null)
			return;

		obj.Transform.LocalScaleX = scale;
		obj.Transform.LocalScaleY = scale;
	}

	NoteVisuals? GetVisuals(UnbeatableWhiteLabelNote note) {
		if (noteVisuals.TryGetValue(note, out var visuals))
			return visuals;

		string prefab = note switch {
			UnbeatableWhiteLabelDodgeNote => "Dodge Note",
			UnbeatableWhiteLabelDoubleNote => "Double Note",
			UnbeatableWhiteLabelFreestyleNote => "Freestyle Note",
			UnbeatableWhiteLabelHoldNote => "Hold Note",
			UnbeatableWhiteLabelSpamNote => "Spam Note",
			_ => "Default Note"
		};

		if (stage.Instantiate(prefab) is not SceneObject root)
			return null;

		return noteVisuals[note] = new(root);
	}

	void RemoveFinishedNotes() {
		List<UnbeatableWhiteLabelNote>? finished = null;
		foreach (var (note, visuals) in noteVisuals) {
			if (!note.Destroyed && note.Spawned)
				continue;

			SceneObject.Destroy(visuals.Root);
			(finished ??= []).Add(note);
		}

		if (finished != null)
			foreach (var note in finished)
				noteVisuals.Remove(note);
	}

	void RenderBand(double time) {
		clef ??= new(UnbeatableWhiteLabelStageComponent.Find(stage.Clef, "ClefSprite")?.GetComponent<SceneAnimator>());
		treble ??= new(UnbeatableWhiteLabelStageComponent.Find(stage.Treble, "TrebleSprite")?.GetComponent<SceneAnimator>());

		bool playing = time >= 0;
		clef.Play(playing ? "ClefCombatBackgroundPlay" : "ClefCombatBackgroundIdle");
		treble.Play(playing ? "TrebleCombatBackgroundPlay" : "TrebleCombatBackgroundIdle");
		stage.Render(stage.Clef);
		stage.Render(stage.Treble);
	}

	void RenderBeat() {
		if (stage.Player == null)
			return;

		Place(stage.Player, player.Beat.X, player.Beat.Y);
		var beat = UnbeatableWhiteLabelStageComponent.Find(stage.Player, "Beat")?.GetComponent<SceneSpriteRenderer>();
		if (beat != null)
			beat.FlipX = player.Side == Side.Left;
		stage.Render(stage.Player);
	}

	void RenderCircles(NoteVisuals visuals, UnbeatableWhiteLabelNote note) {
		float progress = (float)chart.GetProgress(note);
		bool visible = !note.Info.hiding && progress > CIRCLE_PROGRESS_START && !note.Dead;
		bool inRange = judge.WithinHitRange(note.TargetTime);
		float t = Math.Clamp((progress - CIRCLE_PROGRESS_START) / (1 - CIRCLE_PROGRESS_START), 0, 1);

		SetColor(visuals.StaticCircle, Fade(CircleColor, visible ? (inRange ? 0.5f : 0.2f) : 0));
		SetColor(visuals.ShrinkingCircle, Fade(CircleColor, visible ? (inRange ? 1f : 0.4f) : 0));
		SetScale(visuals.ShrinkingCircle, visuals.ShrinkScale + (visuals.StaticScale - visuals.ShrinkScale) * t);
	}

	void RenderDeadNote(UnbeatableWhiteLabelNote note, NoteVisuals visuals) {
		float age = (float)(chart.Position - note.DeathTime);
		if (age >= DEATH_LIFETIME)
			return;

		float side = note.Side.Pick(-1f, 1f);
		float x = GetTargetX(note.Side), y = UnbeatableWhiteLabelPathways.GetPathwayY(note.InitialPathway);
		float rotation = 0;
		float flight = Math.Max(0, age - DEATH_HIT_PAUSE);
		if (flight > 0) {
			float start = 20 * side, end = 2 * side;
			float slowdown = Math.Abs(start - end) / 80;
			float slowing = Math.Min(flight, slowdown);
			x += start * slowing - side * 40 * slowing * slowing + end * Math.Max(0, flight - slowdown);
			y += 16 * flight - DEATH_GRAVITY / 2 * flight * flight;
			rotation = 200 * side * flight * flight;
		}

		RenderCircles(visuals, note);
		if (visuals.Trail != null)
			visuals.Trail.Color = Fade(visuals.Trail.Color, 0);
		if (visuals.End != null)
			visuals.End.Active = false;
		Place(visuals.Root, x, y, rotation);
		stage.Render(visuals.Root);
	}

	void RenderFlipWarning(double time) {
		var flip = chart.NextFlip;
		bool warning = flip != null && !flip.toggleCenter && !player.CameraCentered && chart.Position >= flip.time / 1000d - FLIP_WARNING_BEATS * 60 / Math.Max(1, conductor.BPM);
		if (!warning) {
			warnedFlip = null;
			return;
		}

		if (warnedFlip != flip) {
			warnedFlip = flip;
			warningStart = time;
		}

		float height = 1;
		var next = flip!.nextNote;
		if (flip.twinNote != null || next == null || next.height == Height.Mid)
			height = UnbeatableWhiteLabelPathways.HEIGHT_OFFSET / 2 + 0.5f;
		else if (next.height == Height.Top)
			height = UnbeatableWhiteLabelPathways.HEIGHT_OFFSET;

		var indicator = UnbeatableWhiteLabelStageComponent.Find(stage.Master, player.Side == Side.Right ? "upNextIndicatorLeft" : "upNextIndicatorRight");
		Place(indicator, player.Side.Pick(-1f, 1f) * (UnbeatableWhiteLabelPathways.SPAWN_X + UnbeatableWhiteLabelPathways.PLAYER_REST_X) / 2.5f, height);
		SetColor(indicator, new(1, 0.97f, 0.9f, Math.Clamp((float)(time - warningStart) / FLIP_WARNING_FADE, 0, 1)));
		stage.Render(indicator);
	}

	void RenderHold(UnbeatableWhiteLabelHoldNote hold, NoteVisuals visuals, float y, Vector4 color, double time) {
		float targetX = GetTargetX(hold.Side);
		float headX = hold.Holding ? targetX : (float)hold.XPosFromTimeOffset();
		float tailX = (float)hold.XPosFromTimeOffset((float)(hold.EndTime - hold.HitTime));
		if (hold.Holding && hold.Side.Pick(tailX > targetX, tailX < targetX))
			tailX = targetX;

		float length = Math.Abs(tailX - headX) / visuals.RootScale;
		if (visuals.Trail != null) {
			visuals.Trail.Size = visuals.Trail.Size with { X = length };
			visuals.Trail.FlipX = hold.Side == Side.Left;
		}
		if (visuals.End != null) {
			visuals.End.Transform.LocalX = hold.Side.Pick(-length, length);
			var endRenderer = visuals.End.GetComponent<SceneSpriteRenderer>();
			if (endRenderer != null)
				endRenderer.FlipX = hold.Side == Side.Left;
		}

		float starRotation = (float)(time * STAR_SPEED);
		Place(visuals.BackStar, 0, 0, starRotation);
		Place(visuals.FrontStar, 0, 0, starRotation);

		visuals.Animator.Play(hold.Holding ? "DefaultHoldEnemyFrontBlock" : "DefaultHoldEnemyFrontRun");
		RenderSprite(visuals, hold, color);
		RenderCircles(visuals, hold);
		Place(visuals.Root, headX, y);
		stage.Render(visuals.Root);
	}

	void RenderNote(UnbeatableWhiteLabelNote note, double time) {
		var visuals = GetVisuals(note);
		if (visuals == null)
			return;

		float y = UnbeatableWhiteLabelPathways.GetPathwayY(note.InitialPathway);
		if (note.Info.spawnMid) {
			float midY = UnbeatableWhiteLabelPathways.GetPathwayY(UnbeatableWhiteLabelPathways.Mid);
			y = midY + (y - midY) * Math.Clamp((float)chart.GetProgress(note), 0, 1);
		}

		float x = (float)note.XPosFromTimeOffset();
		float targetX = GetTargetX(note.Side);
		bool hidden = note.Info.hiding && Math.Abs(x - targetX) <= HIDE_DISTANCE;
		var color = hidden ? Fade(visuals.SpriteColor, 0) : judge.WithinHitRange(note.TargetTime) && note.Killable ? HitRangeColor : visuals.SpriteColor;

		if (note.Dead && note.Killable) {
			if (!visuals.Dead)
				visuals.Animator.Play(GetHitState(note), true);
			visuals.Dead = true;
			RenderSprite(visuals, note, visuals.SpriteColor);
			RenderDeadNote(note, visuals);
			return;
		}

		switch (note) {
			case UnbeatableWhiteLabelDodgeNote dodge:
				float alpha = dodge.IsPast ? 1 - Math.Clamp((float)(chart.Position - dodge.TargetTime - UnbeatableWhiteLabelJudgement.LENIENCY) / DODGE_FADE_TIME, 0, 1) : 1;
				if (visuals.SpriteRenderer != null)
					visuals.SpriteRenderer.FlipY = dodge.CurrentPathway == UnbeatableWhiteLabelPathways.Top;
				RenderSprite(visuals, note, Fade(visuals.SpriteColor, alpha));
				Place(visuals.Root, x + note.Side.Pick(-0.5f, 0.5f), y);
				stage.Render(visuals.Root);
				return;
			case UnbeatableWhiteLabelHoldNote hold:
				RenderHold(hold, visuals, y, color, time);
				return;
			case UnbeatableWhiteLabelDoubleNote { Stunned: true } stunnedDouble:
				visuals.Animator.Play("DefaultDoubleEnemyScream");
				x = (float)note.XPosFromTimeOffset((float)(stunnedDouble.EndTime - note.HitTime));
				y = UnbeatableWhiteLabelPathways.GetPathwayY(stunnedDouble.CurrentPathway) + 1;
				break;
			case UnbeatableWhiteLabelSpamNote spam:
				if (spam.Swings != visuals.Hits) {
					visuals.Animator.Play($"DefaultSpamEnemyHit{spam.Swings % 3 + 1}", true);
					visuals.Hits = spam.Swings;
				}
				if (spam.Stunned)
					x = targetX;
				break;
			case UnbeatableWhiteLabelFreestyleNote { Parent: not null }:
				SetScale(visuals.Root, visuals.RootScale * 0.67f);
				color = Fade(visuals.SpriteColor, 0.6f);
				break;
			case UnbeatableWhiteLabelFreestyleNote freestyle:
				int hits = freestyle.Children.Count(child => child.Destroyed) + (freestyle.Stunned ? 1 : 0);
				if (hits != visuals.Hits) {
					visuals.Animator.Play($"DefaultFreestyleEnemyHit{hits % 3 + 1}", true);
					visuals.Hits = hits;
				}
				if (freestyle.Stunned)
					x = targetX;
				break;
		}

		RenderSprite(visuals, note, color);
		RenderCircles(visuals, note);
		Place(visuals.Root, x, y);
		stage.Render(visuals.Root);
	}

	void RenderQuaver() {
		if (stage.Assist == null)
			return;

		var assist = player.Quaver;
		Place(stage.Assist, assist.X, assist.Y);
		stage.Assist.Transform.LocalZ = 0.25f;

		var quaver = UnbeatableWhiteLabelStageComponent.Find(stage.Assist, "Quaver")?.GetComponent<SceneSpriteRenderer>();
		if (quaver == null)
			return;

		quaver.FlipX = assist.Side == Side.Left;
		quaver.Color = new(1, 1, 1, assist.Alpha);
		stage.Render(stage.Assist);
	}

	void RenderSprite(NoteVisuals visuals, UnbeatableWhiteLabelNote note, Vector4 color) {
		if (visuals.SpriteRenderer == null)
			return;

		visuals.SpriteRenderer.FlipX = note.Side == Side.Left;
		visuals.SpriteRenderer.Color = color;
	}
}
