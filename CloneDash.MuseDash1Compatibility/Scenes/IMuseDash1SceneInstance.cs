using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Common.Scenes;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Game;
using Nucleus.Common.Graphics;
using Nucleus.Common.Types;
using Nucleus.Models.Runtime;
using Nucleus.Types;

namespace CloneDash.Scenes;

public interface IMuseDash1SceneInstance : ISceneInstance
{
	void Initialize();
	void Refresh();

	IMuseDash1SceneUI CreateUI();

	void PlaySound(SceneSound sound, int hits);
	/// <summary>
	/// Stops every playback of a scene sound (used to cut the fail BGM on restart/exit).
	/// </summary>
	void StopSound(SceneSound sound);
	void OnPressStateChange(bool wasSustaining, bool startSustaining);

	void Think(double scrollSpeed);
	void RenderBackground();
	void RenderPathway(PathwayId pathway, float alpha, float size, float rotation);
	void RenderOverlay();

	bool BeginPathwayHueShift(PathwayId pathway);
	void EndPathwayHueShift();

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
	MuseDash1ParticleEffect? GetEnemyHitEffect(EntityVariant variant, PathwayId pathway);
	MuseDash1ParticleEffect? GetPressEffect(PathwayId pathway, bool top);
	ModelData? GetHP(out string? mountAnimation);
	string? GetMasherHitAnimation(int speed, EntityEnterDirection dir);
	MD1_SpineActionControllerData GetBossActions();
	string? GetEnemyApproachAnimation(DashEnemy enemy, out double time);
	ref readonly PathwayInformation GetPathwayInformation(PathwayId pathway);
	ref readonly PathwayInformation GetCenterInformation();
	string? GetEnemyHitAnimation(DashEnemy enemy, HitAnimationType hitType);
	BoneInstance? GetHPMount(DashEnemyVisuals enemy);
	void GetSustainResources(PathwayId pathway, out ITexture? start, out ITexture? end, out ITexture? body, out ITexture? up, out ITexture? down, out float rotationDegsPerSecond);
	Color GetPathwayColor(PathwayId pathway);
	Vector2F GetPathwayPosition(PathwayId pathway);
	Vector2F GetCenterPosition();
	Vector2F GetNormalizedPosition(float normalizedY);
	bool UsesAirAssets(PathwayId pathway);
	float GetPathwayHueShift(PathwayId pathway);
	Vector2F GetPathwayRenderOffset(PathwayId pathway);
	bool IsAirPathway(PathwayId pathway);
	bool IsAirTarget(PathwaySet pathways);
}