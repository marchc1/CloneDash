using Nucleus.Models;
using Nucleus.Types;

namespace Nucleus.ModelEditor;

/// <summary>
/// Reads and writes transform values in the toolbar's Local/Parent/World axes.
/// </summary>
public static class TransformAxes
{
	public static EditorTransformMode Mode => ModelEditor.Active.Editor.TransformMode;

	private static Transformation? ParentWorld(IEditorType item) => item switch {
		EditorBone bone => bone.Parent?.WorldTransform,
		EditorAttachment attachment => attachment.Slot.Bone.WorldTransform,
		_ => null
	};

	private static Transformation? World(IEditorType item) => item switch {
		EditorBone bone => bone.WorldTransform,
		EditorRegionAttachment region => region.WorldTransform,
		EditorVertexAttachment vertices => vertices.WorldTransform,
		_ => null
	};

	private static Vector2F Rotate(Vector2F vector, float degrees) {
		var sinCos = MathF.SinCos(degrees * MathF.PI / 180);
		return new(vector.X * sinCos.Cos - vector.Y * sinCos.Sin, vector.X * sinCos.Sin + vector.Y * sinCos.Cos);
	}

	// Rotation
	/// <summary>
	/// World rotation is always 0-360, even in animate mode
	/// </summary>
	public static float GetRotation(IEditorType item, EditorTransformMode mode)
		=> mode == EditorTransformMode.WorldCoordinates ? Wrap(item.GetWorldRotation()) : item.GetRotation();

	private static float Wrap(float angle) => angle is >= 0 and < 360 ? angle : (float)(((double)angle % 360 + 360) % 360);

	public static void SetRotation(IEditorType item, float value, EditorTransformMode mode)
		=> item.EditRotation(value, mode != EditorTransformMode.WorldCoordinates);

	// Translation
	public static Vector2F GetTranslation(IEditorType item, EditorTransformMode mode) {
		var parentSpace = new Vector2F(item.GetTranslationX(), item.GetTranslationY());
		return mode switch {
			EditorTransformMode.LocalCoordinates => Rotate(parentSpace, -item.GetRotation()),
			EditorTransformMode.WorldCoordinates => ParentWorld(item) is Transformation parent ? parent.LocalToWorld(parentSpace) : parentSpace,
			_ => parentSpace
		};
	}

	public static void SetTranslation(IEditorType item, int axis, float value, EditorTransformMode mode) {
		var shown = GetTranslation(item, mode);
		shown = axis == 0 ? new(value, shown.Y) : new(shown.X, value);

		var parentSpace = mode switch {
			EditorTransformMode.LocalCoordinates => Rotate(shown, item.GetRotation()),
			EditorTransformMode.WorldCoordinates => ParentWorld(item) is Transformation parent ? parent.WorldToLocal(shown) : shown,
			_ => shown
		};
		item.EditTranslationX(parentSpace.X);
		item.EditTranslationY(parentSpace.Y);
	}

	// Scale
	public static Vector2F GetScale(IEditorType item, EditorTransformMode mode) {
		var local = new Vector2F(item.GetScaleX(), item.GetScaleY());
		if (mode != EditorTransformMode.WorldCoordinates || World(item) is not Transformation world) return local;
		// item and all its parents combined
		return new(MathF.Sqrt(world.A * world.A + world.C * world.C) * MathF.Sign(local.X), MathF.Sqrt(world.B * world.B + world.D * world.D) * MathF.Sign(local.Y));
	}

	/// <summary>
	/// Sets one scale axis in the given axes.
	/// World scale can't be set directly, it scales the local scale by the same ratio
	/// </summary>
	public static void SetScale(IEditorType item, int axis, float value, EditorTransformMode mode) {
		float local = axis == 0 ? item.GetScaleX() : item.GetScaleY();
		float shown = axis == 0 ? GetScale(item, mode).X : GetScale(item, mode).Y;
		float result = mode == EditorTransformMode.WorldCoordinates && shown != 0 ? local * value / shown : value;
		if (axis == 0)
			item.EditScaleX(result);
		else
			item.EditScaleY(result);
	}

	public static float GizmoRotation(IEditorType item, EditorTransformMode mode) => mode switch {
		EditorTransformMode.WorldCoordinates => 0,
		EditorTransformMode.ParentCoordinates => ParentWorld(item) is Transformation parent ? MathF.Atan2(parent.C, parent.A) * 180 / MathF.PI : 0,
		_ => item.GetScreenRotation()
	};
}
