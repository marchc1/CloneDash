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

	void IMainMenuPanel.SetRichPresence()
	{
		RichPresenceSystem.SetPresence(new RichPresenceState
		{
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

	private readonly Button _selectButton;
	private readonly CharacterSelectorScroller _scroller;

	private ICharacterDescriptor? _lastCharacterSelected;

	private MainMenuCharacter Character => Level.As<MainMenuLevel>().Character;

	public CharacterSelector(Element? parent) : base(parent) {
		SetBgColor(new Color(0, 0, 0, 0));
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

		Panel panel = new(this);
		panel.SetDock(Dock.Bottom);
		panel.DynamicallySized = true;
		panel.SetSize(new Vector2F(0, 0.125f));
		panel.SetBorderSize(0);
		panel.SetPaintBackgroundEnabled(false);
		panel.SetPaintBorderEnabled(false);
		panel.SetPaintEnabled(false);

		_selectButton = new Button(panel);
		_selectButton.SetDock(Dock.Right);
		_selectButton.SetSize(new Vector2F(0.15f));
		_selectButton.DynamicallySized = true;
		_selectButton.SetBgColor(new Color(10, 30, 10));
		_selectButton.SetFgColor(new Color(48, 220, 70));
		_selectButton.OnButtonClick += CharacterSelectButton_MouseReleaseEvent;

		_scroller = new CharacterSelectorScroller(this);
		_scroller.SetDock(Dock.Bottom);
		_scroller.DynamicallySized = true;
		_scroller.SetSize(new Vector2F(0, 0.1f));
		_scroller.SetBorderSize(0);
		_scroller.CharacterSelected += BackPanel_CharacterSelected;

		var currentCharacter = CharacterMod.GetCharacterData();
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
	}

	private void CharacterSelectButton_MouseReleaseEvent(Button self, ButtonCode button) {
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

		_selectButton.SetTextSize(80 * ratio);
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

		_selectButton.SetText("SELECT");
	}

	public bool OnTryClose() {
		Character.SetCharacter(CharacterMod.GetCharacterData());
		return true;
	}
}