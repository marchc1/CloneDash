using Nucleus.Common.Graphics;
using Nucleus.Extensions;
using Nucleus.Models;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;

namespace Nucleus.ModelEditor.UI;

public enum ExportType
{
	Binary,
	JSON
}

/// <summary>
/// Export settings, saved with the project
/// </summary>
public class ExportSettings
{
	public ExportType Type = ExportType.Binary;
	public string? OutputFolder;
	public bool Warnings = true;
	public bool Pack = true;
	public AtlasPackSettings PackSettings = new();
	/// <summary>
	/// Skins left out, as "model/skin"
	/// </summary>
	public HashSet<string> ExcludedSkins = [];

	public static string SkinKey(EditorModel model, EditorSkin skin) => $"{model.Name}/{skin.Name}";
}

/// <summary>
/// The export dialog, data type on the left and its settings on the right
/// </summary>
public static class ExportDialog
{
	private const float ROW_HEIGHT = ToolRows.ROW_HEIGHT;
	private const float GAP = ToolRows.GAP;
	public const float BAR_HEIGHT = ROW_HEIGHT + GAP * 4;

	public static Window? Open { get; private set; }

	/// <summary>
	/// Row of buttons along the bottom of a dialog
	/// </summary>
	public static FlexPanel ButtonBar(Window win) {
		var bar = new Panel(win);
		bar.Dock = Dock.Bottom;
		bar.Size = new(0, BAR_HEIGHT);
		bar.DockPadding = RectangleF.TLRB(GAP * 2);
		bar.SetPaintBackgroundEnabled(false);
		bar.SetPaintBorderEnabled(false);
		return ToolRows.NewFlex(bar);
	}

	public static void Show(ModelEditor editor) {
		if (Open != null) return;
		var file = editor.File;
		var settings = file.Export;
		settings.OutputFolder ??= file.DiskPath != null ? Path.GetDirectoryName(file.DiskPath) : AppContext.BaseDirectory;

		var win = Open = new Window(EngineCore.Level.RootPanel);
		win.Title = "Export";
		win.HideNonCloseButtons();
		win.Resizable = false;
		var skins = new List<(EditorModel Model, EditorSkin Skin)>();
		foreach (var exportModel in file.Models) {
			if (!exportModel.Export) continue;
			foreach (var exportSkin in exportModel.Skins)
				skins.Add((exportModel, exportSkin));
		}
		float content = (ROW_HEIGHT + GAP) * (4 + skins.Count) + (skins.Count > 0 ? ROW_HEIGHT : 0);
		win.Size = new(720, win.Titlebar.Size.H + content + BAR_HEIGHT + GAP * 3);
		win.Center();
		win.MakePopup();
		win.Removed += _ => Open = null;

		var buttons = ButtonBar(win);

		var types = new Panel(win);
		types.Dock = Dock.Left;
		types.Size = new(150, 0);
		types.DockMargin = RectangleF.TLRB(GAP * 2, GAP * 2, 0, GAP);
		types.SetPaintBorderEnabled(false);

		var form = new Form(win);
		form.Dock = Dock.Fill;
		form.DockMargin = RectangleF.TLRB(GAP * 2, GAP * 2, GAP * 2, GAP);
		form.RowGap = GAP;

		foreach (var type in Enum.GetValues<ExportType>()) {
			var button = new Button(types);
			button.Dock = Dock.Top;
			button.Size = new(0, ROW_HEIGHT);
			button.DockMargin = RectangleF.TLRB(0, 0, 0, GAP);
			button.Text = type == ExportType.Binary ? "Binary" : "JSON";
			button.BorderSize = 0;
			var baseBg = button.GetBgColor();
			button.Thinking += (_) =>
				button.SetBgColor(settings.Type == type ? baseBg.Adjust(0, 0.5, 2.4) : baseBg);
			button.OnButtonClick += (_, _) => settings.Type = type;
		}

		// Output folder: path box with a square browse button
		var folderRow = form.AddRow("Output folder");
		var browse = new Button(folderRow.Content);
		browse.Dock = Dock.Right;
		browse.Size = new(ROW_HEIGHT, 0);
		browse.DockMargin = RectangleF.TLRB(0, GAP, 0, 0);
		browse.Text = "...";
		var folder = new Textbox(folderRow.Content);
		folder.Dock = Dock.Fill;
		folder.Text = settings.OutputFolder ?? "";
		folder.SetTextAlignment(Anchor.CenterLeft);
		folder.OnTextChanged += (_, _, text) => settings.OutputFolder = text;
		browse.OnButtonClick += (_, _) => {
			var result = Platform.SelectFolderDialog("Output folder", settings.OutputFolder ?? AppContext.BaseDirectory);
			if (!result.Cancelled)
				folder.Text = settings.OutputFolder = result.Result;
		};

		var extension = form.Add("Extension", r => new Label(r));
		extension.SetTextAlignment(Anchor.CenterLeft);
		extension.Thinking += (_) => extension.Text = settings.Type == ExportType.Binary ? ModelBinary.FULL_EXTENSION : ModelRefJSON.FULL_EXTENSION;

		Check(form, "Warnings", () => settings.Warnings, v => settings.Warnings = v);

		var packRow = form.AddRow("Texture atlas");
		var packCell = ToolRows.NewFlex(packRow.Content);
		var pack = CheckCell(packCell, "Pack", () => settings.Pack, v => settings.Pack = v);
		var packSettings = ToolRows.AddButton(packCell, "Pack settings...", () => {
			win.SetVisible(false);
			PackSettingsDialog.Show(settings.PackSettings, () => {
				if (IValidatable.IsValid(win))
					win.SetVisible(true);
			});
		});
		packSettings.Thinking += (_) => packSettings.SetMouseInputEnabled(settings.Pack);
		ToolRows.SpaceCells(packCell);

		if (skins.Count > 0) {
			form.AddSection("Skins");
			bool many = file.Models.Count > 1;
			foreach (var item in skins) {
				var key = ExportSettings.SkinKey(item.Model, item.Skin);
				var label = many ? $"{item.Model.Name}: {item.Skin.Name}" : item.Skin.Name;
				Check(form, label, () => !settings.ExcludedSkins.Contains(key), included => {
					if (included)
						settings.ExcludedSkins.Remove(key);
					else
						settings.ExcludedSkins.Add(key);
				});
			}
		}

		ToolRows.AddButton(buttons, "Export", () => {
			var warnings = Run(file, settings);
			win.Remove();
			if (settings.Warnings && warnings.Count > 0) {
				var more = warnings.Count > 12 ? $"\n...and {warnings.Count - 12} more" : "";
				EditorDialogs.ConfirmAction("Export warnings", string.Join("\n", warnings.Take(12)) + more, true);
			}
		});
		ToolRows.AddButton(buttons, "Cancel", win.Remove);
		ToolRows.SpaceCells(buttons);
	}

	/// <summary>
	/// Exports every model with <paramref name="settings"/>
	/// </summary>
	/// <returns>The warnings</returns>
	public static List<string> Run(EditorFile file, ExportSettings settings) {
		var warnings = new List<string>();
		var folder = settings.OutputFolder;
		if (string.IsNullOrWhiteSpace(folder)) {
			warnings.Add("No output folder.");
			return warnings;
		}
		Directory.CreateDirectory(folder);

		foreach (var model in file.Models) {
			if (!model.Export) continue;
			var converter = new RuntimeConverter();
			foreach (var skin in model.Skins) {
				if (settings.ExcludedSkins.Contains(ExportSettings.SkinKey(model, skin)))
					converter.ExcludedSkins.Add(skin);
			}

			var data = converter.LoadModelFromEditor(model);
			var name = string.Concat((string.IsNullOrWhiteSpace(model.Name) ? "model" : model.Name)
				.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
			PackedAtlas? atlas;
			if (settings.Type == ExportType.Binary) {
				var format = new ModelBinary { PackAtlas = settings.Pack, PackSettings = settings.PackSettings };
				format.SaveModelToFile(Path.Combine(folder, name + ModelBinary.FULL_EXTENSION), data);
				atlas = format.LastAtlas;
			}
			else {
				var format = new ModelRefJSON { PackAtlas = settings.Pack, PackSettings = settings.PackSettings };
				format.SaveModelToFile(Path.Combine(folder, name + ModelRefJSON.FULL_EXTENSION), data);
				atlas = format.LastAtlas;
			}
			if (atlas != null)
				warnings.AddRange(atlas.Warnings.Select(w => file.Models.Count > 1 ? $"{model.Name}: {w}" : w));
			Logs.Info($"Exported '{model.Name}' to {folder}");
		}
		return warnings;
	}

	public static Checkbox Check(Form form, string label, Func<bool> get, Action<bool> set) {
		var row = form.AddRow(label);
		var box = new Checkbox(row.Content);
		box.Dock = Dock.Left;
		box.Size = new(ROW_HEIGHT - 8);
		box.DockMargin = RectangleF.TLRB(4, 0, 0, 4);
		box.Checked = get();
		box.OnCheckedChanged += (c) => set(c.Checked);
		return box;
	}

	public static Checkbox CheckCell(FlexPanel row, string text, Func<bool> get, Action<bool> set) {
		var cell = new Panel(row);
		cell.SetPaintBackgroundEnabled(false);
		cell.SetPaintBorderEnabled(false);

		var box = new Checkbox(cell);
		box.Dock = Dock.Left;
		box.Size = new(ROW_HEIGHT - 8);
		box.DockMargin = RectangleF.TLRB(4, 0, 6, 4);
		box.Checked = get();
		box.OnCheckedChanged += (c) => set(c.Checked);

		var label = new Label(cell);
		label.Dock = Dock.Fill;
		label.Text = text;
		label.SetTextAlignment(Anchor.CenterLeft);
		label.PassMouseTo(box);
		return box;
	}
}

/// <summary>
/// Texture packer settings, in two columns
/// </summary>
public static class PackSettingsDialog
{
	private const float ROW_HEIGHT = ToolRows.ROW_HEIGHT;
	private const float GAP = ToolRows.GAP;
	private const float BAR_HEIGHT = ExportDialog.BAR_HEIGHT;

	public static Window? Open { get; private set; }

	public static void Show(AtlasPackSettings target, Action? closed = null) {
		if (Open != null) return;
		var settings = target.Clone();

		var win = Open = new Window(EngineCore.Level.RootPanel);
		win.Title = "Pack settings";
		win.HideNonCloseButtons();
		win.Resizable = false;
		// tallest column: 3 sections, 10 rows
		win.Size = new(900, win.Titlebar.Size.H + ROW_HEIGHT * 3 + (ROW_HEIGHT + GAP) * 10 + BAR_HEIGHT + GAP * 3);
		win.Center();
		win.MakePopup();
		bool reopening = false;
		win.Removed += _ => {
			Open = null;
			if (!reopening)
				closed?.Invoke();
		};

		var buttons = ExportDialog.ButtonBar(win);

		var columns = ToolRows.NewFlex(win);
		columns.DockMargin = RectangleF.TLRB(0, GAP * 2, GAP * 2, GAP);

		var left = new Form(columns);
		left.RowGap = GAP;
		var right = new Form(columns);
		right.RowGap = GAP;
		ToolRows.SpaceCells(columns);
		left.DockMargin = RectangleF.TLRB(0, 0, GAP * 2, 0);
		right.DockMargin = RectangleF.TLRB(0, GAP * 2, 0, 0);

		left.AddSection("Regions");
		Pair(left, "Strip whitespace",
			"X", () => settings.StripWhitespaceX, v => settings.StripWhitespaceX = v,
			"Y", () => settings.StripWhitespaceY, v => settings.StripWhitespaceY = v);
		ExportDialog.Check(left, "Rotation", () => settings.Rotation, v => settings.Rotation = v);
		ExportDialog.Check(left, "Alias", () => settings.Alias, v => settings.Alias = v);
		ExportDialog.Check(left, "Ignore blank images",
			() => settings.IgnoreBlankImages, v => settings.IgnoreBlankImages = v);
		Slider(left, "Alpha threshold", () => settings.AlphaThreshold, v => settings.AlphaThreshold = v, 0, 255);

		left.AddSection("Region padding");
		Sliders(left, "Padding",
			() => settings.PaddingX, v => settings.PaddingX = v,
			() => settings.PaddingY, v => settings.PaddingY = v,
			0, 64);
		ExportDialog.Check(left, "Edge padding", () => settings.EdgePadding, v => settings.EdgePadding = v);
		ExportDialog.Check(left, "Duplicate padding",
			() => settings.DuplicatePadding, v => settings.DuplicatePadding = v);

		left.AddSection("Output");
		ExportDialog.Check(left, "Premultiply alpha",
			() => settings.PremultiplyAlpha, v => settings.PremultiplyAlpha = v);
		ExportDialog.Check(left, "Bleed", () => settings.Bleed, v => settings.Bleed = v);

		right.AddSection("Pages");
		Sliders(right, "Min size",
			() => settings.MinWidth, v => settings.MinWidth = v,
			() => settings.MinHeight, v => settings.MinHeight = v,
			1, 8192);
		Sliders(right, "Max size",
			() => settings.MaxWidth, v => settings.MaxWidth = v,
			() => settings.MaxHeight, v => settings.MaxHeight = v,
			16, 8192);
		ExportDialog.Check(right, "Power of two", () => settings.PowerOfTwo, v => settings.PowerOfTwo = v);
		ExportDialog.Check(right, "Divisible by 4", () => settings.DivisibleBy4, v => settings.DivisibleBy4 = v);
		ExportDialog.Check(right, "Square", () => settings.Square, v => settings.Square = v);

		right.AddSection("Runtime");
		Dropdown(right, "Filter min", settings.FilterMin, v => settings.FilterMin = v);
		Dropdown(right, "Filter mag", settings.FilterMag, v => settings.FilterMag = v);
		Dropdown(right, "Wrap", settings.Wrap, v => settings.Wrap = v);

		right.AddSection("Options");
		ExportDialog.Check(right, "Fast", () => settings.Fast, v => settings.Fast = v);

		ToolRows.AddButton(buttons, "OK", () => {
			Copy(settings, target);
			win.Remove();
		});
		ToolRows.AddButton(buttons, "Cancel", win.Remove);
		ToolRows.AddButton(buttons, "Defaults", () => {
			Copy(new AtlasPackSettings(), target);
			reopening = true;
			win.Remove();
			Show(target, closed);
		});
		ToolRows.SpaceCells(buttons);
	}

	private static void Copy(AtlasPackSettings from, AtlasPackSettings to) {
		foreach (var field in typeof(AtlasPackSettings).GetFields())
			field.SetValue(to, field.GetValue(from));
	}

	private static void Pair(Form form, string label, string textA, Func<bool> getA, Action<bool> setA, string textB, Func<bool> getB, Action<bool> setB) {
		var row = ToolRows.NewFlex(form.AddRow(label).Content);
		ExportDialog.CheckCell(row, textA, getA, setA);
		ExportDialog.CheckCell(row, textB, getB, setB);
		ToolRows.SpaceCells(row);
	}

	private static NumSlider NewSlider(Element parent, int value, int min, int max, Action<int> set) {
		var slider = new NumSlider(parent);
		slider.MinimumValue = min;
		slider.MaximumValue = max;
		slider.Digits = 0;
		slider.Value = value;
		slider.OnValueChanged += (_, _, v) => set((int)Math.Round(v));
		return slider;
	}

	private static void Slider(Form form, string label, Func<int> get, Action<int> set, int min, int max)
		=> form.Add(label, r => NewSlider(r, get(), min, max, set));

	private static void Sliders(Form form, string label, Func<int> getX, Action<int> setX, Func<int> getY, Action<int> setY, int min, int max) {
		var row = ToolRows.NewFlex(form.AddRow(label).Content);
		NewSlider(row, getX(), min, max, setX);
		NewSlider(row, getY(), min, max, setY);
		ToolRows.SpaceCells(row);
	}

	private static void Dropdown<T>(Form form, string label, T value, Action<T> set) where T : Enum {
		var box = form.Add(label, r => DropdownSelector<T>.FromEnum(r, value));
		box.OnSelectionChanged += (_, _, v) => set(v);
	}
}
