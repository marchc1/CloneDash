using CloneDash.Game;
using Nucleus.UI;

namespace CloneDash.UI;

public class SettingsCategory : Button
{
	public SettingsPanel Panel;
	public Panel Icon;
	protected override void Initialize() {
		base.Initialize();

		Parent.Parent.Parent.Add(out Panel);
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

		return category.Panel;
	}

	public void SelectCategory(SettingsCategory category) {
		if (activeCategory != null)
			activeCategory.Panel.Visible = activeCategory.Pulsing = false;

		activeCategory = category;
		category.Panel.Visible = category.Pulsing = true;
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

	}
	private void BuildDisplayPanel(SettingsPanel panel) {

	}
	private void BuildInputPanel(SettingsPanel panel) {

	}
	private void BuildOffsetsPanel(SettingsPanel panel) {

	}
}
