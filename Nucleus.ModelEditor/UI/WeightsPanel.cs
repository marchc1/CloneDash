using Nucleus.ModelEditor.UI;
using Nucleus.Types;
using Nucleus.UI;

namespace Nucleus.ModelEditor;

public enum WeightsMode {
	Direct
}

// prototyping this out

public class BindOperator : Operator {
	public override string Name => $"Weights: Bind Bones";
	public override bool SelectMultiple => true;
	public override bool OverrideSelection => true;
	public override Type[]? SelectableTypes => [typeof(EditorBone)];

	public EditorMeshAttachment SelectedAttachment;
	public HashSet<EditorBone> Bones = [];

	public override bool CanActivate(out string? reason) {
		reason = null;
		if (ModelEditor.Active.AreAllSelectedObjectsTheSameType(out Type? t) && t == typeof(EditorMeshAttachment)) {
			// todo: how to adjust for multiple bones
			EditorMeshAttachment attachment = ModelEditor.Active.LastSelectedObject as EditorMeshAttachment ?? throw new Exception();
			this.SelectedAttachment = attachment;
			foreach (var weightpair in attachment.Weights) {
				Bones.Add(weightpair.Bone);
			}

			return true;
		}

		reason = "Must be selecting a mesh attachment.";
		return false;
	}
	public override void ChangeEditorProperties(CenteredObjectsPanel panel) {
		base.ChangeEditorProperties(panel);
	}
	public override void Clicked(ModelEditor editor, Vector2F mousePos) {
		base.Clicked(editor, mousePos);
	}
	protected override void Deactivated(bool canceled) {
		base.Deactivated(canceled);
	}
	public override bool HoverTest(IEditorType? type) {
		if (Bones.Contains(type))
			return false;

		return true;
	}
	public override void Think(ModelEditor editor, Vector2F mousePos) {
		base.Think(editor, mousePos);
	}

	public override void Selected(ModelEditor editor, IEditorType type) {
		Logs.Info($"{type}");
		if (type is not EditorBone bone)
			return;
		Bones.Add(bone);
	}
}

public class WeightsPanel : Panel {
	Panel props;
	Panel topBtns;
	Panel bottomBtns;
	public ListView BoneOrder;
	protected override void Initialize() {
		Add(out props);
		props.Dock = Dock.Fill;
		props.DockMargin = RectangleF.TLRB(4);
		props.DrawPanelBackground = false;

		props.Add(out topBtns);
		topBtns.Dock = Dock.Top;
		topBtns.Size = new(0, 128);
		topBtns.DrawPanelBackground = false;

		props.Add(out bottomBtns);
		bottomBtns.Dock = Dock.Bottom;
		bottomBtns.Size = new(0, 64);
		bottomBtns.DrawPanelBackground = false;

		props.Add(out BoneOrder);
		BoneOrder.Dock = Dock.Fill;
		BoneOrder.DockMargin = RectangleF.TLRB(0, 8,8, 0);
		BoneOrder.DrawPanelBackground = false;

		var lblBones = topBtns.Add<Label>();
		lblBones.Text = "Bones";
		lblBones.AutoSize = true;
		lblBones.TextSize = 22;
		lblBones.Dock = Dock.Bottom;

		// TODO: flex panel these into rows
		PropertiesPanel.OperatorButton<BindOperator>(bottomBtns, "Bind", null);
	}
}
