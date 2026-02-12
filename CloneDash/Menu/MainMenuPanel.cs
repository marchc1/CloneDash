using CloneDash.Characters;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Data;
using CloneDash.Game;
using CloneDash.Levels;
using CloneDash.Menu.Searching;
using CloneDash.Settings;
using CloneDash.Systems;
using Nucleus;
using Nucleus.Audio;
using Nucleus.Commands;
using Nucleus.Common.Input;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Extensions;
using Nucleus.Files;
using Nucleus.Input;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using Nucleus.UI;

using static CloneDash.Compatibility.CustomAlbums.CustomAlbumsCompatibility;



namespace CloneDash.Menu;

public class MainMenuPanel : Panel, IMainMenuPanel
{
	public void SetRichPresence() {
		RichPresenceSystem.SetPresence(new() {
			Details = "Main Menu",
			State = "Idle"
		});
	}
	public string GetName() => "Main Menu";
	public void OnHidden() { }
	public void OnShown() {
		Char.Reset();
	}

	CharacterPanel Char = null!;

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

		foreach (var file in filesystem.FindFiles("charts", "*.mdm", SearchOption.AllDirectories)) {
			try {
				ret.Add(new CustomChartsSong("charts", file));
			}
			catch (Exception ex) {
				Logs.Warn($"The .mdm file '{file}' failed: {ex.Message}");
			}
		}

		return ret;
	}
	public override void OnRemoval() {
		base.OnRemoval();
	}
	protected override void Initialize() {
		base.Initialize();

		BorderSize = 0;
		DrawPanelBackground = false;

		Add(out Char);
		Char.Dock = Dock.Left;
		Char.DynamicallySized = true;
		Char.Size = new(0.6f, 1f);
		Char.LinkToConVar = true;

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
			selector.InCustomCharts = true;
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
		MakeNavigationButton("Change Character", "ui/charselect.png", "Select a character from the characters you have installed.", 20, (menu) => {
			var selector = menu.PushActiveElement(UI.Add<CharacterSelector>());
		});
		MakeNavigationButton("Change Scene", "ui/sceneselect.png", "Select a scene from the scenes you have installed.", 70);
		MakeNavigationButton("Modding Tools", "ui/solder.png", "Various tools for modding the game", 225, ModdingTools_OpenMenuButtons);
		MakeNavigationButton("Options", "ui/pause_settings.png", "Change game settings", 200, (menu) => {
			var settings = menu.PushActiveElement(UI.Add<SettingsEditor>());
			settings.DrawPanelBackground = false;
		});
		MakeNavigationButton("Exit to Desktop", "ui/pause_exit.png", $"Close the application.", 350, (menu) => EngineCore.Close());
	}


	protected override void OnThink(FrameState frameState) {
		base.OnThink(frameState);
		Char.CharacterOffset = new((1 - (float)NMath.Ease.OutCirc(Math.Clamp(Level.Curtime * 1.5, 0, 1))) * -(Level.FrameState.WindowWidth / 2), 0);
	}
	private void Back_MouseReleaseEvent(Element self, FrameState state, ButtonCode button) {
		DestroyNavigationMenu();
	}

	private void ModdingTools_OpenMenuButtons(MainMenuLevel menu) {
		CreateNavigationMenu();
		MakeNavigationButton("Scene Editor", "ui/sceneselect.png", "Opens the scene editor & previewer", 160, (menu) => {
			// TODO: need engine interface. ConCommand.Execute(SceneEditorLevel.sceneedit);
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
}
