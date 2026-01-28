using CloneDash.Characters;
using CloneDash.Game;
using CloneDash.Systems;
using Nucleus;
using Nucleus.Input;
using Nucleus.UI;
using System.Diagnostics;

namespace CloneDash.Menu;

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
	readonly List<(Button label, ICharacterDescriptor character)> chars = [];
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

			var lbl = backPanel.Add<Button>();
			lbl.Text = characterInfo.GetName();
			lbl.Dock = Dock.Top;
			lbl.BorderSize = 0;
			lbl.Size = new(0, 32);

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
		InvalidateLayout();
	}
	public override void KeyPressed(in KeyboardState keyboardState, KeyboardKey key) {
		base.KeyPressed(keyboardState, key);
	}
}