using CloneDash.Common.UI;
using Nucleus.Common.Types;
using Nucleus.UI;

namespace CloneDash.Game;

public interface IMainMenuLevel
{
	Panel Content { get; }
	T PushActiveElement<T>(T element) where T : Element, IMainMenuPanel;

	void PopActiveElement();
	Panel? GetSelectedSongPanel();
}

public interface IMainMenuPanel
{
	string GetName();
	Color GetPrimaryColor() => CloneDashUI.AccentPrimary;
	Color GetBackgroundColor() => CloneDashUI.AccentBackground;
	(Action act, string name, string icon)? GetFooterAction() => null;
	void OnHidden();
	void OnShown();
	void SetRichPresence();
	bool InterceptEscape() => true;
	bool OnTryClose() => true;
}