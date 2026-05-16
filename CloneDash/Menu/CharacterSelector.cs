using CloneDash.Characters;
using CloneDash.Game;
using CloneDash.Systems;
using Nucleus;
using Nucleus.Commands;
using Nucleus.Common.Graphics;
using Nucleus.Common.Input;
using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Input;
using Nucleus.ManagedMemory;
using Nucleus.Types;
using Nucleus.UI;
using System.Diagnostics;
using Nucleus.Common.Types;
using CloneDash.Common;
using CloneDash.Common.UI;

namespace CloneDash.Menu;

public class CharacterButton : Button
{
	public string? CosplayName;
	public string? CharacterName;
	public ITexture? Texture;

	public void Setup(ReadOnlySpan<char> cosplay, ReadOnlySpan<char> character, ITexture? texture) {
		CosplayName = cosplay.Length == 0 ? null : new(cosplay);
		CharacterName = character.Length == 0 ? null : new(character);
		Image = texture;
		ImageOrientation = ImageOrientation.Zoom;
		BackgroundColor = new(0, 0, 0, 0);
		BorderSize = 0;
		ImagePadding = new(0, 0);
		Text = "";
	}

	public override void Paint(float width, float height) {
		base.Paint(width, height);
	}
}

public class CharacterSelectorScroller : Panel
{
	readonly List<(CharacterButton label, ICharacterDescriptor character)> chars = [];

	int lastSelectedIdx = -1;
	ICharacterDescriptor? lastSelected;

	public void SetCharacter(ICharacterDescriptor? chr) {
		lastSelectedIdx = chars.FindIndex(x => chr != null && x.character.UUIDEquals(chr));
		if (lastSelectedIdx == -1)
			Logs.Warn("Unexpectedly couldnt find the character???");

		for (int i = 0; i < chars.Count; i++) {
			var c = chars[i];
			c.label.ForegroundColor = i == lastSelectedIdx ? new(255, 255, 255, 255) : new(155, 155, 155, 255);
			c.label.Pulsing = i == lastSelectedIdx;
			c.label.DrawBackground = i == lastSelectedIdx;
		}

		InvalidateLayout();
	}

	protected override void Initialize() {
		var language = HumanLanguage.GetCurrentLanguage();
		foreach (var characterIdx in CharacterMod.GetAvailableCharacters()) {
			var character = CharacterMod.GetCharacterData(characterIdx);
			Debug.Assert(character != null);

			var lbl = Add<CharacterButton>();
			lbl.Setup(character.GetCosplayName(language, out _), character.GetCharacterName(language, out _),
				character.GetThumbnailTexture());
			lbl.BorderSize = 0;

			lbl.MouseClickEvent += (_, _, _) => PerformPick(character);
			chars.Add((lbl, character));
		}
	}

	public event Action<ICharacterDescriptor?>? CharacterSelected;

	void PerformPick(ICharacterDescriptor? character) {
		SetCharacter(character);
		CharacterSelected?.Invoke(character);
	}

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);
		SetupButtons(width, height);
	}

	private void SetupButtons(float width, float height) {
		for (int i = 0; i < chars.Count; i++) {
			var c = chars[i];
			var btn = c.label;
			var chr = c.character;

			float selectedSizeOffset = Math.Clamp(i == lastSelectedIdx ? 2 : (8 + (Math.Abs(i - lastSelectedIdx) * 1)),
				0, height);
			if (selectedSizeOffset == 0)
				btn.Visible = false;
			else {
				btn.Visible = true;
				btn.Size = new(height, height);
				float baseX = (width / 2) - (height / 2);
				float adjustedIndexX = baseX + (i * height);
				float adjustedSelectedX = adjustedIndexX - (lastSelectedIdx * height);
				btn.Position = new(adjustedSelectedX, 0);

				btn.Position += new Vector2F(selectedSizeOffset);
				btn.Size -= new Vector2F(selectedSizeOffset * 2);
			}
		}
	}
}

public class CharacterSelector : Panel, IMainMenuPanel
{
	public string GetName() => "Character Selector";
	public void OnHidden() { }
	public void OnShown() { }

	public void SetRichPresence() {
		RichPresenceSystem.SetPresence(new() { Details = "Main Menu", State = "Selecting a character" });
	}

	private Label _characterNameLabel = null!;
	private Label _characterCostumeLabel = null!;
	private Label _characterHpLabel = null!;
	private Label _characterAuthorLabel = null!;

	private ITexture _heartTexture = null!;
	private ITexture _voiceTexture = null!;
	private ITexture _starTexture = null!;

	private Panel _topRightPanel = null!;
	private Label _characterSkillLabel = null!;
	private Label _characterPerkLabel = null!;

	CharacterSelectorScroller backPanel = null!;
	CharacterPanel Character => Level.As<MainMenuLevel>().Character;

	protected override void Initialize() {
		base.Initialize();

		_heartTexture = Textures.LoadTextureFromFile("icons/heart-straight.png");
		_voiceTexture = Textures.LoadTextureFromFile("icons/microphone-stage.png");
		_starTexture = Textures.LoadTextureFromFile("icons/star.png");

		DockPadding = RectangleF.Zero;
		BackgroundColor = new Color(0, 0, 0, 0);
		OnHoverTest += Passthru;

		_characterNameLabel = Add<Label>();
		_characterNameLabel.AutoSize = true;
		_characterNameLabel.Font = CloneDashUI.FontBold;
		_characterNameLabel.TextColor = CloneDashUI.CharacterText;

		_characterCostumeLabel = Add<Label>();
		_characterCostumeLabel.AutoSize = true;
		_characterCostumeLabel.TextColor = CloneDashUI.CharacterText;

		_characterHpLabel = Add<Label>();
		_characterHpLabel.AutoSize = true;
		_characterHpLabel.TextColor = CloneDashUI.CharacterText;

		_characterAuthorLabel = Add<Label>();
		_characterAuthorLabel.AutoSize = true;
		_characterAuthorLabel.TextColor = CloneDashUI.CharacterText;

		_topRightPanel = Add<Panel>();
		_topRightPanel.DrawPanelBackground = false;
		_topRightPanel.Clipping = false;
		_topRightPanel.Origin = Anchor.TopRight;

		_characterSkillLabel = _topRightPanel.Add<Label>();
		_characterSkillLabel.Text = "Skill";
		_characterSkillLabel.Font = CloneDashUI.FontBold;
		_characterSkillLabel.TextColor = CloneDashUI.CharacterText;
		_characterSkillLabel.TextAlignment = Anchor.TopRight;
		_characterSkillLabel.Clipping = false;
		_characterSkillLabel.Dock = Dock.Top;
		_characterSkillLabel.TextPadding = new Vector2F(40, 0);

		_characterPerkLabel = _topRightPanel.Add<Label>();
		_characterPerkLabel.TextColor = CloneDashUI.CharacterText;
		_characterPerkLabel.TextOverflowMode = TextOverflowMode.WordWrap;
		_characterPerkLabel.TextAlignment = Anchor.TopRight;
		_characterPerkLabel.Clipping = false;
		_characterPerkLabel.Dock = Dock.Top;

		Panel bottom = Add<Panel>();
		bottom.Dock = Dock.Bottom;
		bottom.Size = new Vector2F(0, 180);
		bottom.BackgroundColor = GetBackgroundColor();
		bottom.BorderSize = 0;
		bottom.DockPadding = RectangleF.Zero;

		Panel line = bottom.Add<Panel>();
		line.Dock = Dock.Top;
		line.Size = new Vector2F(0, 3);
		line.BackgroundColor = GetPrimaryColor();
		line.BorderSize = 0;
		line.DockPadding = RectangleF.Zero;

		backPanel = bottom.Add<CharacterSelectorScroller>();
		backPanel.Dock = Dock.Top;
		backPanel.DynamicallySized = true;
		backPanel.Size = new Vector2F(0, 0.6f);
		backPanel.DockMargin = RectangleF.TLRB(20, 0, 0, 0);
		backPanel.BorderSize = 0;
		backPanel.DrawPanelBackground = false;
		backPanel.CharacterSelected += BackPanel_CharacterSelected;
		backPanel.DockPadding = RectangleF.Zero;

		var currentCharacter = CharacterMod.GetCharacterData();
		BackPanel_CharacterSelected(currentCharacter);
		backPanel.SetCharacter(currentCharacter);
	}

	private void SelectCharacter() {
		if (LastCharacterSelected == null) return;
		ConVar cv = cvar.FindVar("character")!;
		cv.SetValue(LastCharacterSelected.GetUUID());
		Character.PlayApplyExpression();
	}

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);

		float ratio = height / 900;

		_characterNameLabel.TextSize = CloneDashUI.GetFontSize(64) * ratio;
		_characterCostumeLabel.TextSize = CloneDashUI.GetFontSize(36) * ratio;
		_characterHpLabel.TextSize = CloneDashUI.GetFontSize(32) * ratio;
		_characterAuthorLabel.TextSize = CloneDashUI.GetFontSize(32) * ratio;

		_characterNameLabel.Position = new Vector2F(32, 12);
		_characterCostumeLabel.Position =
			new Vector2F(32, _characterNameLabel.Position.Y + _characterNameLabel.TextSize - 20 * ratio);
		_characterHpLabel.Position = new Vector2F(72,
			_characterCostumeLabel.Position.Y + _characterCostumeLabel.TextSize + 16 * ratio);
		_characterAuthorLabel.Position =
			new Vector2F(72, _characterHpLabel.Position.Y + _characterHpLabel.TextSize - 8 * ratio);

		_topRightPanel.Position = new Vector2F(width - 32, 32);
		_topRightPanel.Size = new Vector2F(640 * (width / 1600), 240);

		_characterSkillLabel.TextSize = CloneDashUI.GetFontSize(32) * ratio;

		_characterPerkLabel.TextSize = CloneDashUI.GetFontSize(20) * ratio;
		_characterPerkLabel.Size = _topRightPanel.Size;
		_characterPerkLabel.DockMargin = RectangleF.TLRB(12 * ratio, 0, 0, 0);
	}

	public override void Paint(float width, float height) {
		base.Paint(width, height);

		float ratio = height / 900;

		DrawImage(_characterHpLabel, _heartTexture);
		DrawImage(_characterAuthorLabel, _voiceTexture);
		DrawImage(_characterSkillLabel, _starTexture, width - 64, 32 + (8 * ratio));

		void DrawImage(Label label, ITexture spr, float? x = null, float? y = null) {
			Graphics2D.SetDrawColor(CloneDashUI.CharacterText);
			Graphics2D.SetTexture(spr);
			Graphics2D.DrawImage(new Vector2F(x ?? 32, y ?? label.Position.Y + (8 + ratio)), new Vector2F(32 * ratio));
		}
	}

	private void SelectedInfo_PaintOverride(Element self, float width, float height) {
	}

	ICharacterDescriptor? LastCharacterSelected;

	private void BackPanel_CharacterSelected(ICharacterDescriptor? ch) {
		LastCharacterSelected = ch;
		Character.SetCharacter(ch);

		var lang = HumanLanguage.GetCurrentLanguage();

		if (ch == null) {
			_characterNameLabel.Text = "<NULL>";
			_characterCostumeLabel.Text = "<NULL>";
			_characterHpLabel.Text = "<NULL>";
			_characterAuthorLabel.Text = "<NULL>";
			_characterPerkLabel.Text = "<NULL>";
		}
		else {
			_characterNameLabel.Text = $"{ch.GetCharacterName(lang, out _)}";
			_characterCostumeLabel.Text = $"{ch.GetCosplayName(lang, out _)}";
			_characterHpLabel.Text = $"{ch.GetQuirks().MaxHP}";
			_characterAuthorLabel.Text = $"{ch.GetAuthor(lang, out _)}";
			_characterPerkLabel.Text = $"{ch.GetPerk(lang, out _)}";
		}
	}

	public bool OnTryClose() {
		Character.SetCharacter(CharacterMod.GetCharacterData());
		return true;
	}

	public Color GetPrimaryColor() => CloneDashUI.CharacterPrimary;
	public Color GetBackgroundColor() => CloneDashUI.CharacterBackground;
	public (Action act, string name, string icon)? GetFooterAction() => (SelectCharacter, "Select", "icons/check.png");
}