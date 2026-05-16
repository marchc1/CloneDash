using CloneDash.Characters;
using CloneDash.Common;
using CloneDash.Common.Songs;
using CloneDash.Common.UI;
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
using System.Numerics;

namespace CloneDash.Game;


[Nucleus.MarkForStaticConstruction]
public class MainMenuLevel : Level, IMainMenuLevel
{
	public static ConCommand hologramtest = new(nameof(hologramtest), (_, in _) => {
		var level = EngineCore.Level;
		var window = level.UI.Add<Window>();
		window.Title = "Hologram Test";
		window.Size = new(600, 600);
		window.Center();

		var refresh = window.Add<Button>();
		refresh.Dock = Dock.Bottom;
		refresh.Size = new(32);
		refresh.Text = "Refresh Shader";


		var renderPanel = window.Add<Panel>();
		renderPanel.Dock = Dock.Fill;
		var charData = CharacterMod.GetCharacterData();
		if (charData == null) return;

		// TODO FIXME decluttering-2 var model = charData.GetPlayModel(level).Instantiate();
		// TODO FIXME decluttering-2 var anims = new AnimationHandler();
		// TODO FIXME decluttering-2 anims.SetModel(model);

		var shader = level.Shaders.LoadFragmentShaderFromFile("shaders", "hologram.fs");
		float time = 0;
		var shaderTimeLoc = shader.GetUniformLocation("time");
		// TODO FIXME decluttering-2 model.SetToSetupPose();
		// TODO FIXME decluttering-2 anims.SetAnimation(0, "air_hit_great_2", false);

		renderPanel.PaintOverride += (s, w, h) => {
			EngineCore.Window.BeginMode2D(new() {
				Zoom = 1f,
				Offset = s.GetGlobalPosition().ToNumerics() + new System.Numerics.Vector2(w / 2, h / 2) + new System.Numerics.Vector2(0, 200)
			});

			// TODO FIXME decluttering-2 anims.AddDeltaTime(EngineCore.Level.RendertimeDelta);
			// TODO FIXME decluttering-2 anims.Apply(model);
			time += (float)EngineCore.Level.RendertimeDelta;
			shader.SetUniform("time", Math.Clamp(NMath.Ease.InCubic(time) * 5f, 0, 1));
			if (shader.IsValid()) {
				shader.Activate();
				// TODO FIXME decluttering-2 model.Render(false);
				shader.Deactivate();
			}

			EngineCore.Window.EndMode2D();
		};

		refresh.MouseReleaseEvent += (_, _, _) => {
			shader.Dispose();
			shader = level.Shaders.LoadFragmentShaderFromFile("shaders", "hologram.fs");
			time = 0;
			shaderTimeLoc = shader.GetUniformLocation("time");
			// TODO FIXME decluttering-2 model.SetToSetupPose();
			// TODO FIXME decluttering-2 anims.SetAnimation(0, "air_hit_great_2", false);
		};

		window.Removed += (s) => {
			shader.Dispose();
		};
	});

	public Stack<Element> ActiveElements = [];
	public CharacterPanel Character = null!;

	private static Vector4 _primaryColor;
	private static Vector4 _backgroundColor;

	private static Vector4 _targetPrimaryColor;
	private static Vector4 _targetBackgroundColor;

	public T PushActiveElement<T>(T element) where T : Element, IMainMenuPanel {
		if (ActiveElements.Count > 0) {
			var last = ActiveElements.Peek();
			last.Visible = false;
			last.Enabled = false;
			if (last is IMainMenuPanel mmp) mmp.OnHidden();
		}

		ActiveElements.Push(element);
		element.SetRichPresence();

		_backButton.Action = ActiveElements.Count > 1 ? PopActiveElement : null;
		
		_targetPrimaryColor = element.GetPrimaryColor().ToVector();
		_targetBackgroundColor = element.GetBackgroundColor().ToVector();
		_headerText.Text = element.GetName();

		if (ActiveElements.Count == 1) {
			_primaryColor = _targetPrimaryColor;
			_backgroundColor = _targetBackgroundColor;
		}

		UpdateAction(element);
		element.Dock = Dock.Fill;
		element.BorderSize = 0;
		return element;
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

		_backButton.Action = ActiveElements.Count > 1 ? PopActiveElement : null;
		UpdateAction(next as IMainMenuPanel);

		if (next is IMainMenuPanel nextmmp) {
			nextmmp.OnShown();
			nextmmp.SetRichPresence();
			_targetPrimaryColor = nextmmp.GetPrimaryColor().ToVector();
			_targetBackgroundColor = nextmmp.GetBackgroundColor().ToVector();
			_headerText.Text = nextmmp.GetName();
		}
	}

	private void UpdateAction(IMainMenuPanel? panel) {
		(Action act, string name, string icon)? fa = panel?.GetFooterAction();
		
		if (fa is null) {
			_actionButton.Action = null;
			return;
		}

		_actionButton.Action = fa.Value.act;
		_actionButton.Text = fa.Value.name;
		_actionButton.Image = Textures.LoadTextureFromFile($"{fa.Value.icon}");
	}

	public override void OnUnload() {
		base.OnUnload();
		MDMCWebAPI.CancelPendingRequests();
	}

	private Panel _header = null!;
	private Label _headerText = null!;

	public Panel Content { get; private set; } = null!;
		
	private Panel _footer = null!;
	private FooterButton _backButton = null!;
	private FooterButton _actionButton = null!;
	
	public override void Initialize(params object[] args) {
		Panel charPanel = UI.Add<Panel>();
		charPanel.BorderSize = 0;
		charPanel.DynamicallySized = true;
		charPanel.Size = new(1f, 1f);
		charPanel.DrawPanelBackground = false;

		Character = charPanel.Add<CharacterPanel>();
		Character.DynamicallySized = true;
		Character.Origin = Anchor.TopCenter;
		Character.Size = new(1f);

		_header = UI.Add<Panel>();
		_header.Size = new Vector2F(256, 64);
		_header.Dock = Dock.Top;
		_header.BorderSize = 0;

		_headerText = _header.Add<Label>();
		_headerText.Dock = Dock.Fill;
		_headerText.Font = CloneDashUI.FontBold;
		_headerText.Text = "Clone Dash";
		_headerText.TextSize = 32 * 1.4f;
		_headerText.AutoSize = true;
		_headerText.DockMargin = RectangleF.TLRB(0, 64, 64, 0);

		Content = UI.Add<Panel>();
		Content.DrawPanelBackground = false;
		Content.BorderSize = 0;
		Content.Dock = Dock.Fill;
		
		_footer = UI.Add<Panel>();
		_footer.Size = new Vector2F(256, 48);
		_footer.Dock = Dock.Bottom;
		_footer.BorderSize = 0;
		_footer.Clipping = false;

		_backButton = UI.Add<FooterButton>();
		_backButton.Text = "Back";
		_backButton.Position = new Vector2F(40, -12);
		_backButton.Anchor = Anchor.BottomLeft;
		_backButton.Origin = Anchor.BottomLeft;
		_backButton.Image = Textures.LoadTextureFromFile("icons/arrow-left.png");
		
		_actionButton = UI.Add<FooterButton>();
		_actionButton.Text = "Back";
		_actionButton.Position = new Vector2F(-40, -12);
		_actionButton.Anchor = Anchor.BottomRight;
		_actionButton.Origin = Anchor.BottomRight;

		Keybinds.AddKeybind([ButtonCode.KeyLeftControl, ButtonCode.KeyR], LevelTransitions.LoadMainMenu);

		PushActiveElement(Content.Add<MainMenuPanel>());
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
			Position = new(4 + 6, (int)(_header.RenderBounds.H + 4))
		};
	}

	private Button MenuButton(Panel panel, Dock dock, string icon, string text, Action onClicked) {
		FooterButton menuBtn = panel.Add<FooterButton>();
		menuBtn.Text = text;
		menuBtn.Dock = dock;
		menuBtn.Image = Textures.LoadTextureFromFile(icon);
		menuBtn.MouseReleaseEvent += (_, _, _) => onClicked();
		return menuBtn;
	}


	private float offsetBasedOnLifetime(Element e, float inf, float heightDiv) =>
		(float)(NMath.Remap(1 - NMath.Ease.OutCubic(e.Lifetime * inf), 0, 1, 0, 1, false, true) * (EngineCore.GetWindowHeight() / heightDiv));

	// At some point, this should just become an element type. This whole thing is a wreck otherwise and injects a bunch of callbacks into
	// random things... I hate it

	public Panel? SelectedSong { get; private set; }

	internal void LoadChartSelector(SongSelector selector, ISong song) {
		// Load all slow-to-get info now before the Window loads
		AudioPlaybackHandle track = selector.ActiveTrack;
		var info = song.FetchMetadata(HumanLanguage.GetCurrentLanguage());

		ConstantLengthNumericalQueue<float> framesOverTime = new(240);

		Panel levelSelector = UI.Add<Panel>();
		SelectedSong = levelSelector;
		levelSelector.MakePopup();
		levelSelector.MakeModal();
		levelSelector.ForegroundColor = Color.Blank;
		levelSelector.Dock = Dock.Fill;
		selector.FlyAway = 1;
		levelSelector.Thinking += (s) => {
			s.BackgroundColor = new(0, 0, 0, (int)Math.Clamp(NMath.Ease.OutCubic(s.Lifetime * 1.4f) * 155, 0, 155));
		};
		// TODO: the opposite of whatever this mess is
		SecondOrderSystem animationSmoother = new SecondOrderSystem(6, 0.98f, 1f, 0);
		float currentAvgVolume = 0;
		Vector2F[] lineBufferL = new Vector2F[framesOverTime.Capacity];
		Vector2F[] lineBufferR = new Vector2F[framesOverTime.Capacity];
		levelSelector.PaintOverride += (s, w, h) => {
			s.Paint(w, h);
			var length = framesOverTime.Capacity;
			for (int i = 0; i < framesOverTime.Capacity; i++) {
				float sample = framesOverTime[framesOverTime.Capacity - 1 - i];
				var xL = (w / 2) + ((i / (float)framesOverTime.Capacity * (-w / 2)));
				var xR = (w / 2) + ((i / (float)framesOverTime.Capacity * (w / 2)));
				var y = (h / 2) + (h * .15f * sample);
				lineBufferL[i] = new(xL, y);
				lineBufferR[i] = new(xR, y);
			}
			Graphics2D.SetDrawColor(50, 50, 50, (int)(Math.Clamp(s.Lifetime * .6f, 0, 1) * 190));
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

			selector.DiscRotateAnimation = s.Lifetime * 90;
		};
		selector.EnterSheetSelection();
		selector.DiscRotateSOS.ResetTo(0);
		levelSelector.Removed += (s) => {
			if (selector != null) {
				selector.ExitSheetSelection();
			}
			SelectedSong = null;
		};

		var back = levelSelector.Add<Button>();

		back.Anchor = Anchor.Center;
		back.Origin = Anchor.Center;
		back.Position = new(-256, 0);
		back.Image = Textures.LoadTextureFromFile("ui/back.png");
		back.MouseReleaseEvent += (_, _, _) => levelSelector.Remove();
		back.Text = "";
		back.ImageOrientation = ImageOrientation.Centered;
		back.BackgroundColor = new(0, 0);
		back.ForegroundColor = new(0, 0);
		back.Size = new(106);

		back.PaintOverride += (self, w, h) => {
			self.ImageColor = Element.MixColorBasedOnMouseState(self, new(200, 200, 200,
				(int)(Math.Clamp(NMath.Ease.OutCubic(self.Lifetime - 0.35f), 0, 1) * 255)
				), new(0, 1, 1.3f, 1), new(0, 1, .7f, 1));
			self.Position = new((levelSelector.RenderBounds.W / -5) - ((float)NMath.Ease.InCubic(Math.Clamp(1 - (self.Lifetime - 0.3), 0, 1)) * -64), 0);
			self.Paint(w, h);
		};

		SongLabel title = levelSelector.Add<SongLabel>();
		title.TextSize = 48;
		title.Text = info.Name;
		title.AutoSize = true;
		title.Anchor = Anchor.Center;
		title.Origin = Anchor.Center;

		title.Thinking += (s) => {
			var oldSize = s.TextSize;
			var w = levelSelector.RenderBounds.W;
			s.TextSize = (float)Math.Clamp(NMath.Remap(w, 400, 1920, 20, 80), 12, 155);
			if (oldSize != s.TextSize)
				s.InvalidateLayout();

			s.TextColor = new(255, 255, 255, (int)(NMath.Ease.InOutCubic(Math.Clamp(s.Lifetime * 6, 0, 1)) * 255));
			s.Position = new(0, (w / -5.2f) - offsetBasedOnLifetime(s, 1.35f, 6));
		};

		SongLabel author = levelSelector.Add<SongLabel>();
		author.TextSize = 22;
		author.Text = $"by {info.Author}";
		author.AutoSize = true;
		author.Anchor = Anchor.Center;
		author.Origin = Anchor.Center;

		bool setupTrack = track.IsValid();
		author.Thinking += (s) => {
			var oldSize = s.TextSize;
			var w = levelSelector.RenderBounds.W;
			s.TextSize = (float)Math.Clamp(NMath.Remap(w, 400, 1920, 12, 32), 12, 155);
			if (oldSize != s.TextSize)
				s.InvalidateLayout();

			s.TextColor = new(255, 255, 255, (int)(NMath.Ease.InOutCubic(Math.Clamp(s.Lifetime * 1.3f, 0, 1)) * 255));
			s.Position = new(0, (w / -6f) - offsetBasedOnLifetime(s, 1.35f, 12));

			if (!setupTrack) {
				track = selector.ActiveTrack;
				if (track.IsValid()) {
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
		};
		if (track.IsValid())
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

		var difficulties = levelSelector.Add<FlexPanel>();
		difficulties.Direction = Directional180.Vertical;
		difficulties.ChildrenResizingMode = FlexChildrenResizingMode.FitToOppositeDirection;
		difficulties.Anchor = Anchor.Center;
		difficulties.Origin = Anchor.Center;
		int height = 356;
		difficulties.Thinking += (s) => {
			s.Position = new(levelSelector.RenderBounds.W / 4f, 0);
			s.Size = new(256, height);
		};

		List<Button> btns = [];
		foreach (var chart in song.GetCharts()) {
			var chartInfo = chart.FetchMetadata(HumanLanguage.GetCurrentLanguage());
			var b = CreateDifficulty(difficulties, chart, in chartInfo);
			if (b != null)
				btns.Add(b);
		}

		height = btns.Count * 80;
		float offsetButtonSlide = 2f;
		for (int i = 0; i < btns.Count; i++) {
			var btn = btns[i];
			if (btn == null) continue;
			var thisOffset = offsetButtonSlide;

			btn.PaintOverride += (s, w, h) => {
				var life = s.Lifetime - (thisOffset * .15f);
				var alpha = (float)(NMath.Ease.InOutQuad(Math.Clamp(life * 2.5f, 0, 1)));
				var xOffset = (float)NMath.Ease.InQuart(1 - Math.Clamp(life * 2f, 0, 1)) * -256;

				var a = s.BackgroundColor.A;
				s.BackgroundColor = new(s.BackgroundColor.R, s.BackgroundColor.G, s.BackgroundColor.B, (int)(a * alpha));
				s.ChildRenderOffset = new(xOffset, 0);
				s.Paint(w, h);

				s.BackgroundColor = new(s.BackgroundColor.R, s.BackgroundColor.G, s.BackgroundColor.B, a);
			};

			offsetButtonSlide += 1;
		}
	}

	private static Button? CreateDifficulty(
		FlexPanel levelSelector, ISongChart chart, in SongChartMetadata metadata
	)
		=> CreateDifficulty(levelSelector, (state) => {
			levelSelector.Level.As<MainMenuLevel>().LoadChartSheetLevel(chart, state.Keyboard.AltDown);
		}, metadata);


	public void LoadChartSheetLevel(ISongChart chart, bool autoplay) {
		LevelTransitions.LoadSongChart($"Loading '{chart.GetSong().FetchMetadata(HumanLanguage.GetCurrentLanguage()).Name}'...", chart, new(){
			Autoplay = autoplay
		});
	}

	public override void Think(FrameState frameState) {
		base.Think(frameState);

		var active = ActiveElements.Peek();
		var wasHidden = !Character.Visible;

		if (active is not (CharacterSelector or MainMenuPanel))
		{
			Character.Visible = false;
			Character.StopAudio();
			return;
		}

		if (wasHidden)
		{
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

	private static Button? CreateDifficulty(FlexPanel levelSelector, Action<FrameState> onClick, SongChartMetadata metadata) {
		var difficultyName = metadata.DifficultyName;
		var buttonColor = metadata.Color;
		var designer = metadata.ChartAuthors;

		if (metadata.Difficulty == "") return null;
		if (metadata.Difficulty == "0") return null;

		Button play = levelSelector.Add<Button>();
		play.Size = new(64);
		play.Dock = Dock.Bottom;

		SongLabel mapper = play.Add<SongLabel>();
		mapper.AutoSize = true;
		mapper.Text = $"by {designer}";
		mapper.TextSize = 15;
		mapper.TextAlignment = Anchor.BottomCenter;
		mapper.Position = new(-6, -3);
		mapper.Anchor = Anchor.BottomRight;
		mapper.OnHoverTest += Element.Passthru;
		mapper.Origin = Anchor.BottomRight;
		mapper.TextAlignment = Anchor.TopLeft;


		play.BackgroundColor = buttonColor;
		play.ForegroundColor = buttonColor.Adjust(hue: 0, saturation: -0.5f, value: -0.4f);
		play.Text = "";
		play.TextAlignment = Anchor.CenterLeft;
		play.TextPadding = new(8, 0);
		play.TextSize = 28;


		play.BorderSize = 2;
		play.PaintOverride += delegate (Element self, float w, float h) {
			if (self is not Button btn) return; // make nullable happy, it will always be Button
			btn.Paint(w, h);

			Vector2F textDrawingPosition = Anchor.CenterRight.GetPositionGivenAlignment(btn.RenderBounds.Size, btn.TextPadding);
			Graphics2D.SetDrawColor(btn.TextColor);
			Graphics2D.DrawText(textDrawingPosition + new Vector2F(0, -6), $"{metadata.Difficulty}", btn.Font, btn.TextSize, Anchor.CenterRight);
		};

		play.Thinking += delegate (Element self) {
			if (EngineCore.Level.FrameState.Keyboard.AltDown) {
				play.Text = $"[AUTOPLAY] {difficultyName.ToUpper()}";
			}
			else {
				play.Text = $"{difficultyName.ToUpper()}";
			}
		};

		play.MouseReleaseEvent += delegate (Element self, FrameState state, ButtonCode button) {
			onClick(state);
		};

		return play;
	}

	public override void PreRenderBackground(FrameState frameState) {
		base.PreRenderBackground(frameState);
	}

	public Panel? GetSelectedSongPanel() => SelectedSong;
	
	private static List<BackgroundShape> shapes = new();
	private const int MaxShapes = 32;

	public override void Render(FrameState frameState) {
		_primaryColor = TransitionColor(_primaryColor, _targetPrimaryColor);
		_backgroundColor = TransitionColor(_backgroundColor, _targetBackgroundColor);

		Rlgl.EnableDepthTest();

		Color bg = _backgroundColor.ToColor();
		Graphics2D.SetDrawColor(bg);
		Graphics2D.DrawRectangle(Vector2F.Zero - frameState.WindowSize / 2f, frameState.WindowSize);
		_actionButton.BackgroundColor = _backButton.BackgroundColor = _headerText.TextColor = bg;

		bool first = shapes.Count == 0;

		if (shapes.Count < MaxShapes) {
			for (int i = shapes.Count; i < MaxShapes; i++) {
				shapes.Add(CreateRandomShape(frameState.WindowWidth, frameState.WindowHeight, first));
			}
		}

		Color primary = _primaryColor.ToColor();
		_actionButton.ForegroundColor = _backButton.ForegroundColor = _footer.BackgroundColor = _header.BackgroundColor = primary;
		
		Color shapesColor = primary;
		shapesColor.A = 80;
		Graphics2D.SetDrawColor(shapesColor);

		Graphics2D.DrawRectangle(frameState.WindowSize / 2f, new Vector2F(200));

		for (int i = 0; i < shapes.Count; i++) {
			BackgroundShape shape = shapes[i];
			float movement = (float)RendertimeDelta * shape.Size;
			shape.Position += new Vector2F(movement * 80, movement * -80);
			shape.Rotation += 10 * movement;

			Vector2F pos = shape.Position - frameState.WindowSize / 2f;
			Vector2F size = new(200 * shape.Size);

			switch (shape.Type) {
				case ShapeType.Square:
					Graphics2D.DrawRectangle(RectangleF.XYWH(pos.X, pos.Y, size.X, size.Y), Vector2F.Zero / 2f,
						shape.Rotation);
					break;
				case ShapeType.Circle:
					Graphics2D.DrawCircle(pos, size);
					break;
				/*case ShapeType.Triangle:
					Graphics2D.DrawRectangle(pos, size * 50);
					break;*/
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
			Type = (ShapeType)Random.Shared.Next(2),
			Rotation = Random.Shared.NextSingle() * 360
		};
	}

	private class BackgroundShape
	{
		public Vector2F Position { get; set; }
		public float Size { get; set; } = 1f;
		public float Rotation { get; set; } = 0f;
		public ShapeType Type { get; set; }
	}

	private enum ShapeType
	{
		Square,
		Circle,
		Triangle
	}
}
