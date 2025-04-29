using Nucleus;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using Raylib_cs;
using MouseButton = Nucleus.Types.MouseButton;
using CloneDash.Data;
using CloneDash.Animation;
using Nucleus.Audio;
using static CloneDash.CustomAlbumsCompatibility;
using CloneDash.Systems.CustomAlbums;
using Nucleus.Models.Runtime;
using CloneDash.Modding.Descriptors;
using CloneDash.Modding.Settings;
using System.Diagnostics;
using static AssetStudio.BundleFile;
using CloneDash.Levels;

namespace CloneDash.Game;

public interface IMainMenuPanel
{
	public string GetName();
	public void OnHidden();
	public void OnShown();
}

public class SongSelector : Panel, IMainMenuPanel
{
	public string GetName() => "Song Selector";
	public void OnHidden() { }
	public void OnShown() { }
	public List<ChartSong> Songs { get; set; } = [];
	public List<ChartSong>? SongsPostFilter { get; set; }

	public Predicate<ChartSong>? CurrentFilter { get; private set; }

	public List<ChartSong> GetSongsList() => SongsPostFilter ?? Songs;

	public void AddSongs(IEnumerable<ChartSong> songs) {
		Songs.AddRange(songs);

		ApplyFilter(CurrentFilter);
		InvalidateLayout();
	}
	protected override void OnThink(FrameState frameState) {
		base.OnThink(frameState);
		ThinkDiscs();
	}
	public void ApplyFilter(Predicate<ChartSong>? filter) {
		if (filter == null) {
			ClearFilter();
			return;
		}

		if (SongsPostFilter == null)
			SongsPostFilter = [];
		else
			SongsPostFilter.Clear();

		foreach (var song in Songs) {
			if (filter(song))
				SongsPostFilter.Add(song);
		}

		CurrentFilter = filter;

		SelectionUpdated(false);
	}

	public void ClearFilter() {
		SongsPostFilter?.Clear();
		SongsPostFilter = null;
		SelectionUpdated(true);
	}


	public delegate void UserWantsMore();
	public event UserWantsMore? UserWantsMoreSongs;
	public bool CanAcceptMoreSongs { get; set; } = true;
	public bool InfiniteList { get; set; } = true;

	public float DiscRotateAnimation { get; set; } = 0;

	public SecondOrderSystem DiscRotateSOS = new(2f, 0.94f, 1.1f, 0);
	public SecondOrderSystem FlyAwaySOS = new(1.5f, 0.94f, 1.1f, 0);

	protected virtual void SelectionUpdated(bool cleared) {

	}

	protected void GetMoreSongs() {
		if (!CanAcceptMoreSongs) return;

		Loading.Visible = true;
		Loading.MoveToFront();
		CanAcceptMoreSongs = false;
		UserWantsMoreSongs?.Invoke();
	}

	public Button Disc1;
	public Button Disc2;
	public Button Disc3;
	public Button Disc4;
	public Button Disc5;
	public Label CurrentTrackName;
	public Label CurrentTrackAuthor;

	public int DiscIndex = 0;
	public Button[] Discs;

	public float DiscAnimationOffset = 0;

	public void MoveLeft() {
		if (!InfiniteList && DiscIndex <= 0)
			return;

		DiscIndex--;
		DiscAnimationOffset--;
		ResetDiskTrack();
	}

	public void MoveRight() {
		if (!InfiniteList && DiscIndex >= GetSongsList().Count - 1)
			return;

		DiscIndex++;
		DiscAnimationOffset++;
		ResetDiskTrack();
	}

	public int GetSongIndex(int localIndex) => GetSongsList().Count == 0 ? localIndex : NMath.Modulo(DiscIndex + localIndex, GetSongsList().Count);
	public ChartSong GetDiscSong(int localIndex) {
		var songIndex = GetSongIndex(localIndex);
		return GetSongsList()[songIndex];
	}
	public ChartSong GetDiscSong(Button discButton) {
		int localIndex = discButton.GetTagSafely<int>("localDiscIndex");
		var songIndex = GetSongIndex(localIndex);

		return GetSongsList()[songIndex];
	}

	public void NavigateToDisc(Button discButton) {
		var index = discButton.GetTagSafely<int>("localDiscIndex");
		DiscIndex += index;
		DiscAnimationOffset += index;
		if (index != 0)
			ResetDiskTrack();
	}

	public bool IsDiscOverflowed(int localIndex) {
		var songIndex = DiscIndex + localIndex;
		return songIndex >= GetSongsList().Count || songIndex < 0;
	}

	public bool WillDiscOverflow() {
		return IsDiscOverflowed(DiscIndex);
	}

	public float DiscVibrate = 0;
	public float FlyAway = 0;

	public Button GetActiveDisc() => Discs[Discs.Length / 2];

	MusicTrack? activeTrack;
	bool doNotTryToGetTrackAgain;
	public MusicTrack? ActiveTrack => activeTrack;
	public void ResetDiskTrack() {
		if (IValidatable.IsValid(activeTrack)) {
			activeTrack.Playing = false;
			activeTrack = null;
		}
		doNotTryToGetTrackAgain = false;
	}

	public void FigureOutDisk() {
		if (GetSongsList().Count <= 0) return;
		activeTrack?.Update();
		if (IValidatable.IsValid(activeTrack)) return;
		if (doNotTryToGetTrackAgain) return;

		// Should play track?
		if (Math.Abs(DiscAnimationOffset) < 0.3) {
			var chart = GetDiscSong(0);
			activeTrack = chart.GetDemoTrack();

			if (activeTrack == null) {
				doNotTryToGetTrackAgain = !chart.IsLoadingDemoAsync;
				return;
			}

			activeTrack.Restart();
			activeTrack.Playing = true;
			activeTrack.Volume = 0.5f;
		}
	}

	public bool InSheetSelection { get; private set; }
	public float TargetRotationPostExit { get; private set; }
	public void EnterSheetSelection() {
		InSheetSelection = true;
		TargetRotationPostExit = 1;
	}
	public void ExitSheetSelection() {
		InSheetSelection = false;
		if (DiscRotateAnimation % 360 > 180) {
			DiscRotateSOS.ResetTo((DiscRotateAnimation % 180) - 180);
		}
		DiscRotateAnimation = 0;
		FlyAway = 0;
		DiscVibrate = 0;
		InvalidateLayout();
	}

	public Label Loading;
	// Constantly running logic
	public void ThinkDiscs() {
		FigureOutDisk();
		if (!InSheetSelection)
			DemandKeyboardFocus();

		float width = RenderBounds.W, height = RenderBounds.H;
		ChildRenderOffset = new(0, (float)NMath.Ease.InCirc(1 - (Math.Clamp(Lifetime, 0, 0.5) / 0.5)) * (width / 2));

		if (FlyAwaySOS.Update(FlyAway) > 0.001f || ChildRenderOffset.Y > 0) {
			LayoutDiscs(width, height);
		}

		for (int i = 0; i < Discs.Length; i++) {
			var disc = Discs[i];
			var index = DiscIndex + disc.GetTagSafely<int>("localDiscIndex");

			if (i == Discs.Length / 2 && (FlyAwaySOS.Out > 0.00001 || Math.Abs(DiscRotateSOS.Out) > 0.00001)) {
				disc.ImageRotation = DiscRotateSOS.Update(
					(MathF.Floor(DiscRotateAnimation / 360) * 360)
					+ (DiscRotateAnimation % 360)
				);

				var discWidth = GetDiscSize(width, disc);
				float size = (discWidth * ((FlyAwaySOS.Out / 4) + 1)) - DiscVibrate;
				CalculateDiscPos(width, height, i, out float x, out float y, out float rot);
				disc.SetRenderBounds(x - (size / 2), y - (size / 2), size, size);
			}

			if (!InfiniteList && index > GetSongsList().Count) continue;

			if (GetSongsList().Count > 0) {
				var song = GetDiscSong(disc);
				var cover = song.GetCover();

				disc.Text = "";
				if (cover != null) {
					disc.ImageOrientation = ImageOrientation.Stretch;
					disc.ImagePadding = new(16);
					disc.Image = cover.Texture;
					disc.ImageFlipX = false;
					disc.ImageFlipY = cover.Flipped;
				}
			}
		}
	}
	public void CalculateDiscPos(float width, float height, int index, out float x, out float y, out float rot) {
		var offsetYParent = ChildRenderOffset.Y / (width / 2);
		float flyAway = FlyAwaySOS.Out - (offsetYParent * -0.5f);
		float flyAwayMw = flyAway * (width);
		var widthRatio = MathF.Cos((float)NMath.Remap(index + DiscAnimationOffset, 0, Discs.Length - 1, -1 - (flyAway * 2), 1 + (flyAway * 2)));
		x = (float)NMath.Remap(index + DiscAnimationOffset, 0, Discs.Length - 1, -flyAwayMw, width + flyAwayMw);
		y = (height / 2f) + ((1 - widthRatio) * 250);
		var rR = 150;
		rot = (float)NMath.Remap(index + DiscAnimationOffset, 0, Discs.Length - 1, -15 - (flyAway * rR), 15 + (flyAway * rR));
	}

	public float GetDiscSize(float width, Button b) {
		var mainDiscMult = 0.75f - Math.Clamp(Math.Abs(b.GetTagSafely<int>("localDiscIndex") + DiscAnimationOffset), 0, 1);
		return (width / Discs.Length) + (mainDiscMult * 64);
	}

	public void LayoutDiscs(float width, float height) {
		if (GetSongsList().Count <= 0 || (!InfiniteList && WillDiscOverflow())) {
			GetMoreSongs();
			return;
		}

		for (int i = 0; i < Discs.Length; i++) {
			var disc = Discs[i];
			disc.Visible = true;
			var discWidth = GetDiscSize(width, disc);

			var willOverflow = !InfiniteList && IsDiscOverflowed(i - (Discs.Length / 2));
			if (willOverflow) {
				disc.Visible = false;
				continue;
			}
			else {
				disc.Visible = true;
			}

			disc.Size = new(discWidth, discWidth);

			CalculateDiscPos(width, height, i, out float x, out float y, out float rot);
			disc.ImageRotation = rot;
			disc.Position = new(x, y);
			disc.Text = "";
		}

		var heightDiv2 = height / 2;

		CurrentTrackName.Origin = Anchor.Center;
		CurrentTrackName.Anchor = Anchor.Center;
		CurrentTrackName.AutoSize = true;

		CurrentTrackAuthor.Origin = Anchor.Center;
		CurrentTrackAuthor.Anchor = Anchor.Center;
		CurrentTrackAuthor.AutoSize = true;

		CurrentTrackName.Position = new(0, heightDiv2 / 1.8f);
		CurrentTrackAuthor.Position = new(0, (heightDiv2 / 1.8f) + 42);

		CurrentTrackName.TextSize = 48;
		CurrentTrackAuthor.TextSize = 24;

		var mainSong = GetDiscSong(0);
		var info = mainSong.GetInfo();
		if (info != null) {
			CurrentTrackName.Text = mainSong.Name;
			CurrentTrackAuthor.Text = mainSong.Author;
		}

		if (Math.Abs(DiscAnimationOffset) > 0.001d) {
			DiscAnimationOffset /= 1.02f;
			InvalidateLayout(); // loop for next frame
			if (Math.Abs(DiscAnimationOffset) < 0.2) {
				if (Level.FrameState.KeyboardState.KeyDown(KeyboardLayout.USA.Left) || Level.FrameState.KeyboardState.KeyDown(KeyboardLayout.USA.A)) {
					MoveLeft();
				}
				else if (Level.FrameState.KeyboardState.KeyDown(KeyboardLayout.USA.Right) || Level.FrameState.KeyboardState.KeyDown(KeyboardLayout.USA.D)) {
					MoveRight();
				}
			}
		}
		else if (DiscAnimationOffset != 0) {
			// set it to 0 and don't invalidate again after
			DiscAnimationOffset = 0;
			InvalidateLayout();
		}
	}

	protected override void Initialize() {
		base.Initialize();
		DrawPanelBackground = false;

		Add(out Disc1);
		Add(out Disc2);
		Add(out Disc3);
		Add(out Disc4);
		Add(out Disc5);
		Add(out CurrentTrackName);
		Add(out CurrentTrackAuthor);

		Add(out Loading);
		Loading.Anchor = Anchor.Center;
		Loading.Origin = Anchor.Center;
		Loading.Text = "LOADING";
		Loading.TextSize = 100;
		Loading.AutoSize = true;
		Loading.Visible = false;

		Discs = [Disc1, Disc2, Disc3, Disc4, Disc5];
		for (int i = 0; i < Discs.Length; i++) {
			var disc = Discs[i];
			disc.Visible = false;
			disc.Origin = Anchor.Center;
			disc.SetTag("localDiscIndex", i - (Discs.Length / 2));

			disc.MouseReleaseEvent += (s, _, _) => {
				NavigateToDisc(s as Button);
				var song = GetDiscSong(s as Button);
				if (song is CustomChartsSong customChartsSong) {
					customChartsSong.DownloadOrPullFromCache((c) => EngineCore.Level.As<CD_MainMenu>().LoadChartSelector(this, c));
				}
				else
					EngineCore.Level.As<CD_MainMenu>().LoadChartSelector(this, song);
			};
			disc.BorderSize = 0;
			var midpoint = Discs.Length / 2;
			disc.BackgroundColor = new(0, 0, 0, 0);
			disc.ImageColor = i == midpoint ? new Color(255) : new Color(155);
			disc.PaintOverride += (s, w, h) => {
				var a = i == midpoint ? 1 - FlyAway : 1;
				var c = MixColorBasedOnMouseState(s, new(35, (int)(255 * a)), new(0, 1, 2, 1), new(0, 1, 0.5f, 1));
				Graphics2D.SetDrawColor(c);
				Graphics2D.DrawCircle(new(w / 2, h / 2), (w / 2) - 8);
				ImageColor = new(255, 255, 255, (int)(255 * a));
				s.Paint(w, h);
			};
		}

		DemandKeyboardFocus();
	}

	public override void MouseClick(FrameState state, MouseButton button) {
		base.MouseClick(state, button);
		DemandKeyboardFocus();
	}

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);
		LayoutDiscs(width, height);
	}
	public override void KeyPressed(KeyboardState keyboardState, Nucleus.Types.KeyboardKey key) {
		base.KeyPressed(keyboardState, key);
		if (key == KeyboardLayout.USA.Left || key == KeyboardLayout.USA.A) {
			MoveLeft();
			InvalidateLayout();
		}
		else if (key == KeyboardLayout.USA.Right || key == KeyboardLayout.USA.D) {
			MoveRight();
			InvalidateLayout();
		}
	}

	public override void Paint(float width, float height) {
		base.Paint(width, height);

		CurrentTrackName.TextColor = new(255, 255, 255, (int)(255 * (1 - FlyAway)));
		CurrentTrackAuthor.TextColor = new(255, 255, 255, (int)(255 * (1 - FlyAway)));
	}

	internal void AcceptMoreSongs() {
		CanAcceptMoreSongs = true;
		Loading.Visible = false;
	}
}

public class MainMenuButton : Button
{
	protected override void Initialize() {
		base.Initialize();
		TextAlignment = Anchor.CenterRight;
		ShouldDrawImage = false;
		Clipping = false;
	}

	public string SubText;
	SecondOrderSystem sos = new SecondOrderSystem(1, 1, 1, 100);

	public void SetStart(float x) => sos.ResetTo(x);

	public float Offscreen { get; set; }

	protected override void OnThink(FrameState frameState) {
		base.OnThink(frameState);
		this.ChildRenderOffset = new(sos.Update(Offscreen != 0 ? frameState.WindowWidth / 2 * Offscreen : Hovered ? -50 : 0), 0);
	}

	public override void Paint(float width, float height) {
		Button.ColorStateSetup(this, out Color back, out Color fore);
		Element.PaintBackground(this, width, height, back, fore, BorderSize);

		var decomposed = fore.Adjust(0, 0, 2555, false);

		Graphics2D.SetDrawColor(decomposed);
		var p = 2;

		ImageOrientation = ImageOrientation.None;
		ImageColor = ForegroundColor.Adjust(0, -0.2, 2, false);
		ImageDrawing(new(p / 2, p / 2), new(height - (p * 2), height - (p * 2)));

		Graphics2D.DrawText(new(width - 8, 8), Text, Font, TextSize * 0.85f, Anchor.TopRight);
		if (SubText != null)
			Graphics2D.DrawText(new(width - 4, height - 8), SubText, Font, TextSize * 0.45f, Anchor.BottomRight);
	}
}

public class MainMenuPanel : Panel, IMainMenuPanel
{
	public string GetName() => "Main Menu";
	public void OnHidden() { }
	public void OnShown() {
		music.Restart();
	}

	CharacterDescriptor character;
	ModelInstance model;
	AnimationHandler anims;
	MusicTrack music;

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

	private MainMenuButton MakeNavigationButton(string text, string icon, string description, float hue, Action<CD_MainMenu>? action = null) {
		CD_MainMenu menu = Level.As<CD_MainMenu>();
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

		foreach(var file in Filesystem.FindFiles("charts", "*.mdm", SearchOption.AllDirectories)) {
			ret.Add(new CustomChartsSong("charts", file));
		}

		return ret;
	}
	protected override void Initialize() {
		base.Initialize();
		CharacterDescriptor character = CharacterMod.GetCharacterData();
		if (character == null) return;
		if (character.Filename == null) return;
		this.character = character;

		model = Level.Models.CreateInstanceFromFile("chars", $"{character.Filename}/{character.GetMainShowModel()}");
		anims = new(model.Data);
		anims.SetAnimation(0, character.MainShow.StandbyAnimation, true);

		music = Level.Sounds.LoadMusicFromFile("chars", $"{character.Filename}/{character.GetMainShowMusic()}", true);
		music.Loops = true;

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
		});
		MakeNavigationButton("Play Custom Chart", "ui/play_cam_level.png", "Play a custom chart (.mdm format).", 310, (menu) => {
			var selector = menu.PushActiveElement(UI.Add<SongSelector>());
			selector.AddSongs(RefreshLocalSongs());
		});
		MakeNavigationButton("Search mdmc.moe Charts", "ui/webcharts.png", "Find new charts from the Muse Dash Modding Community.", 340, (menu) => {
			var selector = menu.PushActiveElement(UI.Add<SongSelector>());
			selector.InfiniteList = false;
			int page = 1;
			selector.UserWantsMoreSongs += () => {
				// Load more songs
				menu.PopulateMDMCCharts(selector, page: page);
				page++;
			};
		});
		MakeNavigationButton("Change Character", "ui/charselect.png", "Select a character from the characters you have installed.", 20);
		MakeNavigationButton("Change Scene", "ui/sceneselect.png", "Select a scene from the scenes you have installed.", 70);
		MakeNavigationButton("Modding Tools", "ui/solder.png", "Various tools for modding the game", 225, ModdingTools_OpenMenuButtons);
		MakeNavigationButton("Options", "ui/pause_settings.png", "Change game settings", 200);
		MakeNavigationButton("Exit to Desktop", "ui/pause_exit.png", $"Close the application.", 350, (menu) => EngineCore.Close());
	}

	private void Back_MouseReleaseEvent(Element self, FrameState state, MouseButton button) {
		DestroyNavigationMenu();
	}

	private void ModdingTools_OpenMenuButtons(CD_MainMenu menu) {
		CreateNavigationMenu();
		MakeNavigationButton("Scene Editor", "ui/sceneselect.png", "Opens the scene editor & previewer", 160, (menu) => {
			ConCommand.Execute(CD_SceneEdit.clonedash_sceneedit);
		});
	}

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);

		var btns = this.btns.Peek();
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

			btn.Position = new(width * .75f, (height / 2) + (y * height / 3));
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
	CharacterMainShowTouchResponse touchResponse;
	int click = 0;
	public override void MouseClick(FrameState state, MouseButton button) {
		if (character == null) return;
		touchResponse = character.MainShow.Touch.GetRandomTouchResponse();
		click++;

		anims.SetAnimation(0, character.MainShow.Touch.MainResponse.GetAnimation(click));
		anims.AddAnimation(0, character.MainShow.StandbyAnimation, true);

		anims.SetAnimation(1, touchResponse.Start);
		anims.AddAnimation(1, touchResponse.Standby);
		anims.AddAnimation(1, touchResponse.End);
	}
	public override void Paint(float width, float height) {
		Raylib.BeginMode2D(new() {
			Zoom = height / 900 / 2.4f,
			Offset = new((width / 2) - (width * .2f), height / 1)
		});

		model?.Render();

		Raylib.EndMode2D();
	}
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
			Raylib.BeginMode2D(new() {
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

			Raylib.EndMode2D();
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
			Raylib.BeginMode2D(new() {
				Zoom = 1f,
				Offset = s.GetGlobalPosition().ToNumerics() + new System.Numerics.Vector2(w / 2, h / 2) + new System.Numerics.Vector2(0, 200)
			});

			if (model != null && anims != null) {
				anims.AddDeltaTime(EngineCore.Level.CurtimeDelta);
				anims.Apply(model);
				model.Render();
			}
			Raylib.EndMode2D();
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

		backButton = MenuButton(header, Dock.Left, "ui\\back.png", $"Back", () => {
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

	private CustomChartsSong AddChartSelector(MDMCChart chart) {
		CustomChartsSong song = new CustomChartsSong(chart);
		return song;
	}

	internal void PopulateMDMCCharts(SongSelector selector, string? query = null, MDMCWebAPI.Sort sort = MDMCWebAPI.Sort.LikesCount, int page = 1, bool onlyRanked = false) {

		MDMCWebAPI.SearchCharts(query, sort, page, onlyRanked).Then((resp) => {
			MDMCChart[] charts = resp.FromJSON<MDMCChart[]>() ?? throw new Exception("Parsing failure");
			var songs = new List<CustomChartsSong>();

			foreach (MDMCChart chart in charts) {
				songs.Add(AddChartSelector(chart));
			}

			selector?.AddSongs(songs);
			selector?.AcceptMoreSongs();
		});
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
			levelSelector.Level.As<CD_MainMenu>().LoadChartSheetLevel(song, mapID, state.KeyboardState.AltDown);
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

			Vector2F textDrawingPosition = Anchor.GetPositionGivenAlignment(Anchor.CenterRight, btn.RenderBounds.Size, btn.TextPadding);
			Graphics2D.SetDrawColor(btn.TextColor);
			Graphics2D.DrawText(textDrawingPosition + new Vector2F(0, -6), $"{difficultyLevel}", btn.Font, btn.TextSize, Anchor.CenterRight);
		};

		play.Thinking += delegate (Element self) {
			if (EngineCore.CurrentFrameState.KeyboardState.AltDown) {
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