using CloneDash.Characters;
using CloneDash.Common;
using CloneDash.Common.UI;
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

	void IMainMenuPanel.SetRichPresence() {
		RichPresenceSystem.SetPresence(new RichPresenceState {
			Details = "Main Menu", State = "Selecting a character"
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
		SetDockPadding(new RectangleF());
		SetPassthru(true);

		Color textColor = this.GetTextColor(GetScheme());

		_nameLabel = new Label(this);
		_nameLabel.SetAutoSize(true);
		_nameLabel.SetTextColor(textColor);
		_nameLabel.SetFont(CloneDashUI.GetBoldFont(GetScheme()));

		_costumeLabel = new Label(this);
		_costumeLabel.SetAutoSize(true);
		_costumeLabel.SetTextColor(textColor);

		_healthLabel = new CharacterIconLabel(this, "icons/heart-straight.png") { Color = textColor };
		_voiceLabel = new CharacterIconLabel(this, "icons/microphone-stage.png") { Color = textColor };
		_artistLabel = new CharacterIconLabel(this, "icons/palette.png") { Color = textColor };

		_skill = new CharacterSkillDisplay(this) { Color = textColor };
		_skill.SetSize(new Vector2F(640));
		_skill.SetAnchor(Anchor.TopRight);
		_skill.SetOrigin(Anchor.TopRight);

		_bottom = new Element(this);
		_bottom.SetDock(Dock.Bottom);
		_bottom.SetSize(new Vector2F(0, 180));
		_bottom.SetDockPadding(new RectangleF());
		_bottom.SetBorderSize(0);
		_bottom.SetPaintBackgroundEnabled(true);
		_bottom.SetBgColor(this.GetBackgroundColor(GetScheme()));

		_line = new Element(_bottom);
		_line.SetDock(Dock.Top);
		_line.SetSize(new Vector2F(0, 3));
		_line.SetBorderSize(0);
		_line.SetPaintBackgroundEnabled(true);
		_line.SetBgColor(this.GetPrimaryColor(GetScheme()));

		_scroller = new CharacterSelectorScroller(_bottom);
		_scroller.SetDock(Dock.Top);
		_scroller.SetDockMargin(new RectangleF(20, 0, 0, 0));
		_scroller.SetSize(new Vector2F(0, 80));
		_scroller.SetBorderSize(0);
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

		_nameLabel.SetTextSize(CloneDashUI.GetFontSize(64) * ratio);
		_costumeLabel.SetTextSize(CloneDashUI.GetFontSize(36) * ratio);

		_healthLabel.Scale = _voiceLabel.Scale = _artistLabel.Scale = _skill.Scale = ratio;

		_nameLabel.SetPos(new Vector2F(32, 12));
		_costumeLabel.SetPos(new Vector2F(32, _nameLabel.GetPos().Y + _nameLabel.GetTextSize() - 20 * ratio));
		_healthLabel.SetPos(new Vector2F(32, _costumeLabel.GetPos().Y + _costumeLabel.GetTextSize() + 16 * ratio));
		_voiceLabel.SetPos(new Vector2F(32, _healthLabel.GetPos().Y + _healthLabel.GetSize().Y + 12 * ratio));
		_artistLabel.SetPos(new Vector2F(32, _voiceLabel.GetPos().Y + _voiceLabel.GetSize().Y + 12 * ratio));

		_skill.SetPos(new Vector2F(-32, 32));
	}

	private void BackPanel_CharacterSelected(ICharacterDescriptor? ch) {
		_lastCharacterSelected = ch;
		Character.SetCharacter(ch);

		HumanLanguage lang = HumanLanguage.GetCurrentLanguage();

		if (ch == null) {
			_nameLabel.SetText("<NULL>");
			_costumeLabel.SetText("<NULL>");
			_healthLabel.Text = "<NULL>";
			_voiceLabel.Text = "<NULL>";
			_artistLabel.Text = "<NULL>";

			_skill.Text = "<NULL>";
		}
		else {
			_nameLabel.SetText($"{ch.GetCharacterName(lang, out _)}");
			_costumeLabel.SetText($"{ch.GetCosplayName(lang, out _)}");
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