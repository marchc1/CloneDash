using Nucleus.Core;
using Nucleus.Types;

namespace Nucleus.UI.Elements;

/// <summary>
/// The label of a <see cref="FormRow"/>, dims when the whole row is disabled
/// </summary>
public class FormLabel(FormRow row) : Label(row)
{
	public FormRow Row => row;

	public override bool IsDisabled() {
		bool any = false;
		foreach (var child in row.Content.Children) {
			if (!child.IsVisible()) continue;
			if (!child.IsDisabled()) return false;
			any = true;
		}
		return any;
	}
}

/// <summary>
/// A labeled row in a <see cref="Form"/>, children go into <see cref="Content"/>
/// </summary>
public class FormRow : Panel
{
	public Form Form { get; }
	public Label Label { get; }
	public Panel Content { get; }

	/// <summary>
	/// Rows made without a label span the whole form.
	/// </summary>
	public bool Spans { get; }

	/// <summary>
	/// Height of the row, not counting the gap under it
	/// </summary>
	public float ContentHeight {
		get => Size.H - Form.RowGap;
		set {
			// the gap comes out of the size
			Size = new(0, value + Form.RowGap);
			Form.InvalidateLayout();
		}
	}

	public FormRow(Form form, string? label) : base(form) {
		Form = form;
		Spans = label == null;
		SetPaintBackgroundEnabled(false);
		SetPaintBorderEnabled(false);
		DockPadding = RectangleF.TLRB(0);
		Dock = Dock.Top;

		Label = form.CreateLabel(this);
		Label.Dock = Dock.Left;
		Label.Text = label ?? "";
		Label.SetTextAlignment(Anchor.TopLeft);

		Content = new Panel(this);
		Content.Dock = Dock.Fill;
		Content.SetPaintBackgroundEnabled(false);
		Content.SetPaintBorderEnabled(false);

		SetAddParent(Content);
	}
}

/// <summary>
/// Label/control rows stacked top to bottom, with the labels in one column
/// </summary>
public class Form : Panel
{
	public float RowHeight { get; set; } = 32;
	public float RowGap { get; set; } = 4;
	public float LabelPadding { get; set; } = 8;

	/// <summary>
	/// Width of the label column, null fits the widest label
	/// </summary>
	public float? LabelWidth { get; set; }

	/// <summary>
	/// Most of the form the fitted label column can take, longer labels get cut off
	/// </summary>
	public float MaxLabelFraction { get; set; } = 0.5f;

	public List<FormRow> Rows { get; } = [];

	public Form(Element? parent) : base(parent) {
		SetPaintBackgroundEnabled(false);
		SetPaintBorderEnabled(false);
	}

	public virtual Label CreateLabel(FormRow row) => new FormLabel(row);

	public FormRow AddRow(string? label, float height = 0, string? tooltip = null) {
		var row = new FormRow(this, label);
		row.ContentHeight = height > 0 ? height : RowHeight;
		row.DockMargin = RectangleF.TLRB(0, 0, 0, RowGap);
		if (!string.IsNullOrEmpty(tooltip))
			row.Label.TooltipText = tooltip;
		Rows.Add(row);
		InvalidateLayout();
		return row;
	}

	/// <summary>
	/// Adds a row holding one control, which fills it unless it's already docked.
	/// </summary>
	public T Add<T>(string? label, Func<FormRow, T> create, float height = 0, string? tooltip = null)
		where T : Element {
		var row = AddRow(label, height, tooltip);
		T control = create(row);
		if (control.Dock == Dock.None)
			control.Dock = Dock.Fill;
		return control;
	}

	public void RemoveRow(FormRow row) {
		Rows.Remove(row);
		row.Remove();
		InvalidateLayout();
	}

	public Label AddSection(string title) {
		var header = new Label(this, title);
		header.Dock = Dock.Top;
		header.Size = new(0, RowHeight);
		header.SetTextAlignment(Anchor.BottomLeft);
		header.SetTextPadding(new(LabelPadding / 2, 4));
		header.TextSize = header.TextSize + 2;
		return header;
	}

	protected override void OnThink() {
		base.OnThink();

		float width = LabelWidth ?? 0;
		if (!LabelWidth.HasValue) {
			foreach (var row in Rows) {
				if (!row.IsVisible() || row.Spans) continue;
				var label = row.Label;
				width = MathF.Max(width, Graphics2D.GetTextSize(label.Text, label.Font, label.GetRenderTextSize()).W);
			}
			if (width > 0)
				width = MathF.Ceiling(width + LabelPadding * 2) + 1;
			float max = MathF.Floor(GetRenderBounds().W * MaxLabelFraction);
			if (max > 0)
				width = MathF.Min(width, max);
		}

		foreach (var row in Rows) {
			var label = row.Label;
			// Lines up with the first line of controls in taller rows
			float textHeight = Graphics2D.GetTextSize(" ", label.Font, label.GetRenderTextSize()).H;
			Vector2F padding = new(LabelPadding, MathF.Max(0, MathF.Round((RowHeight - textHeight) / 2)));
			if (label.GetTextPadding() != padding)
				label.SetTextPadding(padding);

			bool show = width > 0 && !row.Spans;
			if (label.IsVisible() != show) {
				label.SetVisible(show);
				row.InvalidateLayout();
			}
			if (label.Size.W != width) {
				label.Size = new(width, 0);
				row.InvalidateLayout();
			}
		}

		if (Dock is Dock.Top or Dock.Bottom or Dock.None) {
			float height = DockPadding.Y + DockPadding.H;
			foreach (var child in Children) {
				if (child.IsVisible() && child.Dock is Dock.Top or Dock.Bottom)
					height += child.Size.H;
			}
			if (Size.H != height) {
				Size = new(Size.W, height);
				GetParent()?.InvalidateLayout();
			}
		}
	}
}
