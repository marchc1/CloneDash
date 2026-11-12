using CloneDash.Common;
using CloneDash.Common.Systems;
using CloneDash.Common.Systems.Discord;
using CloneDash.Common.UI;
using CloneDash.Common.UI.Binding;
using CloneDash.Elfins;
using CloneDash.Game;
using CloneDash.Menu.Character;
using CloneDash.Systems;
using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Common.UI;
using Nucleus.Types;
using Nucleus.UI;

namespace CloneDash.Menu.Elfin;

public class ElfinSelector : Panel, IMainMenuPanel
{
	#region IMainMenuPanel

	string IMainMenuPanel.ColorScheme => "Character";
	string IMainMenuPanel.Name => "Elfin Selector";

	MenuFooterAction IMainMenuPanel.GetAction() => new("Select", "icons/check.png", SelectElfin);

	PanelBinding[] IMainMenuPanel.GetBindings() => [
		new("Change Elfin", ([ButtonCode.KeyLeft], () => _scroller.Cycle(-1)), ([ButtonCode.KeyRight], () => _scroller.Cycle(1))),
		new("Select", ([ButtonCode.KeyEnter], SelectElfin))
	];

	void IMainMenuPanel.SetRichPresence() {
		RichPresenceSystem.SetPresence(new RichPresenceState {
			Details = "Main Menu",
			State = "Selecting an elfin"
		});
	}

	#endregion

	private readonly Label _nameLabel;
	private readonly Label _descriptionLabel;
	private readonly CharacterSkillDisplay _skill;

	private readonly Element _bottom;
	private readonly Element _line;
	private readonly DescriptorSelectorScroller<IElfinDescriptor> _scroller;

	private IElfinDescriptor? _lastElfinSelected;

	private MainMenuCharacter Character => Level.As<MainMenuLevel>().Character;

	public ElfinSelector(Element? parent) : base(parent) {
		SetPaintBackgroundEnabled(false);
		DockPadding = new RectangleF();
		SetPassthru(true);

		Color textColor = this.GetTextColor(GetScheme());

		_nameLabel = new Label(this);
		_nameLabel.SetAutoSize(true);
		_nameLabel.SetTextColor(textColor);
		_nameLabel.Font = CloneDashUI.GetBoldFont(GetScheme());

		_descriptionLabel = new Label(this);
		_descriptionLabel.SetAutoSize(true);
		_descriptionLabel.SetTextColor(textColor);

		_skill = new CharacterSkillDisplay(this) { Color = textColor };
		_skill.Size = new Vector2F(640);
		_skill.Anchor = Anchor.TopRight;
		_skill.Origin = Anchor.TopRight;

		_bottom = new Element(this);
		_bottom.Dock = Dock.Bottom;
		_bottom.Size = new Vector2F(0, 180);
		_bottom.DockPadding = new RectangleF();
		_bottom.BorderSize = 0;
		_bottom.SetPaintBackgroundEnabled(true);
		_bottom.SetBgColor(this.GetBackgroundColor(GetScheme()));
		_bottom.SetPassthru(true);

		_line = new Element(_bottom);
		_line.Dock = Dock.Top;
		_line.Size = new Vector2F(0, 3);
		_line.BorderSize = 0;
		_line.SetPaintBackgroundEnabled(true);
		_line.SetBgColor(this.GetPrimaryColor(GetScheme()));

		_scroller = new DescriptorSelectorScroller<IElfinDescriptor>(_bottom, [null, .. ElfinMod.Instance.GetAllDescriptors()], static elfin => elfin == null ? textures.LoadTextureFromFile("icons/x.png") : elfin.GetThumbnailTexture());
		_scroller.Dock = Dock.Top;
		_scroller.DockMargin = new RectangleF(20, 0, 0, 0);
		_scroller.Size = new Vector2F(0, 100);
		_scroller.BorderSize = 0;
		_scroller.Selected += BackPanel_ElfinSelected;

		IElfinDescriptor? currentElfin = ElfinMod.Instance.GetActive();
		BackPanel_ElfinSelected(currentElfin);
		_scroller.SetSelected(currentElfin);
	}

	public override void OnSchemeChanged(IScheme? prev, IScheme? now) {
		base.OnSchemeChanged(prev, now);

		Color text = this.GetTextColor(now);
		_nameLabel.SetTextColor(text);
		_descriptionLabel.SetTextColor(text);
		_skill.Color = text;

		_bottom.SetBgColor(this.GetBackgroundColor(now));
		_line.SetBgColor(this.GetPrimaryColor(now));
	}

	private void SelectElfin() => ElfinMod.elfin.SetValue(_lastElfinSelected?.GetUniqueID().ToString() ?? "");

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);

		float ratio = height / 900;

		_nameLabel.TextSize = CloneDashUI.GetFontSize(64) * ratio;
		_descriptionLabel.TextSize = CloneDashUI.GetFontSize(28) * ratio;

		_skill.Scale = ratio;

		_nameLabel.Position = new Vector2F(32, 12);
		_descriptionLabel.Position = new Vector2F(32, _nameLabel.Position.Y + _nameLabel.TextSize - 12 * ratio);

		_skill.Position = new Vector2F(-32, 32);
	}

	private void BackPanel_ElfinSelected(IElfinDescriptor? elfin) {
		_lastElfinSelected = elfin;
		Character.SetElfin(elfin);
		_skill.SetVisible(elfin != null);

		if (elfin == null) {
			_nameLabel.Text = "None";
			_descriptionLabel.Text = "Play without an elfin.";
			_skill.Text = "";
		}
		else {
			_nameLabel.Text = $"{elfin.GetElfinName()}";
			_descriptionLabel.Text = $"{elfin.GetDescription()}";
			_skill.Text = elfin.GetPerk().ToString();
		}
	}

	public bool OnTryClose() {
		Character.SetElfin(ElfinMod.Instance.GetActive());
		return true;
	}

	IMainMenuLevel mainMenu = null!;
	public IMainMenuLevel GetMainMenu() => mainMenu;
	public void SetMainMenu(IMainMenuLevel level) => mainMenu = level;
}
