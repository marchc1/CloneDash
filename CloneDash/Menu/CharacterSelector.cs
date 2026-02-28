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

namespace CloneDash.Menu;

public class CharacterButton : Button
{
	public string? CosplayName;
	public string? CharacterName;
	public ITexture? Texture;

	public void Setup(string? cosplay, string? character, ITexture? texture) {
		CosplayName = cosplay;
		CharacterName = character;
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
		lastSelectedIdx = chars.FindIndex(x => x.character.GetUniqueID() == chr?.GetUniqueID());
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
		foreach (var character in CharacterMod.GetAvailableCharacters()) {
			var characterInfo = CharacterMod.GetCharacterData(character);
			Debug.Assert(characterInfo != null);

			var lbl = Add<CharacterButton>();
			lbl.Setup(characterInfo.GetCosplayName(), characterInfo.GetCharacterName(), characterInfo.GetThumbnailTexture());
			lbl.BorderSize = 0;

			lbl.MouseClickEvent += (_, _, _) => PerformPick(characterInfo);
			chars.Add((lbl, characterInfo));
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
	Button characterSelectButton = null!;
	CharacterSelectorScroller backPanel = null!;
	CharacterPanel Character = null!;
	protected override void Initialize() {
		base.Initialize();
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

		characterSelectButton = selectedInfo.Add<Button>();
		characterSelectButton.Dock = Dock.Right;
		characterSelectButton.Size = new(0.1f);
		characterSelectButton.DynamicallySized = true;
		characterSelectButton.BackgroundColor = new(10, 30, 10);
		characterSelectButton.ForegroundColor = new(48, 220, 70);
		characterSelectButton.MouseReleaseEvent += CharacterSelectButton_MouseReleaseEvent;

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

		Add(out Character);
		Character.Dock = Dock.Fill;

		var currentCharacter = CharacterMod.GetCharacterData();
		BackPanel_CharacterSelected(currentCharacter);
		backPanel.SetCharacter(currentCharacter);
	}

	private void CharacterSelectButton_MouseReleaseEvent(Element self, FrameState state, ButtonCode button) {
		if (LastCharacterSelected == null) return;
		ConVar cv = cvar.FindVar("character")!;
		cv.SetValue(LastCharacterSelected.GetUniqueID());
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

		characterNameLabel.Text = ch?.GetCharacterName() ?? "<NULL>";
		characterCostumeLabel.Text = ch?.GetCosplayName() ?? "<NULL>";
		characterHPLabel.Text = $"HP: {ch?.GetDefaultHP() ?? 0}";
		characterAuthorLabel.Text = $"Author: {ch?.GetAuthor() ?? "<NULL>"}";
		characterPerkLabel.Text = ch?.GetPerk() ?? "<NULL>";

		characterSelectButton.Text = "SELECT";
	}
}