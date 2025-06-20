using CloneDash.Game;

using Nucleus.Audio;
using Nucleus.ManagedMemory;
using Nucleus.Models.Runtime;

namespace CloneDash.Scenes;

public enum SceneSound {
	Begin,
	Fever,
	Unpause,
	FullCombo,

	Mash,
	HP,
	Score,
	Jump,
	EmptyAttack,
	EmptyJump,
	Loud1,
	Loud2,
	Medium1,
	Medium2,
	Quiet,
	PressTop
}
public interface ISceneDescriptor
{
	public void Initialize(CD_GameLevel game);

	public void PlaySound(SceneSound sound, int hits);
	public MusicTrack GetPressIdleSound();

	public void Think(CD_GameLevel game);
	public void RenderBackground(CD_GameLevel game);

	public ModelData GetEnemyModel(CD_BaseEnemy enemy);

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
	public string GetBossAnimation(CD_BaseEnemy fired, out double time) =>
		fired.Variant == EntityVariant.Boss1
			? fired.Pathway == PathwaySide.Top ? GetBossAnimation(BossAnimationType.AttackAir1, out time) : GetBossAnimation(BossAnimationType.AttackGround1, out time)
			: fired.Pathway == PathwaySide.Top ? GetBossAnimation(BossAnimationType.AttackAir2, out time) : GetBossAnimation(BossAnimationType.AttackGround2, out time);
	public string GetBossAnimation(CD_BaseEnemy fired) => GetBossAnimation(fired, out _);
	public string GetEnemyApproachAnimation(CD_BaseEnemy enemy, out double time);

	public string GetEnemyHitAnimation(CD_BaseEnemy enemy, HitAnimationType hitType);
	public BoneInstance? GetHPMount(CD_BaseEnemy enemy);
	public void GetSustainResources(PathwaySide pathway, out Texture start, out Texture end, out Texture body, out Texture up, out Texture down, out float rotationDegsPerSecond);
}