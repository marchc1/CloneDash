using Nucleus.UI;

namespace CloneDash.Game;

public interface IMainMenuLevel
{
	T PushActiveElement<T>(T element) where T : Element, IMainMenuPanel;

	void PopActiveElement();
	Panel? GetSelectedSongPanel();
}

public interface IMainMenuPanel
{
	public string GetName();
	public void OnHidden();
	public void OnShown();
	public void SetRichPresence();
	public bool InterceptEscape() => true;
	public bool OnTryClose() => true;
}
