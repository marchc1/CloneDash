using CloneDash.Charts;
using CloneDash.Common.Songs;
using CloneDash.Game;
using CloneDash.Menu.Character;
using CloneDash.Menu.Searching;
using CloneDash.Systems;
using Nucleus;
using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Extensions;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using System.Numerics;
using static CloneDash.CustomAlbumsCompatibility.CustomAlbums.CustomAlbumsCompatibility;

namespace CloneDash.Menu.Main;

public class MainMenuPanel : Panel, IMainMenuPanel
{
	private bool UsingRootNavigationMenu => buttons.Count == 1;
	private readonly Stack<List<MainMenuButton>> buttons = [];
	
	public void SetRichPresence() {
		RichPresenceSystem.SetPresence(new() {
			Details = "Main Menu",
			State = "Idle"
		});
	}
	public string GetName() => "Main Menu";

	public void CreateNavigationMenu() {
		if (buttons.TryPeek(out List<MainMenuButton>? lastList)) {
			foreach (MainMenuButton listBtn in lastList)
				listBtn.Offscreen = 1;
		}

		List<MainMenuButton> newBtns = [];
		buttons.Push(newBtns);
		InvalidateLayout();
		back.SetVisible(!UsingRootNavigationMenu);
	}
	
	public void DestroyNavigationMenu() {
		if (UsingRootNavigationMenu) return;
		var toRemove = buttons.Pop();
		foreach (var btn in toRemove) {
			btn.Offscreen = -2;
		}
		Level.Timers.Simple(1, () => {
			foreach (var btn in toRemove) {
				btn.Remove();
			}
		});

		var menu = buttons.Peek();
		foreach (var btn in menu)
			btn.Offscreen = 0;
		back.SetVisible(!UsingRootNavigationMenu);
		InvalidateLayout();
	}

	private void MakeNavigationButton(string text,string description, float hue, string icon, Action<MainMenuLevel>? action = null) {
		MainMenuLevel menu = Level.As<MainMenuLevel>();
		var menuBtns = buttons.Peek();

		MainMenuButton btn = new(this, text, description, icon);
		btn.SetBgColor(this.GetBackgroundColor(GetScheme()));
		btn.SetFgColor(new Vector3(hue, .33f, 1f).HSVfToRGBub());
		btn.SetText(text);

		btn.OnButtonClick += (_, _) => action?.Invoke(menu);
		btn.StartOffset = (menuBtns.Count + 1) * 24;

		menuBtns.Add(btn);
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
	
	public MainMenuPanel(Element? parent) : base(parent) {
		SetBorderSize(0);
		SetPaintBackgroundEnabled(false);

		SetPassthru(true);

		back = new Button(this);
		back.SetOrigin(Anchor.Center);
		back.SetBorderSize(0);
		back.SetBgColor(new Color(0, 0));
		back.OnButtonClick += (_, _) => DestroyNavigationMenu();;
		
		Image backImage = new(back);
		backImage.SetTexture(Level.Textures.LoadTextureFromFile("ui/back.png"));
		backImage.SetImageOrientation(ImageOrientation.Zoom);
		backImage.SetDock(Dock.Fill);
		
		CreateNavigationMenu();

		MakeNavigationButton(
			"Play", "Play your installed charts.", 200,
			"icons/play.png", menu => {
				var source = ChartMod.GetChartSongProviderByName("Muse Dash");
				if (source == null) {
					UI.DialogOK("Source Error", "The source from ChartMod.GetChartSongProviderByName returned null.");
					return;
				}

				var selector = menu.PushActiveElement(new SongSelector(UI));
				selector.SetSource(source.NewState());
			});

		MakeNavigationButton(
			"Play Custom Charts", "Play a custom chart (.mdm format).", 310,
			"icons/orange-slice.png", menu => {
				var source = ChartMod.GetChartSongProviderByName("Custom Albums");
				if (source == null) {
					UI.DialogOK("Source Error", "The source from ChartMod.GetChartSongProviderByName returned null.");
					return;
				}

				var selector = menu.PushActiveElement(new SongSelector(UI));
				selector.SetSource(source.NewState());
			});

		MakeNavigationButton("Browse mdmc.moe Charts", "Download new charts from the Muse Dash Modding Community.", 340,
			"icons/globe-hemisphere-west.png", (menu) => {
				var source = ChartMod.GetChartSongProviderByName("MDMC");
				if (source == null) {
					UI.DialogOK("Source Error", "The source from ChartMod.GetChartSongProviderByName returned null.");
					return;
				}

				menu.PushActiveElement(new SongSelector(UI)).SetSource(source.NewState());
			});

		MakeNavigationButton(
			"Change Character", "Select a character to play as.", 20,
			"icons/person-simple-run.png", menu => menu.PushActiveElement(new CharacterSelector(UI))
		);

		// Hidden because there is not really any functional stuff here
		/*MakeNavigationButton(
			"Modding Tools", "Various tools for modding the game", 225,
			"icons/wrench.png", ModdingTools_OpenMenuButtons
		);*/

		MakeNavigationButton(
			"Options", "Change game settings", 47,
			"icons/gear-six.png", (menu) => {
				var settings = menu.PushActiveElement(new SettingsEditor(UI));
				settings.SetPaintBackgroundEnabled(false);
			});

		MakeNavigationButton(
			"Exit to Desktop", $"Close the application.", 350,
			"icons/door-open.png", _ => EngineCore.Close()
		);
	}

	private void ModdingTools_OpenMenuButtons() {
		CreateNavigationMenu();
		MakeNavigationButton("Scene Editor", "Opens the scene editor & previewer", 160, "ui/sceneselect.png", menu => {
			// TODO: need engine interface. ConCommand.Execute(SceneEditorLevel.sceneedit);
		});
	}

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);

		if (!buttons.TryPeek(out List<MainMenuButton>? current))
			return;

		int count = current.Count;
		float btnHeight = height / 12f;
		back.SetSize(new Vector2F(btnHeight * 2));
		back.SetPos(new Vector2F(width * .5f, height / 2));
		back.SetVisible(!UsingRootNavigationMenu);

		for (int i = 0; i < count; i++) {
			MainMenuButton btn = current[i];
			float y = count == 1 ? 0 : NMath.Remap(i, 0, count - 1, -count / 2f, count / 2f);

			btn.SetPos(new Vector2F(width * .75f, height / 2f + y * (MainMenuButton.Height + MainMenuButton.Spacing)));
			btn.SetOrigin(Anchor.Center);
		}
	}
}
