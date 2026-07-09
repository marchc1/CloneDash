using CloneDash.Common;
using CloneDash.Common.Songs;
using CloneDash.Common.UI;
using CloneDash.Common.UI.Binding;
using CloneDash.Compatibility.MDMC;
using CloneDash.Menu;
using CloneDash.Menu.Character;
using CloneDash.Menu.Main;
using CloneDash.Menu.Searching;

using Nucleus;
using Nucleus.Common.Audio;
using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Debugging;
using Nucleus.Engine;
using Nucleus.Extensions;
using Nucleus.Types;
using Nucleus.UI;

using Raylib_cs;
using System.Numerics;
using Image = Nucleus.UI.Elements.Image;

namespace CloneDash.Game;


[Nucleus.MarkForStaticConstruction]
public class MainMenuLevel : Level, IMainMenuLevel
{
	public Stack<Element> ActiveElements = [];
	public MainMenuCharacter Character = null!;

	private Panel _header = null!;
	private Label _headerText = null!;

	public Element Content { get; private set; } = null!;

	private Panel _footer = null!;
	private MenuFooterButton _backButton = null!;
	private MenuFooterButton _screenButton = null!;

	#region Panel Switching

	public T PushActiveElement<T>(T element) where T : Element, IMainMenuPanel {
		if (ActiveElements.Count > 0) {
			var last = ActiveElements.Peek();
			last.SetVisible(false);
			if (last is IMainMenuPanel mmp) mmp.OnHidden();
		}

		SwitchBindings(element);
		ActiveElements.Push(element);
		element.SetRichPresence();

		_backButton.Action = ActiveElements.Count > 1 ? PopActiveElement : null;

		_targetPrimaryColor = element.GetPrimaryColor(RootPanel.GetScheme()).ToVector();
		_targetBackgroundColor = element.GetBackgroundColor(RootPanel.GetScheme()).ToVector();
		_headerText.Text = element.Name;

		if (ActiveElements.Count == 1) {
			_primaryColor = _targetPrimaryColor;
			_backgroundColor = _targetBackgroundColor;
		}

		UpdateAction(_screenButton, element.GetAction());
		element.Dock = Dock.Fill;
		element.BorderSize = 0;
		return element;
	}

	public void PopActiveElement() {
		if (ActiveElements.Count <= 1) return;

		Element element = ActiveElements.Peek();
		if (element is IMainMenuPanel mmp && !mmp.OnTryClose())
			return;

		ActiveElements.Pop();
		element.Remove();

		Element next = ActiveElements.Peek();
		IMainMenuPanel? nextPanel = next as IMainMenuPanel;
		next.SetVisible(true);

		_backButton.Action = ActiveElements.Count > 1 ? PopActiveElement : null;
		UpdateAction(_screenButton, nextPanel?.GetAction());
		SwitchBindings(nextPanel);

		if (nextPanel != null) {
			nextPanel.OnShown();
			nextPanel.SetRichPresence();

			_targetPrimaryColor = nextPanel.GetPrimaryColor(RootPanel.GetScheme()).ToVector();
			_targetBackgroundColor = nextPanel.GetBackgroundColor(RootPanel.GetScheme()).ToVector();
			_headerText.Text = nextPanel.Name;
		}
	}

	private void UpdateAction(MenuFooterButton button, MenuFooterAction? action) {
		if (action is null) {
			button.Action = null;
			return;
		}

		button.Action = action.Action;
		button.Icon = action.Icon;
		button.Text = action.Name;
	}

	#endregion

	#region Panel Bindings

	private readonly List<Keybind> _boundKeybindings = new();
	private Flow _bindingFlow = null!;

	private void SwitchBindings(IMainMenuPanel? panel) {
		_boundKeybindings.ForEach(x => Keybinds.RemoveKeybind(x));
		_bindingFlow.ClearChildren();

		if (panel is null) return;

		PanelBinding[] binds = panel.GetBindings();

		foreach (PanelBinding binding in binds) {
			_boundKeybindings.AddRange(binding.Bindings.Select(x => Keybinds.AddKeybind(x.buttons.ToList(), x.action)));
			VisualPanelBinding visual = new(_bindingFlow, binding);
			visual.SetAnchor(Anchor.CenterLeft);
			visual.SetOrigin(Anchor.CenterLeft);
		}
	}

	private void UpdateBindingColors(Color back, Color front) {
		foreach (Element element in _bindingFlow.GetChildren()) {
			element.SetBgColor(back);
			element.SetFgColor(front);
		}
	}

	#endregion

	public override void OnUnload() {
		base.OnUnload();
		MDMCWebAPI.CancelPendingRequests();
	}


	protected override UserInterface CreateUI() => new CloneDashUI();

	public override void Initialize(params object[] args) {
		var charPanel = new Panel(RootPanel);
		charPanel.BorderSize = 0;
		charPanel.DynamicallySized = true;
		charPanel.Size = new Vector2F(1f, 1f);
		charPanel.Clipping = false;
		charPanel.SetPaintBackgroundEnabled(false);

		Character = new MainMenuCharacter(charPanel) { DynamicallySized = true };
		Character.SetOrigin(Anchor.TopCenter);
		Character.Size = new Vector2F(1f);

		_header = new Panel(RootPanel);
		_header.Position = new Vector2F(0);
		_header.Size = new Vector2F(256, 64);
		_header.Dock = Dock.Top;
		_header.BorderSize = 0;

		_headerText = new Label(_header);
		_headerText.Dock = Dock.Fill;
		_headerText.Font = CloneDashUI.GetBoldFont(RootPanel.GetScheme());
		_headerText.TextSize = 32 * 1.4f;
		_headerText.SetAutoSize(true);

		Content = new Element(RootPanel);
		Content.Dock = Dock.Fill;
		Content.BorderSize = 0;
		Content.DockPadding = new RectangleF(0, 0, 0, 48);
		Content.SetPassthru(true);

		_footer = new Panel(RootPanel);
		_footer.Size = new Vector2F(256, 48);
		_footer.Dock = Dock.Bottom;
		_footer.BorderSize = 0;
		_footer.Clipping = false;

		_backButton = new MenuFooterButton(_footer, "icons/arrow-left.png", "Back");
		_backButton.SetAnchor(Anchor.BottomLeft);
		_backButton.SetOrigin(Anchor.BottomLeft);
		_backButton.Position = new Vector2F(40, -12);

		_screenButton = new MenuFooterButton(_footer);
		_screenButton.SetAnchor(Anchor.BottomRight);
		_screenButton.SetOrigin(Anchor.BottomRight);
		_screenButton.Position = new Vector2F(-40, -12);

		_bindingFlow = new Flow(_footer) {
			AutoSize = Axis.Both,
			Spacing = 20
		};
		_bindingFlow.SetAnchor(Anchor.Center);
		_bindingFlow.SetOrigin(Anchor.Center);

		Keybinds.AddKeybind([ButtonCode.KeyLeftControl, ButtonCode.KeyR], LevelTransitions.LoadMainMenu);

		PushActiveElement(new MainMenuPanel(RootPanel));
	}

	public override void PreThink(ref FrameState frameState) {
		base.PreThink(ref frameState);
		if (frameState.Keyboard.WasKeyPressed(ButtonCode.KeyEscape)) {
			// hacky but it should work
			if (ActiveElements.Count > 1) {
				var element = (ActiveElements.Peek() as IMainMenuPanel)!;
				if (element.InterceptEscape())
					PopActiveElement();
			}
		}
	}

	public override ConsoleOverlaySettings GetConsoleOverlaySettings() {
		return base.GetConsoleOverlaySettings() with {
			TextSize = 11,
			Position = new(4 + 6, (int)(_header.GetRenderBounds().H + 4))
		};
	}

	public override DeveloperOverlaySettings GetDeveloperOverlaySettings() {
		return base.GetDeveloperOverlaySettings() with {
			Offset = new(0, -_footer.GetRenderBounds().H + -8)
		};
	}

	private static float offsetBasedOnLifetime(Element e, float inf, float heightDiv) =>
		(float)(NMath.Remap(1 - NMath.Ease.OutCubic(e.Lifetime * inf), 0, 1, 0, 1, false, true) * (EngineCore.GetWindowHeight() / heightDiv));

	// At some point, this should just become an element type. This whole thing is a wreck otherwise and injects a bunch of callbacks into
	// random things... I hate it

	LevelSelectorPanel? SelectedSong;

	class LevelSelectorPanel(Element parent, SongSelector selector) : Panel(parent)
	{
		const int BUFFER_CAPACITY = 240;
		readonly ConstantLengthNumericalQueue<float> framesOverTime = new(BUFFER_CAPACITY);
		readonly Vector2F[] lineBufferL = new Vector2F[BUFFER_CAPACITY];
		readonly Vector2F[] lineBufferR = new Vector2F[BUFFER_CAPACITY];
		float currentAvgVolume = 0;
		readonly SecondOrderSystem animationSmoother = new SecondOrderSystem(6, 0.98f, 1f, 0);
		AudioPlaybackHandle track;
		bool setupTrack;

		protected override void OnThink() {
			base.OnThink();

			SetBgColor(GetBgColor() with { A = (byte)(int)Math.Clamp(NMath.Ease.OutCubic(Lifetime * 1.4f) * 155, 0, 155) });

			if (!setupTrack)
				TrySetupTrack();
		}

		public override void Paint(float w, float h) {
			base.Paint(w, h);

			var length = framesOverTime.Capacity;
			for (int i = 0; i < framesOverTime.Capacity; i++) {
				float sample = framesOverTime[framesOverTime.Capacity - 1 - i];
				var xL = (w / 2) + ((i / (float)framesOverTime.Capacity * (-w / 2)));
				var xR = (w / 2) + ((i / (float)framesOverTime.Capacity * (w / 2)));
				var y = (h / 2) + (h * .15f * sample);
				lineBufferL[i] = new(xL, y);
				lineBufferR[i] = new(xR, y);
			}

			Graphics2D.SetDrawColor(50, 50, 50, (int)(Math.Clamp(Lifetime * .6f, 0, 1) * 190));
			Rlgl.DrawRenderBatchActive();
			Rlgl.SetLineWidth(2);
			Graphics2D.DrawLineStrip(lineBufferL);
			Graphics2D.DrawLineStrip(lineBufferR);
			Rlgl.DrawRenderBatchActive();
			Rlgl.SetLineWidth(1);

			var distance = 16;
			var size = (distance * 2) - Math.Clamp(Math.Abs(animationSmoother.Update(currentAvgVolume) * 80), 0, 16);
			selector.DiscVibrate = size;

			// force-render the selector active disc
			var disc = selector.GetActiveDisc();
			var pos = disc.GetRenderBounds().Pos;
			Graphics2D.OffsetDrawing(pos);

			disc.Paint(disc.GetRenderBounds().W, disc.GetRenderBounds().H);
			// Paint the disc's children too (cover image etc.)
			foreach (var child in disc.GetChildren()) {
				if (child.IsVisible()) {
					Graphics2D.OffsetDrawing(child.GetRenderBounds().Pos);
					child.Paint(child.GetRenderBounds().W, child.GetRenderBounds().H);
					Graphics2D.OffsetDrawing(-child.GetRenderBounds().Pos);
				}
			}
			Graphics2D.OffsetDrawing(-pos);

			selector.DiscRotateAnimation = Lifetime * 90;
		}

		internal void TrySetupTrack() {
			track = selector.ActiveTrack;
			if (track.IsValid())
				setupTrack = true;
			audiosystem.AttachProcessor(track, (frames, userdata) => {
				currentAvgVolume = 0;
				for (int i = 0; i < frames.Length; i++) {
					float val = frames[i];
					currentAvgVolume += val;
					if (i % 256 == 0)
						framesOverTime.Add(val);
				}
				currentAvgVolume /= frames.Length;
				currentAvgVolume = Math.Clamp(NMath.Ease.InQuad(MathF.Abs(currentAvgVolume) * 1.5f), 0, 1.5f);
			});
		}
	}

	class LevelSelectorBackButton(LevelSelectorPanel levelSelector, SongSelector selector) : Button(levelSelector)
	{
		public override void Paint(float w, float h) {
			Position = new((levelSelector.GetRenderBounds().W / -5) - ((float)NMath.Ease.InCubic(Math.Clamp(1 - (Lifetime - 0.3), 0, 1)) * -64), 0);
			base.Paint(w, h);
		}
	}

	class LevelSelectorTitleLabel(LevelSelectorPanel levelSelector, SongSelector selector) : SongLabel(levelSelector)
	{
		protected override void OnThink() {
			base.OnThink();

			var oldSize = TextSize;
			var w = levelSelector.GetRenderBounds().W;
			TextSize = (float)Math.Clamp(NMath.Remap(w, 400, 1920, 20, 80), 12, 155);
			if (oldSize != TextSize)
				InvalidateLayout();

			SetTextColor(new(255, 255, 255, (int)(NMath.Ease.InOutCubic(Math.Clamp(Lifetime * 6, 0, 1)) * 255)));
			Position = new(0, (w / -5.2f) - offsetBasedOnLifetime(this, 1.35f, 6));
		}
	}


	class LevelSelectorAuthorLabel(LevelSelectorPanel levelSelector, SongSelector selector) : SongLabel(levelSelector)
	{
		protected override void OnThink() {
			base.OnThink();

			var oldSize = TextSize;
			var w = levelSelector.GetRenderBounds().W;
			TextSize = (float)Math.Clamp(NMath.Remap(w, 400, 1920, 12, 32), 12, 155);
			if (oldSize != TextSize)
				InvalidateLayout();

			SetTextColor(new(255, 255, 255, (int)(NMath.Ease.InOutCubic(Math.Clamp(Lifetime * 1.3f, 0, 1)) * 255)));
			Position = new(0, (w / -6f) - offsetBasedOnLifetime(this, 1.35f, 12));
		}
	}

	class LevelSelectorDifficultiesPanel(LevelSelectorPanel levelSelector, SongSelector selector, float height) : FlexPanel(levelSelector)
	{
		float height = height;

		protected override void OnThink() {
			base.OnThink();

			Position = levelSelector.GetRenderBounds().Size / 2;
			Position = Position - GetRenderBounds().Size / 2;
			Position = Position + new Vector2F(levelSelector.GetRenderBounds().W / 4f, 0);
			Size = new(256, height);
		}

		protected override void PerformLayout(float width, float height) {
			base.PerformLayout(width, height);
		}

		internal void UpdateHeight(int v) {
			height = v;
		}
	}

	class LevelSelectorSelectDifficultyButton(Element parent, string difficultyName, SongChartMetadata metadata) : Button(parent)
	{
		public override void PaintBackground(float w, float h) {
			var life = Lifetime - (offset * .15f);
			var alpha = (float)(NMath.Ease.InOutQuad(Math.Clamp(life * 2.5f, 0, 1)));

			ColorStateSetup(out var back, out var fore);
			back.A = (byte)(int)Math.Clamp(back.A * alpha, 0, 255);

			Graphics2D.SetDrawColor(back);
			Graphics2D.DrawRectangle(0, 0, w, h);

			if (BorderSize > 0) {
				fore.A = (byte)(int)Math.Clamp(fore.A * alpha, 0, 255);
				Graphics2D.SetDrawColor(fore);
				Graphics2D.DrawRectangleOutline(0, 0, w, h, BorderSize);
			}
		}
		public override void Paint(float w, float h) {
			var life = Lifetime - (offset * .15f);
			var xOffset = (float)NMath.Ease.InQuart(1 - Math.Clamp(life * 2f, 0, 1)) * -256;
			ChildRenderOffset = new(xOffset, 0);

			base.Paint(w, h);

			Vector2F textDrawingPosition = Anchor.CenterRight.GetPositionGivenAlignment(GetRenderBounds().Size, GetTextPadding());
			Graphics2D.SetDrawColor(GetTextColor());
			Graphics2D.DrawText(textDrawingPosition + new Vector2F(0, -h * 0.25f), $"{metadata.Difficulty}", Font, TextSize, Anchor.CenterRight);
		}

		bool autoplayChart;
		float offset;

		protected override void OnThink() {
			base.OnThink();
			var frameState = EngineCore.Level.FrameState;
			if (frameState.Keyboard.AltDown)
				Text = $"[AUTOPLAY] {difficultyName.ToUpper()}";
			else
				Text = $"{difficultyName.ToUpper()}";

			autoplayChart = frameState.Keyboard.AltDown;
		}

		internal void RunFunction(Action<bool> onClick) {
			onClick(autoplayChart);
		}

		internal void SetButtonOffset(float thisOffset) {
			offset = thisOffset;
		}
	}

	internal void LoadChartSelector(SongSelector selector, ISong song) {
		// Load all slow-to-get info now before the Window loads
		AudioPlaybackHandle track = selector.ActiveTrack;
		var info = song.FetchMetadata(HumanLanguage.GetCurrentLanguage());
		selector.FlyAway = 1;

		LevelSelectorPanel levelSelector = new LevelSelectorPanel(RootPanel, selector);
		SelectedSong = levelSelector;
		levelSelector.MakePopup();
		levelSelector.MakeModal();
		levelSelector.SetFgColor(Color.Blank);
		levelSelector.Dock = Dock.Fill;

		selector.EnterSheetSelection();
		selector.DiscRotateSOS.ResetTo(0);
		levelSelector.Removed += (s) => {
			if (selector != null) {
				selector.ExitSheetSelection();
			}
			SelectedSong = null;
		};

		LevelSelectorBackButton back = new(levelSelector, selector);
		back.SetAnchor(Anchor.Center);
		back.SetOrigin(Anchor.Center);
		back.Position = new(-256, 0);

		var backImage = new Image(back);
		backImage.Texture = Textures.LoadTextureFromFile("ui/back.png");
		backImage.ImageOrientation = ImageOrientation.Centered;
		backImage.Dock = Dock.Fill;
		back.OnButtonClick += (_, _) => levelSelector.Remove();
		back.Text = "";
		back.SetBgColor(new Color(0, 0));
		back.SetFgColor(new Color(0, 0));
		back.Size = new(106);

		back.Thinking += (_) => {
			backImage.ImageColor = Element.MixColorBasedOnMouseState(back, new(200, 200, 200,
				(int)(Math.Clamp(NMath.Ease.OutCubic(back.Lifetime - 0.35f), 0, 1) * 255)
				), new(0, 1, 1.3f, 1), new(0, 1, .7f, 1));
		};

		LevelSelectorTitleLabel title = new LevelSelectorTitleLabel(levelSelector, selector);
		title.TextSize = 48;
		title.Text = info.Name;
		title.SetAutoSize(true);
		title.SetAnchor(Anchor.Center);
		title.SetOrigin(Anchor.Center);

		LevelSelectorAuthorLabel author = new LevelSelectorAuthorLabel(levelSelector, selector);
		author.TextSize = 22;
		author.Text = $"by {info.Author}";
		author.SetAutoSize(true);
		author.SetAnchor(Anchor.Center);
		author.SetOrigin(Anchor.Center);

		levelSelector.TrySetupTrack();

		LevelSelectorDifficultiesPanel difficulties = new(levelSelector, selector, 356);
		difficulties.SetPaintBorderEnabled(false);
		difficulties.SetPaintBackgroundEnabled(false);
		difficulties.Direction = Axis.Vertical;
		difficulties.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;

		List<LevelSelectorSelectDifficultyButton> btns = [];
		foreach (var chart in song.GetCharts()) {
			var chartInfo = chart.FetchMetadata(HumanLanguage.GetCurrentLanguage());
			var b = CreateDifficulty(difficulties, chart, in chartInfo);
			if (b != null)
				btns.Add(b);
		}

		difficulties.UpdateHeight(btns.Count * 80);
		float offsetButtonSlide = 2f;
		for (int i = 0; i < btns.Count; i++) {
			var btn = btns[i];
			if (btn == null) continue;
			btn.SetButtonOffset(offsetButtonSlide);

			offsetButtonSlide += 1;
		}
	}

	private static LevelSelectorSelectDifficultyButton? CreateDifficulty(
		FlexPanel levelSelector, ISongChart chart, in SongChartMetadata metadata
	)
		=> CreateDifficulty(levelSelector, (autoplay) => {
			levelSelector.Level.As<MainMenuLevel>().LoadChartSheetLevel(chart, autoplay);
		}, metadata);


	public void LoadChartSheetLevel(ISongChart chart, bool autoplay) {
		LevelTransitions.LoadSongChart($"Loading '{chart.GetSong().FetchMetadata(HumanLanguage.GetCurrentLanguage()).Name}'...", chart, new() {
			Autoplay = autoplay
		});
	}

	public override void Think(FrameState frameState) {
		base.Think(frameState);

		var active = ActiveElements.Peek();
		var wasHidden = !Character.IsVisible();

		if (active is not (CharacterSelector or MainMenuPanel)) {
			Character.SetVisible(false);
			Character.StopAudio();
			return;
		}

		if (wasHidden) {
			Character.SetVisible(true);
			Character.Reset();
			Character.PlayAudio();
		}

		var center = ActiveElements.Peek() is CharacterSelector;
		var target = FrameState.WindowWidth * (center ? 0.5f : 1 / 3f);

		float x;

		if (Math.Abs(target - Character.Position.X) < 0.1)
			x = target;
		else
			x = (float)double.Lerp(target, Character.Position.X, Math.Exp(-10f * CurtimeDelta));

		Character.
		Position = new(x, 0);
		Character.CharacterOffset = new((1 - (float)NMath.Ease.OutCirc(Math.Clamp(Curtime * 1.5, 0, 1))) * -(FrameState.WindowWidth / 2), 0);
	}

	private static LevelSelectorSelectDifficultyButton? CreateDifficulty(FlexPanel levelSelector, Action<bool> onClick, SongChartMetadata metadata) {
		var difficultyName = metadata.DifficultyName;
		var buttonColor = metadata.Color;
		var designer = metadata.ChartAuthors;

		if (metadata.Difficulty == "") return null;
		if (metadata.Difficulty == "0") return null;

		LevelSelectorSelectDifficultyButton play = new(levelSelector, difficultyName, metadata);
		play.Size = new(64);
		play.DockMargin = RectangleF.TLRB(8);

		SongLabel mapper = new SongLabel(play);
		mapper.SetAutoSize(true);
		mapper.Text = $"by {designer}";
		mapper.TextSize = 16;
		mapper.SetTextAlignment(Anchor.BottomCenter);
		mapper.TextOverflowMode = TextOverflowMode.None;
		mapper.Clipping = false;
		mapper.Position = new(-8, -8);
		mapper.SetAnchor(Anchor.BottomRight);
		mapper.SetPassthru(true);
		mapper.SetOrigin(Anchor.BottomRight);
		mapper.SetTextAlignment(Anchor.TopLeft);

		play.SetBgColor(buttonColor);
		play.SetFgColor(buttonColor.Adjust(hue: 0, saturation: -0.5f, value: -0.4f));
		play.Text = "";
		play.SetTextAlignment(Anchor.CenterLeft);
		play.SetTextPadding(new(8, 0));
		play.TextSize = 28;

		play.BorderSize = 2;
		play.SetPaintBackgroundEnabled(true);

		play.OnButtonClick += delegate (Button self, ButtonCode button) {
			play.RunFunction(onClick);
		};

		return play;
	}

	public override void PreRenderBackground(FrameState frameState) {
		base.PreRenderBackground(frameState);
	}

	public Panel? GetSelectedSongPanel() => SelectedSong;

	#region Colors

	private static Vector4 _primaryColor;
	private static Vector4 _backgroundColor;

	private static Vector4 _targetPrimaryColor;
	private static Vector4 _targetBackgroundColor;

	public static Color PrimaryColor => _primaryColor.ToColor();
	public static Color BackgroundColor => _backgroundColor.ToColor();

	#endregion

	#region Background Rendering

	private static List<BackgroundShape> shapes = new();
	private const int MaxShapes = 32;

	public override void Render(FrameState frameState) {
		_primaryColor = TransitionColor(_primaryColor, _targetPrimaryColor);
		_backgroundColor = TransitionColor(_backgroundColor, _targetBackgroundColor);

		Rlgl.EnableDepthTest();

		var bg = _backgroundColor.ToColor();
		Graphics2D.SetDrawColor(bg);
		Graphics2D.DrawRectangle(Vector2F.Zero - frameState.WindowSize / 2f, frameState.WindowSize);
		_backButton.SetBgColor(bg);
		_screenButton.SetBgColor(bg);
		_headerText.SetTextColor(bg);

		bool first = shapes.Count == 0;

		if (shapes.Count < MaxShapes) {
			for (int i = shapes.Count; i < MaxShapes; i++) {
				shapes.Add(CreateRandomShape(frameState.WindowWidth, frameState.WindowHeight, first));
			}
		}

		var primary = _primaryColor.ToColor();
		_backButton.SetFgColor(primary);
		_screenButton.SetFgColor(primary);
		_header.SetBgColor(primary);
		_footer.SetBgColor(primary);

		UpdateBindingColors(bg, primary);

		var shapesColor = primary;
		shapesColor.A = 80;
		Graphics2D.SetDrawColor(shapesColor);

		Graphics2D.DrawRectangle(frameState.WindowSize / 2f, new Vector2F(200));

		for (int i = 0; i < shapes.Count; i++) {
			BackgroundShape shape = shapes[i];
			float movement = (float)RendertimeDelta * shape.Size;
			shape.Position += new Vector2F(movement * 80, movement * -80);
			shape.Rotation += 10 * movement;
			shape.Rotation %= 360;

			Vector2F pos = shape.Position - frameState.WindowSize / 2f;
			Vector2F size = new(200 * shape.Size);

			switch (shape.Type) {
				case ShapeType.Square:
					Graphics2D.DrawRectangle(RectangleF.XYWH(pos.X, pos.Y, size.X, size.Y), Vector2F.Zero / 2f, shape.Rotation);
					break;
				case ShapeType.Circle:
					Graphics2D.DrawCircle(pos, size);
					break;
				case ShapeType.Triangle:
					float triSize = size.X * 0.5f;
					Vector2F p1 = GetTriangleCorner(pos, triSize, shape.Rotation);
					Vector2F p2 = GetTriangleCorner(pos, triSize, shape.Rotation + 120);
					Vector2F p3 = GetTriangleCorner(pos, triSize, shape.Rotation + 240);
					Graphics2D.DrawTriangle(p1, p2, p3);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			if (shape.Position.X > frameState.WindowWidth + size.Y + 50 || shape.Position.Y < -size.Y - 50) {
				shapes.RemoveAt(i);
				i--;
			}
		}

		Rlgl.DisableDepthTest();
		base.Render(frameState);

		Vector2F GetTriangleCorner(Vector2F origin, float size, float angle) {
			double rads = angle * (Math.PI / 180);
			return new Vector2F(
				(float)Math.Round(origin.X + size * Math.Cos(rads), 5),
				(float)Math.Round(origin.Y + size * Math.Sin(rads), 5)
			);
		}
	}

	private Vector4 TransitionColor(Vector4 current, Vector4 target) {
		return new Vector4(
			TransitionNumber(current.X, target.X),
			TransitionNumber(current.Y, target.Y),
			TransitionNumber(current.Z, target.Z),
			TransitionNumber(current.W, target.W)
		);
	}

	private float TransitionNumber(float current, float target) {
		if (Math.Abs(target - current) < .01)
			return target;

		return (float)double.Lerp(target, current, Math.Exp(-5f * RendertimeDelta));
	}

	private BackgroundShape CreateRandomShape(float width, float height, bool shuffle) {
		bool bottom = Random.Shared.NextSingle() >= 0.5f;
		float value = Random.Shared.NextSingle();
		float size = 1f - Random.Shared.NextSingle() * .8f;

		Vector2F pos = shuffle
			? new Vector2F(Random.Shared.NextSingle() * width, Random.Shared.NextSingle() * height)
			: new Vector2F(bottom ? value * width : -300, bottom ? height + size * 200 : value * height);

		return new BackgroundShape {
			Position = pos,
			Size = size,
			Type = (ShapeType)Random.Shared.Next(3),
			Rotation = Random.Shared.NextSingle() * 360
		};
	}

	private class BackgroundShape
	{
		public Vector2F Position { get; set; }
		public float Size { get; set; } = 1f;
		public float Rotation { get; set; }
		public ShapeType Type { get; set; }
	}

	private enum ShapeType
	{
		Square,
		Circle,
		Triangle
	}

	#endregion
}
