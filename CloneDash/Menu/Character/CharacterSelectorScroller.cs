using CloneDash.Characters;
using CloneDash.Common;
using Nucleus;
using Nucleus.Common.Types;
using Nucleus.Types;
using Nucleus.UI;
using System.Diagnostics;

namespace CloneDash.Menu.Character
{
	public class CharacterSelectorScroller : Element
	{
		public event Action<ICharacterDescriptor?>? CharacterSelected;
		
		private readonly List<(CharacterButton label, ICharacterDescriptor character)> _chars = [];
		private int _lastSelectedIdx = -1;

		public CharacterSelectorScroller(Element? parent) : base(parent) {
			foreach (string characterIdx in CharacterMod.GetAvailableCharacters()) {
				ICharacterDescriptor? character = CharacterMod.GetCharacterData(characterIdx);
				Debug.Assert(character != null);

				CharacterButton lbl = new(this, character.GetThumbnailTexture());
				lbl.OnButtonClick += (_, _) => PerformPick(character);
				_chars.Add((lbl, character));
			}
		}

		public void SetCharacter(ICharacterDescriptor? chr) {
			_lastSelectedIdx = _chars.FindIndex(x => chr != null && x.character.UUIDEquals(chr));
			if (_lastSelectedIdx == -1)
				Logs.Warn("Unexpectedly couldnt find the character???");

			for (int i = 0; i < _chars.Count; i++) {
				var c = _chars[i];
				c.label.SetFgColor(i == _lastSelectedIdx ? new Color(255, 255, 255, 255) : new Color(155, 155, 155, 255));
				c.label.Pulsing = i == _lastSelectedIdx;
				c.label.SetPaintBackgroundEnabled(i == _lastSelectedIdx);
			}

			InvalidateLayout();
		}

		private void PerformPick(ICharacterDescriptor? character) {
			SetCharacter(character);
			CharacterSelected?.Invoke(character);
		}

		protected override void PerformLayout(float width, float height) {
			base.PerformLayout(width, height);
			SetupButtons(width, height);
		}

		private void SetupButtons(float width, float height) {
			for (int i = 0; i < _chars.Count; i++) {
				(CharacterButton label, ICharacterDescriptor character) c = _chars[i];
				CharacterButton btn = c.label;

				float selectedSizeOffset = Math.Clamp(i == _lastSelectedIdx ? 2 : 8 + (Math.Abs(i - _lastSelectedIdx) * 1), 0, height);
				if (selectedSizeOffset == 0)
					btn.SetVisible(false);
				else {
					btn.SetVisible(true);
					btn.SetSize(new Vector2F(height, height));
					float baseX = (width / 2) - (height / 2);
					float adjustedIndexX = baseX + (i * height);
					float adjustedSelectedX = adjustedIndexX - (_lastSelectedIdx * height);
					btn.SetPos(new Vector2F(adjustedSelectedX, 0));

					btn.SetPos(btn.GetPos() + new Vector2F(selectedSizeOffset));
					btn.SetSize(btn.GetSize() - new Vector2F(selectedSizeOffset));
				}
			}
		}
	}
}