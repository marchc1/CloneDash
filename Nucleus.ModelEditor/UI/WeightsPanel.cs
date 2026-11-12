using Nucleus.Commands;
using Nucleus.Common.Input;
using Nucleus.Core;
using Nucleus.Input;
using Nucleus.ModelEditor.UI;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;

namespace Nucleus.ModelEditor;

public enum WeightsMode
{
	Direct,
	Add,
	Remove,
	Replace
}

/// <summary>
/// Bind mode, binds clicked bones to the selected mesh or the selected bones to clicked meshes
/// </summary>
public class BindOperator : Operator
{
	public override string Name => mesh != null ? "Weights: Bind Bones" : "Weights: Bind to Mesh";
	public override bool SelectMultiple => true;
	public override bool OverrideSelection => true;
	public override Type[]? SelectableTypes => mesh != null ? [typeof(EditorBone)] : [typeof(EditorMeshAttachment), typeof(EditorClippingAttachment)];

	EditorVertexAttachment? mesh;
	readonly List<EditorBone> bones = [];
	readonly Dictionary<EditorVertexAttachment, MeshState> before = [];
	// Meshes that had no bones when bind mode started get auto weights for everything bound.
	readonly HashSet<EditorVertexAttachment> fresh = [];

	public override bool CanActivate(out string? reason) {
		reason = null;
		var editor = ModelEditor.Active;
		if (editor.AreAllSelectedObjectsTheSameType(out Type? selectedType)) {
			if (editor.LastSelectedObject is EditorVertexAttachment attachment && WeightsPanel.CanHaveWeights(attachment)) {
				mesh = attachment;
				return true;
			}
			if (selectedType == typeof(EditorBone)) {
				bones.AddRange(editor.SelectedObjects.OfType<EditorBone>());
				return true;
			}
		}

		reason = "Select a mesh, a clipping attachment or some bones first.";
		return false;
	}

	public override bool HoverTest(IEditorType? type) => mesh == null ? type is EditorVertexAttachment attachment && WeightsPanel.CanHaveWeights(attachment)
		: type is EditorBone bone && !mesh.Weights.Any(x => x.Bone == bone);

	public override void Selected(ModelEditor editor, IEditorType type) {
		if (mesh != null && type is EditorBone bone)
			Bind(mesh, [bone]);
		else if (mesh == null && type is EditorVertexAttachment target && WeightsPanel.CanHaveWeights(target))
			Bind(target, bones);
	}

	private void Bind(EditorVertexAttachment target, List<EditorBone> add) {
		if (add.All(b => target.Weights.Any(x => x.Bone == b))) return;
		if (!before.ContainsKey(target)) {
			before[target] = MeshState.Capture(target);
			if (target.Weights.Count == 0)
				fresh.Add(target);
		}
		MeshWeights.Bind(target, add);
		if (fresh.Contains(target))
			MeshWeights.Auto(target);
	}

	// no cancel, escape finishes too
	protected override void Deactivated(bool cancelled) {
		List<KeyValuePair<EditorVertexAttachment, MeshState>> changed = [];
		foreach (var pair in before) {
			if (!pair.Value.Matches(pair.Key))
				changed.Add(pair);
		}
		if (changed.Count == 0) return;
		List<MeshState> after = [];
		foreach (var pair in changed)
			after.Add(MeshState.Capture(pair.Key));
		var name = changed.Count == 1
			? $"Bind bones to {changed[0].Key.GetName()}"
			: $"Bind bones to {changed.Count} meshes";
		ModelEditor.Active.Actions.Push(name, () => after.ForEach(x => x.Restore()), () => changed.ForEach(x => x.Value.Restore()));
	}
}

public class WeightsPanel : View
{
	public static ConVar weights_mode = new("weights_mode", "0", FCvar.Saved,
		"Weights tool mode. 0 = direct, 1 = add, 2 = remove, 3 = replace.");
	public static ConVar weights_strength = new("weights_strength", "10", FCvar.Saved,
		"Weights brush strength, in percent.");
	public static ConVar weights_size = new("weights_size", "60", FCvar.Saved, "Weights brush size, in pixels.");
	public static ConVar weights_feather = new("weights_feather", "50", FCvar.Saved,
		"How much of the weights brush fades out, in percent of its size.");
	public static ConVar weights_pies = new("weights_pies", "1", FCvar.Saved,
		"Draws weight pies on mesh vertices in the Weights tool.");
	public static ConVar weights_overlay = new("weights_overlay", "1", FCvar.Saved,
		"Fills meshes with their bone colors in the Weights tool.");
	public static ConVar weights_selected = new("weights_selected", "0", FCvar.Saved,
		"Weights display only for selected vertices and bones.");
	public static ConVar weights_prune_bones = new("weights_prune_bones", "4", FCvar.Saved,
		"Prune: most bones a vertex can keep weights for.");
	public static ConVar weights_prune_threshold = new("weights_prune_threshold", "1", FCvar.Saved,
		"Prune: weights under this percentage are removed.");

	public override string Name => "Weights";

	private const float ROW_HEIGHT = ToolRows.ROW_HEIGHT;
	private const float GAP = ToolRows.GAP;

	public static WeightsMode Mode => (WeightsMode)Math.Clamp(weights_mode.GetInt(), 0, (int)WeightsMode.Replace);

	readonly FormRow weightRow;
	readonly FormRow strengthRow;
	readonly FormRow sizeRow;
	readonly FormRow featherRow;
	readonly NumSlider weightSlider;
	readonly ListView bones;
	readonly List<BoneRow> rows = [];
	readonly Button updateButton;
	readonly Button removeButton;
	readonly Button swapButton;
	readonly Button smoothButton;
	readonly Button autoButton;
	readonly Button pruneButton;

	EditorVertexAttachment? activeAttachment;
	EditorWeights? activeWeights;

	/// <summary>
	/// Can this attachment have weights?
	/// </summary>
	public static bool CanHaveWeights(EditorVertexAttachment attachment)
		=> attachment is EditorClippingAttachment || attachment is EditorMeshAttachment { LinkedParent: null };

	public EditorVertexAttachment? ActiveAttachment => activeAttachment;
	/// <summary>
	/// The last bone clicked in the list
	/// </summary>
	public EditorWeights? ActiveWeights => activeWeights;
	public HashSet<EditorWeights> SelectedBones { get; } = [];

	public WeightsPanel(Element parent) : base(parent) {
		var content = new Panel(this);
		content.Dock = Dock.Fill;
		content.DockPadding = RectangleF.TLRB(GAP);
		content.SetPaintBackgroundEnabled(false);
		content.SetPaintBorderEnabled(false);

		var form = new Form(content);
		form.Dock = Dock.Top;
		form.RowGap = GAP;

		var mode = form.Add("Mode", r => DropdownSelector<WeightsMode>.FromEnum(r, Mode));
		mode.Dock = Dock.Left;
		mode.Size = new(160, 0);
		mode.OnSelectionChanged += (_, _, value) => {
			weights_mode.SetValue((int)value);
			UpdateModeRows();
		};

		weightSlider = Slider(form, "Weight", 0, 100, "%", out weightRow);
		weightSlider.Digits = 1;
		weightSlider.OnValueChanged += (_, _, value) => SetSelectedWeight((float)value / 100);

		ConvarSlider(form, "Strength", weights_strength, 0, 100, "%", out strengthRow);
		ConvarSlider(form, "Size", weights_size, 1, 400, " px", out sizeRow);
		ConvarSlider(form, "Feather", weights_feather, 0, 100, "%", out featherRow);
		UpdateModeRows();

		var actions = ToolRows.NewFlex(form.AddRow(null));
		smoothButton = ToolRows.AddButton(actions, "Smooth", () => {
			if (activeAttachment == null) return;
			ModelEditor.Active.Actions.TrackMesh(activeAttachment);
			MeshWeights.Smooth(activeAttachment, activeAttachment.SelectedVertices);
		});
		autoButton = ToolRows.AddButton(actions, "Auto", () => {
			if (activeAttachment == null) return;
			ModelEditor.Active.Actions.TrackMesh(activeAttachment);
			MeshWeights.Auto(activeAttachment, activeAttachment.SelectedVertices.Count > 0 ? activeAttachment.SelectedVertices : null, SelectedBones);
		});
		pruneButton = ToolRows.AddButton(actions, "Prune", OpenPrune);
		ToolRows.SpaceCells(actions);

		var display = ToolRows.NewFlex(form.AddRow(null));
		ToolRows.AddCheckbox(display, "Pies", weights_pies);
		ToolRows.AddCheckbox(display, "Overlay", weights_overlay);
		ToolRows.AddCheckbox(display, "Selected", weights_selected);
		ToolRows.SpaceCells(display);

		form.AddSection("Bones");

		var bottom = new Panel(content);
		bottom.Dock = Dock.Bottom;
		bottom.Size = new(0, GAP + ROW_HEIGHT * 2 + GAP);
		bottom.DockPadding = RectangleF.TLRB(GAP, 0, 0, 0);
		bottom.SetPaintBackgroundEnabled(false);
		bottom.SetPaintBorderEnabled(false);

		var binding = ToolRows.AddRow(bottom);
		PropertiesPanel.OperatorButton<BindOperator>(binding, "Bind", null);
		removeButton = ToolRows.AddButton(binding, "Remove", () => {
			if (activeAttachment == null || SelectedBones.Count == 0) return;
			ModelEditor.Active.Actions.TrackMesh(activeAttachment);
			MeshWeights.Remove(activeAttachment, SelectedBones.ToList());
			SelectedBones.Clear();
			activeWeights = null;
		});
		swapButton = ToolRows.AddButton(binding, "Swap", () => {
			if (activeAttachment == null || SelectedBones.Count != 2) return;
			var pair = SelectedBones.ToArray();
			ModelEditor.Active.Actions.TrackMesh(activeAttachment);
			MeshWeights.Swap(activeAttachment, pair[0], pair[1], activeAttachment.SelectedVertices.Count > 0 ? activeAttachment.SelectedVertices : activeAttachment.GetVertices());
		});
		ToolRows.SpaceCells(binding);

		var update = ToolRows.AddRow(bottom);
		update.Size = new(0, ROW_HEIGHT);
		update.DockMargin = RectangleF.TLRB(0);
		updateButton = ToolRows.AddButton(update, "Update Bindings", () => {
			if (activeAttachment == null) return;
			ModelEditor.Active.Actions.TrackMesh(activeAttachment);
			MeshWeights.UpdateBindings(activeAttachment);
		});

		bones = new ListView(content);
		bones.Dock = Dock.Fill;
	}

	private static NumSlider Slider(Form form, string label, double min, double max, string suffix, out FormRow row) {
		row = form.AddRow(label);
		var slider = new NumSlider(row);
		slider.Dock = Dock.Fill;
		slider.MinimumValue = min;
		slider.MaximumValue = max;
		slider.Digits = 0;
		slider.Suffix = suffix;
		return slider;
	}

	private static void ConvarSlider(Form form, string label, ConVar convar, double min, double max, string suffix, out FormRow row) {
		var slider = Slider(form, label, min, max, suffix, out row);
		slider.Value = convar.GetDouble();
		slider.OnValueChanged += (_, _, value) => convar.SetValue(value);
	}

	private void UpdateModeRows() {
		bool direct = Mode == WeightsMode.Direct;
		weightRow.SetVisible(direct);
		strengthRow.SetVisible(!direct);
		sizeRow.SetVisible(!direct);
		featherRow.SetVisible(!direct);
	}

	/// <summary>
	/// The bone's weight on the selected vertices, null when none are selected or they differ
	/// </summary>
	public float? SharedWeight(EditorWeights weights) => WeightOnSelection(weights, out _, out _);

	/// <summary>
	/// Like <see cref="SharedWeight"/>, also says if the weights differ and if any selected vertex has weight for the bone
	/// </summary>
	public float? WeightOnSelection(EditorWeights weights, out bool mixed, out bool any) {
		mixed = any = false;
		if (activeAttachment == null || activeAttachment.SelectedVertices.Count == 0) return null;
		float? shared = null;
		foreach (var vertex in activeAttachment.SelectedVertices) {
			float weight = weights.TryGetVertexWeight(vertex);
			any |= weight > 0;
			if (shared == null)
				shared = weight;
			else if (MathF.Abs(shared.Value - weight) > 0.00001f)
				mixed = true;
		}
		return mixed ? null : shared;
	}

	private void SetSelectedWeight(float value) {
		var mesh = activeAttachment;
		if (mesh == null || SelectedBones.Count == 0 || mesh.SelectedVertices.Count == 0) return;
		ModelEditor.Active.Actions.TrackMesh(mesh);
		MeshWeights.EnsurePositions(mesh);
		var targets = SelectedBones.ToDictionary(x => x, _ => value);
		foreach (var vertex in mesh.SelectedVertices)
			MeshWeights.Adjust(mesh, vertex, targets);
		shownWeight = value;
	}

	float? shownWeight;
	protected override void OnThink() {
		base.OnThink();

		// Don't lose the mesh when a bone gets selected (right clicking one in the list does that)
		var selected = ModelEditor.Active.LastSelectedObject;
		EditorVertexAttachment? mesh;
		if (selected is EditorVertexAttachment attachment && CanHaveWeights(attachment))
			mesh = attachment;
		else if (selected is EditorBone)
			mesh = activeAttachment;
		else
			mesh = null;
		if (mesh != activeAttachment) {
			activeAttachment = mesh;
			SelectedBones.Clear();
			activeWeights = null;
			RebuildRows();
		}
		else if (mesh != null)
			SyncRows(mesh);

		bool weighted = mesh != null && mesh.Weights.Count > 0;
		updateButton.SetMouseInputEnabled(weighted);
		smoothButton.SetMouseInputEnabled(weighted);
		autoButton.SetMouseInputEnabled(weighted);
		pruneButton.SetMouseInputEnabled(PruneTargets().Count > 0 && pruneWindow == null);
		removeButton.SetMouseInputEnabled(SelectedBones.Count > 0);
		swapButton.SetMouseInputEnabled(SelectedBones.Count == 2);

		// Follows the selection unless being dragged. Holds its value while the weights differ.
		bool mixed = false;
		float? shared = activeWeights == null ? null : WeightOnSelection(activeWeights, out mixed, out _);
		if (!mixed && shared != shownWeight && !weightSlider.IsDepressed()) {
			shownWeight = shared;
			weightSlider.SetValueNoUpdate((shared ?? 0) * 100);
		}
	}

	// prune every selected mesh
	private List<EditorVertexAttachment> PruneTargets() {
		List<EditorVertexAttachment> selected = [];
		foreach (var item in ModelEditor.Active.SelectedObjects) {
			if (item is EditorVertexAttachment attachment && CanHaveWeights(attachment) && attachment.Weights.Count > 0)
				selected.Add(attachment);
		}
		if (selected.Count == 0 && activeAttachment is { Weights.Count: > 0 })
			selected.Add(activeAttachment);
		return selected;
	}

	Window? pruneWindow;
	public Window? PruneWindow => pruneWindow;

	private void OpenPrune() {
		var targets = PruneTargets();
		if (targets.Count == 0 || pruneWindow != null) return;
		List<MeshState> before = [];
		foreach (var target in targets)
			before.Add(MeshState.Capture(target));
		bool applied = false;

		var window = pruneWindow = new Window(EngineCore.Level.RootPanel);
		window.Title = targets.Count == 1 ? "Prune" : $"Prune {targets.Count} meshes";
		window.HideNonCloseButtons();
		window.Resizable = false;
		window.Size = new(400, window.Titlebar.Size.H + 8 + GAP * 2 + (ROW_HEIGHT + GAP) * 4);
		window.Center();
		window.Removed += _ => {
			// closing without Prune puts everything back
			if (!applied)
				before.ForEach(x => x.Restore());
			pruneWindow = null;
		};

		var form = new Form(window);
		form.Dock = Dock.Top;
		form.DockPadding = RectangleF.TLRB(GAP);
		form.RowGap = GAP;
		var bonesSlider = Slider(form, "Bones", 1, 8, "", out _);
		bonesSlider.Value = weights_prune_bones.GetDouble();
		var thresholdSlider = Slider(form, "Threshold", 0, 50, "%", out _);
		thresholdSlider.Digits = 1;
		thresholdSlider.Value = weights_prune_threshold.GetDouble();
		var count = form.Add(null, r => new Label(r));
		count.SetTextAlignment(Anchor.CenterLeft);
		count.SetTextPadding(new(form.LabelPadding, 0));
		bonesSlider.OnValueChanged += (_, _, value) => {
			weights_prune_bones.SetValue((int)Math.Round(value));
			Preview(targets, before, count);
		};
		thresholdSlider.OnValueChanged += (_, _, value) => {
			weights_prune_threshold.SetValue(value);
			Preview(targets, before, count);
		};

		var buttons = ToolRows.NewFlex(form.AddRow(null));
		ToolRows.AddButton(buttons, "Prune", () => {
			Preview(targets, before, count);
			List<MeshState> after = [];
			foreach (var target in targets)
				after.Add(MeshState.Capture(target));
			bool changed = false;
			for (int i = 0; i < after.Count; i++) {
				if (!before[i].Matches(targets[i])) {
					changed = true;
					break;
				}
			}
			if (changed)
				ModelEditor.Active.Actions.Push(
					targets.Count == 1 ? $"Prune {targets[0].GetName()}" : $"Prune {targets.Count} meshes",
					() => after.ForEach(x => x.Restore()), () => before.ForEach(x => x.Restore()));
			applied = true;
			window.Remove();
		});
		ToolRows.AddButton(buttons, "Cancel", window.Remove);
		ToolRows.SpaceCells(buttons);

		Preview(targets, before, count);
	}

	private static void Preview(List<EditorVertexAttachment> targets, List<MeshState> before, Label count) {
		int removed = 0;
		for (int i = 0; i < targets.Count; i++) {
			before[i].Restore();
			removed += MeshWeights.Prune(targets[i], weights_prune_bones.GetInt(), weights_prune_threshold.GetFloat() / 100);
		}
		count.Text = removed == 1 ? "1 weight will be removed." : $"{removed} weights will be removed.";
	}

	private void RebuildRows() {
		bones.ClearChildren();
		rows.Clear();
		if (activeAttachment == null) return;
		foreach (var weights in activeAttachment.Weights)
			rows.Add(new BoneRow(bones, this, weights));
	}

	// Undo, bind and reordering all change the list under us.
	private void SyncRows(EditorVertexAttachment mesh) {
		var list = mesh.Weights;
		SelectedBones.RemoveWhere(x => !list.Contains(x));
		if (activeWeights != null && !list.Contains(activeWeights))
			activeWeights = null;

		if (rows.Count != list.Count || rows.Any(x => !list.Contains(x.Weights))) {
			RebuildRows();
			return;
		}

		bool ordered = true;
		for (int i = 0; i < rows.Count; i++) {
			if (rows[i].Weights != list[i])
				ordered = false;
		}
		if (ordered) return;

		rows.Sort((a, b) => list.IndexOf(a.Weights).CompareTo(list.IndexOf(b.Weights)));
		bones.SortChildren((a, b) => list.IndexOf(((BoneRow)a).Weights).CompareTo(list.IndexOf(((BoneRow)b).Weights)));
		bones.InvalidateChildren(true, true);
	}

	public void ClickBone(EditorWeights weights, bool ctrl) {
		if (ctrl) {
			if (!SelectedBones.Remove(weights)) {
				SelectedBones.Add(weights);
				activeWeights = weights;
			}
			else if (activeWeights == weights)
				activeWeights = SelectedBones.LastOrDefault();
		}
		else {
			SelectedBones.Clear();
			SelectedBones.Add(weights);
			activeWeights = weights;
		}
	}

	private void DragRow(BoneRow row, float mouseY) {
		var mesh = activeAttachment;
		if (mesh == null) return;
		int from = mesh.Weights.IndexOf(row.Weights);
		int to = from;
		for (int i = 0; i < rows.Count; i++) {
			float top = rows[i].GetGlobalPosition().Y;
			if (mouseY >= top && mouseY < top + rows[i].GetRenderBounds().H)
				to = i;
		}
		if (from < 0 || to == from) return;

		ModelEditor.Active.Actions.TrackMesh(mesh);
		mesh.Weights.RemoveAt(from);
		mesh.Weights.Insert(to, row.Weights);
		SyncRows(mesh);
	}

	class BoneRow : ListViewItem
	{
		public readonly EditorWeights Weights;
		readonly WeightsPanel panel;

		public BoneRow(ListView list, WeightsPanel panel, EditorWeights weights) : base(list) {
			this.panel = panel;
			Weights = weights;
			Size = new(0, ROW_HEIGHT);
			DockMargin = RectangleF.TLRB(0, 0, 0, 2);
		}

		private RectangleF Swatch(float height) => RectangleF.XYWH(6, 6, height - 12, height - 12);
		private bool OverSwatch() => IsHovered() && Swatch(GetRenderBounds().H).ContainsPoint(GetMousePos());

		public override void PaintBackground(float width, float height) {
			if (panel.SelectedBones.Contains(Weights))
				Graphics2D.SetDrawColor(110, 160, 255, 70);
			else if (IsHovered())
				Graphics2D.SetDrawColor(255, 255, 255, 22);
			else
				Graphics2D.SetDrawColor(255, 255, 255, 8);
			Graphics2D.DrawRectangle(0, 0, width, height);
		}

		public override void Paint(float width, float height) {
			var mesh = panel.activeAttachment;
			if (mesh == null) return;

			var box = Swatch(height);
			Graphics2D.SetDrawColor(mesh.WeightColor(Weights));
			Graphics2D.DrawRectangle(box);
			if (Weights.Locked || OverSwatch())
				DrawLock(box, Weights.Locked ? 230 : 110);

			// Bones with weight on the selection are highlighted
			var shared = panel.WeightOnSelection(Weights, out bool mixed, out bool any);
			bool dim = mesh.SelectedVertices.Count > 0 && !any;
			Graphics2D.SetDrawColor(255, 255, 255, dim ? 110 : 255);
			Graphics2D.DrawText(box.X + box.W + 10, height / 2, Weights.Bone.Name, Graphics2D.UI_FONT_NAME, 20, Anchor.CenterLeft);

			string? text = null;
			if (mixed)
				text = "*";
			else if (shared is float weight && weight > 0)
				text = $"{MathF.Round(weight * 100, 1)}%";
			if (text != null) {
				Graphics2D.SetDrawColor(255, 255, 255);
				Graphics2D.DrawText(width - 8, height / 2, text, Graphics2D.UI_FONT_NAME, 20, Anchor.CenterRight);
			}
		}

		private static void DrawLock(RectangleF box, int alpha) {
			Graphics2D.SetDrawColor(0, 0, 0, alpha);
			float w = box.W * 0.56f, h = box.H * 0.4f;
			float x = box.X + (box.W - w) / 2, y = box.Y + box.H * 0.48f;
			Graphics2D.DrawRectangle(x, y, w, h);
			float radius = w * 0.32f;
			Graphics2D.DrawRing(new(box.X + box.W / 2, y), radius - 1.5f, radius + 0.5f, 180, 360);
			Graphics2D.DrawRectangle(box.X + box.W / 2 - radius - 0.5f, y - 1, 2, 2);
			Graphics2D.DrawRectangle(box.X + box.W / 2 + radius - 1.5f, y - 1, 2, 2);

		}

		Vector2F? dragFrom;
		bool dragging;

		protected override bool MouseDrag(Element self, FrameState state, Vector2F delta) {
			dragFrom ??= state.Mouse.MousePos;
			if (!dragging && (state.Mouse.MousePos - dragFrom.Value).Length > 4)
				dragging = true;
			if (dragging)
				panel.DragRow(this, state.Mouse.MousePos.Y);
			return base.MouseDrag(self, state, delta);
		}

		protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
			bool dragged = dragging;
			dragFrom = null;
			dragging = false;
			if (dragged || !IsHovered()) return true;

			// Right click selects the bone in the tree and viewport
			if (button == ButtonCode.Mouse2) {
				ModelEditor.Active.SelectObject(Weights.Bone);
				return true;
			}
			if (OverSwatch())
				Weights.Locked = !Weights.Locked;
			else
				panel.ClickBone(Weights, state.Keyboard.ControlDown);
			return base.MouseRelease(self, state, button);
		}
	}
}
