using CloneDash.Game;

using Nucleus.Audio;
using Nucleus.Common.Graphics;
using Nucleus.Common.Types;
using Nucleus.ManagedMemory;
using Nucleus.Models.Runtime;
using Nucleus.Types;

namespace CloneDash.Scenes;

public struct PathwayInformation
{
	public Color Color;
	public Vector2F Position;
	public object? UserData;

	public PathwayInformation(float x, float y, object? userdata) {
		Position = new(x, y);
		UserData = userdata;
	}
}

/// <summary>
/// An interface to various scene operations and information. This is abstracted away into an interface to allow some form of scene descriptor versions 
/// and potentially, in the future, loading straight from Muse Dash (requires a LOT of work!)
/// </summary>
public interface ISceneDescriptor
{
	public void Initialize(DashGameLevel game);
	public void Refresh(DashGameLevel game);

	public void PlaySound(SceneSound sound, int hits);
	public MusicTrack? GetPressIdleSound();

	public void Think(DashGameLevel game);
	public void RenderBackground(DashGameLevel game);
	public void RenderPathway(DashGameLevel game, PathwaySide side, float alpha, float size, float rotation);

	public ModelData? GetEnemyModel(DashEnemy enemy);

	public ModelData? GetHP(out string? mountAnimation);

	public string? GetMasherHitAnimation();

	/// <summary>
	/// Please return seconds in time!!!!
	/// </summary>
	/// <param name="type"></param>
	/// <param name="time"></param>
	/// <returns></returns>
	public string? GetBossAnimation(BossAnimationType type, out double time);
	public string? GetBossAnimation(BossAnimationType type) => GetBossAnimation(type, out _);
	public string? GetBossAnimation(DashEnemy fired, out double time) =>
		fired.Variant == EntityVariant.Boss1
			? fired.Pathway == PathwaySide.Top ? GetBossAnimation(BossAnimationType.AttackAir1, out time) : GetBossAnimation(BossAnimationType.AttackGround1, out time)
			: fired.Pathway == PathwaySide.Top ? GetBossAnimation(BossAnimationType.AttackAir2, out time) : GetBossAnimation(BossAnimationType.AttackGround2, out time);
	public string? GetBossAnimation(DashEnemy fired) => GetBossAnimation(fired, out _);
	public string? GetEnemyApproachAnimation(DashEnemy enemy, out double time);

	public ref readonly PathwayInformation GetPathwayInformation(PathwaySide pathway);

	public string? GetEnemyHitAnimation(DashEnemy enemy, HitAnimationType hitType);
	public BoneInstance? GetHPMount(DashEnemy enemy);
	public void GetSustainResources(PathwaySide pathway, out ITexture? start, out ITexture? end, out ITexture? body, out ITexture? up, out ITexture? down, out float rotationDegsPerSecond);


	Color GetPathwayColor(PathwaySide side);
	Vector2F GetPathwayPosition(PathwaySide side);
}