using CloneDash.Game;

using Nucleus.Audio;
using Nucleus.Common.Audio;
using Nucleus.Common.Graphics;
using Nucleus.Common.Types;
using Nucleus.Engine;
using Nucleus.ManagedMemory;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using Nucleus.Util;

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

public record struct SceneDescriptorID(string Source, string Name){
	public static implicit operator string(SceneDescriptorID id) => $"{id.Source}/{id.Name}";
}

/// <summary>
/// An interface to various scene operations and information. This is abstracted away into an interface to allow some form of scene descriptor versions 
/// and potentially, in the future, loading straight from Muse Dash (requires a LOT of work!)
/// </summary>
public interface ISceneDescriptor
{
	public void Initialize(DashGameLevel game);
	public void Refresh(DashGameLevel game);

	ISceneRuntime CreateRuntime();
}

public interface ISceneRuntime : IDashGameRuntimeComponent
{
	ISceneDescriptor GetSceneDescriptor();
	/// <summary>
	/// Render the background
	/// </summary>
	void RenderBackground();
	/// <summary>
	/// Render a pathway
	/// </summary>
	/// <param name="side"></param>
	void RenderPathway(PathwaySide side);
	/// <summary>
	/// Render an enemy
	/// </summary>
	/// <param name="enemy"></param>
	void RenderEnemy(IDashEnemy enemy);
	/// <summary>
	/// Game thinking
	/// </summary>
	void Think();
	/// <summary>
	/// Ran on scene activations, which may be at the start of a game, or may be due to scene changes.
	/// </summary>
	void Activate();
	/// <summary>
	/// Ran on scene deactivations, which would only be because of a scene change
	/// </summary>
	void Deactivate();
	/// <summary>
	/// Gets a pathway color
	/// </summary>
	/// <param name="side"></param>
	/// <returns></returns>
	Color GetPathwayColor(PathwaySide side);
	/// <summary>
	/// Gets a pathway position
	/// </summary>
	/// <param name="side"></param>
	/// <returns></returns>
	Vector2F GetPathwayPosition(PathwaySide side);
	void PlaySound(SceneSound sound, int hits);
	bool VisTest(BaseDashEnemy entCD);
	void EvaluatePressIdleSoundState(bool nowInsustain, bool wasSustainingBefore);
}