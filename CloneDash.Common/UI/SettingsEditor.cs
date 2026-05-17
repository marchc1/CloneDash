using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Game;
using CloneDash.Settings;
using CloneDash.Systems;
using Nucleus;
using Nucleus.Audio;
using Nucleus.Commands;
using Nucleus.Common.Audio;
using Nucleus.Common.Input;
using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Input;
using Nucleus.Types;
using Nucleus.UI;
using System;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;

namespace CloneDash.Menu;

public class SettingsCategory(Element? parent) : Button(parent)
{
	public SettingsPanel Panel;
	public Panel Icon;
	public void Setup(SettingsEditor panel) {
		Panel = new(panel);
		Panel.Category = this;
		Panel.Dock = Dock.Fill;

		TextAlignment = Anchor.CenterLeft; ;
		DynamicTextSizeReference = DynamicSizeReference.SelfHeight;
		TextSize = 16;

		Icon = new(this);
		BorderSize = 0;
		Icon.DrawPanelBackground = false;
		Icon.ImagePadding = new(4);
		Icon.ImageOrientation = ImageOrientation.Zoom;
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
	public SettingsPanel(Element? parent) : base(parent) {
		BorderSize = 0;
	}

	private (Panel Top, Panel Bottom, Label Name, Label Description) buildBackPanel(string nameTxt, string descTxt) {
		var panel = new Panel(this);
		panel.DrawPanelBackground = false;
		panel.DynamicallySized = true;
		panel.Dock = Dock.Top;
		panel.Size = new(0.08f);

		var top = new Panel(panel);
		top.DynamicallySized = true;
		top.Size = new(0.5f);
		top.Dock = Dock.Top;
		top.DrawPanelBackground = false;

		var name = new Label(top);
		name.Dock = Dock.Left;
		name.TextAlignment = Anchor.CenterLeft;
		name.DynamicallySized = true;
		name.TextPadding = new(16);
		name.AutoSize = true;
		name.TextSize = 24;
		name.Text = nameTxt;

		var desc = new Label(top);
		desc.Dock = Dock.Fill;
		desc.TextAlignment = Anchor.CenterLeft;
		desc.DynamicallySized = true;
		desc.TextPadding = new(16);
		desc.Text = descTxt;

		return (top, panel, name, desc);
	}

	public Panel Blank(string name, string description) {
		var back = buildBackPanel(name, description);
		return back.Bottom;
	}

	public Label Label(string text) {
		var name = new Label(this);
		name.Dock = Dock.Top;
		name.TextAlignment = Anchor.BottomLeft;
		name.DynamicallySized = true;
		name.TextPadding = new(16);
		name.AutoSize = true;
		name.Text = text;
		name.TextOverflowMode = TextOverflowMode.WordWrap;
		name.TextSize = 20;
		return name;
	}

	public NumSlider Number(ConVar cv, string name, [StringSyntax(StringSyntaxAttribute.NumericFormat)] string format) {
		var back = buildBackPanel(name, cv.HelpString);
		var slider = new NumSlider(back.Bottom);
		slider.Dock = Dock.Fill;

		if (cv.GetMin(out double min)) slider.MinimumValue = min;
		if (cv.GetMax(out double max)) slider.MaximumValue = max;
		slider.TextFormat = format;
		slider.Value = cv.GetDouble();
		if (cv.IsFlagSet(FCvar.AlwaysDefault)) {
			slider.InputDisabled = true;
			slider.Parent.TooltipText = "This element's ConVar is marked as AlwaysDefault and cannot be modified or saved this session.";
		}
		else
			slider.OnValueChanged += (_, _, nv) => cv.SetValue(nv);
		return slider;
	}
	public NumSlider PercentageNumber(ConVar cv, string name) => Number(cv, name, "{0:P0}");

	public InputActionKeybindingButtonsPanel InputActionKeybindingButtonsPanel(InputAction action, string name) {
		var back = buildBackPanel(name, "");
		var buttons = new InputActionKeybindingButtonsPanel(back.Bottom);
		back.Bottom.Size = new Vector2F(0, 0.13f);
		buttons.Dock = Dock.Fill;
		buttons.SetInputAction(action);
		return buttons;
	}
}

public class SettingsEditor : Panel, IMainMenuPanel
{
	NumSlider judgementSlider;
	NumSlider visualSlider;

	public void SetRichPresence() {
		RichPresenceSystem.SetPresence(new() {
			Details = "Main Menu",
			State = "In Settings"
		});
	}
	public string GetName() => "";
	public void OnHidden() { }

	public bool needsClosed = false;
	public bool OnTryClose() {
		if (needsClosed) return true;

		double offset = InputSettings.offset_judgement.GetDouble();
		double hitVolume = AudioSettings.snd_hitvolume.GetDouble();

		if (Math.Abs(offset) > 20 && hitVolume > 0) {

			UI.DialogOKCancel(
				"Desync Warning",
				"Your offset is high and hit sounds are on.\nThis can cause major audio desync.\n\nProceed anyway?",
				onOK: () => {
					needsClosed = true;

					if (Level is IMainMenuLevel level) {
						level.PopActiveElement();
					}
				}
			);

			return false;
		}

		return true;
	}

	public void OnShown() {
		judgementSlider?.Value = InputSettings.offset_judgement.GetDouble();
		visualSlider?.Value = InputSettings.offset_visual.GetDouble();
	}

	ScrollPanel settingCategoryPicker;
	List<SettingsCategory> categories = [];
	SettingsCategory? activeCategory;

	public SettingsPanel Category(string name, string? icon = null) {
		var category = new SettingsCategory(settingCategoryPicker);
		category.Setup(this);
		categories.Add(category);

		category.Text = name;
		category.MouseReleaseEvent += (_, _, _) => SelectCategory(category);
		category.Dock = Dock.Top;
		category.DynamicallySized = true;
		category.Size = new(0.06f);
		if (icon != null)
			category.Icon.Image = Level.Textures.LoadTextureFromFile(icon);

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

	public SettingsEditor(Element? parent) : base(parent) {
		settingCategoryPicker = new(this);
		settingCategoryPicker.DrawPanelBackground = false;
		settingCategoryPicker.Dock = Dock.Left;
		settingCategoryPicker.DynamicallySized = true;
		settingCategoryPicker.Size = new(0.25f);

		BuildAudioPanel(Category("Audio", "oxygen/preferences-desktop-sound.png"));
		BuildDisplayPanel(Category("Display", "oxygen/video-display.png"));
		BuildInputPanel(Category("Input", "oxygen/input-keyboard.png"));
	}

	private void BuildAudioPanel(SettingsPanel panel) {
		panel.PercentageNumber(EngineCore.snd_volume, "Sound Volume");
		panel.PercentageNumber(AudioSettings.snd_musicvolume, "Music Volume");
		panel.PercentageNumber(AudioSettings.snd_voicevolume, "Voice Volume");
		panel.PercentageNumber(AudioSettings.snd_hitvolume, "Hit-sound Volume");
	}
	private void BuildDisplayPanel(SettingsPanel panel) { }

	public void OpenOffsetWizard() {
		// TODO: Make offset wizard level-agnostic
		if (Level is IMainMenuLevel level)
			level.PushActiveElement(new JudgementOffsetWizard(UI));
		else
			UI.DialogOK("No Access", "You can only access the offset wizard from the main menu.");
	}
	public Button OffsetWizardCreator(Button btn) {
		btn.DynamicallySized = true;
		btn.Dock = Dock.Fill;
		btn.Text = "Open Offset Wizard";
		btn.DynamicTextSizeReference = DynamicSizeReference.SelfHeight;
		btn.MouseReleaseEvent += (_, _, _) => OpenOffsetWizard();

		return btn;
	}

	bool offsetsLinked = true;

	private void BuildInputPanel(SettingsPanel panel) {
		var offsets = panel.Blank("Offset Wizard", "Input offset wizard.");
		var judgeBtn = OffsetWizardCreator(new Button(offsets));

		var linkBack = panel.Blank("Bind Offsets", "Keep visual and judgement offsets bound (recommended).");
		var linkBtn = new Button(linkBack);
		linkBtn.Dock = Dock.Fill;
		linkBtn.Text = "Bound";

		judgementSlider = panel.Number(InputSettings.offset_judgement, "Judgement Offset", "{0:0} ms");
		visualSlider = panel.Number(InputSettings.offset_visual, "Visual Offset", "{0:0} ms");

		var isUpdating = false;

		judgementSlider.OnValueChanged += (_, _, newValue) => {
			if (offsetsLinked && !isUpdating) {
				isUpdating = true;
				visualSlider.Value = newValue;
				isUpdating = false;
			}
		};

		visualSlider.OnValueChanged += (_, _, newValue) => {
			if (offsetsLinked && !isUpdating) {
				isUpdating = true;
				judgementSlider.Value = newValue;
				isUpdating = false;
			}
		};

		linkBtn.MouseReleaseEvent += (_, _, _) => {
			offsetsLinked = !offsetsLinked;
			linkBtn.Text = offsetsLinked ? "Bound" : "Unbound";

			if (offsetsLinked) {
				isUpdating = true;
				visualSlider.Value = judgementSlider.Value;
				isUpdating = false;
			}
		};

		panel.Label("Left-click an existing key to rebind the key.\nRight-click an existing key to unbind the key.\nUse the Add button to add a new key.");

		var topButtons = panel.InputActionKeybindingButtonsPanel(InputAction.AirAttack, "Top Keys");
		var bottomButtons = panel.InputActionKeybindingButtonsPanel(InputAction.GroundAttack, "Bottom Keys");
	}
}

public class InputActionKeybindingButtonsPanel(Element? parent) : Panel(parent)
{
	InputAction action = 0;
	readonly List<ButtonCode> keys = [];
	readonly List<Button> buttons = [];

	public void LoadButtons(IEnumerable<ButtonCode> keys) {
		this.keys.Clear();
		this.keys.AddRange(keys);
		InvalidateKeyButtons();
	}

	Button? addButton;

	private void ButtonModal(string action, Action<ButtonCode> keySubmitted) {
		var dialog = UI.DialogBase($"{action} Key");

		var lbl = new Label(dialog);
		lbl.Text = "Press a key...";
		lbl.AutoSize = true;
		lbl.Anchor = Anchor.TopCenter;
		lbl.Origin = Anchor.TopCenter;

		dialog.OnKeyPressed += (_, in _, key) => {
			keySubmitted(key);
			dialog.Close();
		};
	}

	private void InvalidateKeyButtons() {
		foreach (var btn in buttons)
			btn.Remove();
		buttons.Clear();

		Button b;
		foreach (var key in keys) {
			b = new Button(this);
			b.BackgroundColor = BackgroundColor;
			b.ForegroundColor = ForegroundColor;
			b.Text = key.GetString();
			b.SetTag("key", key);

			b.MouseReleaseEvent += ButtonEditOrRemoveHandler;

			buttons.Add(b);
		}

		b = new Button(this);
		b.BackgroundColor = BackgroundColor;
		b.ForegroundColor = ForegroundColor;
		b.Text = "Add...";
		b.MouseReleaseEvent += ButtonAddHandler;
		buttons.Add(b);
		addButton = b;

		InvalidateLayout();
	}

	private void ButtonAddHandler(Element self, FrameState state, ButtonCode button) {
		if (button == ButtonCode.Mouse1)
			ButtonModal("Bind", AddSubmittedHandler);
	}

	private void AddSubmittedHandler(ButtonCode key) {
		// Confirm that key isn't bound.
		if (InputSettings.IsKeyBound(key, out InputAction action)) {
			Logs.Warn($"Keyboard key {key} is already bound to {action}. TODO: notification");
			return;
		}

		InputSettings.BindKey(key, this.action);

		InvalidateKeys();
	}

	private void EditSubmittedHandler(ButtonCode keyTarget, ButtonCode keyReplace) {
		if (!InputSettings.IsKeyBound(keyTarget, out _)) {
			Logs.Warn($"Keyboard key target {keyTarget} is not bound. TODO: notification");
			return;
		}

		if (InputSettings.IsKeyBound(keyReplace, out InputAction action)) {
			Logs.Warn($"Keyboard key replacement {keyReplace} is already bound to {action}. TODO: notification");
			return;
		}

		InputSettings.RebindKey(keyTarget, keyReplace, this.action);

		InvalidateKeys();
	}

	private void RemoveSubmittedHandler(ButtonCode key) {
		if (!InputSettings.IsKeyBound(key, out _)) {
			Logs.Warn($"Keyboard key target {key} is not bound. TODO: notification");
			return;
		}

		InputSettings.UnbindKey(key);

		InvalidateKeys();
	}


	private void ButtonEditOrRemoveHandler(Element self, FrameState state, ButtonCode button) {
		if (button == ButtonCode.Mouse2) {
			RemoveSubmittedHandler(self.GetTag<ButtonCode>("key"));
		}
		else if (button == ButtonCode.Mouse1) {
			ButtonModal("Rebind", x => EditSubmittedHandler(self.GetTag<ButtonCode>("key"), x));
		}
	}

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);
		int padding = 4;
		int innerPadding = 4;
		float x = padding;
		foreach (var btn in buttons) {
			btn.Position = new(x, innerPadding);
			float sizeW = height - padding;
			if (btn == addButton)
				sizeW = sizeW * 1.5f;
			btn.Size = new(sizeW, height - (innerPadding * 2));
			if (btn == addButton)
				btn.TextSize = height / 2f;
			else
				btn.TextSize = height / 1.4f;
			x += height;
		}
	}

	public void SetInputAction(InputAction action) {
		this.action = action;

		switch (action) {
			case InputAction.AirAttack:
				BackgroundColor = PathwayExts.GetColor(PathwaySide.Top).Adjust(0, -0.2, -0.5);
				ForegroundColor = PathwayExts.GetColor(PathwaySide.Top);
				InvalidateKeys();
				break;
			case InputAction.GroundAttack:
				BackgroundColor = PathwayExts.GetColor(PathwaySide.Bottom).Adjust(0, -0.2, -0.5);
				ForegroundColor = PathwayExts.GetColor(PathwaySide.Bottom);
				InvalidateKeys();
				break;
			default: throw new InvalidOperationException($"Unsupported {nameof(InputAction)} provided to {nameof(InputActionKeybindingButtonsPanel)}.{nameof(SetInputAction)}");
		}
	}

	private void InvalidateKeys() {
		LoadButtons(InputSettings.GetKeysOfAction(action));
	}
}

public class JudgementOffsetWizard : Panel, IMainMenuPanel
{
	public void SetRichPresence() {
		RichPresenceSystem.SetPresence(new() {
			Details = "Main Menu",
			State = "In Settings"
		});
	}

	public string GetName() => "Offset Wizard";
	public void OnHidden() { }
	public void OnShown() { }

	bool isDragging = false;
	float currentWidth = 0;
	List<(float X, DateTime Time)> hitMarkers = new();

	Label currentOffsetLabel;
	Label lastHitLabel;
	float? lastHitOffsetMs = null;

	public JudgementOffsetWizard(Element? parent) : base(parent) {
		currentOffsetLabel = new Label(this);
		currentOffsetLabel.Anchor = Anchor.TopCenter;
		currentOffsetLabel.Origin = Anchor.TopCenter;
		currentOffsetLabel.TextAlignment = Anchor.TopCenter;
		currentOffsetLabel.Position = new(0, 24);
		currentOffsetLabel.TextSize = 36;
		currentOffsetLabel.AutoSize = true;

		lastHitLabel = new Label(this);
		lastHitLabel.Anchor = Anchor.TopCenter;
		lastHitLabel.Origin = Anchor.TopCenter;
		lastHitLabel.TextAlignment = Anchor.TopCenter;
		lastHitLabel.Position = new(0, 64);
		lastHitLabel.TextSize = 28;
		lastHitLabel.AutoSize = true;
		lastHitLabel.Text = "Press any key to the beat";

		var clip = audiosystem.CreateFileAudioClip("offset_cowbell.wav");
		track = audiosystem.CreatePlayback(clip);
		BorderSize = 0;

		MouseClickEvent += (_, _, btn) => {
			if (btn == ButtonCode.Mouse1) isDragging = true;
			DemandKeyboardFocus();
		};

		MouseReleaseEvent += (_, _, btn) => {
			if (btn == ButtonCode.Mouse1) isDragging = false;
			DemandKeyboardFocus();
		};

		OnKeyPressed += (_, in _, key) => {
			if (currentWidth > 0 && audiosystem.GetSoundPlayhead(track, out double playhead)) {
				var len = audiosystem.GetPlaybackDuration(track);
				float midpoint = currentWidth / 2f;
				float normX = CalculateJudgementOffset((float)playhead);
				float currentX = midpoint + normX * (currentWidth / 2f);
				hitMarkers.Add((currentX, DateTime.Now));

				lastHitOffsetMs = normX * ((float)len / 2f) * 1000f;
			}
		};

		DemandKeyboardFocus();
	}

	protected override void OnThink(FrameState frameState) {
		base.OnThink(frameState);
		audiosystem.UpdatePlayback(track);
		currentOffsetLabel.Text = $"Current Offset: {InputSettings.offset_judgement.GetDouble():0} ms";

		if (lastHitOffsetMs != null)
			lastHitLabel.Text = $"Last Hit: {lastHitOffsetMs:0} ms";

		if (isDragging && currentWidth > 0) {
			float mouseX = frameState.Mouse.MousePos.X;

			mouseX = Math.Clamp(mouseX, 0, currentWidth);

			float midpoint = currentWidth / 2f;
			float mld2 = (float)audiosystem.GetPlaybackDuration(track) / 2f;

			float normalizedX = (mouseX - midpoint) / midpoint;
			float newOffset = normalizedX * mld2 * 1000f;

			InputSettings.offset_judgement.SetValue((double)newOffset);
			InputSettings.offset_visual.SetValue((double)newOffset);
		}
	}

	AudioPlaybackHandle track;

	public float CalculateJudgementOffset(float localToPlayhead) {
		var len = audiosystem.GetPlaybackDuration(track);
		var mld2 = len / 2f;
		return (float)((localToPlayhead > mld2 ? (len - localToPlayhead) * -1 : localToPlayhead) / mld2);
	}

	public override void Paint(float width, float height) {
		currentWidth = width;

		BackgroundColor = DefaultBackgroundColor.Adjust(0, -0.5f, 0) with { A = 255 };
		base.Paint(width, height);

		Graphics2D.SetDrawColor(BackgroundColor.Adjust(0, -0.3f, 2));
		var h = height / 2;
		Graphics2D.DrawRectangle(0, height / 2 - h / 2, width, h);

		var offset = InputSettings.JudgementOffset;
		var midpoint = width / 2f;

		Graphics2D.SetDrawColor(255, 255, 255);
		audiosystem.GetSoundPlayhead(track, out double playhead);
		var musicPlayhead = midpoint + CalculateJudgementOffset((float)offset) * (width / 2);
		var offsetPlayhead = midpoint + CalculateJudgementOffset((float)playhead) * (width / 2);
		var padding = h * 0.25f;
		var ls = height / 2 - h / 2 + padding;
		Graphics2D.DrawLine(offsetPlayhead, ls, offsetPlayhead, ls + (h - padding * 2), height / 100f);

		var now = DateTime.Now;
		for (int i = hitMarkers.Count - 1; i >= 0; i--) {
			var (X, Time) = hitMarkers[i];
			var age = (now - Time).TotalSeconds;

			if (age >= 1.0) {
				hitMarkers.RemoveAt(i);
				continue;
			}

			byte alpha = (byte)(255 * (1.0 - age));
			Graphics2D.SetDrawColor(255, 255, 0, alpha);
			Graphics2D.DrawLine(X, ls, X, ls + (h - padding * 2), height / 100f);
		}

		var triangleSize = height / 26f;

		if (isDragging)
			Graphics2D.SetDrawColor(255, 255, 255, 220);
		else
			Graphics2D.SetDrawColor(200, 220, 255, 150);

		var startY = h - h / 2;
		var endY = h - h / 2 + h;
		Graphics2D.DrawLine(musicPlayhead, startY, musicPlayhead, endY, 4);
		Graphics2D.SetDrawColor(BackgroundColor);
		Graphics2D.DrawTriangle(new(musicPlayhead - triangleSize, startY), new(musicPlayhead + triangleSize, startY), new(musicPlayhead, startY + triangleSize));
		Graphics2D.DrawTriangle(new(musicPlayhead - triangleSize, endY), new(musicPlayhead + triangleSize, endY), new(musicPlayhead, endY - triangleSize));
	}
}