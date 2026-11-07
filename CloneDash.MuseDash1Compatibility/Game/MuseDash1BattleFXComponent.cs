using CloneDash.Characters;
using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;
using CloneDash.Scenes;
using Nucleus;
using Nucleus.Common.Graphics;
using Nucleus.Particles;
using Nucleus.Types;
using System.Numerics;

namespace CloneDash.Game;

public class MuseDash1BattleFXComponent : IRhythmGameComponent, IRhythmGameEntityPostRenderer, IRhythmGameThinker
{
	const float ATTACK_AIR_Y = 1.28f;
	const float ATTACK_ROAD_Y = -1f;
	const float BONUS_AIR_Y = 0.94f;
	const float BONUS_ROAD_Y = -0.94f;
	const int CHARACTER_SORTING_ORDER = 5;
	const float ENEMY_HIT_X = -3.88f;
	const float PRESS_AIR_Y = 1.15f;
	const float SKILL_AIR_Y = 1.25f;
	const float SKILL_ROAD_Y = -0.75f;

	static readonly Dictionary<string, string> SkillEffects = new() {
		["char_1_racer"] = "racer_girl_normal_fx_skill",
		["char_1_rampage"] = "rampage_girl_fx_skill",
		["char_1_thief"] = "thief_girl_fx_skill",
		["char_2_joker"] = "joker_girl_fx_skill",
		["char_2_zombie"] = "zombie_girl_fx_skill",
		["char_5_neko"] = "neko_girl_fx_skill",
		["char_7_clear"] = "clear_girl_fx_skill",
		["char_9_amiya"] = "amiya_girl_fx_skill",
		["char_13_wisadel"] = "W_fx_skill"
	};
	static readonly Matrix4x4 UnityToWorld = Matrix4x4.CreateScale(1, -1, 1);

	readonly MuseDash1ChartComponent Chart;
	readonly Dictionary<DashEnemy, ParticleCollection> hearts = [];
	readonly ParticleSystemManager Particles = new();
	readonly PathwaysComponent Pathways;
	readonly HashSet<AssetStudio.SerializedFile> pinnedFiles = [];
	readonly ParticleCollection?[] presses = new ParticleCollection?[PathwayLayout.MAX_PATHWAYS];
	readonly MuseDash1ScenesComponent Scenes;
	readonly RhythmGameLevel level;
	readonly HashSet<DashEnemy> seenHearts = [];
	readonly Dictionary<AssetStudio.Texture2D, ITexture> textures = [];

	MuseDash1ParticleEffect? attackCrit;
	MuseDash1ParticleEffect? attackGreat;
	MuseDash1ParticleEffect? attackPerfect;
	MuseDash1ParticleEffect? heartOnNote;
	MuseDash1ParticleEffect? hpPickup;
	MuseDash1ParticleEffect? scorePickup;
	MuseDash1ParticleEffect? skill;

	public MuseDash1BattleFXComponent(MuseDash1ChartComponent chart, GameEventManager events, PathwaysComponent pathways, MuseDash1ScenesComponent scenes, RhythmGameLevel level) {
		Chart = chart;
		Pathways = pathways;
		Scenes = scenes;
		this.level = level;

		events.Listen<BonusCollectedGameEvent>(PlayBonusEffect);
		events.Listen<CharacterSkillGameEvent>(PlaySkillEffect);
		events.Listen<NoteHitGameEvent>(PlayHitEffects);
		events.Listen<SustainStateChangedGameEvent>(UpdatePressEffect);
	}

	public RhythmGameThinkPhase Phase => RhythmGameThinkPhase.Present;

	public void Load() {
		attackCrit = LoadEffect("girl_fx_atk_c");
		attackGreat = LoadEffect("girl_fx_atk_g");
		attackPerfect = LoadEffect("girl_fx_atk_p");
		heartOnNote = LoadEffect("hp_on_note");
		hpPickup = LoadEffect("fx_hp_ground");
		scorePickup = LoadEffect("fx_score_ground");

		var character = CharacterMod.Instance.GetActive()?.GetID();
		if (character != null && SkillEffects.TryGetValue(character, out var skillEffect))
			skill = LoadEffect(skillEffect);

		foreach (var scene in Scenes.GetAllScenes())
			for (int i = 0; i < Pathways.Layout.Count; i++) {
				var pathway = Pathways.Layout.PathwayAt(i);
				scene.GetEnemyHitEffect(EntityVariant.Small, pathway);
				scene.GetEnemyHitEffect(EntityVariant.Boss1, pathway);
				scene.GetPressEffect(pathway, false);
				scene.GetPressEffect(pathway, true);
			}

		foreach (var file in pinnedFiles)
			MuseDash1Compatibility.StreamingAssets.Release(file);
		pinnedFiles.Clear();
	}

	public void PostRenderEntities(FrameState frameState) {
		UpdateHeartEffects();
		Particles.Render(UnityToWorld, CHARACTER_SORTING_ORDER);
	}

	public void RenderBelowEntities() => Particles.Render(UnityToWorld, maxSortOrder: CHARACTER_SORTING_ORDER - 1);

	public void Reset() {
		Particles.Clear();
		hearts.Clear();
		Array.Clear(presses);
	}

	public void Think(FrameState frameState) {
		if (!level.Paused)
			Particles.Simulate((float)globals.CurTimeDelta);
	}

	MuseDash1ParticleEffect? LoadEffect(string name)
		=> MuseDash1ParticleEffect.Load(MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.GameObject>(name), LoadTexture,
			fileName => MuseDash1Compatibility.StreamingAssets.LoadDependenciesContaining(name, new HashSet<string> { fileName }, pinnedFiles));

	ITexture LoadTexture(AssetStudio.Texture2D texture) {
		if (!textures.TryGetValue(texture, out var result))
			textures[texture] = result = UnityAssetUtils.ConvertTexture(EngineCore.Level, texture);
		return result;
	}

	float PathwayY(PathwayId pathway, float roadY, float airY) => PathwayLayout.LerpY(Pathways.Layout[pathway].NormalizedY, roadY, airY);

	void Play(MuseDash1ParticleEffect? effect, in Matrix4x4 transform) {
		if (effect != null)
			Particles.Create(effect.Definition, transform);
	}

	void PlayBonus(DashEnemy note, PathwayId pathway) {
		var effect = note.Type == MuseDash1EntityType.Heart ? hpPickup : scorePickup;
		if (effect != null)
			Play(effect, effect.At(PathwayY(pathway, BONUS_ROAD_Y, BONUS_AIR_Y)));
	}

	void PlayBonusEffect(ref BonusCollectedGameEvent ev) => PlayBonus(ev.Note, ev.Pathway);

	void PlayHitEffects(ref NoteHitGameEvent ev) {
		if (ev.Note.Type is MuseDash1EntityType.Heart or MuseDash1EntityType.Score) {
			PlayBonus(ev.Note, ev.Pathway);
			return;
		}

		float y = PathwayY(ev.Pathway, ATTACK_ROAD_Y, ATTACK_AIR_Y);
		var attack = ev.InFever ? attackCrit : ev.Perfect ? attackPerfect : attackGreat;
		if (attack != null)
			Play(attack, attack.At(y));

		if (ev.Note.Type == MuseDash1EntityType.Single && Scenes.HasActiveScene(out var scene)) {
			var enemyHit = scene.GetEnemyHitEffect(ev.Note.Variant, ev.Pathway);
			if (enemyHit != null)
				Play(enemyHit, enemyHit.At(ENEMY_HIT_X, y));
		}
	}

	void PlaySkillEffect(ref CharacterSkillGameEvent ev) {
		if (skill != null)
			Play(skill, skill.At(PathwayY(ev.Pathway, SKILL_ROAD_Y, SKILL_AIR_Y)));
	}

	float PressY(IMuseDash1SceneInstance scene, PathwayId pathway, MuseDash1ParticleEffect effect, bool top)
		=> PathwayY(pathway, scene.GetPressEffect(Pathways.GroundPathway, top)?.Transform.M42 ?? effect.Transform.M42, PRESS_AIR_Y);

	void UpdateHeartEffects() {
		if (heartOnNote == null)
			return;

		seenHearts.Clear();
		foreach (var enemy in Chart.EnemyManager.GetLastVisible()) {
			if (!enemy.Blood || enemy.Dead)
				continue;

			var visuals = enemy.GetActiveVisuals();
			var mount = visuals?.MountedHeart;
			if (mount == null || visuals!.MountBone == null || visuals.MountedHeartAnimation == null)
				continue;

			var transform = heartOnNote.At(mount.Position.X, -mount.Position.Y);
			if (hearts.TryGetValue(enemy, out var heart))
				heart.SetControlPoint(0, transform);
			else {
				hearts[enemy] = heart = Particles.Create(heartOnNote.Definition, transform);
				heart.ReleaseWhenFinished = false;
			}
			seenHearts.Add(enemy);
		}

		foreach (var (enemy, heart) in hearts) {
			if (seenHearts.Contains(enemy))
				continue;
			Particles.Remove(heart);
			hearts.Remove(enemy);
		}
	}

	void UpdatePressEffect(ref SustainStateChangedGameEvent ev) {
		if (!Scenes.HasActiveScene(out var scene))
			return;

		ref var press = ref presses[ev.Pathway.Index];
		if (ev.IsSustainingNow) {
			var effect = scene.GetPressEffect(ev.Pathway, false);
			if (effect == null)
				return;

			if (press == null || press.Definition != effect.Definition) {
				if (press != null)
					Particles.Remove(press);
				press = Particles.Create(effect.Definition, effect.At(PressY(scene, ev.Pathway, effect, false)));
				press.ReleaseWhenFinished = false;
			}
			press.StartEmission();
			return;
		}

		press?.StopEmission();
		if (ev.Completed) {
			var top = scene.GetPressEffect(ev.Pathway, true);
			if (top != null)
				Play(top, top.At(PressY(scene, ev.Pathway, top, true)));
		}
	}
}
