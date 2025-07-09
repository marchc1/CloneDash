using CloneDash.Characters;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Data;
using CloneDash.Game;
using CloneDash.Levels;
using CloneDash.Menu.Searching;

using Nucleus;
using Nucleus.Audio;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Extensions;
using Nucleus.Files;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using Nucleus.UI;

using static CloneDash.Compatibility.CustomAlbums.CustomAlbumsCompatibility;

using MouseButton = Nucleus.Input.MouseButton;

namespace CloneDash.Menu;

public class MainMenuPanel : Panel, IMainMenuPanel
{
	public string GetName() => "Main Menu";
	public void OnHidden() { }
	public void OnShown() {
		music?.Restart();
	}

	ICharacterDescriptor character;
	ModelInstance model;
	AnimationHandler anims;
	MusicTrack? music;

	Stack<List<MainMenuButton>> btns = [];

	public List<MainMenuButton> CreateNavigationMenu() {
		if (btns.TryPeek(out var lastList)) {
			foreach (var listBtn in lastList)
				listBtn.Offscreen = 1;
		}

		List<MainMenuButton> newBtns = [];
		btns.Push(newBtns);
		InvalidateLayout();
		back.Visible = back.Enabled = !UsingRootNavigationMenu;
		return newBtns;
	}
	public bool UsingRootNavigationMenu => btns.Count == 1;
	public void DestroyNavigationMenu() {
		if (UsingRootNavigationMenu) return;
		var toRemove = btns.Pop();
		foreach (var btn in toRemove) {
			btn.Offscreen = -2;
		}
		Level.Timers.Simple(1, () => {
			foreach (var btn in toRemove) {
				btn.Remove();
			}
		});

		var menu = btns.Peek();
		foreach (var btn in menu)
			btn.Offscreen = 0;
		back.Visible = back.Enabled = !UsingRootNavigationMenu;
		InvalidateLayout();
	}

	private MainMenuButton MakeNavigationButton(string text, string icon, string description, float hue, Action<MainMenuLevel>? action = null) {
		MainMenuLevel menu = Level.As<MainMenuLevel>();
		var menuBtns = btns.Peek();

		Add(out MainMenuButton btn);
		btn.BackgroundColor = new System.Numerics.Vector3(hue, 0.3f, 0.1f).ToRGB();
		btn.ForegroundColor = new System.Numerics.Vector3(hue, 0.4f, 0.6f).ToRGB();
		btn.Text = text;
		btn.Image = menu.Textures.LoadTextureFromFile(icon);
		btn.SubText = description;

		btn.MouseReleaseEvent += (_, _, _) => action?.Invoke(menu);
		btn.SetStart((menuBtns.Count + 1) * 24);

		menuBtns.Add(btn);
		return btn;
	}

	Button back;
	public List<ChartSong> RefreshLocalSongs() {
		List<ChartSong> ret = [];

		foreach (var file in Filesystem.FindFiles("charts", "*.mdm", SearchOption.AllDirectories)) {
			ret.Add(new CustomChartsSong("charts", file));
		}

		return ret;
	}
	protected override void Initialize() {
		base.Initialize();
		ICharacterDescriptor? character = CharacterMod.GetCharacterData();
		if (character != null) {
			this.character = character;

			model = character.GetMainShowModel(Level).Instantiate();
			anims = new(model.Data);

			var standby = character.GetMainShowStandby();
			if (model.Data.FindAnimation(standby) == null) standby = "standby";
			if (model.Data.FindAnimation(standby) == null) standby = "Bgmstandby"; // EXCLUSIVELY for miku for whatever reason
			anims.SetAnimation(0, standby, true);

			music = character.GetMainShowMusic(Level);
			if (music != null) {
				music.Playing = true;
				music.Loops = true;
			}
		}

		Add(out back);
		back.Origin = Anchor.Center;
		back.BorderSize = 0;
		back.BackgroundColor = new(0, 0);
		back.Image = Textures.LoadTextureFromFile("ui/back.png");
		back.ImageOrientation = ImageOrientation.Zoom;
		back.Text = "";
		back.MouseReleaseEvent += Back_MouseReleaseEvent;

		CreateNavigationMenu();
		MakeNavigationButton("Play Muse Dash Chart", "ui/play_md_level.png", "Play a Muse Dash chart (if you have Muse Dash installed).", 48, (menu) => {
			var selector = menu.PushActiveElement(UI.Add<SongSelector>());
			selector.AddSongs(MuseDashCompatibility.Songs);
			selector.SearchFilter = new MuseDashSearchFilter();
		});
		MakeNavigationButton("Play Custom Chart", "ui/play_cam_level.png", "Play a custom chart (.mdm format).", 310, (menu) => {
			var selector = menu.PushActiveElement(UI.Add<SongSelector>());
			selector.AddSongs(RefreshLocalSongs());
		});
		MakeNavigationButton("Search mdmc.moe Charts", "ui/webcharts.png", "Find new charts from the Muse Dash Modding Community.", 340, (menu) => {
			var selector = menu.PushActiveElement(UI.Add<SongSelector>());
			selector.InfiniteList = false;
			selector.SearchFilter = new MDMCSearchFilter();
			selector.UserWantsMoreSongs += () => {
				// Load more songs
				(selector.SearchFilter as MDMCSearchFilter).PopulateMDMCCharts(selector);
			};
		});
		MakeNavigationButton("Change Character", "ui/charselect.png", "Select a character from the characters you have installed.", 20);
		MakeNavigationButton("Change Scene", "ui/sceneselect.png", "Select a scene from the scenes you have installed.", 70);
		MakeNavigationButton("Modding Tools", "ui/solder.png", "Various tools for modding the game", 225, ModdingTools_OpenMenuButtons);
		MakeNavigationButton("Options", "ui/pause_settings.png", "Change game settings", 200, (menu) => {
			var settings = menu.PushActiveElement(UI.Add<SettingsEditor>());
			settings.DrawPanelBackground = false;

		});
		MakeNavigationButton("Exit to Desktop", "ui/pause_exit.png", $"Close the application.", 350, (menu) => EngineCore.Close());
	}

	private void Back_MouseReleaseEvent(Element self, FrameState state, MouseButton button) {
		DestroyNavigationMenu();
	}

	private void ModdingTools_OpenMenuButtons(MainMenuLevel menu) {
		CreateNavigationMenu();
		MakeNavigationButton("Scene Editor", "ui/sceneselect.png", "Opens the scene editor & previewer", 160, (menu) => {
			ConCommand.Execute(SceneEditorLevel.sceneedit);
		});
	}

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);

		if (this.btns.TryPeek(out var btns)) {
			var textHeight = height / 20f;
			var btnWidth = Math.Clamp(width / 3f, 460, 155555);
			var btnHeight = height / 12f;
			var btnsLen = btns.Count;
			back.Size = new(btnHeight * 2);
			back.Position = new(width * .5f, height / 2);
			back.Visible = back.Enabled = !UsingRootNavigationMenu;

			for (int i = 0; i < btnsLen; i++) {
				var btn = btns[i];

				btn.Origin = Anchor.Center;
				btn.TextSize = textHeight;
				btn.Size = new(btnWidth, btnHeight);

				var y = btnsLen == 1 ? 0 : (float)NMath.Remap(i, 0, btnsLen - 1, -1, 1);

				btn.Position = new(width * .75f, height / 2 + y * height / 3);
			}
		}
	}

	protected override void OnThink(FrameState frameState) {
		base.OnThink(frameState);
		if (model != null) {
			model.Position = new((1 - (float)NMath.Ease.OutCirc(Math.Clamp(Level.Curtime * 1.5, 0, 1))) * -(frameState.WindowWidth / 2), 0);

			anims?.AddDeltaTime(Level.CurtimeDelta);
			anims?.Apply(model);
		}

		music?.Update();
	}
	ICharacterExpression? touchResponse;
	int click = 0;
	double startExpressionTime;
	double nextExpressionTime;
	string? expressionText;
	public override void MouseClick(FrameState state, MouseButton button) {
		if (character == null) return;
		if (Level.Curtime < nextExpressionTime) return;

		touchResponse = character.GetMainShowExpression();
		click++;

		var mainResponse = character.GetMainShowInitialExpression();
		if (mainResponse != null) {
			anims.SetAnimation(0, mainResponse);
			var standby = character.GetMainShowStandby();
			if (model.Data.FindAnimation(standby) == null) standby = "standby";
			anims.AddAnimation(0, standby, true);
		}

		string? text = null;
		double duration = 0;
		touchResponse?.Run(Level, model, anims, out text, out duration);
		startExpressionTime = Level.Curtime;
		nextExpressionTime = Level.Curtime + duration + 0.1;
		expressionText = text;
	}
	public override void Paint(float width, float height) {
		EngineCore.Window.BeginMode2D(new() {
			Zoom = height / 900 / 2.4f,
			Offset = new(width / 2 - width * .2f, height / 1)
		});

		model?.Render();

		EngineCore.Window.EndMode2D();

		if (NMath.InRange(Level.Curtime, startExpressionTime, nextExpressionTime) && expressionText != null) {
			float alphaMult1 = (float)NMath.Remap(Level.Curtime, startExpressionTime, startExpressionTime + 0.1, 0, 1, true);
			float alphaMult1_2 = (float)NMath.Remap(Level.Curtime, startExpressionTime, startExpressionTime + 0.4, 0, 1, true);
			float alphaMult2 = (float)NMath.Remap(Level.Curtime, nextExpressionTime - 0.2, nextExpressionTime, 0, 1, true);
			float alphaMult = NMath.Ease.InCirc(alphaMult1) - NMath.Ease.OutQuad(alphaMult2);
			string font = "Noto Sans";
			float fontSize = Math.Clamp(24 * (height / 900f), 12, 120);
			Vector2F textSize = Graphics2D.GetTextSize(expressionText, font, fontSize);
			Vector2F textPos = new Vector2F(width / 2 - width * .2f, height * 0.9f) + new Vector2F(0, (float)NMath.Ease.OutBack(alphaMult1_2) * (height * -.05f));
			Graphics2D.SetDrawColor(10, 20, 25, (int)(alphaMult * 200));
			textSize += new Vector2F(16);
			Graphics2D.DrawRectangle(textPos - textSize / 2, textSize);
			Graphics2D.SetDrawColor(255, 255, 255, (int)(alphaMult * 255));
			Graphics2D.DrawText(textPos, expressionText, font, fontSize, Anchor.Center);
		}
	}
}
