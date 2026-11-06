using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;

namespace Nucleus.HelloWorld;

public enum TestEnum { Alpha, Bravo, Charlie, Delta }

/// <summary>
/// Test windows for the tools panel
/// </summary>
public static class Scenes
{
	private static Window MakeWindow(UserInterface ui, string title, Vector2F size, Vector2F? position = null) {
		var window = new Window(ui, title);
		window.Size = size;
		if (position.HasValue)
			window.Position = position.Value;
		else
			window.Position = new(40, 40);
		return window;
	}

	private static Panel Row(Element parent, string label, float height = 36) {
		var row = new Panel(parent);
		row.Dock = Dock.Top;
		row.Size = new(0, height);
		row.SetPaintBackgroundEnabled(false);
		row.SetPaintBorderEnabled(false);

		var nameLabel = new Label(row, label);
		nameLabel.Dock = Dock.Left;
		nameLabel.Size = new(150, 0);
		nameLabel.SetTextAlignment(Anchor.CenterLeft);
		nameLabel.SetTextPadding(new(8, 0));
		return row;
	}

	// Scroll panel

	public record ScrollFieldsScene(Window Window, ScrollPanel Scroll, Textbox FirstTextbox, NumSlider FirstSlider, Textbox MultiLine);

	public static ScrollFieldsScene ScrollFields(UserInterface ui) {
		var window = MakeWindow(ui, "Scroll over fields", new(560, 420));
		var scroll = new ScrollPanel(window);
		scroll.Dock = Dock.Fill;

		Textbox? firstTextbox = null;
		NumSlider? firstSlider = null;
		Textbox? multi = null;
		for (int i = 0; i < 24; i++) {
			if (i == 6) {
				var multiRow = Row(scroll, "Multi-line", 90);
				multi = new Textbox(multiRow);
				multi.Dock = Dock.Fill;
				multi.MultiLine = true;
				multi.Text = "Short multi-line box\nthat doesn't overflow";
				continue;
			}

			var row = Row(scroll, $"Field {i}");
			if (i % 2 == 0) {
				var textbox = new Textbox(row);
				textbox.Dock = Dock.Fill;
				textbox.Text = $"Text field {i}";
				firstTextbox ??= textbox;
			}
			else {
				var slider = new NumSlider(row);
				slider.Dock = Dock.Fill;
				slider.Value = i * 1.5;
				firstSlider ??= slider;
			}
		}

		return new(window, scroll, firstTextbox!, firstSlider!, multi!);
	}

	// Text boxes

	public record TextboxScene(Window Window, Textbox Normal, Textbox Tall, Textbox LeftAligned, Textbox Prefilled);

	public static TextboxScene Textboxes(UserInterface ui) {
		var window = MakeWindow(ui, "Text boxes", new(560, 330));

		var normalRow = Row(window, "Normal (30px)", 30);
		var normal = new Textbox(normalRow);
		normal.Dock = Dock.Fill;
		normal.SetHelperText("Type here...");

		var tallRow = Row(window, "Tall (52px)", 52);
		var tall = new Textbox(tallRow);
		tall.Dock = Dock.Fill;
		tall.Text = "Some text";

		var leftRow = Row(window, "Left aligned", 30);
		var left = new Textbox(leftRow);
		left.Dock = Dock.Fill;
		left.SetTextAlignment(Anchor.CenterLeft);
		left.SetTextPadding(new(6, 0));
		left.Text = "Left aligned text";

		var prefilledRow = Row(window, "Prefilled long", 30);
		var prefilled = new Textbox(prefilledRow);
		prefilled.Dock = Dock.Fill;
		prefilled.SetTextAlignment(Anchor.CenterLeft);
		prefilled.SetTextPadding(new(6, 0));
		prefilled.Text = "This text is way too long to fit in the box, so the end of it should be cut off until you go there";

		return new(window, normal, tall, left, prefilled);
	}

	// Number sliders

	public record NumSliderScene(Window Window, NumSlider Digits0, NumSlider Digits2, NumSlider Digits5, NumSlider BigValue, NumSlider Bounded);

	public static NumSliderScene NumSliders(UserInterface ui) {
		var window = MakeWindow(ui, "Number sliders", new(560, 300));

		var digits0 = Make("Digits 0", 0, 1, window);
		var digits2 = Make("Digits 2", 2, 1, window);
		var digits5 = Make("Digits 5", 5, 1, window);
		var big = Make("Big value", 1, 2500, window);
		var bounded = Make("Bounded 0-1", 3, 0.25, window, 0, 1);

		return new(window, digits0, digits2, digits5, big, bounded);
	}

	private static NumSlider Make(string label, int digits, double value, Window window, double? min = null, double? max = null) {
		var row = Row(window, label);
		var slider = new NumSlider(row);
		slider.Dock = Dock.Fill;
		slider.Digits = digits;
		slider.MinimumValue = min;
		slider.MaximumValue = max;
		slider.Value = value;
		return slider;
	}

	// Menus and dropdowns

	public record MenuScene(Window Window, Button ContextButton, Button EdgeButton, DropdownSelector<TestEnum> Small, DropdownSelector<string> Long);

	public static MenuContext BuildContextMenu(UserInterface ui) {
		var menu = new MenuContext(ui);
		menu.AddButton("Cut");
		menu.AddButton("Copy");
		menu.AddButton("Paste");
		menu.AddSeparator();
		menu.AddSubMenu("Insert", callback: submenu => {
			submenu.AddButton("Number");
			submenu.AddButton("Vector");
			submenu.AddButton("Color");
			return true;
		});
		menu.AddSubMenu("Convert to", callback: submenu => {
			submenu.AddButton("Integer");
			submenu.AddButton("Float");
			return true;
		});
		menu.AddButton("Delete");
		return menu;
	}

	public static MenuScene Menus(UserInterface ui) {
		var window = MakeWindow(ui, "Menus and dropdowns", new(560, 300));

		var contextRow = Row(window, "Context menu");
		var contextButton = new Button(contextRow, "Open context menu");
		contextButton.Dock = Dock.Fill;
		contextButton.OnButtonClick += (_, _) => BuildContextMenu(ui).Show();

		var smallRow = Row(window, "Enum dropdown");
		var small = DropdownSelector<TestEnum>.FromEnum(smallRow, TestEnum.Bravo);
		small.Dock = Dock.Fill;

		var longRow = Row(window, "Long dropdown");
		var longDropdown = new DropdownSelector<string>(longRow);
		longDropdown.Dock = Dock.Fill;
		for (int i = 0; i < 80; i++)
			longDropdown.Items.Add($"Item number {i}");
		longDropdown.Selected = "Item number 40";

		// right edge, for menu flipping
		var screen = EngineCore.GetScreenSize();
		var edgeWindow = MakeWindow(ui, "Edge", new(220, 110), new(screen.W - 230, 40));
		var edgeButton = new Button(edgeWindow, "Menu at the edge");
		edgeButton.Dock = Dock.Fill;
		edgeButton.OnButtonClick += (_, _) => BuildContextMenu(ui).Show();

		return new(window, contextButton, edgeButton, small, longDropdown);
	}

	// Buttons

	public record ButtonScene(Window Window, Button Button);

	public static ButtonScene Buttons(UserInterface ui) {
		var window = MakeWindow(ui, "Buttons", new(360, 160));
		var button = new Button(window, "Hover me");
		button.Dock = Dock.Fill;
		return new(window, button);
	}

	// Every stock control in one window

	public record GalleryScene(Window Window, TabView Tabs, Button Normal);

	public static GalleryScene Gallery(UserInterface ui) {
		var window = MakeWindow(ui, "Controls", new(820, 560));

		var menubar = new Menubar(window);
		menubar.AddButton("File").AddButton("New");
		menubar.AddButton("Edit").AddButton("Undo");
		menubar.AddButton("View").AddButton("Zoom");

		var left = new Panel(window);
		left.Dock = Dock.Left;
		left.Size = new(400, 0);

		var form = new Form(left);
		form.Dock = Dock.Fill;

		var buttonRow = form.AddRow("Buttons");
		var normal = new Button(buttonRow, "Normal");
		normal.Dock = Dock.Left;
		normal.Size = new(80, 0);
		normal.TooltipText = "A tooltip";
		var disabled = new Button(buttonRow, "Disabled");
		disabled.Dock = Dock.Left;
		disabled.Size = new(80, 0);
		disabled.SetMouseInputEnabled(false);
		var primary = new Button(buttonRow, "Default");
		primary.Dock = Dock.Fill;
		primary.TriggeredWhenEnterPressed = true;

		var checkRow = form.AddRow("Checkbox / radio");
		var check = new Checkbox(checkRow);
		check.Dock = Dock.Left;
		check.Size = new(30, 0);
		check.Checked = true;
		var check2 = new Checkbox(checkRow);
		check2.Dock = Dock.Left;
		check2.Size = new(30, 0);
		var radioA = new Checkbox(checkRow);
		radioA.Dock = Dock.Left;
		radioA.Size = new(30, 0);
		radioA.Radio = true;
		radioA.Checked = true;
		var radioB = new Checkbox(checkRow);
		radioB.Dock = Dock.Left;
		radioB.Size = new(30, 0);
		radioB.Radio = true;
		radioA.LinkRadioButton(radioB);

		var textbox = form.Add("Textbox", row => new Textbox(row));
		textbox.SetHelperText("Placeholder");

		var readOnlyBox = form.Add("Read-only textbox", row => new Textbox(row));
		readOnlyBox.Text = "Can't edit this";
		readOnlyBox.ReadOnly = true;

		var slider = form.Add("NumSlider", row => new NumSlider(row), tooltip: "Drag or type a value");
		slider.MinimumValue = 0;
		slider.MaximumValue = 1;
		slider.Digits = 2;
		slider.Value = 0.4;

		var labeled = form.Add(null, row => new LabeledNumSlider(row));
		labeled.Text = "Labeled";
		labeled.Value = 12;

		form.Add("Dropdown", row => DropdownSelector<TestEnum>.FromEnum(row, TestEnum.Charlie));

		var longLabel = form.Add("Label", row => new Label(row, "A label with more text than fits in the space it has"));
		longLabel.SetTextAlignment(Anchor.CenterLeft);

		form.AddSection("More");

		var notes = form.Add("Notes", row => new Textbox(row), height: 64);
		notes.MultiLine = true;
		notes.SetTextAlignment(Anchor.TopLeft);
		notes.SetTextPadding(new(6, 4));
		notes.Text = "A taller row.\nIts label lines up with the first line.";

		var disabledRow = form.AddRow("Disabled");
		var disabledCheck = new Checkbox(disabledRow);
		disabledCheck.Dock = Dock.Left;
		disabledCheck.Size = new(30, 0);
		disabledCheck.Checked = true;
		disabledCheck.SetMouseInputEnabled(false);
		var disabledText = new Textbox(disabledRow);
		disabledText.Dock = Dock.Fill;
		disabledText.Text = "Disabled textbox";
		disabledText.SetKeyboardInputEnabled(false);

		var tabs = new TabView(window);
		tabs.Dock = Dock.Fill;

		var listTab = tabs.AddTab("List");
		var list = new ListView(listTab.Panel);
		list.Dock = Dock.Fill;
		for (int i = 0; i < 30; i++) {
			var item = new ListViewItem(list);
			item.Text = $"List item {i}";
			item.SetTextAlignment(Anchor.CenterLeft);
			item.SetTextPadding(new(8, 0));
			item.Size = new(0, 26);
		}

		var treeTab = tabs.AddTab("Tree");
		var tree = new TreeView(treeTab.Panel);
		tree.Dock = Dock.Fill;
		var root = tree.AddNode("Root");
		root.AddNode("Child A");
		var childB = root.AddNode("Child B");
		childB.AddNode("Grandchild");
		childB.AddNode("Another grandchild");
		childB.Expand();
		root.Expand();
		tree.AddNode("Another root").AddNode("Hidden child");
		tree.AddNode("Leaf root");

		var nestTab = tabs.AddTab("Nested panels");
		Element parent = nestTab.Panel;
		for (int i = 0; i < 4; i++) {
			var panel = new Panel(parent);
			panel.Dock = Dock.Fill;
			panel.DockMargin = RectangleF.TLRB(10);
			var label = new Label(panel, $"Panel depth {i + 1}");
			label.Dock = Dock.Top;
			label.Size = new(0, 24);
			parent = panel;
		}

		tabs.AddTab("A tab with a long name");

		return new(window, tabs, normal);
	}

	// Text alignment and truncation

	public record TextLayoutScene(Window Window, Textbox[] Aligned, Textbox[] MultiAligned, Label Truncated, Label MultiLineTruncated, Label Exact);

	public static TextLayoutScene TextLayout(UserInterface ui) {
		var window = MakeWindow(ui, "Text layout", new(900, 640));

		Anchor[] anchors = [
			Anchor.TopLeft, Anchor.TopCenter, Anchor.TopRight,
			Anchor.CenterLeft, Anchor.Center, Anchor.CenterRight,
			Anchor.BottomLeft, Anchor.BottomCenter, Anchor.BottomRight
		];

		var grid = new Panel(window);
		grid.Dock = Dock.Top;
		grid.Size = new(0, 3 * 48 + 4);
		grid.SetPaintBackgroundEnabled(false);
		grid.SetPaintBorderEnabled(false);
		var aligned = new Textbox[9];
		for (int i = 0; i < 9; i++) {
			var textbox = new Textbox(grid);
			textbox.Size = new(284, 44);
			textbox.Position = new(4 + (i % 3) * 290, 4 + (i / 3) * 48);
			textbox.SetTextAlignment(anchors[i]);
			textbox.SetTextPadding(new(6, 2));
			textbox.Text = anchors[i].ToString();
			aligned[i] = textbox;
		}

		var multiGrid = new Panel(window);
		multiGrid.Dock = Dock.Top;
		multiGrid.Size = new(0, 100);
		multiGrid.SetPaintBackgroundEnabled(false);
		multiGrid.SetPaintBorderEnabled(false);
		Anchor[] multiAnchors = [Anchor.TopLeft, Anchor.Center, Anchor.BottomRight];
		var multi = new Textbox[3];
		for (int i = 0; i < 3; i++) {
			var textbox = new Textbox(multiGrid);
			textbox.MultiLine = true;
			textbox.Size = new(284, 96);
			textbox.Position = new(4 + i * 290, 0);
			textbox.SetTextAlignment(multiAnchors[i]);
			textbox.SetTextPadding(new(6, 4));
			textbox.Text = $"Multi-line\n{multiAnchors[i]}";
			multi[i] = textbox;
		}

		var truncated = TruncRow("Truncated", "This sentence is much too long to fit in its label", 260, window);
		var exact = TruncRow("Exact fit", "Fits",
			Graphics2D.GetTextSize("Fits", Graphics2D.UI_FONT_NAME, 18).W + 1, window);
		var multiRow = Row(window, "Multi-line", 72);
		var multiTrunc = new Label(multiRow,
			"Short line\nThis second line is much too long to fit in its label\nThird line");
		multiTrunc.Dock = Dock.Left;
		multiTrunc.Size = new(260, 0);
		multiTrunc.SetTextAlignment(Anchor.CenterLeft);
		multiTrunc.SetPaintBackgroundEnabled(true);

		var wordRow = Row(window, "Word truncate");
		var word = new Label(wordRow, "Truncates on whole words when it can");
		word.Dock = Dock.Left;
		word.Size = new(220, 0);
		word.SetTextAlignment(Anchor.CenterLeft);
		word.TextOverflowMode = TextOverflowMode.WordTruncate;
		word.SetPaintBackgroundEnabled(true);

		var unsizedRow = Row(window, "Unsized");
		var unsized = new Label(unsizedRow, "Unsized labels draw their whole text");
		unsized.Position = new(150, 8);
		unsized.Size = new(0, 20);
		unsized.Clipping = false;
		unsized.SetTextAlignment(Anchor.CenterLeft);

		return new(window, aligned, multi, truncated, multiTrunc, exact);
	}

	private static Label TruncRow(string name, string text, float width, Window window) {
		var row = Row(window, name);
		var label = new Label(row, text);
		label.Dock = Dock.Left;
		label.Size = new(width, 0);
		label.SetTextAlignment(Anchor.CenterLeft);
		label.SetPaintBackgroundEnabled(true);
		return label;
	}

	// Color selector

	public record ColorSelectorScene(Window Window, ColorSelector Selector, ColorPicker Inline, Label Value);

	public static ColorSelectorScene ColorSelector(UserInterface ui) {
		var window = MakeWindow(ui, "Color selector",
			new(ColorPicker.PreferredSize.W + 24, ColorPicker.PreferredSize.H + 120));

		var row = Row(window, "Popup");
		var selector = new ColorSelector(row);
		selector.Dock = Dock.Left;
		selector.Size = new(96, 0);
		selector.DockMargin = RectangleF.TLRB(4);
		selector.SelectedColor = new(255, 128, 64, 255);

		var value = new Label(window);
		value.Dock = Dock.Top;
		value.Size = new(0, 32);
		value.SetTextPadding(new(8, 0));
		value.SetTextAlignment(Anchor.CenterLeft);
		Show(selector.SelectedColor, value);

		var inline = new ColorPicker(window);
		inline.Dock = Dock.Fill;
		inline.DockMargin = RectangleF.TLRB(8);
		inline.Color = selector.SelectedColor;

		selector.OnColorChanged += (_, ref color) => {
			Show(color, value);
			inline.Color = color;
		};
		inline.ColorChanged += (_, color) => {
			selector.SetColorNoUpdate(color);
			Show(color, value);
		};

		return new(window, selector, inline, value);
	}

	private static void Show(Color color, Label value)
		=> value.Text = $"R {color.R}  G {color.G}  B {color.B}  A {color.A}";

}
