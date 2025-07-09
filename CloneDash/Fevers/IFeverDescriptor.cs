using CloneDash.Game;

namespace CloneDash.Fevers;

/// <summary>
/// Provides a way to render fevers.
/// </summary>
public interface IFeverDescriptor
{
	public void Initialize(DashGameLevel game);
	public void Start(DashGameLevel game);
	public void Think(DashGameLevel game);
	public void Render(DashGameLevel game);
}