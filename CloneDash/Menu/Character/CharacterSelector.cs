using CloneDash.Characters;
using CloneDash.Common;
using CloneDash.Common.UI;
using CloneDash.Common.UI.Binding;
using CloneDash.Game;
using CloneDash.Systems;
using Nucleus.Commands;
using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Common.UI;
using Nucleus.Types;
using Nucleus.UI;

namespace CloneDash.Menu.Character;

public class CharacterSelector : Panel, IMainMenuPanel
{
	#region IMainMenuPanel

	string IMainMenuPanel.ColorScheme => "Character";
	string IMainMenuPanel.Name => "Character Selector";

	MenuFooterAction IMainMenuPanel.GetAction() => new("Select", "icons/check.png", SelectCharacter);

	PanelBinding[] IMainMenuPanel.GetBindings() => [
		new("Change Character", ([ButtonCode.KeyLeft], () => _scroller.Cycle(-1)), ([ButtonCode.KeyRight], () => _scroller.Cycle(1))),
		new("Select", ([ButtonCode.KeyEnter], SelectCharacter))
	];

	void IMainMenuPanel.SetRichPresence() {
		RichPresenceSystem.SetPresence(new RichPresenceState {
			Details = "Main Menu",
			State = "Selecting a character"
		});
	}

	#endregion

	private readonly Label _nameLabel;
	private readonly Label _costumeLabel;
	private readonly CharacterIconLabel _healthLabel;
	private readonly CharacterIconLabel _voiceLabel;
	private readonly CharacterIconLabel _artistLabel;
	private readonly CharacterSkillDisplay _skill;

	private readonly Element _bottom;
	private readonly Element _line;
	private readonly CharacterSelectorScroller _scroller;

	private ICharacterDescriptor? _lastCharacterSelected;

	private MainMenuCharacter Character => Level.As<MainMenuLevel>().Character;

	public CharacterSelector(Element? parent) : base(parent) {
		SetPaintBackgroundEnabled(false);
		DockPadding = new RectangleF();
		SetPassthru(true);

		Color textColor = this.GetTextColor(GetScheme());

		_nameLabel = new Label(this);
		_nameLabel.SetAutoSize(true);
		_nameLabel.SetTextColor(textColor);
		_nameLabel.Font = CloneDashUI.GetBoldFont(GetScheme());

		_costumeLabel = new Label(this);
		_costumeLabel.SetAutoSize(true);
		_costumeLabel.SetTextColor(textColor);

		_healthLabel = new CharacterIconLabel(this, "icons/heart-straight.png") { Color = textColor };
		_voiceLabel = new CharacterIconLabel(this, "icons/microphone-stage.png") { Color = textColor };
		_artistLabel = new CharacterIconLabel(this, "icons/palette.png") { Color = textColor };

		_skill = new CharacterSkillDisplay(this) { Color = textColor };
		_skill.Size = new Vector2F(640);
		_skill.		Anchor = Anchor.TopRight;
		_skill.		Origin = Anchor.TopRight;

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

		_scroller = new CharacterSelectorScroller(_bottom);
		_scroller.Dock = Dock.Top;
		_scroller.DockMargin = new RectangleF(20, 0, 0, 0);
		_scroller.Size = new Vector2F(0, 100); // margin subtracts from size??
		_scroller.BorderSize = 0;
		_scroller.CharacterSelected += BackPanel_CharacterSelected;

		ICharacterDescriptor? currentCharacter = CharacterMod.GetCharacterData();
		BackPanel_CharacterSelected(currentCharacter);
		_scroller.SetCharacter(currentCharacter);
	}

	public override void OnSchemeChanged(IScheme? prev, IScheme? now) {
		base.OnSchemeChanged(prev, now);

		Color text = this.GetTextColor(now);
		_nameLabel.SetTextColor(text);
		_costumeLabel.SetTextColor(text);
		_healthLabel.Color = text;
		_voiceLabel.Color = text;
		_skill.Color = text;

		_bottom.SetBgColor(this.GetBackgroundColor(now));
		_line.SetBgColor(this.GetPrimaryColor(now));
	}

	private void SelectCharacter() {
		if (_lastCharacterSelected == null) return;
		ConVar cv = cvar.FindVar("character")!;
		cv.SetValue(_lastCharacterSelected.GetUUID());
		Character.PlayApplyExpression();
	}

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);

		float ratio = height / 900;

		_nameLabel.
		TextSize = CloneDashUI.GetFontSize(64) * ratio;
		_costumeLabel.TextSize = CloneDashUI.GetFontSize(36) * ratio;

		_healthLabel.Scale = _voiceLabel.Scale = _artistLabel.Scale = _skill.Scale = ratio;

		_nameLabel.
		Position = new Vector2F(32, 12);
		_costumeLabel.Position = new Vector2F(32, _nameLabel.Position.Y + _nameLabel.TextSize - 20 * ratio);
		_healthLabel.Position = new Vector2F(32, _costumeLabel.Position.Y + _costumeLabel.TextSize + 16 * ratio);
		_voiceLabel.Position = new Vector2F(32, _healthLabel.Position.Y + _healthLabel.Size.Y + 12 * ratio);
		_artistLabel.Position = new Vector2F(32, _voiceLabel.Position.Y + _voiceLabel.Size.Y + 12 * ratio);

		_skill.
		Position = new Vector2F(-32, 32);
	}

	private void BackPanel_CharacterSelected(ICharacterDescriptor? ch) {
		_lastCharacterSelected = ch;
		Character.SetCharacter(ch);

		HumanLanguage lang = HumanLanguage.GetCurrentLanguage();

		if (ch == null) {
			_nameLabel.Text = "<NULL>";
			_costumeLabel.Text = "<NULL>";
			_healthLabel.Text = "<NULL>";
			_voiceLabel.Text = "<NULL>";
			_artistLabel.Text = "<NULL>";

			_skill.Text = "<NULL>";
		}
		else {
			_nameLabel.Text = $"{ch.GetCharacterName(lang, out _)}";
			_costumeLabel.Text = $"{ch.GetCosplayName(lang, out _)}";
			_healthLabel.Text = "250"; // no clue how to get HP values
			_voiceLabel.Text = $"{ch.GetAuthor(lang, out _)}";
			_artistLabel.Text = "???"; // needs to be added at some point

			_skill.Text = ch.GetPerk(lang, out _).ToString();
		}
	}

	public bool OnTryClose() {
		Character.SetCharacter(CharacterMod.GetCharacterData());
		return true;
	}
}