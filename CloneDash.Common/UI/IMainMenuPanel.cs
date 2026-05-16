using CloneDash.Common.UI;
using Nucleus.Common.Types;
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
	Color GetPrimaryColor() => CloneDashUI.AccentPrimary;
	Color GetBackgroundColor() => CloneDashUI.AccentBackground;
	public void OnHidden();
	public void OnShown();
	public void SetRichPresence();
	public bool InterceptEscape() => true;
	public bool OnTryClose() => true;
}