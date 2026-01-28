using CloneDash.Characters;
using CloneDash.Game;
using CloneDash.Systems;
using Nucleus;
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
	public Texture? Texture;

	public void Setup(string? cosplay, string? character, ITexture? texture) {
		CosplayName = cosplay;
		CharacterName = character;
		Texture = (Texture?)texture;
	}

	public override void Paint(float width, float height) {
		ColorStateSetup(this, out var back, out var fore);

		Graphics2D.SetDrawColor(back);
		var whd2 = new Vector2F(width / 2, width / 2);
		var whd3 = new Vector2F(width / 3, width / 3);
		if (DrawAsCircle)
			Graphics2D.DrawCircle(whd2, whd3);
		else
			Graphics2D.DrawRectangle(0, 0, width, height);

		Graphics2D.SetDrawColor(new(160, 160, 160));
		Graphics2D.DrawText(new Vector2F(48, 4) + Anchor.TopLeft.GetPositionGivenAlignment(RenderBounds.Size, TextPadding), CosplayName, Graphics2D.UI_FONT_NAME, 18, Anchor.TopLeft);
		Graphics2D.SetDrawColor(new(255, 255, 255));
		Graphics2D.DrawText(new Vector2F(48, 0) + Anchor.BottomLeft.GetPositionGivenAlignment(RenderBounds.Size, TextPadding), CharacterName, Graphics2D.UI_FONT_NAME, 24, Anchor.BottomLeft);
		if (Texture != null) {
			Graphics2D.SetTexture(Texture);
			Graphics2D.DrawImage(new((height / 2) - (32 / 2), 8, 32, 32));
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
	readonly List<(CharacterButton label, ICharacterDescriptor character)> chars = [];
	Panel backPanel = null!;
	CharacterPanel Character = null!;
	protected override void Initialize() {
		base.Initialize();
		backPanel = Add<Panel>();
		backPanel.Dock = Dock.Left;
		backPanel.DynamicallySized = true;
		backPanel.Size = new(0.25f, 0);
		backPanel.BorderSize = 0;
		foreach (var character in CharacterMod.GetAvailableCharacters()) {
			var characterInfo = CharacterMod.GetCharacterData(character);
			Debug.Assert(characterInfo != null);

			var lbl = backPanel.Add<CharacterButton>();
			lbl.Setup(characterInfo.GetCosplayName(), characterInfo.GetCharacterName(), characterInfo.GetThumbnailTexture());
			lbl.Dock = Dock.Top;
			lbl.BorderSize = 0;
			lbl.Size = new(0, 48);

			chars.Add((lbl, characterInfo));
		}

		Add(out Character);
		Character.Dock = Dock.Fill;

		var currentCharacter = CharacterMod.GetCharacterData();
		PerformPick(currentCharacter);
		SetupButtons();
	}
	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);

		SetupButtons();
	}
	ICharacterDescriptor? lastSelected;
	int lastSelectedIdx = -1;
	private void SetupButtons() {
		lastSelectedIdx = chars.FindIndex(x => x.character.GetName() == lastSelected?.GetName());
		if (lastSelectedIdx == -1) {
			Logs.Warn("Unexpectedly couldnt find the character???");
			return;
		}

		backPanel.ChildRenderOffset = new(0, (RenderBounds.Height / 2) - 16 - (lastSelectedIdx * 34));
	}

	public void PerformPick(ICharacterDescriptor? character) {
		for (int i = 0; i < chars.Count; i++) {
			var c = chars[i];
			c.label.ForegroundColor = i == lastSelectedIdx ? new(255, 255, 255, 255) : new(155, 155, 155, 255);
			c.label.Pulsing = i == lastSelectedIdx;

			c.label.MouseClickEvent += (_, _, _) => PerformPick(c.character);
		}
		Character.SetCharacter(character);
		lastSelected = character;
		SetupButtons();
	}
	public override void KeyPressed(in KeyboardState keyboardState, KeyboardKey key) {
		base.KeyPressed(keyboardState, key);
	}
}