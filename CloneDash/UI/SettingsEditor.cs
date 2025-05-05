using CloneDash.Game;
using CloneDash.Settings;
using Nucleus;
using Nucleus.Audio;
using Nucleus.UI;

namespace CloneDash.UI;

public class SettingsCategory : Button
{
	public SettingsPanel Panel;
	public Panel Icon;
	protected override void Initialize() {
		base.Initialize();
	}

	public void Setup(SettingsEditor panel) {
		panel.Add(out Panel);
		Panel.Category = this;
		Panel.Dock = Dock.Fill;

		TextAlignment = Nucleus.Types.Anchor.CenterLeft; ;

		Add(out Icon);
		BorderSize = 0;
		Icon.DrawPanelBackground = false;
		Icon.ImagePadding = new(4);
		Icon.ImageOrientation = Nucleus.Types.ImageOrientation.Zoom;
	}

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);
		Icon.Position = new(4, 0);
		Icon.Size = new(height, height);
		TextPadding = new(height + 16, 0);
	}
}

public class SettingsPanel : ScrollPanel
{
	public SettingsCategory Category;
	protected override void Initialize() {
		base.Initialize();
		BorderSize = 0;
	}

	private (Panel Top, Panel Bottom, Label Name, Label Description) buildBackPanel(string nameTxt, string descTxt) {
		var panel = Add<Panel>();
		panel.DrawPanelBackground = false;
		panel.DynamicallySized = true;
		panel.Dock = Dock.Top;
		panel.Size = new(0.08f);

		var top = panel.Add<Panel>();
		top.DynamicallySized = true;
		top.Size = new(0.5f);
		top.Dock = Dock.Top;
		top.DrawPanelBackground = false;

		var name = top.Add<Label>();
		name.Dock = Dock.Left;
		name.TextAlignment = Nucleus.Types.Anchor.CenterLeft;
		name.DynamicallySized = true;
		name.TextPadding = new(16);
		name.AutoSize = true;
		name.TextSize = 24;
		name.Text = nameTxt;

		var desc = top.Add<Label>();
		desc.Dock = Dock.Fill;
		desc.TextAlignment = Nucleus.Types.Anchor.CenterLeft;
		desc.DynamicallySized = true;
		desc.TextPadding = new(16);
		desc.Text = descTxt;

		return (top, panel, name, desc);
	}

	public void Number(ConVar cv, string name) {
		var back = buildBackPanel(name, cv.HelpString);
		var slider = back.Bottom.Add<NumSlider>();
		slider.Dock = Dock.Fill;
		slider.MinimumValue = cv.Minimum;
		slider.MaximumValue = cv.Maximum;
		slider.TextFormat = "{0:P0}";
		slider.Value = cv.GetDouble();
		slider.OnValueChanged += (_, _, nv) => cv.SetValue(nv);
	}
}

public class SettingsEditor : Panel, IMainMenuPanel
{
	public string GetName() => "";
	public void OnHidden() { }
	public void OnShown() { }

	ScrollPanel settingCategoryPicker;
	List<SettingsCategory> categories = [];
	SettingsCategory? activeCategory;

	public SettingsPanel Category(string name, string? icon = null) {
		var category = settingCategoryPicker.Add<SettingsCategory>();
		category.Setup(this);
		categories.Add(category);

		category.Text = name;
		category.MouseReleaseEvent += (_, _, _) => SelectCategory(category);
		category.Dock = Dock.Top;
		category.DynamicallySized = true;
		category.Size = new(0.06f);
		category.TextSize = 32;
		if (icon != null)
			category.Icon.Image = Textures.LoadTextureFromFile(icon);

		if (activeCategory == null)
			SelectCategory(category);
		else {
			category.Panel.Visible = category.Panel.Enabled = false;
		}
		return category.Panel;
	}

	public void SelectCategory(SettingsCategory category) {
		if (activeCategory != null)
			activeCategory.Panel.Visible = activeCategory.Panel.Enabled = activeCategory.Pulsing = false;

		activeCategory = category;
		category.Panel.Visible = category.Panel.Enabled = category.Pulsing = true;
	}

	protected override void Initialize() {
		base.Initialize();

		Add(out settingCategoryPicker);
		settingCategoryPicker.DrawPanelBackground = false;
		settingCategoryPicker.Dock = Dock.Left;
		settingCategoryPicker.DynamicallySized = true;
		settingCategoryPicker.Size = new(0.25f);

		BuildAudioPanel(Category("Audio", "oxygen/preferences-desktop-sound.png"));
		BuildDisplayPanel(Category("Display", "oxygen/video-display.png"));
		BuildInputPanel(Category("Input", "oxygen/input-keyboard.png"));
		BuildOffsetsPanel(Category("Offsets", "ui/offsets.png"));
	}

	private void BuildAudioPanel(SettingsPanel panel) {
		panel.Number(SoundManagement.snd_volume, "Sound Volume");
	}
	private void BuildDisplayPanel(SettingsPanel panel) {

	}
	private void BuildInputPanel(SettingsPanel panel) {

	}
	private void BuildOffsetsPanel(SettingsPanel panel) {

	}
}
