using CloneDash.Game;

namespace CloneDash.Fevers;

/// <summary>
/// Provides a way to render fevers.
/// </summary>
public interface IFeverDescriptor
{
	public void Initialize(CD_GameLevel game);
	public void Start(CD_GameLevel game);
	public void Think(CD_GameLevel game);
	public void Render(CD_GameLevel game);
}