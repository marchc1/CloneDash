using CloneDash.Common.UI;
using CloneDash.Common.UI.Binding;
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
	string Name { get; }
	string ColorScheme => "Accent";

	void OnHidden() {}
	void OnShown() {}
	void SetRichPresence();
	bool InterceptEscape() => true;
	bool OnTryClose() => true;
	MenuFooterAction? GetAction() => null;
	PanelBinding[] GetBindings() => [];
}

public class MenuFooterAction
{
	public string Name { get; }
	public string Icon { get; }
	public Action Action { get; }
	
	public MenuFooterAction(string name, string icon, Action action)
	{
		Name = name;
		Icon = icon;
		Action = action;
	}
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