using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;

namespace Nucleus.ModelEditor
{
	public class KeyframeButton : Button
	{
		public static readonly Raylib_cs.Color KEYFRAME_COLOR_NO_PENDING_KEYFRAME = new Raylib_cs.Color(90, 240, 120);
		public static readonly Raylib_cs.Color KEYFRAME_COLOR_PENDING_KEYFRAME = new Raylib_cs.Color(240, 90, 20);
		public static readonly Raylib_cs.Color KEYFRAME_COLOR_ACTIVE_KEYFRAME = new Raylib_cs.Color(240, 30, 5);

		public KeyframeProperty Property { get; set; }

		public override void Paint(float width, float height) {
			bool canInput = CanInput();
			bool isAnimateMode = ModelEditor.Active.AnimationMode;
			var activeAnimation = ModelEditor.Active.File.Models.First().ActiveAnimation;

			bool canUse = (canInput && ModelEditor.Active.IsPropertyCurrentlyAnimatable(Property));

			if (canUse) {
				// todo: determine background color
				BackgroundColor = KEYFRAME_COLOR_NO_PENDING_KEYFRAME;
				ImageColor = Color.Black;
			}
			else {
				BackgroundColor = DefaultBackgroundColor;
				ImageColor = Color.Gray;
			}

			base.Paint(width, height);
		}

		public override bool CanInput() {
			if (ModelEditor.Active.File.Models.First().ActiveAnimation == null) return false;

			return base.CanInput();
		}

		protected override void Initialize() {
			Text = "";
			Image = Textures.LoadTextureFromFile("models/keyframe.png");
			ImageOrientation = ImageOrientation.Centered;
		}
	}
}
