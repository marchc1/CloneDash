namespace CloneDash.Game;

public interface IMainMenuPanel
{
	public string GetName();
	public void OnHidden();
	public void OnShown();
	public void SetRichPresence();
}
