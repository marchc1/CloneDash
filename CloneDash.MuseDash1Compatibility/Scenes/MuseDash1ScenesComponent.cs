using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.Common.Scenes;
using CloneDash.Game;
using Nucleus;
using Nucleus.Types;
using Nucleus.Util;
using System.Diagnostics.CodeAnalysis;

namespace CloneDash.Scenes;

public class MuseDash1ScenesComponent : IRhythmGameComponent
{
	readonly MD1_GamemodeData data;
	readonly GameEventManager events;
	readonly PathwaysComponent pathways;
	readonly Dictionary<UtlSymId_t, IMuseDash1SceneInstance> sceneLUT = [];
	readonly List<IMuseDash1SceneInstance> scenes = [];
	readonly ContentSlot<IMuseDash1SceneInstance> slot;

	IMuseDash1SceneInstance? ActiveScene;
	IMuseDash1SceneInstance? FirstScene;

	public MuseDash1ScenesComponent(ContentSlot<IMuseDash1SceneInstance> slot, MD1_GamemodeData data, GameEventManager events, PathwaysComponent pathways) {
		this.slot = slot;
		this.data = data;
		this.events = events;
		this.pathways = pathways;

		var sceneToActivate = slot.Value;
		if (sceneToActivate != null)
			AddScene(sceneToActivate);

		SetScene(sceneToActivate);
		FirstScene = sceneToActivate;
	}

	public IMuseDash1SceneInstance? AddOrGetScene(ISceneDescriptor? descriptor) {
		if (descriptor == null) return null;
		if (sceneLUT.TryGetValue(descriptor.GetUUID().SliceNullTerminatedString().Hash(), out var instance))
			return instance;

		// Does the scene support the gamemode?
		if (!slot.TryCreate(descriptor, out instance))
			return null;

		AddScene(instance);
		return instance;
	}

	public void AddScene(IMuseDash1SceneInstance instance) {
		scenes.Add(instance);
		sceneLUT[instance.GetScene().GetUUID().Hash()] = instance;
		instance.SetSceneArrayIndex(scenes.Count - 1);
	}

	public bool BeginPathwayHueShift(PathwayId pathway) => HasActiveScene(out var scene) && scene.BeginPathwayHueShift(pathway);

	public void EndPathwayHueShift() {
		if (HasActiveScene(out var scene))
			scene.EndPathwayHueShift();
	}

	public IMuseDash1SceneInstance? GetActiveScene() => ActiveScene;

	public int GetActiveSceneIdx() => ActiveScene?.GetSceneArrayIndex() ?? -1;

	public IReadOnlyList<IMuseDash1SceneInstance> GetAllScenes() => scenes;

	public Vector2F GetCenterPosition() => HasActiveScene(out var scene) ? scene.GetCenterPosition() : default;

	public IMuseDash1SceneInstance? GetFirstScene() => FirstScene;

	public Vector2F GetEntityPosition(PathwaySet pathways) => this.pathways.Layout.Clamp(pathways).Count == 1 ? GetPathwayPosition(pathways.First) : GetCenterPosition();

	public Vector2F GetNormalizedPosition(float normalizedY) => HasActiveScene(out var scene) ? scene.GetNormalizedPosition(normalizedY) : default;

	public int GetNumScenes() => scenes.Count;

	// Its own function in case we have player-specific overrides (not sure if this exists yet)
	public Vector2F GetPathwayPosition(PathwayId pathway) => HasActiveScene(out var scene) ? scene.GetPathwayPosition(pathway) : default;

	public Vector2F GetPathwayRenderOffset(PathwayId pathway) => HasActiveScene(out var scene) ? scene.GetPathwayRenderOffset(pathway) : default;

	public bool HasActiveScene([NotNullWhen(true)] out IMuseDash1SceneInstance? scene) => (scene = ActiveScene) != null;

	public bool HasSceneInitialized(ISceneDescriptor descriptor) {
		return sceneLUT.ContainsKey(descriptor.GetUUID().Hash());
	}

	public void Load() {
		if (SceneMod.Instance.GetActive() == null)
			foreach (var sceneChange in data.SceneChanges)
				AddOrGetScene(SceneMod.Instance.GetDescriptorByUID(sceneChange.SceneUID));

		foreach (var scene in GetAllScenes())
			scene.Initialize();
	}

	public void Reset() => SetScene(FirstScene);

	public bool SetScene(IMuseDash1SceneInstance? scene) {
		ActiveScene?.Deactivate(scene);
		var oldScene = ActiveScene;
		ActiveScene = scene;
		scene?.Activate(oldScene);
		slot.Set(scene);
		events.Fire(new SceneChangedGameEvent(oldScene, scene));
		return true;
	}

	public bool SetScene(int sceneIdx) {
		return SetScene(scenes[sceneIdx]);
	}

	public void SetScene(ReadOnlySpan<char> scene) {
		if (sceneLUT.TryGetValue(scene.SliceNullTerminatedString().Hash(), out var instance))
			SetScene(instance);
	}

	internal void SetEnemyKilledPosition(DashEnemy ent) {
		var pos = GetEntityPosition(ent.Pathway);
		ent.Position = new(pos.X, -pos.Y);
	}

	internal void SetEnemyPosition(DashEnemy ent) {
		ent.Position = new Vector2F(0, 2.25f) + GetPathwayRenderOffset(pathways.PrimaryPathway(ent.Pathway));
	}
}
