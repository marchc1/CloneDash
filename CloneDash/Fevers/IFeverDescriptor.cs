using CloneDash.Game;

namespace CloneDash.Fevers;

public interface IFeverDescriptor
{
	public void Initialize(CD_GameLevel game);
	public void Start(CD_GameLevel game);
	public void Think(CD_GameLevel game);
	public void Render(CD_GameLevel game);
}