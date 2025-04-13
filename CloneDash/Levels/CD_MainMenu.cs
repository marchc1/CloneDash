using Nucleus;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using Raylib_cs;
using MouseButton = Nucleus.Types.MouseButton;
using static CloneDash.MuseDashCompatibility;
using CloneDash.Data;
using CloneDash.Animation;
using Nucleus.Audio;
using static CloneDash.CustomAlbumsCompatibility;
using CloneDash.Systems.CustomAlbums;
using Nucleus.Rendering;
using System.Buffers;

namespace CloneDash.Game;

public class SongSelector : Panel
{
	public List<ChartSong> Songs { get; set; } = [];
	public List<ChartSong>? SongsPostFilter { get; set; }

	public Predicate<ChartSong>? CurrentFilter { get; private set; }

	public void AddSongs(IEnumerable<ChartSong> songs) {
		Songs.AddRange(songs);

		ApplyFilter(CurrentFilter);
		InvalidateLayout();
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

	protected virtual void SelectionUpdated(bool cleared) {

	}

	protected void GetMoreSongs() {
		if (!CanAcceptMoreSongs) return;

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
	}

	public void MoveRight() {
		if (!InfiniteList && DiscIndex >= Songs.Count - 1)
			return;

		DiscIndex++;
		DiscAnimationOffset++;
	}

	public int GetSongIndex(int localIndex) => NMath.Modulo(DiscIndex + localIndex, Songs.Count);
	public ChartSong GetDiscSong(int localIndex) {
		var songIndex = GetSongIndex(localIndex);
		return Songs[songIndex];
	}
	public ChartSong GetDiscSong(Button discButton) {
		int localIndex = discButton.GetTagSafely<int>("localDiscIndex");
		var songIndex = GetSongIndex(localIndex);
		return Songs[songIndex];
	}

	public bool IsDiscOverflowed(int localIndex) {
		var songIndex = DiscIndex + localIndex;
		return songIndex >= Songs.Count || songIndex < 0;
	}

	public bool WillDiscOverflow() {
		return IsDiscOverflowed(DiscIndex);
	}

	public float FlyAway = 0;

	public void LayoutDiscs(float width, float height) {
		if (Songs.Count <= 0 || WillDiscOverflow()) {
			GetMoreSongs();
			return;
		}
		for (int i = 0; i < Discs.Length; i++) {
			var disc = Discs[i];
			var discWidth = (width / Discs.Length);

			var willOverflow = !InfiniteList && IsDiscOverflowed(i - (Discs.Length / 2));
			if (willOverflow) {
				disc.Visible = false;
				continue;
			}
			else {
				disc.Visible = true;
			}

			disc.Size = new(discWidth, discWidth);

			float flyAway = FlyAway * (width);
			var discX = (float)NMath.Remap(i + DiscAnimationOffset, 0, Discs.Length - 1, -flyAway, width + flyAway);

			var rot = (float)NMath.Remap(i + DiscAnimationOffset, 0, Discs.Length - 1, -15, 15);
			disc.ImageRotation = rot;

			var widthRatio = MathF.Cos((float)NMath.Remap(i + DiscAnimationOffset, 0, Discs.Length - 1, -1 - (FlyAway * 2), 1 + (FlyAway * 2)));

			var discY = (height / 2f) + ((1 - widthRatio) * 250);
			disc.Position = new(discX, discY);

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
		Discs = [Disc1, Disc2, Disc3, Disc4, Disc5];
		for (int i = 0; i < Discs.Length; i++) {
			var disc = Discs[i];
			disc.Origin = Anchor.Center;
			disc.SetTag("localDiscIndex", i - (Discs.Length / 2));

			disc.MouseReleaseEvent += (s, _, _) => {
				var song = GetDiscSong(s as Button);
				if (song is CustomChartsSong customChartsSong) {
					customChartsSong.DownloadOrPullFromCache((c) => EngineCore.Level.As<CD_MainMenu>().LoadSongSelectorFancy(c));
				}
				else
					EngineCore.Level.As<CD_MainMenu>().LoadSongSelectorFancy(song);
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
}

public class CD_MainMenu : Level
{
	public SongSelector Selector;
	public SongSelector InitializeSelector() {
		Selector?.Remove();
		Selector = UI.Add<SongSelector>();
		Selector.Dock = Dock.Fill;
		return Selector;
	}
	public override void Initialize(params object[] args) {
		var header = UI.Add<Panel>();
		header.Position = new Vector2F(0);
		header.Size = new Vector2F(256, 64);
		header.Dock = Dock.Top;

		var loadMDLevel = header.Add<Button>();
		loadMDLevel.AutoSize = false;
		loadMDLevel.Size = new Vector2F(64);
		loadMDLevel.Text = "";
		loadMDLevel.ImageOrientation = ImageOrientation.Zoom;
		loadMDLevel.Dock = Dock.Right;
		loadMDLevel.Image = Textures.LoadTextureFromFile("ui\\play_md_level.png");
		loadMDLevel.TextSize = 21;
		loadMDLevel.DockMargin = RectangleF.TLRB(0);
		loadMDLevel.BorderSize = 0;
		loadMDLevel.MouseReleaseEvent += (_, _, _) => {
			var selector = InitializeSelector();
			selector.AddSongs(MuseDashCompatibility.Songs);
		};
		loadMDLevel.TooltipText = "Load Muse Dash Level";

		var loadMDCC = header.Add<Button>();
		loadMDCC.AutoSize = false;
		loadMDCC.Size = new Vector2F(64);
		loadMDCC.Text = "";
		loadMDCC.ImageOrientation = ImageOrientation.Zoom;
		loadMDCC.Dock = Dock.Right;
		loadMDCC.Image = Textures.LoadTextureFromFile("ui\\play_cam_level.png");
		loadMDCC.TextSize = 21;
		loadMDCC.DockMargin = RectangleF.TLRB(0);
		loadMDCC.BorderSize = 0;
		loadMDCC.MouseReleaseEvent += (_, _, _) => {
			var selector = InitializeSelector();
			selector.InfiniteList = false;
			selector.UserWantsMoreSongs += () => {
				// Load more songs
				PopulateWindow();
			};
		};
		loadMDCC.TooltipText = "Load CustomAlbums .mdm File";

		var test2 = header.Add<Label>();
		test2.Size = new Vector2F(158, 32);
		test2.Dock = Dock.Left;
		test2.Text = "Clone Dash";
		test2.TextSize = 30;
		test2.AutoSize = true;
		test2.DockMargin = RectangleF.TLRB(4);

		Keybinds.AddKeybind([KeyboardLayout.USA.LeftControl, KeyboardLayout.USA.R], () => EngineCore.LoadLevel(new CD_MainMenu()));
	}

	Panel searchPanel;
	ScrollPanel scrollPanel;

	private void ClearWindow() {
		scrollPanel.AddParent.ClearChildren();
	}

	private CustomChartsSong AddChartSelector(MDMCChart chart) {
		CustomChartsSong song = new CustomChartsSong(chart);
		return song;
		/*chartBtn.MouseReleaseEvent += (_, _, _) => {
			var filename = Filesystem.Resolve($"charts/{chart.ID}.mdm", "game", false);
			if (!File.Exists(filename)) {
				chart.DownloadTo(filename, (worked) => {
					System.Diagnostics.Debug.Assert(worked);
					if (worked) {
						LoadMDM(filename);
						Logs.Info($"Downloaded {chart.ID}.mdm");
					}
					else Logs.Warn($"Couldn't download {chart.ID}.mdm");
				});
				Logs.Info($"Downloading {chart.ID}.mdm..");
			}
			else {
				Logs.Info($"Already cached {chart.ID}.mdm");
				LoadMDM(filename);
			}
		};*/
	}
	private void LoadMDM(string filename) => LoadSongSelectorFancy(new CustomChartsSong(filename));
	private void ImageRenderer_PaintOverride(Element self, float width, float height) {
		if (IValidatable.IsValid(self.Image))
			self.ImageDrawing(new(0), new(width, height));
	}

	private void PopulateWindow(string? query = null, MDMCWebAPI.Sort sort = MDMCWebAPI.Sort.LikesCount, int page = 1, bool onlyRanked = false) {
		/*var tempLabel = scrollPanel.Add<Label>();
		tempLabel.Text = "Loading...";
		tempLabel.Dock = Dock.Top;
		tempLabel.TextAlignment = Anchor.Center;
		tempLabel.AutoSize = true;
		tempLabel.TextSize = 26;*/

		MDMCWebAPI.SearchCharts(query, sort, page, onlyRanked).Then((resp) => {
			MDMCChart[] charts = resp.FromJSON<MDMCChart[]>() ?? throw new Exception("Parsing failure");
			var songs = new List<CustomChartsSong>();

			foreach (MDMCChart chart in charts) {
				songs.Add(AddChartSelector(chart));
			}

			Selector?.AddSongs(songs);
		});
	}

	private void LoadMDCC_MouseReleaseEvent(Element self, FrameState state, MouseButton button) {
		LevelSelectWindow = UI.Add<Window>();
		LevelSelectWindow.Title = "Open Custom Albums Chart";
		//test.Title = "Non-Rendertexture Window";
		LevelSelectWindow.Size = new Vector2F(600, 600);
		LevelSelectWindow.DockPadding = RectangleF.TLRB(4);
		LevelSelectWindow.HideNonCloseButtons();
		LevelSelectWindow.Center();

		LevelSelectWindow.Add(out searchPanel);
		LevelSelectWindow.Add(out scrollPanel);

		searchPanel.Dock = Dock.Top;
		scrollPanel.Dock = Dock.Fill;
		searchPanel.Size = new Vector2F(96);

		PopulateWindow();

		/*var result = TinyFileDialogs.OpenFileDialog(".mdm file", "", ["*.mdm"], "Muse Dash Custom Album Chart", false);
		if (!result.Cancelled) {
			LoadSongSelector(new CustomChartsSong(result.Result));
		}*/
	}

	public record MuseDashMap(string map_first, List<string> maps);

	public Window LevelSelectWindow { get; set; }
	private void LoadMDLevel_MouseReleaseEvent(Element self, FrameState state, MouseButton button) {
		if (IValidatable.IsValid(LevelSelectWindow))
			LevelSelectWindow.Remove();

		LevelSelectWindow = UI.Add<Window>();
		LevelSelectWindow.Title = "Open Muse Dash Level";
		//test.Title = "Non-Rendertexture Window";
		LevelSelectWindow.Size = new Vector2F(600, 600);
		LevelSelectWindow.DockPadding = RectangleF.TLRB(4);
		LevelSelectWindow.HideNonCloseButtons();
		LevelSelectWindow.Center();

		var txt = LevelSelectWindow.Add<Textbox>();
		var list = LevelSelectWindow.Add<ListView>();

		txt.Dock = Dock.Top;
		txt.HelperText = "Filter by Level Name...";
		txt.TextChangedEvent += delegate (Element self, string oldT, string newT) {
			foreach (Element e in list.MainPanel.GetChildren()) {
				ListViewItem item = e as ListViewItem;
				var song = item.GetTag<MuseDashSong>("musedash_song");

				if (song.Name.ToLower().Contains(newT.ToLower())) item.ShowLVItem = true;
				else if (song.BaseName.ToLower().Contains(newT.ToLower())) item.ShowLVItem = true;
				else if (song.Author.ToLower().Contains(newT.ToLower())) item.ShowLVItem = true;
				else item.ShowLVItem = false;
			}
			MainThread.RunASAP(() => list.InvalidateChildren(self: true, recursive: true));
		};

		list.Dock = Dock.Fill;

		foreach (var item in Songs) {
			var lvitem = list.Add<ListViewItem>();

			lvitem.SetTag("musedash_song", item);
			lvitem.Text = $"\"{item.Name}\" by {item.Author}";
			lvitem.MouseReleaseEvent += Lvitem_MouseReleaseEvent;
		}
	}
	private float offsetBasedOnLifetime(Element e, float inf, float heightDiv) =>
		(float)(NMath.Remap(1 - NMath.Ease.OutCubic(e.Lifetime * inf), 0, 1, 0, 1, false, true) * (EngineCore.GetWindowHeight() / heightDiv));

	internal void LoadSongSelectorFancy(ChartSong song) {
		// Load all slow-to-get info now before the Window loads
		MusicTrack? track = song.GetDemoTrack();
		var info = song.GetInfo();
		var cover = song.GetCover();

		ConstantLengthNumericalQueue<float> framesOverTime = new(240);

		Panel levelSelector = UI.Add<Panel>();
		levelSelector.MakePopup();
		levelSelector.ForegroundColor = Color.Blank;
		levelSelector.Dock = Dock.Fill;
		levelSelector.Thinking += (s) => {
			s.BackgroundColor = new(0, 0, 0, (int)Math.Clamp(NMath.Ease.OutCubic(s.Lifetime * 1.4f) * 155, 0, 155));
			Selector.FlyAway = Math.Clamp(NMath.Ease.OutCubic(s.Lifetime * 1.4f) * 1, 0, 1);
			if (s.Lifetime < 1.5)
				Selector.InvalidateLayout();
		};
		// TODO: the opposite of whatever this mess is
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
		};
		levelSelector.Removed += (s) => {
			if (Selector != null) {
				Selector.FlyAway = 0;
				Selector.InvalidateLayout();
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
			self.Position = new(-160 + (NMath.Ease.OutCubic(Math.Clamp(self.Lifetime - 0.3f, 0, 1)) * -96), 0);
			self.Paint(w, h);
		};

		float currentAvgVolume = 0;
		SecondOrderSystem animationSmoother = new SecondOrderSystem(6, 0.98f, 1f, 0);

		Panel imageCanvas = levelSelector.Add<Panel>();
		imageCanvas.Anchor = Anchor.Center;
		imageCanvas.Origin = Anchor.Center;
		imageCanvas.Clipping = false;
		imageCanvas.Size = new Vector2F(320 - 36);
		imageCanvas.OnHoverTest += Element.Passthru;
		imageCanvas.Clipping = false;

		imageCanvas.PaintOverride += delegate (Element self, float width, float height) {
			var c = song.GetCover();
			if (c == null) return;

			Graphics2D.SetTexture(c.Texture);
			var distance = 16;
			var size = new Vector2F(width - (distance * 2) - Math.Clamp(Math.Abs(animationSmoother.Update(currentAvgVolume) * 80), 0, 16));
			var offset = Graphics2D.Offset;
			Graphics2D.ResetDrawingOffset();
			Rlgl.PushMatrix();
			var imgOffset = offsetBasedOnLifetime(self, 1.5f, 2);
			var sizeOffset = offsetBasedOnLifetime(self, 1.5f, 8);
			size -= sizeOffset;
			Rlgl.Translatef(
				(offset.X + (width / 2)),
				offset.Y + (height / 2) + imgOffset,
			0);
			Rlgl.Rotatef(self.Lifetime * 90, 0, 0, 1);
			Rlgl.Translatef(-size.X / 2, -size.Y / 2, 0);
			var alpha = 1 - NMath.Remap(1 - NMath.Ease.OutCubic(self.Lifetime * 2), 0, 1, 0, 1, false, true);
			Graphics2D.SetDrawColor(25, 25, 25, (int)(255 * alpha));
			Graphics2D.DrawCircle(size / 2, (size.W / 2) + 12);
			Graphics2D.SetDrawColor(255, 255, 255, (int)(255 * alpha));
			Graphics2D.DrawImage(new(0, 0), size, flipY: c.Flipped);
			Graphics2D.OffsetDrawing(offset);
			Rlgl.PopMatrix();
		};

		var title = levelSelector.Add<Label>();
		title.TextSize = 48;
		title.Text = song.Name;
		title.AutoSize = true;
		title.Anchor = Anchor.Center;
		title.Origin = Anchor.Center;

		title.Thinking += (s) => {
			s.TextColor = new(255, 255, 255, (int)(NMath.Ease.InOutCubic(Math.Clamp(s.Lifetime * 6, 0, 1)) * 255));
			s.Position = new(0, -190 - offsetBasedOnLifetime(s, 1.35f, 6));
		};

		var author = levelSelector.Add<Label>();
		author.TextSize = 22;
		author.Text = $"by {song.Author}";
		author.AutoSize = true;
		author.Anchor = Anchor.Center;
		author.Origin = Anchor.Center;

		author.Thinking += (s) => {
			s.TextColor = new(255, 255, 255, (int)(NMath.Ease.InOutCubic(Math.Clamp(s.Lifetime * 1.3f, 0, 1)) * 255));
			s.Position = new(0, -162 - offsetBasedOnLifetime(s, 1.35f, 12));
		};

		if (track != null) {
			track.Volume = 0.4f;
			track.Restart();
			track.Playing = true;

			levelSelector.Thinking += delegate (Element self) {
				track.Update();
			};

			levelSelector.Removed += delegate (Element self) {
				track.Paused = true;
			};

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

		var difficulties = levelSelector.Add<FlexPanel>();
		difficulties.Direction = Directional180.Vertical;
		difficulties.ChildrenResizingMode = FlexChildrenResizingMode.FitToOppositeDirection;
		difficulties.Anchor = Anchor.Center;
		difficulties.Origin = Anchor.Center;
		difficulties.Position = new(256 + 64, 0);
		int height = 356;
		difficulties.Thinking += (s) => {
			s.Size = new(256, height);
		};

		var d1 = CreateDifficulty(difficulties, song, MuseDashDifficulty.Easy, song.Difficulty1);
		var d2 = CreateDifficulty(difficulties, song, MuseDashDifficulty.Normal, song.Difficulty2);
		var d3 = CreateDifficulty(difficulties, song, MuseDashDifficulty.Hard, song.Difficulty3);
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
	private void Lvitem_MouseReleaseEvent(Element self, FrameState state, MouseButton button) {
		var song = self.GetTag<MuseDashSong>("musedash_song");

		LoadSongSelectorFancy(song);
	}

	private static Button? CreateDifficulty(FlexPanel levelSelector, ChartSong song, MuseDashDifficulty difficulty, string difficultyLevel)
		=> CreateDifficulty(levelSelector, (mapID, state) => {
			var sheet = song.GetSheet(mapID);
			var lvl = new CD_GameLevel(sheet);
			EngineCore.LoadLevel(lvl, state.KeyboardState.AltDown);
		}, difficulty, song.GetInfo()?.Designer((int)difficulty - 1) ?? "", difficultyLevel);
	private static Button? CreateDifficulty(FlexPanel levelSelector, Action<int, FrameState> onClick, MuseDashDifficulty difficulty, string designer, string difficultyLevel) {
		if (difficultyLevel == "") return null;
		if (difficultyLevel == "0") return null;

		Button play = levelSelector.Add<Button>();
		play.Size = new(64);
		play.Dock = Dock.Bottom;

		var difficultyName = difficulty switch {
			MuseDashDifficulty.Easy => "Easy",
			MuseDashDifficulty.Normal => "Normal",
			MuseDashDifficulty.Hard => "Hard",
			MuseDashDifficulty.Hidden => "Hidden",
			MuseDashDifficulty.Touhou => "Touhou",
			_ => throw new Exception($"Unsupported difficulty level '{difficulty}'")
		};
		Color buttonColor = difficulty switch {
			MuseDashDifficulty.Easy => new Color(88, 199, 76, 60),
			MuseDashDifficulty.Normal => new Color(109, 196, 199, 60),
			MuseDashDifficulty.Hard => new Color(188, 95, 184, 60),
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
		//play.Text = $"Play on {difficultyName} Mode [difficulty: {difficultyLevel}]";
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
