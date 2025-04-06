using Nucleus.Core;
using Nucleus.ModelEditor.UI;
using Nucleus.Types;
using Nucleus.UI;

namespace Nucleus.ModelEditor;

public enum WeightsMode
{
	Direct
}

// prototyping this out

public class BindOperator : Operator
{
	public override string Name => $"Weights: Bind Bones";
	public override bool SelectMultiple => true;
	public override bool OverrideSelection => true;
	public override Type[]? SelectableTypes => [typeof(EditorBone)];

	public EditorMeshAttachment SelectedAttachment;
	public HashSet<EditorBone> AlreadyBoundBones = [];
	public List<EditorBone> BonesToBind = [];

	public override bool CanActivate(out string? reason) {
		reason = null;
		if (ModelEditor.Active.AreAllSelectedObjectsTheSameType(out Type? t) && t == typeof(EditorMeshAttachment)) {
			// todo: how to adjust for multiple bones
			EditorMeshAttachment attachment = ModelEditor.Active.LastSelectedObject as EditorMeshAttachment ?? throw new Exception();
			this.SelectedAttachment = attachment;
			foreach (var weightpair in attachment.Weights) {
				AlreadyBoundBones.Add(weightpair.Bone);
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
	protected override void Deactivated(bool cancelled) {
		if (cancelled) {
			// Disassociate any bones
			foreach (var bone in BonesToBind) {
				ModelEditor.Active.File.DisassociateBoneFromMesh(SelectedAttachment, bone);
			}

			return; // nothing left to do if cancelled
		}

		if (BonesToBind.Count <= 0) return; // nothing to bind

		if (AlreadyBoundBones.Count <= 0) { // Logic when bindings don't exist
			ModelEditor.Active.File.AutoBindVertices(SelectedAttachment);
		}
		else { // Logic when bindings already exist

		}

		// Either way, need to set up local vertex positions
		ModelEditor.Active.File.UpdateVertexPositions(SelectedAttachment, BonesToBind);
	}
	public override bool HoverTest(IEditorType? type) {
		if (AlreadyBoundBones.Contains(type) || BonesToBind.Contains(type))
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
		if (BonesToBind.Contains(bone)) return;

		BonesToBind.Add(bone);
		editor.File.AssociateBoneToMesh(SelectedAttachment, bone);
	}
}

public class WeightsPanel : Panel
{
	Panel props;
	Panel topBtns;
	FlexPanel bottomBtns;
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
		bottomBtns.Direction = Directional180.Horizontal;
		bottomBtns.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;

		props.Add(out BoneOrder);
		BoneOrder.Dock = Dock.Fill;
		BoneOrder.DockMargin = RectangleF.TLRB(0, 8, 8, 0);
		BoneOrder.DrawPanelBackground = false;

		var lblBones = topBtns.Add<Label>();
		lblBones.Text = "Bones";
		lblBones.AutoSize = true;
		lblBones.TextSize = 22;
		lblBones.Dock = Dock.Bottom;

		// TODO: flex panel these into rows
		PropertiesPanel.OperatorButton<BindOperator>(bottomBtns, "Bind", null);
		PropertiesPanel.ButtonIcon(bottomBtns, "Update", null, (_, _, _) => {
			if (ModelEditor.Active.LastSelectedObject is not EditorMeshAttachment meshAttachment)
				return;

			ModelEditor.Active.File.UpdateVertexPositions(meshAttachment);
		});

		ModelEditor.Active.SelectedChanged += Active_SelectedChanged;
	}

	private void Active_SelectedChanged() {
		BoneOrder.ClearChildren();

		if (ModelEditor.Active.LastSelectedObject is not EditorMeshAttachment meshAttachment)
			return;

		foreach (var bonepair in meshAttachment.Weights) {
			var btn = BoneOrder.Add<ListViewItem>();
			btn.Text = bonepair.Bone.Name;
			btn.SetTag("bonepair", bonepair);
			btn.PaintOverride += Btn_PaintOverride;
		}
	}

	private void Btn_PaintOverride(Element self, float width, float height) {
		var lvi = self as ListViewItem ?? throw new Exception();
		EditorMeshWeights bonepair = self.GetTag<EditorMeshWeights>("bonepair");
		Graphics2D.SetDrawColor(255, 255, 255);

		if (ModelEditor.Active.LastSelectedObject is not EditorMeshAttachment meshAttachment)
			return;

		Graphics2D.DrawText(32, height / 2, bonepair.Bone.Name, "Noto Sans", 18, Anchor.CenterLeft);

		float weight = 0;
		bool hasWeight = false;
		bool sharesWeight = true;

		foreach (var vertex in meshAttachment.GetSelectedVertices()) {
			var vertexWeight = bonepair.Weights.TryGetValue(vertex, out float _t) ? _t : -1;

			if (!hasWeight && vertexWeight != -1) {
				weight = vertexWeight;
				hasWeight = true;
			}
			else if (hasWeight && sharesWeight && vertexWeight != weight) {
				sharesWeight = false;
				break; // We're going to show no value
			}
		}

		if (hasWeight)
			Graphics2D.DrawText(width - 8, height / 2, sharesWeight ? $"{weight:P2}" : "*", "Noto Sans", 18, Anchor.CenterRight);
	}
}
