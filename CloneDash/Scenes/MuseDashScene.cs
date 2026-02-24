using AssetStudio;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;
using CloneDash.Game;
using Nucleus.Audio;
using Nucleus.Common.Graphics;
using Nucleus.ManagedMemory;
using Nucleus.Models.Runtime;
using OdinSerializer;
using System;
using System.Collections.Generic;
using System.Text;
using Texture = Nucleus.ManagedMemory.Texture;

namespace CloneDash.Scenes;

public class MuseDashScene : ISceneDescriptor
{
	readonly PathwayInformation[] pathwayInfo = new PathwayInformation[3];

	public MuseDashScene() {

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

		var scenePoint = sceneSubControl.Get<GameObject>("scenePoint");
		var transform = scenePoint!.GetFirstComponent<Transform>()!;
		transform.m_Children[0].TryGet(out var child1);
		transform.m_Children[1].TryGet(out var child2);

		var mdScene = new MuseDashScene();
		var v1 = transform.m_LocalPosition + child1!.m_LocalPosition;
		var v2 = transform.m_LocalPosition + child2!.m_LocalPosition;
		mdScene.pathwayInfo[(int)PathwaySide.Top] = new(v2.X, v2.Y);
		mdScene.pathwayInfo[(int)PathwaySide.Bottom] = new(v1.X, v1.Y);

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
