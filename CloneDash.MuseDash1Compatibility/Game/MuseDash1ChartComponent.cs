using CloneDash.Common;
using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.Common.Scenes;
using CloneDash.Game.Entities;
using CloneDash.Game.Events;
using CloneDash.Game.Statistics;
using CloneDash.MD1_Compat.Game.Events;
using CloneDash.Scenes;
using Nucleus;
using Nucleus.Commands;
using Nucleus.Types;

namespace CloneDash.Game;

[MarkForStaticConstruction]
public class MuseDash1ChartComponent : IRhythmGameComponent, IRhythmGameThinker
{
	public static readonly ConVar md1_debugevents = new ConVar(nameof(md1_debugevents), "0", 0, "Prints event debugging to console.");

	public HashSet<DashEvent> ActiveEvents = [];
	public readonly TimeBasedChunkedManager<DashEnemy> EnemyManager = new();
	public int EnemySortIndexCounter;
	public readonly TimeBasedChunkedManager<DashEvent> EventManager = new();
	public readonly List<DashEvent> Events = [];
	public int EventSortIndexCounter;
	public readonly HashSet<DashEvent> HandledEvents = [];

	readonly Conductor Conductor;
	readonly MD1_GamemodeData data;
	readonly GameEventManager events;
	readonly HitsoundComponent Hitsounds;
	readonly RhythmGameLevel level;
	readonly List<DashEnemy> ReadyToBuildEntities = [];
	readonly List<DashEvent> ReadyToBuildEvents = [];
	readonly ResultComponent Results;
	readonly List<SceneChange> sceneChanges = [];
	readonly MuseDash1ScenesComponent scenes;
	readonly ScrollComponent Scroll;
	readonly StatisticsData Stats;

	DashEventComponents eventComponents = null!;

	public MuseDash1ChartComponent(Conductor conductor, MD1_GamemodeData data, GameEventManager events, HitsoundComponent hitsounds, RhythmGameLevel level, ResultComponent results, MuseDash1ScenesComponent scenes, ScrollComponent scroll, StatisticsData stats) {
		Conductor = conductor;
		this.data = data;
		this.events = events;
		Hitsounds = hitsounds;
		this.level = level;
		Results = results;
		this.scenes = scenes;
		Scroll = scroll;
		Stats = stats;

		events.Listen<SceneChangedGameEvent>(RelaySceneChange);
	}

	public Boss Boss { get; private set; } = null!;

	public void ActivateEvent(DashEvent ev) {
		ActiveEvents.Add(ev);
		ev.Activate();
		if (md1_debugevents.GetBool() && !level.IsSeeking && ev.ShouldDebug()) {
			if (ev.Length == 0)
				Logs.Debug($"Triggering {ev.ToString()}");
			else
				Logs.Debug($"Activating {ev.ToString()}");
		}
	}

	public void BroadcastEntitySignal(DashEnemy? entityFrom, EntitySignalType signalType, object? data = null) {
		foreach (var enemy in EnemyManager.GetAll())
			enemy.OnSignalReceived(entityFrom, signalType, data);
	}

	public void DeactivateEvent(DashEvent ev) {
		HandledEvents.Add(ev);
		ActiveEvents.Remove(ev);
		ev.Deactivate();
		if (md1_debugevents.GetBool() && !level.IsSeeking && ev.ShouldDebug()) {
			if (ev.Length != 0)
				Logs.Debug($"Deactivating {ev.GetType().Name}");
		}
	}

	public IMuseDash1SceneInstance GetSceneAtTime(double time) {
		for (int i = sceneChanges.Count - 1; i >= 0; i--) {
			var sceneChange = sceneChanges[i];
			if (time >= sceneChange.Time)
				return scenes.GetAllScenes()[sceneChange.ArrayIdx];
		}
		return scenes.GetAllScenes()[0];
	}

	public void IterateEvents() {
		foreach (var ev in EventManager.GetLastVisible()) {
			if (ActiveEvents.Contains(ev)) {
				// Determine if the event needs to be deactivated
				if (shouldDeactivateEvent(ev))
					DeactivateEvent(ev);
			}
			else if (!HandledEvents.Contains(ev)) {
				// Determine if the event needs to be activated
				if (shouldActivateEvent(ev))
					ActivateEvent(ev);
			}
			// The event has both been activated and deactivated, so its ignored
		}
	}

	public void Load() {
		Boss = level.Component<Boss>();
		eventComponents = level.Component<DashEventComponents>();

		if (SceneMod.Instance.GetActive() == null) {
			foreach (var sceneChange in data.SceneChanges) {
				ISceneDescriptor? sceneDescToChangeTo = SceneMod.Instance.GetDescriptorByUID(sceneChange.SceneUID);

				var sceneChangeInstance = scenes.AddOrGetScene(sceneDescToChangeTo);
				if (sceneChangeInstance != null) {
					var ev = new SceneChange(eventComponents, sceneChangeInstance.GetSceneArrayIndex());
					Events.Add(ev);
					ReadyToBuildEvents.Add(ev);
					EventManager.Add(ev);
					ev.Time = sceneChange.Time;
					sceneChanges.Add(ev);
				}
			}
		}

		sceneChanges.Add(new(eventComponents, 0));

		using (StaticSequentialProfiler.StartStackFrame("Load Enemies")) {
			Boss.PreBuildVisuals();
			foreach (var scene in scenes.GetAllScenes())
				Boss.BuildForScene(scene);

			foreach (var ent in data.Entities)
				LoadEntity(ent);

			foreach (var ev in data.Events)
				LoadEvent(ev);

			BuildQueues();
			Boss.PreBuildVisuals();
			foreach (var scene in scenes.GetAllScenes())
				Boss.BuildForScene(scene);
			BuildQueues();
			LinkSameTimeNotes();

			Events.Sort((x, y) => x.Time.CompareTo(y.Time));
		}
	}

	/// <summary>
	/// Loads an entity from a <see cref="ChartEntity"/> representation, builds a <see cref="MapEntity"/> out of it, and adds it to <see cref="GameplayManager.Entities"/>.
	/// </summary>
	/// <param name="ChartEntity"></param>
	public virtual DashEnemy? LoadEntity(MD1_SongChartEntity ChartEntity) {
		Interlude.Spin(submessage: "Loading entities...");

		if (!DashEnemy.TryCreateFromType(this, level, ChartEntity.Type, out DashEnemy? ent)) {
			Console.WriteLine("No load entity handler for type " + ChartEntity.Type);
			return null;
		}

		ent.Pathway = ChartEntity.Pathway;
		ent.EnterDirection = ChartEntity.EnterDirection;
		ent.Variant = ChartEntity.Variant;

		ent.HitTime = ChartEntity.HitTime;
		ent.ShowTime = ChartEntity.ShowTime;
		ent.Length = ChartEntity.Length;
		ent.Speed = ChartEntity.Speed;
		ent.Dt = ChartEntity.Dt;
		ent.Layer = ChartEntity.Layer;
		ent.ScrollSpeed = 13.87 / (ChartEntity.Dt <= 0 ? 1 : ChartEntity.Dt);
		ent.Flipped = ChartEntity.Flipped;
		ent.Blood = ChartEntity.Blood;
		ent.PostGreatRange = (float)(ChartEntity.LeftPerfectRange + ChartEntity.LeftGreatRange);
		ent.PostPerfectRange = (float)ChartEntity.LeftPerfectRange;
		ent.PreGreatRange = (float)(ChartEntity.RightPerfectRange + ChartEntity.RightGreatRange);
		ent.PrePerfectRange = (float)ChartEntity.RightPerfectRange;
		ent.SetTimeRemap(Scroll.GetRemap(ent));
		if (ent is SustainBeam beam)
			beam.Curve = ChartEntity.Curve;

		ent.FeverGiven = ChartEntity.Fever;
		ent.DamageTaken = ChartEntity.Damage;
		ent.ScoreGiven = ChartEntity.Score;
		ent.HealthGiven = ChartEntity.Health;

		ent.RelatedToBoss = ChartEntity.RelatedToBoss;
		ent.RendersItself = false;
		ent.DebuggingInfo = ChartEntity.DebuggingInfo;

		Stats.RegisterEnemy(ent);
		ReadyToBuildEntities.Add(ent);
		EnemyManager.Add(ent);

		return ent;
	}

	/// <summary>
	/// Loads an event from a <see cref="ChartEvent"/> representation, builds a <see cref="MapEvent"/> out of it, and adds it to  <see cref="GameplayManager.Events"/>.
	/// </summary>
	/// <param name="ChartEvent"></param>
	public void LoadEvent(MD1_SongChartEvent ChartEvent) {
		Interlude.Spin(submessage: "Loading events...");
		var ev = DashEvent.CreateFromType(eventComponents, ChartEvent.Type);

		ev.Time = ChartEvent.Time;
		ev.Length = ChartEvent.Length;

		ev.Score = ChartEvent.Score;
		ev.Fever = ChartEvent.Fever;
		ev.Damage = ChartEvent.Damage;
		ev.BossAction = ChartEvent.BossAction;

		Events.Add(ev);
		ReadyToBuildEvents.Add(ev);
		EventManager.Add(ev);
		// This is a hack... whatever
		if (ev is FlashbangEffect flash)
			eventComponents.Effects.Get<FlashbangScreenEffect>().AddFlashbang(flash);
	}

	public void Reset() {
		foreach (var entity in level.Entities) {
			if (entity is not DashEnemy entCD)
				continue;

			entCD.Reset();
		}

		Boss.Reset();
		ActiveEvents.Clear();
		HandledEvents.Clear();
	}

	public void SendEntitySignal(DashEnemy? entityFrom, DashEnemy? entityTo, EntitySignalType signalType, object? data = null) {
		entityTo?.OnSignalReceived(entityFrom, signalType, data);
	}

	public void Think(FrameState frameState) {
		EnemyManager.Rebuild(Conductor.Time);
		EventManager.Rebuild(Conductor.Time);

		var lastEntity = EnemyManager.GetLast();

		if (lastEntity != null && lastEntity.GetJudgementHitTime() + lastEntity.Length < Conductor.Time && !Results.LastNoteHit) {
			Results.MarkLastNoteHit();
			if (Stats.CalculateFullCombo()) {
				Logs.Info("Full combo achieved.");
				Hitsounds.Play(SceneSound.FullCombo, 0);
			}
		}

		IterateEvents();
	}

	void BuildQueues() {
		// This prepares the arrays...
		foreach (var ent in ReadyToBuildEntities) {
			if (ent is DashEnemy dashEnemy)
				dashEnemy.PreBuildVisuals();
		}

		// ...and then this builds the visuals in those arrays
		foreach (var scene in scenes.GetAllScenes()) {
			foreach (var ent in ReadyToBuildEntities) {
				if (ent is DashEnemy dashEnemy)
					dashEnemy.BuildForScene(scene);
			}
		}

		ReadyToBuildEntities.Clear();
		foreach (var ev in ReadyToBuildEvents)
			ev.Build();

		ReadyToBuildEvents.Clear();

		// If new entities exist, build those then (events might create entities)
		if (ReadyToBuildEntities.Count != 0)
			BuildQueues();
	}

	bool IsAirSide(DashEnemy note) => data.Layout.IsAirSide(data.Layout.Clamp(note.Pathway).First);

	bool IsSameTimeCandidate(DashEnemy note) => data.Layout.Clamp(note.Pathway).Count == 1
		&& (note.Type & (MuseDash1EntityType.Single | MuseDash1EntityType.Double | MuseDash1EntityType.Ghost | MuseDash1EntityType.SustainBeam | MuseDash1EntityType.Hammer | MuseDash1EntityType.Raider)) != 0;

	void LinkSameTimeNotes() {
		Dictionary<double, List<DashEnemy>> notesByTime = [];
		foreach (var note in EnemyManager.GetAll()) {
			if (!IsSameTimeCandidate(note))
				continue;

			if (!notesByTime.TryGetValue(note.HitTime, out var notes))
				notesByTime[note.HitTime] = notes = [];
			notes.Add(note);
		}

		foreach (var notes in notesByTime.Values)
			foreach (var note in notes)
				foreach (var other in notes)
					if (IsAirSide(note) != IsAirSide(other))
						note.SameTimeNotes.Add(other);
	}

	void RelaySceneChange(ref SceneChangedGameEvent ev) {
		BroadcastEntitySignal(null, EntitySignalType.SceneChange, (ev.From, ev.To));
	}

	private bool shouldActivateEvent(DashEvent ev) => ev.TriggerType switch {
		EventTriggerType.AtTimeMinusLength => Conductor.Time >= (ev.Time - ev.Length),
		EventTriggerType.AtTime => Conductor.Time >= ev.Time,
		_ => false
	};

	private bool shouldDeactivateEvent(DashEvent ev) => ev.TriggerType switch {
		EventTriggerType.AtTimeMinusLength => Conductor.Time >= ev.Time,
		EventTriggerType.AtTime => Conductor.Time >= (ev.Time + ev.Length),
		_ => false
	};
}
