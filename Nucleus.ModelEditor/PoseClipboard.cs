using Nucleus.Models;
using Nucleus.Types;

namespace Nucleus.ModelEditor;

public static class PoseClipboard
{
	public readonly record struct Entry(EditorBone Bone, float X, float Y, float Rotation, float ScaleX, float ScaleY, float ShearX, float ShearY, Vector2F WorldPosition, float WorldAngle);

	private static List<Entry>? entries;
	public static IReadOnlyList<Entry>? Entries => entries;
	public static bool HasPose => entries is { Count: > 0 };

	static IEnumerable<EditorBone> selectedBones => ModelEditor.Active.SelectedObjects.OfType<EditorBone>();

	// direction of the bone's x axis
	private static float WorldAngle(Transformation world) {
		var direction = world.LocalToWorld(1, 0) - world.LocalToWorld(0, 0);
		return MathF.Atan2(direction.Y, direction.X) * 180 / MathF.PI;
	}

	public static bool Copy() {
		var bones = selectedBones.ToList();
		if (bones.Count == 0) return false;
		var copied = new List<Entry>(bones.Count);
		foreach (var bone in bones) {
			copied.Add(new Entry(bone, bone.GetTranslationX(), bone.GetTranslationY(), bone.GetRotation(),
				bone.GetScaleX(), bone.GetScaleY(), bone.GetShearX(), bone.GetShearY(),
				bone.WorldTransform.Translation, WorldAngle(bone.WorldTransform)));
		}
		entries = copied;
		return true;
	}

	private static int Depth(EditorBone bone) {
		int depth = 0;
		for (var parent = bone.Parent; parent != null; parent = parent.Parent)
			depth++;
		return depth;
	}

	public static bool Paste(bool mirrored = false) {
		if (entries == null) return false;
		var targets = selectedBones.ToList();
		if (targets.Count == 0) return false;
		bool world = mirrored || ModelEditor.Active.Editor.TransformMode == EditorTransformMode.WorldCoordinates;
		var root = targets[0].Model.Root.WorldTransform.Translation;

		int pairCount = Math.Min(targets.Count, entries.Count);
		var depths = new int[pairCount];
		var order = new List<int>(pairCount);
		for (int i = 0; i < pairCount; i++) {
			depths[i] = Depth(targets[i]);
			order.Add(i);
		}
		order.Sort((first, second) => {
			int compare = depths[first].CompareTo(depths[second]);
			return compare != 0 ? compare : first.CompareTo(second);
		});
		var pairs = new List<(EditorBone First, Entry Second)>(pairCount);
		foreach (int index in order)
			pairs.Add((targets[index], entries[index]));
		foreach (var pair in pairs) {
			var bone = pair.First;
			var entry = pair.Second;
			ModelEditor.Active.Actions.TrackTransform(bone);
			if (world) {
				var position = entry.WorldPosition;
				float angle = entry.WorldAngle;
				if (mirrored) {
					position = new(2 * root.X - position.X, position.Y);
					angle = 180 - angle;
				}
				var parent = bone.Parent?.WorldTransform;
				var local = parent?.WorldToLocal(position) ?? position;
				bone.EditTranslationX(local.X);
				bone.EditTranslationY(local.Y);
				float localAngle = angle;
				if (parent is Transformation parentTransform) {
					var radians = angle * MathF.PI / 180;
					var ahead = position + new Vector2F(MathF.Cos(radians), MathF.Sin(radians));
					var direction = parentTransform.WorldToLocal(ahead) - parentTransform.WorldToLocal(position);
					localAngle = MathF.Atan2(direction.Y, direction.X) * 180 / MathF.PI;
				}
				bone.EditRotation(localAngle);
			}
			else {
				bone.EditTranslationX(entry.X);
				bone.EditTranslationY(entry.Y);
				bone.EditRotation(entry.Rotation);
			}
			bone.EditScaleX(entry.ScaleX);
			bone.EditScaleY(entry.ScaleY);
			bone.EditShearX(entry.ShearX);
			bone.EditShearY(entry.ShearY);
			ModelEditor.Active.File.NotifyTransformed(bone);
		}
		return true;
	}
}
