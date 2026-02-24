using AssetStudio;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;
using CloneDash.Game;
using Nucleus.Audio;
using Nucleus.Common.Graphics;
using Nucleus.ManagedMemory;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using OdinSerializer;
using System;
using System.Collections.Generic;
using System.Text;
using Texture = Nucleus.ManagedMemory.Texture;
using Color = Nucleus.Common.Types.Color;
using Nucleus;

namespace CloneDash.Scenes;

public class MuseDashScenePiece
{
	public string Name = "";
	public const float MUSEDASH_MULTIPLIER_POSITIONS = 200; // seems to be the right base

	internal MuseDashScene Scene = null!;
	MuseDashScenePiece? Parent;
	readonly List<MuseDashScenePiece> Children = [];

	public MuseDashScene GetScene() => Scene;
	public IReadOnlyList<MuseDashScenePiece> GetChildren() => Children;
	public MuseDashScenePiece? GetParent() => Parent;
	public void AddChild(MuseDashScenePiece child) => child.SetParent(this);
	public void SetParent(MuseDashScenePiece? parent) {
		Parent?.Children.Remove(this);
		Parent = parent;
		Parent?.Children.Add(this);
	}
}


public class MuseDashScene : MuseDashScenePiece, ISceneDescriptor
{
	readonly PathwayInformation[] pathwayInfo = new PathwayInformation[4];
	readonly List<MuseDashScenePiece> AllPieces = [];
	readonly Dictionary<long, MuseDashScenePiece> UnityPathIDToScenePiece = [];

	public T GetOrCreateScenePiece<T>(long pathID) where T : MuseDashScenePiece, new() {
		if (UnityPathIDToScenePiece.TryGetValue(pathID, out var piece))
			return (T)piece;
		piece = new T {
			Scene = this
		};
		AllPieces.Add(piece);
		UnityPathIDToScenePiece[pathID] = piece;
		return (T)piece;
	}

	/// <summary>
	/// Performs some final setup that can't be done until all data is retrieved
	/// </summary>
	public void FinalSetup() {
		pathwayInfo[(int)PathwaySide.Both] = new() {
			Position = (pathwayInfo[(int)PathwaySide.Top].Position + pathwayInfo[(int)PathwaySide.Bottom].Position) / 2,
			Color = Pathway.PATHWAY_DUAL_COLOR
		};
		pathwayInfo[(int)PathwaySide.Top].Color = Pathway.PATHWAY_TOP_COLOR;
		pathwayInfo[(int)PathwaySide.Bottom].Color = Pathway.PATHWAY_BOTTOM_COLOR;
	}

	public static MuseDashScene? GetScene(ReadOnlySpan<char> name) {
		int sceneIDX = name.IndexOf('_');
		if (sceneIDX == -1)
			return null;

		sceneIDX = int.TryParse(name[(sceneIDX + 1)..], out int i) ? i : -1;
		if (sceneIDX == -1)
			return null;

		string strSceneIdx = sceneIDX.ToString().PadLeft(2, '0');
		var sceneGameObject = MuseDashCompatibility.StreamingAssets.FindAssetByName<GameObject>($"scene_{strSceneIdx}")!;

		var sceneSubControl = new MonoBehaviourReader(sceneGameObject.GetComponentByName<MonoBehaviour>("SceneSubControl") ?? throw new NullReferenceException("No scene control?"));
		if (sceneSubControl == null)
			return null;

		// HITPOINTS
		// This gets the hitpoints and their positions out of the scene data
		var scenePoint = sceneSubControl.Get<GameObject>("scenePoint");
		var transform = scenePoint!.GetFirstComponent<Transform>()!;
		transform.m_Children[0].TryGet(out var child1);
		transform.m_Children[1].TryGet(out var child2);

		var mdScene = new MuseDashScene();
		mdScene.Name = strSceneIdx;
		child1!.ComputeGlobalTransform(out var v1, out _);
		child2!.ComputeGlobalTransform(out var v2, out _);
		int tempOffset = -42; // TODO: What is this? 
		mdScene.pathwayInfo[(int)PathwaySide.Top] = new(v1.X * MUSEDASH_MULTIPLIER_POSITIONS, (v1.Y * MUSEDASH_MULTIPLIER_POSITIONS) + tempOffset);
		mdScene.pathwayInfo[(int)PathwaySide.Bottom] = new(v2.X * MUSEDASH_MULTIPLIER_POSITIONS, (v2.Y * MUSEDASH_MULTIPLIER_POSITIONS) + tempOffset);

		// OBJECTS
		// This gets all of the background elements of the scene
		foreach (var animatorData in sceneSubControl.GetList<Animator>("m_Animators")) {
			if (animatorData == null) continue;

			var gameObject = animatorData.GetGameObject();
			if (gameObject == null) continue;

			bool containsSomethingElse = gameObject.Components.Any(x => x is not Transform && x is not Animator);
			Logs.Info("Game Object");
			Logs.Info($"  - Name:   {gameObject.m_Name}");
			Logs.Info($"  - Visual: {containsSomethingElse}");
		}

		mdScene.FinalSetup();
		return mdScene;
	}

	public string? GetBossAnimation(BossAnimationType type, out double time) {
		time = 0;
		return null;
	}

	public string? GetEnemyApproachAnimation(DashEnemy enemy, out double time) {
		time = 0;
		return null;
	}

	public string? GetEnemyHitAnimation(DashEnemy enemy, HitAnimationType hitType) {
		return null;
	}

	public ModelData? GetEnemyModel(DashEnemy enemy) {
		return null;

	}

	public ModelData? GetHP(out string? mountAnimation) {
		mountAnimation = null;
		return null;
	}

	public BoneInstance? GetHPMount(DashEnemy enemy) {
		return null;
	}

	public string GetMasherHitAnimation() {
		return "";
	}

	public ref readonly PathwayInformation GetPathwayInformation(PathwaySide pathway) => ref pathwayInfo[(int)pathway];

	public Nucleus.Common.Types.Color GetPathwayColor(PathwaySide side) => GetPathwayInformation(side).Color;
	public Vector2F GetPathwayPosition(PathwaySide side) => GetPathwayInformation(side).Position;

	public MusicTrack? GetPressIdleSound() {
		return null;
	}

	public void GetSustainResources(PathwaySide pathway, out ITexture start, out ITexture end, out ITexture body, out ITexture up, out ITexture down, out float rotationDegsPerSecond) {
		start = null!;
		end = null!;
		body = null!;
		up = null!;
		down = null!;
		rotationDegsPerSecond = 0;
		// todo: refactor this part of the API.
	}

	public void Initialize(DashGameLevel game) {

	}

	public void PlaySound(SceneSound sound, int hits) {

	}

	public void Refresh(DashGameLevel game) {

	}

	public void RenderBackground(DashGameLevel game) {

	}

	public void Think(DashGameLevel game) {

	}

	internal void MountToFilesystem() {

	}
}
