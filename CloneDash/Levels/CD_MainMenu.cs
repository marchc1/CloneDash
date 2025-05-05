using Nucleus;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using Raylib_cs;
using MouseButton = Nucleus.Input.MouseButton;
using CloneDash.Data;
using CloneDash.Animation;
using Nucleus.Audio;
using static CloneDash.CustomAlbumsCompatibility;
using CloneDash.Systems.CustomAlbums;
using Nucleus.Models.Runtime;
using CloneDash.Modding.Settings;
using System.Diagnostics;
using CloneDash.UI;
using Nucleus.Files;
using Nucleus.Extensions;
using Nucleus.Input;

namespace CloneDash.Game;

public interface IMainMenuPanel
{
	public string GetName();
	public void OnHidden();
	public void OnShown();
}


[Nucleus.MarkForStaticConstruction]
public class CD_MainMenu : Level
{
	public static ConCommand clonedash_hologramtest = ConCommand.Register("clonedash_hologramtest", (_, _) => {
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


		var model = level.Models.CreateInstanceFromFile("chars", $"{charData.Filename}/{charData.GetPlayModel()}");
		var anims = new AnimationHandler(model);

		var shader = Filesystem.ReadFragmentShader("shaders", "hologram.fs");
		float time = 0;
		var shaderTimeLoc = shader.GetShaderLocation("time");
		model.SetToSetupPose();
		anims.SetAnimation(0, "air_hit_great_2", false);

		renderPanel.PaintOverride += (s, w, h) => {
			EngineCore.Window.BeginMode2D(new() {
				Zoom = 1f,
				Offset = s.GetGlobalPosition().ToNumerics() + new System.Numerics.Vector2(w / 2, h / 2) + new System.Numerics.Vector2(0, 200)
			});

			anims.AddDeltaTime(EngineCore.Level.CurtimeDelta);
			anims.Apply(model);
			time += EngineCore.Level.CurtimeDeltaF;
			shader.SetShaderValue("time", Math.Clamp(NMath.Ease.InCubic(time) * 5f, 0, 1));
			if (Raylib.IsShaderReady(shader)) {
				Raylib.BeginShaderMode(shader);
				model.Render();
				Raylib.EndShaderMode();
			}

			EngineCore.Window.EndMode2D();
		};

		refresh.MouseReleaseEvent += (_, _, _) => {
			Raylib.UnloadShader(shader);
			shader = Filesystem.ReadFragmentShader("shaders", "hologram.fs");
			time = 0;
			shaderTimeLoc = shader.GetShaderLocation("time");
			model.SetToSetupPose();
			anims.SetAnimation(0, "air_hit_great_2", false);
		};

		window.Removed += (s) => {
			Raylib.UnloadShader(shader);
		};
	});

	public static ConCommand clonedash_scenetest = ConCommand.Register("clonedash_scenetest", (_, _) => {
		var level = EngineCore.Level;
		var window = level.UI.Add<Window>();
		window.Title = "Hologram Test";
		window.Size = new(600, 600);
		window.Center();

		var refresh = window.Add<Button>();
		refresh.Dock = Dock.Bottom;
		refresh.Size = new(32);
		refresh.Text = "Refresh";

		var name = window.Add<Textbox>();
		name.Dock = Dock.Top;

		var renderPanel = window.Add<Panel>();
		renderPanel.Dock = Dock.Fill;
		var sceneData = SceneMod.GetSceneData();

		ModelInstance? model = null;
		AnimationHandler? anims = null;

		renderPanel.PaintOverride += (s, w, h) => {
			EngineCore.Window.BeginMode2D(new() {
				Zoom = 1f,
				Offset = s.GetGlobalPosition().ToNumerics() + new System.Numerics.Vector2(w / 2, h / 2) + new System.Numerics.Vector2(0, 200)
			});

			if (model != null && anims != null) {
				anims.AddDeltaTime(EngineCore.Level.CurtimeDelta);
				anims.Apply(model);
				model.Render();
			}
			EngineCore.Window.EndMode2D();
		};

		refresh.MouseReleaseEvent += (_, _, _) => {
			try {
				model = level.Models.CreateInstanceFromFile("scenes", $"{sceneData.Filename}/{name.Text}.nm4rj");
				anims = new AnimationHandler(model);

				model.SetToSetupPose();
				anims.SetAnimation(0, "air_hit_great_2", false);
			}
			catch (Exception ex) {
				Debug.Assert(false, ex.Message);
			}
		};
	});

	public Stack<Element> ActiveElements = [];

	public T PushActiveElement<T>(T element) where T : Element, IMainMenuPanel {
		if (ActiveElements.Count > 0) {
			var last = ActiveElements.Peek();
			last.Visible = false;
			last.Enabled = false;
			if (last is IMainMenuPanel mmp) mmp.OnHidden();
		}

		ActiveElements.Push(element);

		backButton.Enabled = backButton.Visible = ActiveElements.Count > 1;

		element.Dock = Dock.Fill;
		return element;
	}

	private Button backButton;

	public Element PopActiveElement() {
		if (ActiveElements.Count <= 1) return ActiveElements.Peek();
		var element = ActiveElements.Pop();
		element.Remove();

		var next = ActiveElements.Peek();
		next.Visible = true;
		next.Enabled = true;

		backButton.Enabled = backButton.Visible = ActiveElements.Count > 1;

		if (next is IMainMenuPanel mmp) mmp.OnShown();

		return next;
	}

	Panel header;
	public override void Initialize(params object[] args) {
		header = UI.Add<Panel>();
		header.Position = new Vector2F(0);
		header.Size = new Vector2F(256, 64);
		header.Dock = Dock.Top;

		backButton = MenuButton(header, Dock.Left, "ui/back.png", $"Back", () => {
			PopActiveElement();
		});

		var test2 = header.Add<Label>();
		test2.Size = new Vector2F(158, 32);
		test2.Dock = Dock.Left;
		test2.Text = "Clone Dash";
		test2.TextSize = 30;
		test2.AutoSize = true;
		test2.DockMargin = RectangleF.TLRB(4);

		Keybinds.AddKeybind([KeyboardLayout.USA.LeftControl, KeyboardLayout.USA.R], () => EngineCore.LoadLevel(new CD_MainMenu()));

		PushActiveElement(UI.Add<MainMenuPanel>());
		ConsoleSystem.AddScreenBlocker(UI);
	}

	public override void PostRender(FrameState frameState) {
		base.PostRender(frameState);

		if (!EngineCore.ShowConsoleLogsInCorner || !CommandLineArguments.IsParamTrue("debug"))
			return;

		ConsoleSystem.TextSize = 11;
		ConsoleSystem.RenderToScreen(4 + 6, (int)(header.RenderBounds.H + 4));
	}

	Button MenuButton(Panel header, Dock dock, string icon, string text, Action onClicked) {
		var menuBtn = header.Add<Button>();
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
		menuBtn.MouseReleaseEvent += (_, _, _) => onClicked();
		menuBtn.TooltipText = text;

		return menuBtn;
	}


	private float offsetBasedOnLifetime(Element e, float inf, float heightDiv) =>
		(float)(NMath.Remap(1 - NMath.Ease.OutCubic(e.Lifetime * inf), 0, 1, 0, 1, false, true) * (EngineCore.GetWindowHeight() / heightDiv));

	// At some point, this should just become an element type. This whole thing is a wreck otherwise and injects a bunch of callbacks into
	// random things... I hate it

	internal void LoadChartSelector(SongSelector selector, ChartSong song) {
		// Load all slow-to-get info now before the Window loads
		MusicTrack? track = selector.ActiveTrack;
		var info = song.GetInfo();
		var cover = song.GetCover();

		ConstantLengthNumericalQueue<float> framesOverTime = new(240);

		Panel levelSelector = UI.Add<Panel>();
		levelSelector.MakePopup();
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
			Graphics2D.SetDrawColor(50, 50, 50, (int)(Math.Clamp(s.Lifetime * .6f, 0, 1) * 140));
			Graphics2D.DrawLineStrip(lineBufferL);
			Graphics2D.DrawLineStrip(lineBufferR);

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
			self.Position = new((levelSelector.RenderBounds.W / -5) - (NMath.Ease.InCubic(Math.Clamp(1 - (self.Lifetime - 0.3f), 0, 1)) * -64), 0);
			self.Paint(w, h);
		};

		var title = levelSelector.Add<Label>();
		title.TextSize = 48;
		title.Text = song.Name;
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

		var author = levelSelector.Add<Label>();
		author.TextSize = 22;
		author.Text = $"by {song.Author}";
		author.AutoSize = true;
		author.Anchor = Anchor.Center;
		author.Origin = Anchor.Center;

		bool setupTrack = track != null;
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
				if (track != null) {
					setupTrack = true;
					track.Processing += (self, frames) => {
						currentAvgVolume = 0;
						for (int i = 0; i < frames.Length; i++) {
							float val = frames[i];
							currentAvgVolume += val;
							if (i % 256 == 0)
								framesOverTime.Add(val);
						}
						currentAvgVolume /= frames.Length;
						currentAvgVolume = Math.Clamp(NMath.Ease.InQuad(MathF.Abs(currentAvgVolume) * 1.5f), 0, 1.5f);
					};
				}
			}
		};
		if (track != null)
			track.Processing += (self, frames) => {
				currentAvgVolume = 0;
				for (int i = 0; i < frames.Length; i++) {
					float val = frames[i];
					currentAvgVolume += val;
					if (i % 256 == 0)
						framesOverTime.Add(val);
				}
				currentAvgVolume /= frames.Length;
				currentAvgVolume = Math.Clamp(NMath.Ease.InQuad(MathF.Abs(currentAvgVolume) * 1.5f), 0, 1.5f);
			};

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

		var d1 = CreateDifficulty(difficulties, song, MuseDashDifficulty.Easy, song.Difficulty1);
		var d2 = CreateDifficulty(difficulties, song, MuseDashDifficulty.Hard, song.Difficulty2);
		var d3 = CreateDifficulty(difficulties, song, MuseDashDifficulty.Master, song.Difficulty3);
		var d4 = CreateDifficulty(difficulties, song, MuseDashDifficulty.Hidden, song.Difficulty4);
		var d5 = CreateDifficulty(difficulties, song, MuseDashDifficulty.Touhou, song.Difficulty5);

		height = (d1 == null ? 0 : 80) + (d2 == null ? 0 : 80) + (d3 == null ? 0 : 80) + (d4 == null ? 0 : 80) + (d5 == null ? 0 : 80);
		float offsetButtonSlide = 2f;
		var btns = new Button[] { d1, d2, d3, d4, d5 };
		for (int i = 0; i < btns.Length; i++) {
			var btn = btns[i];
			if (btn == null) continue;
			var thisOffset = offsetButtonSlide;

			btn.PaintOverride += (s, w, h) => {
				var life = s.Lifetime - (thisOffset * .15f);
				var alpha = (float)(NMath.Ease.InOutQuad(Math.Clamp(life * 2.5f, 0, 1)));
				var xOffset = NMath.Ease.InQuart(1 - Math.Clamp(life * 2f, 0, 1)) * -256;

				var a = s.BackgroundColor.A;
				s.BackgroundColor = new(s.BackgroundColor.R, s.BackgroundColor.G, s.BackgroundColor.B, (int)(a * alpha));
				s.ChildRenderOffset = new(xOffset, 0);
				s.Paint(w, h);

				s.BackgroundColor = new(s.BackgroundColor.R, s.BackgroundColor.G, s.BackgroundColor.B, a);
			};

			offsetButtonSlide += 1;
		}
	}

	private static Button? CreateDifficulty(FlexPanel levelSelector, ChartSong song, MuseDashDifficulty difficulty, string difficultyLevel)
		=> CreateDifficulty(levelSelector, (mapID, state) => {
			levelSelector.Level.As<CD_MainMenu>().LoadChartSheetLevel(song, mapID, state.Keyboard.AltDown);
		}, difficulty, song.GetInfo()?.Designer((int)difficulty - 1) ?? "", difficultyLevel);


	private CD_GameLevel? workingLevel;

	public void LoadChartSheetLevel(ChartSong song, int mapID, bool autoplay) {
		if (workingLevel != null) return;

		workingLevel = CD_GameLevel.LoadLevel(song, mapID, autoplay);
	}

	public override void Think(FrameState frameState) {
		base.Think(frameState);
	}

	private static Button? CreateDifficulty(FlexPanel levelSelector, Action<int, FrameState> onClick, MuseDashDifficulty difficulty, string designer, string difficultyLevel) {
		if (difficultyLevel == "") return null;
		if (difficultyLevel == "0") return null;

		Button play = levelSelector.Add<Button>();
		play.Size = new(64);
		play.Dock = Dock.Bottom;

		var difficultyName = difficulty switch {
			MuseDashDifficulty.Easy => "Easy",
			MuseDashDifficulty.Hard => "Hard",
			MuseDashDifficulty.Master => "Master",
			MuseDashDifficulty.Hidden => "Hidden",
			MuseDashDifficulty.Touhou => "Touhou",
			_ => throw new Exception($"Unsupported difficulty level '{difficulty}'")
		};
		Color buttonColor = difficulty switch {
			MuseDashDifficulty.Easy => new Color(88, 199, 76, 60),
			MuseDashDifficulty.Hard => new Color(109, 196, 199, 60),
			MuseDashDifficulty.Master => new Color(188, 95, 184, 60),
			MuseDashDifficulty.Hidden => new Color(199, 35, 35, 60),
			MuseDashDifficulty.Touhou => new Color(109, 103, 194, 60),
			_ => play.BackgroundColor
		};
		int mapID = (int)difficulty;


		Label mapper = play.Add<Label>();
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
			Graphics2D.DrawText(textDrawingPosition + new Vector2F(0, -6), $"{difficultyLevel}", btn.Font, btn.TextSize, Anchor.CenterRight);
		};

		play.Thinking += delegate (Element self) {
			if (EngineCore.CurrentFrameState.Keyboard.AltDown) {
				play.Text = $"[AUTOPLAY] {difficultyName.ToUpper()}";
			}
			else {
				play.Text = $"{difficultyName.ToUpper()}";
			}
		};

		play.MouseReleaseEvent += delegate (Element self, FrameState state, MouseButton button) {
			onClick(mapID, state);
		};

		return play;
	}

	public override void PreRenderBackground(FrameState frameState) {
		base.PreRenderBackground(frameState);
	}
}