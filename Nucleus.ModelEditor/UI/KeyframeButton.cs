using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;

namespace Nucleus.ModelEditor
{
	public class KeyframeButton : Button
	{
		public static readonly Raylib_cs.Color KEYFRAME_COLOR_NOT_KEYFRAMED = new Raylib_cs.Color(90, 240, 120);
		public static readonly Raylib_cs.Color KEYFRAME_COLOR_PENDING_KEYFRAME = new Raylib_cs.Color(240, 90, 20);
		public static readonly Raylib_cs.Color KEYFRAME_COLOR_ACTIVE_KEYFRAME = new Raylib_cs.Color(240, 30, 5);

		public KeyframeProperty Property { get; set; }
		public int ArrayIndex { get; set; } = -1;

		public override void Paint(float width, float height) {
			bool canInput = CanInput();
			if (canInput) {
				// todo: determine background color
				var selected = ModelEditor.Active.LastSelectedObject;
				var timelineSearch = ModelEditor.Active.File.ActiveAnimation?.SearchTimelineByProperty(selected, Property, ArrayIndex, false);
				if (timelineSearch == null) {
					BackgroundColor = KEYFRAME_COLOR_NOT_KEYFRAMED;
				}
				else {
					BackgroundColor = timelineSearch.KeyframedAt(ModelEditor.Active.File.Timeline.Frame) switch {
						KeyframeState.NotKeyframed => KEYFRAME_COLOR_NOT_KEYFRAMED,
						KeyframeState.PendingKeyframe => KEYFRAME_COLOR_PENDING_KEYFRAME,
						KeyframeState.Keyframed => KEYFRAME_COLOR_ACTIVE_KEYFRAME
					};
				}
				ImageColor = Color.Black;
			}
			else {
				BackgroundColor = DefaultBackgroundColor;
				ImageColor = Color.Gray;
			}

			base.Paint(width, height);
		}

		public override bool CanInput() {
			return ModelEditor.Active.IsPropertyCurrentlyAnimatable(Property) && base.CanInput();
		}

		protected override void Initialize() {
			Text = "";
			Image = Textures.LoadTextureFromFile("models/keyframe.png");
			ImageOrientation = ImageOrientation.Centered;
		}
	}
}
