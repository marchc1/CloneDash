using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Common.Scenes;
using CloneDash.Game;
using Nucleus.Common.Audio;
using Nucleus.Common.Graphics;
using Nucleus.Common.Types;
using Nucleus.Models.Runtime;
using Nucleus.Types;

namespace CloneDash.Scenes;

public interface IMuseDash1SceneDescriptor : ISceneDescriptor {

}

public interface IMuseDash1SceneInstance : ISceneInstance {
	void Initialize();
	void Refresh();

	void PlaySound(SceneSound sound, int hits);
	void OnPressStateChange(bool wasSustaining, bool startSustaining);

	void Think();
	void RenderBackground();
	void RenderPathway(PathwaySide side, float alpha, float size, float rotation);

	void Activate(IMuseDash1SceneInstance? transitioningTo);
	void Deactivate(IMuseDash1SceneInstance? transitioningFrom);

	/// <summary>
	/// Used in DashEnemy visuals mostly
	/// </summary>
	int GetSceneArrayIndex();
	/// <summary>
	/// Used in DashEnemy visuals mostly
	/// </summary>
	void SetSceneArrayIndex(int idx);

	ModelData? GetEnemyModel(DashEnemy enemy);
	ModelData? GetHP(out string? mountAnimation);
	string? GetMasherHitAnimation(int speed, EntityEnterDirection dir);
	double GetBossAnimationTime(BossAnimationType type, AnimationHandler anim);
	double PlayBossAnimation(int channel, BossAnimationType type, AnimationHandler anim);
	public double PlayBossAnimation(BossAnimationType type, AnimationHandler anim) => PlayBossAnimation(0, type, anim);
	public double PlayBossAnimation(int channel, DashEnemy fired, AnimationHandler anim) =>
		fired.Variant == EntityVariant.Boss1
			? fired.Pathway == PathwaySide.Top ? PlayBossAnimation(BossAnimationType.AttackAir1, anim) : PlayBossAnimation(BossAnimationType.AttackGround1, anim)
			: fired.Pathway == PathwaySide.Top ? PlayBossAnimation(BossAnimationType.AttackAir2, anim) : PlayBossAnimation(BossAnimationType.AttackGround2, anim);
	string? GetEnemyApproachAnimation(DashEnemy enemy, out double time);
	ref readonly PathwayInformation GetPathwayInformation(PathwaySide pathway);
	string? GetEnemyHitAnimation(DashEnemy enemy, HitAnimationType hitType);
	BoneInstance? GetHPMount(DashEnemyVisuals enemy);
	void GetSustainResources(PathwaySide pathway, out ITexture? start, out ITexture? end, out ITexture? body, out ITexture? up, out ITexture? down, out float rotationDegsPerSecond);
	Color GetPathwayColor(PathwaySide side);
	Vector2F GetPathwayPosition(PathwaySide side);
}