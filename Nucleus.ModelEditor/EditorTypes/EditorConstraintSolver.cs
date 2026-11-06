namespace Nucleus.ModelEditor;

/// <summary>
/// Runs a model's constraints on top of the posed bones, writing straight into them
/// </summary>
public class EditorConstraintSolver(EditorModel model)
{
	static readonly Comparison<EditorConstraint> byOrder = (a, b) => a.Order.CompareTo(b.Order);

	readonly List<EditorConstraint> sorted = [];
	bool solvedLast;

	public void Update() {
		UpdateActiveBones();

		sorted.Clear();
		sorted.AddRange(model.IkConstraints);
		sorted.AddRange(model.TransformConstraints);
		sorted.AddRange(model.PathConstraints);

		var root = model.Root;
		if (root == null) return;

		if (sorted.Count == 0) {
			// drop whatever the last solve left behind
			if (solvedLast)
				root.InvalidateTransform();
			solvedLast = false;
			return;
		}
		sorted.Sort(byOrder);
		solvedLast = true;

		root.InvalidateTransform();
		foreach (var bone in model.GetAllBones())
			bone.ResetApplied();

		bool animating = ModelEditor.Active?.AnimationMode ?? false;
		var skin = model.ActiveSkin;
		foreach (var constraint in sorted) {
			if (!constraint.IsActiveIn(skin)) continue;
			constraint.Solve(animating);

			var bones = constraint.Bones;
			foreach (var bone in bones) {
				foreach (var child in bone.Children) {
					if (!bones.Contains(child))
						child.InvalidateTransform();
				}
			}
		}
	}

	private void UpdateActiveBones() {
		var bones = model.GetAllBones();
		foreach (var bone in bones)
			bone.IsActive = !bone.SkinRequired;

		var skin = model.ActiveSkin;
		if (skin == null) return;
		foreach (var skinBone in skin.Bones) {
			for (var bone = skinBone; bone != null; bone = bone.Parent)
				bone.IsActive = true;
		}
	}
}
