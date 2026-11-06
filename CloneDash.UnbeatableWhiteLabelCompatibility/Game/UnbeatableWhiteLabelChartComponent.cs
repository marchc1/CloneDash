using CloneDash.Common.Game;
using CloneDash.Game;
using CloneDash.Settings;
using CloneDash.Unbeatable.Internal;
using Microsoft.Extensions.DependencyInjection;
using Nucleus;
using Nucleus.Commands;
using Nucleus.Types;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

[MarkForStaticConstruction]
public class UnbeatableWhiteLabelChartComponent(Beatmap beatmap, Conductor conductor, RhythmGameLevel level, UnbeatableWhiteLabelPlayerComponent player, ResultComponent results) : IRhythmGameComponent, IRhythmGameThinker
{
	public static readonly ConVar ubwl_scrollspeed = new(nameof(ubwl_scrollspeed), 4, FCvar.Saved, "", 0, 9);

	const double DEAD_NOTE_LIFETIME = 1;
	const double PAST_NOTE_PROGRESS = 1.33;

	readonly List<UnbeatableWhiteLabelNote> active = [];
	readonly List<UnbeatableWhiteLabelNote> notes = [];
	int nextFlip;
	int nextNote;

	public IReadOnlyList<UnbeatableWhiteLabelNote> Active => active;
	public FlipInfo? NextFlip => nextFlip < beatmap.flips.Count ? beatmap.flips[nextFlip] : null;
	public double Position => conductor.Time - InputSettings.JudgementOffset;
	public double TravelTime { get; private set; } = 1;

	public double GetProgress(UnbeatableWhiteLabelNote note) => (Position - (note.HitTime - TravelTime)) / TravelTime;

	public void Load() {
		TravelTime = 5d / (ubwl_scrollspeed.GetInt() + 1);

		UnbeatableWhiteLabelFreestyleNote? freestyleParent = null;
		foreach (var info in beatmap.notes) {
			var note = CreateNote(info);
			note.ShowTime = note.HitTime - TravelTime;
			note.ScrollSpeed = info.side.Pick(-1, 1) * (UnbeatableWhiteLabelPathways.SPAWN_X - UnbeatableWhiteLabelPathways.TARGET_X) / TravelTime;

			if (note is UnbeatableWhiteLabelFreestyleNote freestyle) {
				if (freestyleParent != null && freestyleParent.Side == freestyle.Side)
					freestyleParent.AddChild(freestyle);
				else
					freestyleParent = freestyle;
			}
			else
				freestyleParent = null;

			notes.Add(note);
		}
	}

	public void Reset() {
		foreach (var note in notes)
			note.Reset();
		active.Clear();
		nextFlip = 0;
		nextNote = 0;
	}

	public void SeekTo(double time) {
		while (nextFlip < beatmap.flips.Count && beatmap.flips[nextFlip].time / 1000d < time)
			ApplyFlip(beatmap.flips[nextFlip++]);

		while (nextNote < notes.Count && notes[nextNote].HitTime < time) {
			var note = notes[nextNote++];
			note.Spawned = true;
			note.Destroyed = true;
			note.SoftKill();
		}
	}

	public void Think(FrameState frameState) {
		double position = Position;

		while (nextFlip < beatmap.flips.Count && position >= beatmap.flips[nextFlip].time / 1000d)
			ApplyFlip(beatmap.flips[nextFlip++]);

		while (nextNote < notes.Count && (notes[nextNote].Spawned || position >= notes[nextNote].ShowTime)) {
			var note = notes[nextNote++];
			if (note.Spawned)
				continue;

			Spawn(note);
			if (note is UnbeatableWhiteLabelFreestyleNote freestyle)
				foreach (var child in freestyle.Children)
					Spawn(child);
		}

		if (nextNote >= notes.Count && !results.LastNoteHit)
			results.MarkLastNoteHit();

		for (int i = active.Count - 1; i >= 0; i--) {
			var note = active[i];
			if (!note.Destroyed) {
				if (note.Dead)
					note.Destroyed = position - note.DeathTime >= DEAD_NOTE_LIFETIME;
				else if (note.Travelling)
					note.Destroyed = GetProgress(note) >= PAST_NOTE_PROGRESS;
			}

			if (note.Destroyed)
				active.RemoveAt(i);
		}
	}

	void ApplyFlip(FlipInfo flip) {
		if (flip.toggleCenter)
			player.CameraCentered = !player.CameraCentered;
		else
			player.ChangeSide(player.Side.GetOpposite());
	}

	UnbeatableWhiteLabelNote CreateNote(NoteInfo info) {
		Type type = info.type switch {
			NoteType.Dodge => typeof(UnbeatableWhiteLabelDodgeNote),
			NoteType.Double => typeof(UnbeatableWhiteLabelDoubleNote),
			NoteType.Freestyle => typeof(UnbeatableWhiteLabelFreestyleNote),
			NoteType.Hold => typeof(UnbeatableWhiteLabelHoldNote),
			NoteType.Spam => typeof(UnbeatableWhiteLabelSpamNote),
			_ => typeof(UnbeatableWhiteLabelDefaultNote)
		};
		return level.Add((UnbeatableWhiteLabelNote)ActivatorUtilities.CreateInstance(level, type, info));
	}

	void Spawn(UnbeatableWhiteLabelNote note) {
		note.Spawned = true;
		active.Add(note);
	}
}
