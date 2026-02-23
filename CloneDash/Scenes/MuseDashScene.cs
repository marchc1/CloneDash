using AssetStudio;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;
using CloneDash.Game;
using Nucleus.Audio;
using Nucleus.ManagedMemory;
using Nucleus.Models.Runtime;
using System;
using System.Collections.Generic;
using System.Text;
using Texture = Nucleus.ManagedMemory.Texture;

namespace CloneDash.Scenes;

public class MuseDashScene : ISceneDescriptor
{
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

		MuseDashScene mdScene = new();
		return mdScene;
	}

	public string GetBossAnimation(BossAnimationType type, out double time) {
		throw new NotImplementedException();
	}

	public string GetEnemyApproachAnimation(DashEnemy enemy, out double time) {
		throw new NotImplementedException();
	}

	public string GetEnemyHitAnimation(DashEnemy enemy, HitAnimationType hitType) {
		throw new NotImplementedException();
	}

	public ModelData GetEnemyModel(DashEnemy enemy) {
		throw new NotImplementedException();
	}

	public ModelData GetHP(out string mountAnimation) {
		throw new NotImplementedException();
	}

	public BoneInstance? GetHPMount(DashEnemy enemy) {
		throw new NotImplementedException();
	}

	public string GetMasherHitAnimation() {
		throw new NotImplementedException();
	}

	public MusicTrack GetPressIdleSound() {
		throw new NotImplementedException();
	}

	public void GetSustainResources(PathwaySide pathway, out Texture start, out Texture end, out Texture body, out Texture up, out Texture down, out float rotationDegsPerSecond) {
		throw new NotImplementedException();
	}

	public void Initialize(DashGameLevel game) {
		throw new NotImplementedException();
	}

	public void PlaySound(SceneSound sound, int hits) {
		throw new NotImplementedException();
	}

	public void Refresh(DashGameLevel game) {
		throw new NotImplementedException();
	}

	public void RenderBackground(DashGameLevel game) {
		throw new NotImplementedException();
	}

	public void Think(DashGameLevel game) {
		throw new NotImplementedException();
	}

	internal void MountToFilesystem() {
		throw new NotImplementedException();
	}
}
