using AssetStudio;
using CloneDash.Compatibility.MuseDash;
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
	public MuseDashScene(int sceneIDX) {
		string strSceneIdx = sceneIDX.ToString().PadLeft(2, '0');
		var scene = MuseDashCompatibility.StreamingAssets.FindAssetByName<GameObject>($"scene_{strSceneIdx}")!;

		var defaultEggControl = scene.GetComponentByName<MonoBehaviour>("DefaultEggControl");
		var sceneSubControl = scene.GetComponentByName<MonoBehaviour>("SceneSubControl");
	}

	public static MuseDashScene? GetScene(ReadOnlySpan<char> name) {
		int sceneIDX = name.IndexOf('_');
		if (sceneIDX == -1) return null;

		sceneIDX = int.TryParse(name[(sceneIDX + 1)..], out int i) ? i : -1;
		if (sceneIDX == -1) return null;
		return new MuseDashScene(sceneIDX);
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
