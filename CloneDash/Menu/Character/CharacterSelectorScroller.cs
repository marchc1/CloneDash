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

		public void Cycle(int by = 0) {
			_lastSelectedIdx += by;
			if (_lastSelectedIdx > _chars.Count - 1) _lastSelectedIdx = 0;
			else if (_lastSelectedIdx < 0) _lastSelectedIdx = _chars.Count - 1;
			PerformPick(_chars[_lastSelectedIdx].character);
		}

		public void SetCharacter(ICharacterDescriptor? chr) {
			_lastSelectedIdx = _chars.FindIndex(x => chr != null && x.character.UUIDEquals(chr));
			if (_lastSelectedIdx == -1) Logs.Warn("Unexpectedly couldnt find the character???");
			InvalidateLayout();
		}

		private void PerformPick(ICharacterDescriptor? character) {
			SetCharacter(character);
			CharacterSelected?.Invoke(character);
		}

		protected override void PerformLayout(float width, float height) {
			base.PerformLayout(width, height);

			float x = 0;
			float centerX = 0;
			
			for (int i = 0; i < _chars.Count; i++) {
				(CharacterButton label, ICharacterDescriptor character) c = _chars[i];
				CharacterButton btn = c.label;

				const int maxIndex = 8;
				int offsetIndex = i - _lastSelectedIdx;
				int clampedIndex = Math.Min(Math.Abs(offsetIndex), maxIndex);
				float scale = 1f - .4f * (clampedIndex / (float)maxIndex);
				float size = scale * height;
				
				btn.SetPos(new Vector2F(x /*offsetIndex * (size + 24)*/, (height - size) / 2f));
				btn.SetSize(new Vector2F(size, size));

				if (offsetIndex == 0) centerX = x;
				x += size + 24;
			}
			
			SetChildRenderOffset(new Vector2F(-centerX, 0));
		}
	}
}