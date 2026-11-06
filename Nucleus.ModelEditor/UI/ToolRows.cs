using Nucleus.Commands;
using Nucleus.Types;
using Nucleus.UI;

namespace Nucleus.ModelEditor.UI;

public static class ToolRows
{
	public const float ROW_HEIGHT = 32;
	public const float GAP = 4;

	public static FlexPanel NewFlex(Element parent) {
		var row = new FlexPanel(parent);
		row.Dock = Dock.Fill;
		row.Direction = Axis.Horizontal;
		row.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
		row.SetPaintBackgroundEnabled(false);
		row.SetPaintBorderEnabled(false);
		return row;
	}

	public static FlexPanel AddRow(Element parent) {
		var row = NewFlex(parent);
		row.Dock = Dock.Top;
		row.Size = new(0, ROW_HEIGHT + GAP);
		row.DockMargin = RectangleF.TLRB(0, 0, 0, GAP);
		return row;
	}

	// Gaps between flex cells, none on the outside edges
	public static void SpaceCells(FlexPanel row) {
		using var children = row.GetChildren();
		int count = children.Length;
		for (int i = 0; i < count; i++)
			children[i].DockMargin = RectangleF.TLRB(0, i == 0 ? 0 : GAP / 2, i == count - 1 ? 0 : GAP / 2, 0);
	}

	public static Button AddButton(FlexPanel row, string text, Action? click) {
		var button = new Button(row);
		button.Text = text;
		if (click != null)
			button.OnButtonClick += (_, _) => click();
		else
			button.SetMouseInputEnabled(false);
		return button;
	}

	public static Checkbox AddCheckbox(FlexPanel row, string text, ConVar convar) {
		var cell = new Panel(row);
		cell.SetPaintBackgroundEnabled(false);
		cell.SetPaintBorderEnabled(false);

		var checkbox = new Checkbox(cell);
		checkbox.Dock = Dock.Left;
		checkbox.Size = new(ROW_HEIGHT - 8);
		checkbox.DockMargin = RectangleF.TLRB(4, 0, 6, 4);
		checkbox.BindToConVar(convar);

		var label = new Label(cell);
		label.Dock = Dock.Fill;
		label.Text = text;
		label.SetTextAlignment(Anchor.CenterLeft);
		return checkbox;
	}
}
