using CloneDash.Common.UI;
using Nucleus.Common.Types;
using Nucleus.Common.UI;
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
	string ColorScheme => "Accent";

	string GetName();
	void OnHidden() {}
	void OnShown() {}
	void SetRichPresence();
	bool InterceptEscape() => true;
	bool OnTryClose() => true;
}

public static class MainMenuPanelExtensions
{
	public static Color GetPrimaryColor(this IMainMenuPanel panel, IScheme? scheme)
		=> scheme?.GetColor($"Menu.{panel.ColorScheme}.Primary") ?? Color.White;

	public static Color GetBackgroundColor(this IMainMenuPanel panel, IScheme? scheme)
		=> scheme?.GetColor($"Menu.{panel.ColorScheme}.Background") ?? Color.White;

	public static Color GetTextColor(this IMainMenuPanel panel, IScheme? scheme)
		=> scheme?.GetColor($"Menu.{panel.ColorScheme}.Text") ?? Color.White;
}