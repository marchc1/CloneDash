using CloneDash.Characters;
using CloneDash.Common;
using CloneDash.Common.Songs;
using CloneDash.Compatibility.MDMC;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Menu;
using CloneDash.Menu.Searching;

using Nucleus;
using Nucleus.Audio;
using Nucleus.Commands;
using Nucleus.Common.Audio;
using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Extensions;
using Nucleus.Files;
using Nucleus.Input;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;

using Raylib_cs;

namespace CloneDash.Game;


[Nucleus.MarkForStaticConstruction]
public class MainMenuLevel : Level, IMainMenuLevel
{
	public Stack<Element> ActiveElements = [];
	public CharacterPanel Character = null!;

	public T PushActiveElement<T>(T element) where T : Element, IMainMenuPanel {
		if (ActiveElements.Count > 0) {
			var last = ActiveElements.Peek();
			last.Visible = false;
			last.Enabled = false;
			if (last is IMainMenuPanel mmp) mmp.OnHidden();
		}

		ActiveElements.Push(element);
		element.SetRichPresence();

		backButton.Enabled = backButton.Visible = ActiveElements.Count > 1;

		element.Dock = Dock.Fill;
		return element;
	}

	private Button backButton;
	public override void OnUnload() {
		base.OnUnload();
		MDMCWebAPI.CancelPendingRequests();
	}
	public void PopActiveElement() {
		if (ActiveElements.Count <= 1) return;

		var element = ActiveElements.Peek();
		if (element is IMainMenuPanel mmp)
			if (!mmp.OnTryClose())
				return;

		ActiveElements.Pop();
		element.Remove();

		var next = ActiveElements.Peek();
		next.Visible = true;
		next.Enabled = true;

		backButton.Enabled = backButton.Visible = ActiveElements.Count > 1;

		if (next is IMainMenuPanel nextmmp) {
			nextmmp.OnShown();
			nextmmp.SetRichPresence();
		}


	}

	Panel header;
	public override void Initialize(params object[] args) {
		var charPanel = new Panel(UI);
		charPanel.BorderSize = 0;
		charPanel.DynamicallySized = true;
		charPanel.Size = new(1f, 1f);

		Character = new(charPanel);
		Character.DynamicallySized = true;
		Character.Origin = Anchor.TopCenter;
		Character.Size = new(1f);

		header = new(UI);
		header.Position = new Vector2F(0);
		header.Size = new Vector2F(256, 64);
		header.Dock = Dock.Top;
		header.BorderSize = 0;
		header.BackgroundColor = header.BackgroundColor.Adjust(0, 0, value: 0.5f);

		backButton = MenuButton(header, Dock.Left, "ui/back.png", $"Back", () => {
			PopActiveElement();
		});

		var test2 = new Label(header);
		test2.Size = new Vector2F(158, 32);
		test2.Dock = Dock.Left;
		test2.Text = "Clone Dash";
		test2.TextSize = 30;
		test2.AutoSize = true;
		test2.DockMargin = RectangleF.TLRB(4);

		Keybinds.AddKeybind([ButtonCode.KeyLeftControl, ButtonCode.KeyR], LevelTransitions.LoadMainMenu);

		PushActiveElement(new MainMenuPanel(UI));
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
			Position = new(4 + 6, (int)(header.RenderBounds.H + 4))
		};
	}

	Button MenuButton(Panel header, Dock dock, string icon, string text, Action onClicked) {
		var menuBtn = new Button(header);
		menuBtn.AutoSize = false;
		menuBtn.Size = new Vector2F(64);
		menuBtn.Text = "";
		menuBtn.ImageOrientation = ImageOrientation.Zoom;
		menuBtn.Dock = dock;
		menuBtn.Image = Textures.LoadTextureFromFile(icon);
		menuBtn.ImagePadding = new(4);
		menuBtn.TextSize = 21;
		menuBtn.DockMargin = RectangleF.TLRB(0);
		menuBtn.BorderSize = 0;
		menuBtn.OnButtonClick += (_, _) => onClicked();
		menuBtn.TooltipText = text;

		return menuBtn;
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

		protected override void OnThink(FrameState frameState) {
			base.OnThink(frameState);

			BackgroundColor = BackgroundColor with { A = (byte)(int)Math.Clamp(NMath.Ease.OutCubic(Lifetime * 1.4f) * 155, 0, 155) };

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
			var pos = disc.RenderBounds.Pos;
			Graphics2D.OffsetDrawing(pos);

			disc.Paint(disc.RenderBounds.W, disc.RenderBounds.H);
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
			ImageColor = Element.MixColorBasedOnMouseState(this, new(200, 200, 200,
				(int)(Math.Clamp(NMath.Ease.OutCubic(Lifetime - 0.35f), 0, 1) * 255)
				), new(0, 1, 1.3f, 1), new(0, 1, .7f, 1));
			Position = new((levelSelector.RenderBounds.W / -5) - ((float)NMath.Ease.InCubic(Math.Clamp(1 - (Lifetime - 0.3), 0, 1)) * -64), 0);
			base.Paint(w, h);
		}
	}

	class LevelSelectorTitleLabel(LevelSelectorPanel levelSelector, SongSelector selector) : SongLabel(levelSelector)
	{
		protected override void OnThink(FrameState frameState) {
			base.OnThink(frameState);

			var oldSize = TextSize;
			var w = levelSelector.RenderBounds.W;
			TextSize = (float)Math.Clamp(NMath.Remap(w, 400, 1920, 20, 80), 12, 155);
			if (oldSize != TextSize)
				InvalidateLayout();

			TextColor = new(255, 255, 255, (int)(NMath.Ease.InOutCubic(Math.Clamp(Lifetime * 6, 0, 1)) * 255));
			Position = new(0, (w / -5.2f) - offsetBasedOnLifetime(this, 1.35f, 6));
		}
	}


	class LevelSelectorAuthorLabel(LevelSelectorPanel levelSelector, SongSelector selector) : SongLabel(levelSelector)
	{
		protected override void OnThink(FrameState frameState) {
			base.OnThink(frameState);

			var oldSize = TextSize;
			var w = levelSelector.RenderBounds.W;
			TextSize = (float)Math.Clamp(NMath.Remap(w, 400, 1920, 12, 32), 12, 155);
			if (oldSize != TextSize)
				InvalidateLayout();

			TextColor = new(255, 255, 255, (int)(NMath.Ease.InOutCubic(Math.Clamp(Lifetime * 1.3f, 0, 1)) * 255));
			Position = new(0, (w / -6f) - offsetBasedOnLifetime(this, 1.35f, 12));
		}
	}

	class LevelSelectorDifficultiesPanel(LevelSelectorPanel levelSelector, SongSelector selector, float height) : FlexPanel(levelSelector)
	{
		float height = height;

		protected override void OnThink(FrameState frameState) {
			base.OnThink(frameState);

			Position = new(levelSelector.RenderBounds.W / 4f, 0);
			Size = new(256, height);
		}

		internal void UpdateHeight(int v) {
			height = v;
		}
	}

	class LevelSelectorSelectDifficultyButton(Element parent, string difficultyName, SongChartMetadata metadata) : Button(parent)
	{
		public override void Paint(float w, float h) {
			var life = Lifetime - (offset * .15f);
			var alpha = (float)(NMath.Ease.InOutQuad(Math.Clamp(life * 2.5f, 0, 1)));
			var xOffset = (float)NMath.Ease.InQuart(1 - Math.Clamp(life * 2f, 0, 1)) * -256;

			var a = BackgroundColor.A;
			BackgroundColor = new(BackgroundColor.R, BackgroundColor.G, BackgroundColor.B, (int)(a * alpha));
			ChildRenderOffset = new(xOffset, 0);

			base.Paint(w, h);

			Vector2F textDrawingPosition = Anchor.CenterRight.GetPositionGivenAlignment(RenderBounds.Size, TextPadding);
			Graphics2D.SetDrawColor(TextColor);
			Graphics2D.DrawText(textDrawingPosition + new Vector2F(0, -6), $"{metadata.Difficulty}", Font, TextSize, Anchor.CenterRight);

			BackgroundColor = new(BackgroundColor.R, BackgroundColor.G, BackgroundColor.B, a);
		}

		bool autoplayChart;
		float offset;

		protected override void OnThink(FrameState frameState) {
			base.OnThink(frameState);
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

		LevelSelectorPanel levelSelector = new LevelSelectorPanel(UI, selector);
		SelectedSong = levelSelector;
		levelSelector.MakePopup();
		levelSelector.MakeModal();
		levelSelector.ForegroundColor = Color.Blank;
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
		back.Anchor = Anchor.Center;
		back.Origin = Anchor.Center;
		back.Position = new(-256, 0);
		back.Image = Textures.LoadTextureFromFile("ui/back.png");
		back.OnButtonClick += (_, _) => levelSelector.Remove();
		back.Text = "";
		back.ImageOrientation = ImageOrientation.Centered;
		back.BackgroundColor = new(0, 0);
		back.ForegroundColor = new(0, 0);
		back.Size = new(106);

		LevelSelectorTitleLabel title = new LevelSelectorTitleLabel(levelSelector, selector);
		title.TextSize = 48;
		title.Text = info.Name;
		title.AutoSize = true;
		title.Anchor = Anchor.Center;
		title.Origin = Anchor.Center;

		LevelSelectorAuthorLabel author = new LevelSelectorAuthorLabel(levelSelector, selector);
		author.TextSize = 22;
		author.Text = $"by {info.Author}";
		author.AutoSize = true;
		author.Anchor = Anchor.Center;
		author.Origin = Anchor.Center;

		levelSelector.TrySetupTrack();

		LevelSelectorDifficultiesPanel difficulties = new(levelSelector, selector, 356);
		difficulties.Direction = Directional180.Vertical;
		difficulties.ChildrenResizingMode = FlexChildrenResizingMode.FitToOppositeDirection;
		difficulties.Anchor = Anchor.Center;
		difficulties.Origin = Anchor.Center;

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
		var wasHidden = !Character.Visible;

		if (active is not (CharacterSelector or MainMenuPanel)) {
			Character.Visible = false;
			Character.StopAudio();
			return;
		}

		if (wasHidden) {
			Character.Visible = true;
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

		Character.Position = new(x, 0);
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
		play.Dock = Dock.Bottom;

		SongLabel mapper = new SongLabel(play);
		mapper.AutoSize = true;
		mapper.Text = $"by {designer}";
		mapper.TextSize = 15;
		mapper.TextAlignment = Anchor.BottomCenter;
		mapper.Position = new(-6, -3);
		mapper.Anchor = Anchor.BottomRight;
		mapper.SetPassthru(true);
		mapper.Origin = Anchor.BottomRight;
		mapper.TextAlignment = Anchor.TopLeft;

		play.BackgroundColor = buttonColor;
		play.ForegroundColor = buttonColor.Adjust(hue: 0, saturation: -0.5f, value: -0.4f);
		play.Text = "";
		play.TextAlignment = Anchor.CenterLeft;
		play.TextPadding = new(8, 0);
		play.TextSize = 28;

		play.BorderSize = 2;
		play.SetPaintBackgroundEnabled(false);

		play.OnButtonClick += delegate (Button self, ButtonCode button) {
			play.RunFunction(onClick);
		};

		return play;
	}

	public override void PreRenderBackground(FrameState frameState) {
		base.PreRenderBackground(frameState);
	}

	public Panel? GetSelectedSongPanel() => SelectedSong;
}
