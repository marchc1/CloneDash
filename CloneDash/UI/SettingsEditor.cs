using CloneDash.Game;
using CloneDash.Settings;
using Nucleus;
using Nucleus.Audio;
using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Types;
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
		DynamicTextSizeReference = DynamicSizeReference.SelfHeight;
		TextSize = 22;

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

	public Panel Blank(string name, string description) {
		var back = buildBackPanel(name, description);
		return back.Bottom;
	}

	public NumSlider Number(ConVar cv, string name, string format) {
		var back = buildBackPanel(name, cv.HelpString);
		var slider = back.Bottom.Add<NumSlider>();
		slider.Dock = Dock.Fill;
		slider.MinimumValue = cv.Minimum;
		slider.MaximumValue = cv.Maximum;
		slider.TextFormat = format;
		slider.Value = cv.GetDouble();
		slider.OnValueChanged += (_, _, nv) => cv.SetValue(nv);
		return slider;
	}
	public NumSlider PercentageNumber(ConVar cv, string name) => Number(cv, name, "{0:P0}");
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
	}

	private void BuildAudioPanel(SettingsPanel panel) {
		panel.PercentageNumber(SoundManagement.snd_volume, "Sound Volume");
		panel.PercentageNumber(AudioSettings.clonedash_music_volume, "Music Volume");
		panel.PercentageNumber(AudioSettings.clonedash_voice_volume, "Voice Volume");
		panel.PercentageNumber(AudioSettings.clonedash_hitsound_volume, "Hit-sound Volume");
	}
	private void BuildDisplayPanel(SettingsPanel panel) {

	}

	public void OpenOffsetWizard() {
		var offsetWizard = Level.As<CD_MainMenu>().PushActiveElement(UI.Add<JudgementOffsetWizard>());
	}
	public Button OffsetWizardCreator(Button btn) {
		btn.DynamicallySized = true;
		btn.Dock = Dock.Fill;
		btn.Text = "Judgement Offset Wizard";
		btn.DynamicTextSizeReference = DynamicSizeReference.SelfHeight;
		btn.MouseReleaseEvent += (_, _, _) => OpenOffsetWizard();

		return btn;
	}
	private void BuildInputPanel(SettingsPanel panel) {
		var offsets = panel.Blank("Wizards", "Input offset wizards.");
		var judgeBtn = OffsetWizardCreator(offsets.Add<Button>());

		var judgementSlider = panel.Number(InputSettings.clonedash_judgementoffset, "Judgement Offset", "{0:0}");
		var visualSlider = panel.Number(InputSettings.clonedash_visualoffset, "Visual Offset", "{0:0}");
	}
}

public class JudgementOffsetWizard : Panel, IMainMenuPanel
{
	public string GetName() => "Offset Wizard";
	public void OnHidden() { }
	public void OnShown() { }

	protected override void Initialize() {
		base.Initialize();

		track = Level.Sounds.LoadMusicFromFile("offset_cowbell.wav", true);
		BorderSize = 0;
	}

	protected override void OnThink(FrameState frameState) {
		base.OnThink(frameState);
		track.Update();
	}

	MusicTrack track;

	public float CalculateJudgementOffset(float localToPlayhead) {
		var mld2 = track.Length / 2f;
		return (localToPlayhead > mld2 ? ((track.Length - localToPlayhead) * -1) : localToPlayhead) / mld2;
	}

	public override void Paint(float width, float height) {
		BackgroundColor = DefaultBackgroundColor.Adjust(0, -0.5f, 0) with { A = 255 };
		base.Paint(width, height);

		Graphics2D.SetDrawColor(BackgroundColor.Adjust(0, -0.3f, 2));
		var h = height / 2;
		Graphics2D.DrawRectangle(0, (height / 2) - (h / 2), width, h);

		var offset = InputSettings.JudgementOffset;
		var midpoint = width / 2f;

		Graphics2D.SetDrawColor(255, 255, 255);
		var musicPlayhead = midpoint + (CalculateJudgementOffset((float)offset) * (width / 2));
		var offsetPlayhead = midpoint + (CalculateJudgementOffset(track.Playhead) * (width / 2));
		var padding = h * 0.25f;
		var ls = (height / 2) - (h / 2) + padding;
		Graphics2D.DrawLine(offsetPlayhead, ls, offsetPlayhead, ls + (h - (padding * 2)), height / 100f);

		var triangleSize = height / 26f;
		Graphics2D.SetDrawColor(200, 220, 255, 150);
		var startY = h - (h / 2);
		var endY = (h - (h / 2)) + h;
		Graphics2D.DrawLine(musicPlayhead, startY, musicPlayhead, endY, 4);
		Graphics2D.SetDrawColor(BackgroundColor);
		Graphics2D.DrawTriangle(new(musicPlayhead - triangleSize, startY), new(musicPlayhead + triangleSize, startY), new(musicPlayhead, startY + triangleSize));
		Graphics2D.DrawTriangle(new(musicPlayhead - triangleSize, endY), new(musicPlayhead + triangleSize, endY), new(musicPlayhead, endY - triangleSize));
	}
}