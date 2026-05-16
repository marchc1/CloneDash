using CloneDash.Characters;
using CloneDash.Charts;
using CloneDash.Common.Songs;
using CloneDash.Common.UI;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Game;
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
using System.Numerics;
using static CloneDash.CustomAlbumsCompatibility.CustomAlbums.CustomAlbumsCompatibility;



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
		// Char.Reset();
	}

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
		btn.BackgroundColor = CloneDashUI.AccentBackground;
		btn.ForegroundColor = new Vector3(hue, 0.33f, 1f).HSVfToRGBub();
		btn.Text = text;
		btn.Image = menu.Textures.LoadTextureFromFile(icon);
		btn.SubText = description;

		btn.MouseReleaseEvent += (_, _, _) => action?.Invoke(menu);
		btn.SetStart((menuBtns.Count + 1) * 24);

		menuBtns.Add(btn);
		return btn;
	}

	Button back;
	public List<ISong> RefreshLocalSongs() {
		List<ISong> ret = [];

		foreach (var file in filesystem.FindFiles("charts", "*.mdm", SearchOption.AllDirectories)) {
			try {
				ret.Add(new MD1_CustomChartsSong("charts", file));
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

		OnHoverTest += Element.Passthru;

		Add(out back);
		back.Origin = Anchor.Center;
		back.BorderSize = 0;
		back.BackgroundColor = new(0, 0);
		back.Image = Textures.LoadTextureFromFile("ui/back.png");
		back.ImageOrientation = ImageOrientation.Zoom;
		back.Text = "";
		back.MouseReleaseEvent += Back_MouseReleaseEvent;
		CreateNavigationMenu();
		MakeNavigationButton("Play Muse Dash Chart", "icons/play.png",
			"Play your installed charts.", 200, (menu) => {
				var source = ChartMod.GetChartSongProviderByName("Muse Dash");
				if (source == null) {
					UI.DialogOK("Source Error", "The source from ChartMod.GetChartSongProviderByName returned null.");
					return;
				}

				var selector = menu.PushActiveElement(menu.Content.Add<SongSelector>());
				selector.SetSource(source.NewState());
			});
		MakeNavigationButton("Play Custom Chart", "icons/orange-slice.png", "Play a custom chart (.mdm format).", 310,
			(menu) => {
				var source = ChartMod.GetChartSongProviderByName("Custom Albums");
				if (source == null) {
					UI.DialogOK("Source Error", "The source from ChartMod.GetChartSongProviderByName returned null.");
					return;
				}

				var selector = menu.PushActiveElement(menu.Content.Add<SongSelector>());
				selector.SetSource(source.NewState());
			});
		MakeNavigationButton("Search mdmc.moe Charts", "icons/globe-hemisphere-west.png",
			"Download new charts from the Muse Dash Modding Community.", 340, (menu) => {
				var source = ChartMod.GetChartSongProviderByName("MDMC");
				if (source == null) {
					UI.DialogOK("Source Error", "The source from ChartMod.GetChartSongProviderByName returned null.");
					return;
				}

				var selector = menu.PushActiveElement(menu.Content.Add<SongSelector>());
				selector.SetSource(source.NewState());
			});
		MakeNavigationButton("Change Character", "icons/person-simple-run.png",
			"Select a character to play as.", 20, (menu) => {
				var selector = menu.PushActiveElement(menu.Content.Add<CharacterSelector>());
			});
		MakeNavigationButton("Modding Tools", "icons/wrench.png", "Various tools for modding the game.", 225,
			ModdingTools_OpenMenuButtons);
		MakeNavigationButton("Options", "icons/gear-six.png", "Change the game's settings.", 47, (menu) => {
			var settings = menu.PushActiveElement(menu.Content.Add<SettingsEditor>());
			settings.DrawPanelBackground = false;
		});
		MakeNavigationButton("Exit to Desktop", "icons/door-open.png", $"Close the application.", 350, (menu) => EngineCore.Close());
	}


	protected override void OnThink(FrameState frameState) {
		base.OnThink(frameState);
		// Char.CharacterOffset = new((1 - (float)NMath.Ease.OutCirc(Math.Clamp(Level.Curtime * 1.5, 0, 1))) * -(Level.FrameState.WindowWidth / 2), 0);
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
		
		if (!btns.TryPeek(out List<MainMenuButton>? currentButtons))
			return;
		
		int buttonCount = currentButtons.Count;
		
		// back.Size = new Vector2F(btnHeight * 2);
		// back.Position = new Vector2F(width * .5f, height / 2);
		// back.Visible = back.Enabled = !UsingRootNavigationMenu;

		for (int i = 0; i < buttonCount; i++) {
			MainMenuButton btn = currentButtons[i];
			btn.Origin = Anchor.CenterRight;

			float y = buttonCount == 1 ? 0 : NMath.Remap(i, 0, buttonCount - 1, -1, 1);
			btn.Position = new Vector2F(width - 96, height / 2 + y * height / 3);
		}
	}
}
