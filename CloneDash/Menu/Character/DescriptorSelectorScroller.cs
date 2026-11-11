using CloneDash.Common;
using CloneDash.Common.Compatibility;
using Nucleus;
using Nucleus.Common.Graphics;
using Nucleus.Common.Types;
using Nucleus.Types;
using Nucleus.UI;
using System.Diagnostics;

namespace CloneDash.Menu.Character
{
	public class DescriptorSelectorScroller<TDescriptor> : Element where TDescriptor : class, ICloneDashModDescriptor
	{
		public event Action<TDescriptor?>? Selected;

		private readonly List<(CharacterButton label, TDescriptor? descriptor)> _entries = [];
		private int _lastSelectedIdx = -1;

		public DescriptorSelectorScroller(Element? parent, IEnumerable<TDescriptor?> descriptors, Func<TDescriptor?, ITexture?> getThumbnail) : base(parent) {
			foreach (TDescriptor? descriptor in descriptors) {
				CharacterButton lbl = new(this, getThumbnail(descriptor));
				lbl.OnButtonClick += (_, _) => PerformPick(descriptor);
				_entries.Add((lbl, descriptor));
			}
		}

		public void Cycle(int by = 0) {
			_lastSelectedIdx += by;
			if (_lastSelectedIdx > _entries.Count - 1) _lastSelectedIdx = 0;
			else if (_lastSelectedIdx < 0) _lastSelectedIdx = _entries.Count - 1;
			PerformPick(_entries[_lastSelectedIdx].descriptor);
		}

		public void SetSelected(TDescriptor? descriptor) {
			_lastSelectedIdx = _entries.FindIndex(x => x.descriptor.UUIDEquals(descriptor));
			if (_lastSelectedIdx == -1) Logs.Warn("Unexpectedly couldnt find the descriptor???");
			InvalidateLayout();
		}

		private void PerformPick(TDescriptor? descriptor) {
			SetSelected(descriptor);
			Selected?.Invoke(descriptor);
		}

		protected override void PerformLayout(float width, float height) {
			base.PerformLayout(width, height);

			float x = 0;
			float centerX = 0;

			for (int i = 0; i < _entries.Count; i++) {
				(CharacterButton label, TDescriptor? descriptor) c = _entries[i];
				CharacterButton btn = c.label;

				const int maxIndex = 8;
				int offsetIndex = i - _lastSelectedIdx;
				int clampedIndex = Math.Min(Math.Abs(offsetIndex), maxIndex);
				float scale = 1f - .4f * (clampedIndex / (float)maxIndex);
				float size = scale * height;

				btn.Position = new Vector2F(x /*offsetIndex * (size + 24)*/, (height - size) / 2f);
				btn.Size = new Vector2F(size, size);

				if (offsetIndex == 0) centerX = x;
				x += size + 24;
			}

			ChildRenderOffset = new Vector2F(-centerX, 0);
		}
	}
}
