using CloneDash.Game;

using Nucleus.Audio;
using Nucleus.ManagedMemory;
using Nucleus.Models.Runtime;

namespace CloneDash.Scenes;

/// <summary>
/// An interface to various scene operations and information. This is abstracted away into an interface to allow some form of scene descriptor versions 
/// and potentially, in the future, loading straight from Muse Dash (requires a LOT of work!)
/// </summary>
public interface ISceneDescriptor
{
	public void Initialize(DashGameLevel game);
	public void Refresh(DashGameLevel game);

	public void PlaySound(SceneSound sound, int hits);
	public MusicTrack GetPressIdleSound();

	public void Think(DashGameLevel game);
	public void RenderBackground(DashGameLevel game);

	public ModelData GetEnemyModel(DashEnemy enemy);

	public ModelData GetHP(out string mountAnimation);

	public string GetMasherHitAnimation();

	/// <summary>
	/// Please return seconds in time!!!!
	/// </summary>
	/// <param name="type"></param>
	/// <param name="time"></param>
	/// <returns></returns>
	public string GetBossAnimation(BossAnimationType type, out double time);
	public string GetBossAnimation(BossAnimationType type) => GetBossAnimation(type, out _);
	public string GetBossAnimation(DashEnemy fired, out double time) =>
		fired.Variant == EntityVariant.Boss1
			? fired.Pathway == PathwaySide.Top ? GetBossAnimation(BossAnimationType.AttackAir1, out time) : GetBossAnimation(BossAnimationType.AttackGround1, out time)
			: fired.Pathway == PathwaySide.Top ? GetBossAnimation(BossAnimationType.AttackAir2, out time) : GetBossAnimation(BossAnimationType.AttackGround2, out time);
	public string GetBossAnimation(DashEnemy fired) => GetBossAnimation(fired, out _);
	public string GetEnemyApproachAnimation(DashEnemy enemy, out double time);

	public string GetEnemyHitAnimation(DashEnemy enemy, HitAnimationType hitType);
	public BoneInstance? GetHPMount(DashEnemy enemy);
	public void GetSustainResources(PathwaySide pathway, out Texture start, out Texture end, out Texture body, out Texture up, out Texture down, out float rotationDegsPerSecond);
}