using Nucleus.Core;
using Nucleus.Rendering;
using Nucleus.Types;

namespace Nucleus.ModelEditor
{
	/// <summary>
	/// Pose tool, drag a bone's origin to move it or the rest of it to aim it
	/// With several bones selected, rotating uses IK up the chain
	/// </summary>
	public class PoseBonesOperator : DefaultOperator
	{
		private const float ORIGIN_RADIUS = 10;

		enum Mode { None, Translate, Rotate, Box }
		Mode mode;
		EditorBone? bone;
		List<EditorBone> chain = [];
		Vector2F grabOffset;
		Vector2F boxStart, boxEnd;


		private static bool OverOrigin(EditorPanel editorPanel, EditorBone editorBone, Vector2F mouseScreen)
			=> (editorPanel.GridToScreen(editorBone.GetWorldPosition()) - mouseScreen).Length <= ORIGIN_RADIUS;

		private static float WorldAngle(EditorBone editorBone) {
			var world = editorBone.WorldTransform;
			return MathF.Atan2(world.C, world.A).ToDegrees();
		}

		private static float Wrap(float degrees) => ((degrees % 360) + 540) % 360 - 180;

		public override bool ToolClick(EditorPanel editorPanel, Vector2F mouseScreen, bool ctrl) {
			// ctrl = normal toggle and box select
			if (ctrl) return false;
			var editor = ModelEditor.Active;
			boxStart = boxEnd = mouseScreen;
			switch (editorPanel.HoveredObject) {
				case null:
					bone = null;
					mode = Mode.Box;
					return true;
				case EditorBone hovered:
					bone = hovered;
					break;
				// images and the rest select like usual
				default:
					return false;
			}

			if (!editor.IsObjectSelected(bone))
				editor.SelectObject(bone);

			if (OverOrigin(editorPanel, bone, mouseScreen)) {
				mode = Mode.Translate;
				grabOffset = bone.GetWorldPosition() - editorPanel.ScreenToGrid(mouseScreen);
			}
			else {
				mode = Mode.Rotate;
				// The selected bones from this one up, the top one first
				chain.Clear();
				for (var current = bone; current != null; current = current.Parent) {
					if (editor.IsObjectSelected(current))
						chain.Insert(0, current);
				}
			}
			return true;
		}

		public override void ToolDrag(EditorPanel editorPanel, Vector2F mouseScreenStart, Vector2F mouseScreenNow) {
			var file = ModelEditor.Active.File;
			var target = editorPanel.ScreenToGrid(mouseScreenNow);
			switch (mode) {
				case Mode.Box:
					boxEnd = mouseScreenNow;
					break;
				case Mode.Translate when bone != null:
					file.MoveItemWorldspace(bone, target + grabOffset);
					break;
				case Mode.Rotate when bone != null:
					if (chain.Count <= 1)
						Aim(file, bone, target);
					else {
						// CCD: each bone from the tip up turns the tip toward the target
						for (int pass = 0; pass < 10; pass++) {
							for (int i = chain.Count - 1; i >= 0; i--) {
								var chainBone = chain[i];
								var origin = chainBone.GetWorldPosition();
								var tip = bone.WorldTransform.LocalToWorld(bone.Length, 0);
								var have = tip - origin;
								var want = target - origin;
								if (have.Length < 0.001f || want.Length < 0.001f) continue;
								float turn = Wrap(MathF.Atan2(want.Y, want.X).ToDegrees() - MathF.Atan2(have.Y, have.X).ToDegrees());
								if (MathF.Abs(turn) > 0.0001f)
									file.RotateItem(chainBone, turn);
							}
						}
					}
					break;
			}
		}

		private static void Aim(EditorFile file, EditorBone editorBone, Vector2F target) {
			var toTarget = target - editorBone.GetWorldPosition();
			if (toTarget.Length < 0.001f) return;
			float turn = Wrap(MathF.Atan2(toTarget.Y, toTarget.X).ToDegrees() - WorldAngle(editorBone));
			if (MathF.Abs(turn) > 0.0001f)
				file.RotateItem(editorBone, turn);
		}

		public override void ToolRelease(EditorPanel editorPanel, Vector2F mouseScreenStart, Vector2F mouseScreenEnd, bool dragged) {
			var editor = ModelEditor.Active;
			if (mode == Mode.Box) {
				if (!dragged)
					editor.UnselectAllObjects();
				else {
					var found = editorPanel.ItemsInBox(boxStart, boxEnd).OfType<EditorBone>().ToArray();
					editor.UnselectAllObjects();
					if (found.Length > 0)
						editor.SelectObjects(found);
				}
			}
			mode = Mode.None;
			bone = null;
			chain.Clear();
		}

		public override RectangleF? SelectionBox => mode == Mode.Box && (boxEnd - boxStart).Length > 3 ? EditorPanel.ScreenBox(boxStart, boxEnd) : null;

		// Translate ring on the origin, target on the tip
		public override void PaintOverlay(EditorPanel editorPanel, float width, float height) {
			if (!editorPanel.IsHovered() || mode == Mode.Box) return;
			var mouse = editorPanel.GetMousePos();
			var overlayBone = mode == Mode.None ? editorPanel.HoveredObject as EditorBone : bone;
			if (overlayBone == null) return;
			bool translate = mode == Mode.Translate || (mode == Mode.None && OverOrigin(editorPanel, overlayBone, mouse));
			Graphics2D.SetDrawColor(255, 255, 255, 220);
			if (translate) {
				var origin = editorPanel.GridToScreen(overlayBone.GetWorldPosition());
				Graphics2D.DrawRing(origin, ORIGIN_RADIUS - 1.5f, ORIGIN_RADIUS, 0, 360, 32);
			}
			else {
				var tip = editorPanel.GridToScreen(overlayBone.WorldTransform.LocalToWorld(overlayBone.Length, 0));
				Graphics2D.DrawRing(tip, 6, 7.5f, 0, 360, 24);
				Graphics2D.DrawLine(tip.X - 10, tip.Y, tip.X + 10, tip.Y, 1.5f);
				Graphics2D.DrawLine(tip.X, tip.Y - 10, tip.X, tip.Y + 10, 1.5f);
			}
		}
	}
}
