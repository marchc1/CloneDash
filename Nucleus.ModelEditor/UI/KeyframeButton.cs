using Nucleus.Common.Types;
using Nucleus.Types;
using Nucleus.UI;

namespace Nucleus.ModelEditor
{
	public class KeyframeButton : Button
	{
		public static readonly Color KEYFRAME_COLOR_NOT_KEYFRAMED = new Color(90, 240, 120);
		public static readonly Color KEYFRAME_COLOR_PENDING_KEYFRAME = new Color(240, 90, 20);
		public static readonly Color KEYFRAME_COLOR_ACTIVE_KEYFRAME = new Color(240, 30, 5);

		public KeyframeProperty Property { get; set; }
		public int ArrayIndex { get; set; } = -1;

		public override void Paint(float width, float height) {
			bool canInput = ModelEditor.Active.IsPropertyCurrentlyAnimatable(Property) && IsMouseInputEnabled();
			// TODO FIX ICONS ImagePadding = new(7);
			if (canInput) {
				// todo: determine background color
				var selected = ModelEditor.Active.LastSelectedObject;
				var timelineSearch = ModelEditor.Active.File.ActiveAnimation?.SearchTimelineByProperty(selected, Property, ArrayIndex, false);
				if (timelineSearch == null) {
					SetBgColor(KEYFRAME_COLOR_NOT_KEYFRAMED);
				}
				else {
					SetBgColor(timelineSearch.KeyframedAt(ModelEditor.Active.File.Timeline.Frame) switch {
						KeyframeState.NotKeyframed => KEYFRAME_COLOR_NOT_KEYFRAMED,
						KeyframeState.PendingKeyframe => KEYFRAME_COLOR_PENDING_KEYFRAME,
						KeyframeState.Keyframed => KEYFRAME_COLOR_ACTIVE_KEYFRAME
					});
				}
				// TODO FIX ICONS ImageColor = Color.Black;
			}
			else {
				SetBgColor(DefaultBackgroundColor);
				// TODO FIX ICONS ImageColor = Color.Gray;
			}

			base.Paint(width, height);
		}

		// TODO
		// public override bool CanInput() {
		// 	return ;
		// }

		public KeyframeButton(Element parent) : base(parent) {
			SetText("");
			// TODO FIX ICONS Image = Textures.LoadTextureFromFile("models/keyframe.png");
			// TODO FIX ICONS ImageOrientation = ImageOrientation.Centered;
		}
	}
}
