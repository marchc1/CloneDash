using Nucleus.Models;
using Nucleus.Types;

namespace Nucleus.ModelEditor;

/// <summary>
/// Keeps child bones and attachments in place in world space while a bone is transformed.
/// Child bones that don't inherit normally only keep their position
/// </summary>
public static class Compensation
{
	public readonly record struct Held(IEditorType Item, Transformation World);

	private static Transformation AttachmentWorld(EditorAttachment attachment, Transformation bone) =>
		attachment switch {
			EditorRegionAttachment region => Transformation.CalculateWorldTransformation(
				region.Position, region.Rotation, region.Scale, Vector2F.Zero, TransformMode.Normal, bone),
			EditorVertexAttachment vertices => Transformation.CalculateWorldTransformation(
				vertices.Position, vertices.Rotation, vertices.Scale, Vector2F.Zero, TransformMode.Normal, bone),
			_ => default
		};

	/// <summary>
	/// An attachment's current world transform, or null if it doesn't have its own
	/// </summary>
	public static Held? Hold(EditorAttachment attachment) {
		if (attachment is EditorVertexAttachment { Weights.Count: > 0 }) return null;
		if (attachment is not (EditorRegionAttachment or EditorVertexAttachment)) return null;
		return new(attachment, AttachmentWorld(attachment, attachment.Slot.Bone.WorldTransform));
	}

	public static void Capture(EditorBone bone, bool bones, bool images, ICollection<IEditorType> skip, List<Held> into) {
		if (bones) {
			foreach (var child in bone.Children) {
				if (!skip.Contains(child))
					into.Add(new(child, child.WorldTransform));
			}
		}

		if (images) {
			foreach (var slot in bone.Slots) {
				foreach (var attachment in slot.Attachments) {
					if (skip.Contains(attachment)) continue;
					// weighted vertices follow their bones
					if (attachment is EditorVertexAttachment { Weights.Count: > 0 }) continue;
					if (attachment is EditorRegionAttachment or EditorVertexAttachment)
						into.Add(new(attachment, AttachmentWorld(attachment, bone.WorldTransform)));
				}
			}
		}
	}

	private static float Nearest(float angle, float to) => to + ((angle - to) % 360 + 540) % 360 - 180;

	/// <summary>
	/// Splits a local 2x2 into rotation/scale/shearY, keeping the given shearX.
	/// </summary>
	private static void Decompose(float ra, float rb, float rc, float rd, float shearX, out float rotation, out float scaleX, out float scaleY, out float shearY) {
		scaleX = MathF.Sqrt(ra * ra + rc * rc);
		if (scaleX > 0.0001f) {
			// reflection ends up in scaleY
			float sign = ra * rd - rb * rc < 0 ? -1 : 1;
			scaleY = sign * MathF.Sqrt(rb * rb + rd * rd);
			rotation = MathF.Atan2(rc, ra).ToDegrees() - shearX;
			shearY = MathF.Atan2(rd * sign, rb * sign).ToDegrees() - 90 - rotation;
		}
		else {
			scaleX = 0;
			scaleY = MathF.Sqrt(rb * rb + rd * rd);
			shearY = 0;
			rotation = 90 - MathF.Atan2(rd, rb).ToDegrees();
		}
	}

	/// <summary>
	/// Puts a held item back at its old world transform
	/// </summary>
	public static void Restore(Held held) {
		var world = held.World;
		switch (held.Item) {
			case EditorBone bone: {
				if (bone.Parent == null) return;
				var parent = bone.Parent.WorldTransform;

				var local = parent.WorldToLocal(world.X, world.Y);
				bone.EditTranslationX(local.X);
				bone.EditTranslationY(local.Y);

				if (bone.TransformMode != TransformMode.Normal) return;

				parent.Decompose(out var pa, out var pb, out var pc, out var pd, out _, out _);
				float pid = 1 / (pa * pd - pb * pc);
				float ia = pid * pd, id = pid * pa, ib = pid * pb, ic = pid * pc;
				float ra = ia * world.A - ib * world.C, rb = ia * world.B - ib * world.D;
				float rc = id * world.C - ic * world.A, rd = id * world.D - ic * world.B;

				Decompose(ra, rb, rc, rd, bone.GetShearX(), out var rotation, out var scaleX, out var scaleY, out var shearY);
				bone.EditRotation(Nearest(rotation, bone.GetRotation()));
				bone.EditScaleX(scaleX);
				bone.EditScaleY(scaleY);
				bone.EditShearY(Nearest(shearY, bone.GetShearY()));
				break;
			}
			case EditorAttachment attachment: {
				var parent = attachment.Slot.Bone.WorldTransform;
				var local = parent.WorldToLocal(world.X, world.Y);

				parent.Decompose(out var pa, out var pb, out var pc, out var pd, out _, out _);
				float pid = 1 / (pa * pd - pb * pc);
				float ia = pid * pd, id = pid * pa, ib = pid * pb, ic = pid * pc;
				float ra = ia * world.A - ib * world.C, rb = ia * world.B - ib * world.D;
				float rc = id * world.C - ic * world.A, rd = id * world.D - ic * world.B;

				// Attachments have no shear, leftover skew is dropped.
				Decompose(ra, rb, rc, rd, 0, out var rotation, out var scaleX, out var scaleY, out _);
				attachment.EditTranslationX(local.X);
				attachment.EditTranslationY(local.Y);
				attachment.EditRotation(Nearest(rotation, attachment.GetRotation()));
				attachment.EditScaleX(scaleX);
				attachment.EditScaleY(scaleY);
				break;
			}
		}
	}
}
