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

	protected override void Initialize()
	{
		var language = HumanLanguage.GetCurrentLanguage();
		foreach (var characterIdx in CharacterMod.GetAvailableCharacters()) {
			var character = CharacterMod.GetCharacterData(characterIdx);
			Debug.Assert(character != null);

			var lbl = Add<CharacterButton>();
			lbl.Setup(character.GetCosplayName(language, out _), character.GetCharacterName(language, out _), character.GetThumbnailTexture());
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

			float selectedSizeOffset = Math.Clamp(i == lastSelectedIdx ? 2 : (8 + (Math.Abs(i - lastSelectedIdx) * 1)), 0, height);
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
		RichPresenceSystem.SetPresence(new() {
			Details = "Main Menu",
			State = "Selecting a character"
		});
	}
	Panel selectedInfo = null!;
	Label characterNameLabel = null!;
	Label characterCostumeLabel = null!;
	Label characterHPLabel = null!;
	Label characterAuthorLabel = null!;
	Label characterPerkLabel = null!;
	CharacterSelectorScroller backPanel = null!;
	CharacterPanel Character => Level.As<MainMenuLevel>().Character;
	protected override void Initialize() {
		base.Initialize();

		BackgroundColor = new Color(0, 0, 0, 0);
		OnHoverTest += Passthru;
		
		selectedInfo = Add<Panel>();
		selectedInfo.Dock = Dock.Bottom;
		selectedInfo.DynamicallySized = true;
		selectedInfo.Size = new(0, 0.125f);
		selectedInfo.BorderSize = 0;
		selectedInfo.PaintOverride += SelectedInfo_PaintOverride;

		characterNameLabel = Add<Label>();
		characterNameLabel.AutoSize = true;

		characterCostumeLabel = Add<Label>();
		characterCostumeLabel.AutoSize = true;

		characterHPLabel = Add<Label>();
		characterHPLabel.AutoSize = true;

		characterAuthorLabel = Add<Label>();
		characterAuthorLabel.AutoSize = true;
		characterAuthorLabel.Origin = Anchor.TopRight;

		characterPerkLabel = selectedInfo.Add<Label>();
		characterPerkLabel.TextOverflowMode = TextOverflowMode.WordWrap;
		characterPerkLabel.DockMargin = RectangleF.TLRB(8, 32, 32, 8);
		characterPerkLabel.TextAlignment = Anchor.CenterLeft;
		characterPerkLabel.TextPadding = new(0, 0);
		characterPerkLabel.Dock = Dock.Fill;
		characterPerkLabel.TextSize = 24;
		characterPerkLabel.DynamicallySized = true;
		characterPerkLabel.BackgroundColor = new(100, 100, 100, 100); // temp

		backPanel = Add<CharacterSelectorScroller>();
		backPanel.Dock = Dock.Bottom;
		backPanel.DynamicallySized = true;
		backPanel.Size = new(0, 0.1f);
		backPanel.BorderSize = 0;
		backPanel.CharacterSelected += BackPanel_CharacterSelected;

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

		characterNameLabel.TextSize = 80 * ratio;
		characterCostumeLabel.TextSize = 40 * ratio;
		characterHPLabel.TextSize = 40 * ratio;
		characterAuthorLabel.TextSize = 28 * ratio;

		characterNameLabel.Position = new(32, 10 * ratio);
		characterCostumeLabel.Position = new(32, 72 * ratio);
		characterHPLabel.Position = new(32, 104 * ratio);
		characterAuthorLabel.Position = new(width - 32, 48 * ratio);
	}

	private void SelectedInfo_PaintOverride(Element self, float width, float height) {
	
	}

	ICharacterDescriptor? LastCharacterSelected;

	private void BackPanel_CharacterSelected(ICharacterDescriptor? ch) {
		LastCharacterSelected = ch;
		Character.SetCharacter(ch);

		var lang = HumanLanguage.GetCurrentLanguage();

		if (ch == null) {
			characterNameLabel.Text = "<NULL>";
			characterCostumeLabel.Text = "<NULL>";
			characterAuthorLabel.Text = "<NULL>";
			characterPerkLabel.Text = "<NULL>";
		}
		else {
			characterNameLabel.Text = $"{ch.GetCharacterName(lang, out _)}";
			characterCostumeLabel.Text = $"{ch.GetCosplayName(lang, out _)}";
			characterAuthorLabel.Text = $"Author: {ch.GetAuthor(lang, out _)}";
			characterPerkLabel.Text = $"{ch.GetPerk(lang, out _)}";
		}
	}

	public bool OnTryClose()
	{
		Character.SetCharacter(CharacterMod.GetCharacterData());
		return true;
	}

	public Color GetPrimaryColor() => CloneDashUI.CharacterPrimary;
	public Color GetBackgroundColor() => CloneDashUI.CharacterBackground;
	public (Action act, string name, string icon)? GetFooterAction() => (SelectCharacter, "Select", "icons/check.png");
}