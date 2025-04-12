using Nucleus;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using Raylib_cs;
using MouseButton = Nucleus.Types.MouseButton;
using Nucleus.Platform;
using static CloneDash.MuseDashCompatibility;
using CloneDash.Data;
using CloneDash.Animation;
using Nucleus.Audio;
using static CloneDash.CustomAlbumsCompatibility;
using CloneDash.Systems.CustomAlbums;
using System.Diagnostics;
using Nucleus.ManagedMemory;
using FMOD;

namespace CloneDash.Game
{
	public class CD_MainMenu : Level
	{
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
			loadMDLevel.MouseReleaseEvent += LoadMDLevel_MouseReleaseEvent;
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
			loadMDCC.MouseReleaseEvent += LoadMDCC_MouseReleaseEvent;
			loadMDCC.TooltipText = "Load CustomAlbums .mdm File";

			var test2 = header.Add<Label>();
			test2.Size = new Vector2F(158, 32);
			test2.Dock = Dock.Left;
			test2.Text = "Clone Dash [Alpha]";
			test2.TextSize = 30;
			test2.AutoSize = true;
			test2.DockMargin = RectangleF.TLRB(4);
		}

		Panel searchPanel;
		ScrollPanel scrollPanel;

		private void ClearWindow() {
			scrollPanel.AddParent.ClearChildren();
		}

		private void AddChartSelector(MDMCChart chart) {
			Button chartBtn = scrollPanel.Add<Button>();
			chartBtn.BorderSize = 0;
			chartBtn.Text = "";
			chartBtn.Dock = Dock.Top;
			chartBtn.Dock = Dock.Top;
			chartBtn.Size = new(96);

			Panel imageRenderer = chartBtn.Add<Panel>();
			imageRenderer.Size = new(64);
			imageRenderer.Position = new(4);
			imageRenderer.PaintOverride += ImageRenderer_PaintOverride;

			chart.GetCoverAsTexture((tex) => {
				if (IValidatable.IsValid(tex)) {
					imageRenderer.Image = tex;
				}
			});

			Label songAuthor, songName, likeCount, chartAuthor;
			Panel userIcon, likeIcon;
			Button playDemo, downloadOrPlay;

			chartBtn.Add(out songAuthor);
			chartBtn.Add(out songName);
			chartBtn.Add(out likeCount);
			chartBtn.Add(out chartAuthor);
			chartBtn.Add(out userIcon);
			chartBtn.Add(out likeIcon);
			chartBtn.Add(out playDemo);

			userIcon.PaintOverride += ImageRenderer_PaintOverride;
			likeIcon.PaintOverride += ImageRenderer_PaintOverride;
			playDemo.PaintOverride += ImageRenderer_PaintOverride;

			imageRenderer.ImageOrientation = ImageOrientation.Zoom;
			userIcon.ImageOrientation = ImageOrientation.Zoom;
			likeIcon.ImageOrientation = ImageOrientation.Zoom;
			playDemo.ImageOrientation = ImageOrientation.Zoom;

			songAuthor.Text = chart.Artist;
			songAuthor.AutoSize = true;

			songName.Text = string.IsNullOrEmpty(chart.TitleRomanized) ? chart.Title : chart.TitleRomanized;
			songName.AutoSize = true;

			likeCount.Text = $"{chart.Likes}";
			chartAuthor.Text = $"{chart.Charter}";

			userIcon.Image = userIcon.Level.Textures.LoadTextureFromFile("ui/user.png");
			likeIcon.Image = userIcon.Level.Textures.LoadTextureFromFile("ui/heart.png");
			playDemo.Image = userIcon.Level.Textures.LoadTextureFromFile("ui/listen.png");

			songAuthor.Position = new(96, 16);
			songAuthor.TextSize = 20;

			songName.Position = new(96, 32);
			songName.TextSize = 24;

			userIcon.Anchor = Anchor.BottomLeft;
			userIcon.Origin = Anchor.BottomLeft;
			userIcon.Size = new(24);

			chartAuthor.AutoSize = true;
			chartAuthor.Anchor = Anchor.BottomLeft;
			chartAuthor.Origin = Anchor.BottomLeft;
			chartAuthor.Position = new(24 + 2, -1);
			chartAuthor.TextSize = 22;
			chartAuthor.Size = new(24);

			playDemo.Anchor = Anchor.BottomRight;
			playDemo.Origin = Anchor.BottomRight;
			playDemo.Position = new(-4, 0);
			playDemo.Size = new(24);

			likeCount.Anchor = Anchor.BottomRight;
			likeCount.Origin = Anchor.BottomRight;
			likeCount.Position = new(-38, -3);
			likeCount.AutoSize = true;

			likeIcon.Anchor = Anchor.BottomRight;
			likeIcon.Origin = Anchor.BottomRight;
			likeIcon.Position = new(-38 + -22, 0);
			likeIcon.Size = new(24);

			imageRenderer.OnHoverTest += Element.Passthru;
			songAuthor.OnHoverTest += Element.Passthru;
			songName.OnHoverTest += Element.Passthru;
			likeCount.OnHoverTest += Element.Passthru;
			chartAuthor.OnHoverTest += Element.Passthru;
			userIcon.OnHoverTest += Element.Passthru;
			likeIcon.OnHoverTest += Element.Passthru;

			chartBtn.MouseReleaseEvent += (_, _, _) => {
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
			};
		}
		private void LoadMDM(string filename) => LoadSongSelectorFancy(new CustomChartsSong(filename));
		private void ImageRenderer_PaintOverride(Element self, float width, float height) {
			if (IValidatable.IsValid(self.Image))
				self.ImageDrawing(new(0), new(width, height));
		}

		private void PopulateWindow(string? query = null, MDMCWebAPI.Sort sort = MDMCWebAPI.Sort.LikesCount, int page = 1, bool onlyRanked = false) {
			var tempLabel = scrollPanel.Add<Label>();
			tempLabel.Text = "Loading...";
			tempLabel.Dock = Dock.Top;
			tempLabel.TextAlignment = Anchor.Center;
			tempLabel.AutoSize = true;
			tempLabel.TextSize = 26;

			MDMCWebAPI.SearchCharts(query, sort, page, onlyRanked).Then((resp) => {
				tempLabel.Remove();
				MDMCChart[] charts = resp.FromJSON<MDMCChart[]>() ?? throw new Exception("Parsing failure");

				foreach (MDMCChart chart in charts) {
					AddChartSelector(chart);
				}
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

		private void LoadSongSelectorFancy(ChartSong song) {
			// Load all slow-to-get info now before the Window loads
			MusicTrack? track = song.GetDemoTrack();
			var info = song.GetInfo();
			var cover = song.GetCover();

			ConstantLengthNumericalQueue<float> framesOverTime = new(480);

			Panel levelSelector = UI.Add<Panel>();
			levelSelector.MakePopup();
			levelSelector.Dock = Dock.Fill;
			levelSelector.Thinking += (s) =>
				s.BackgroundColor = new(0, 0, 0, (int)Math.Clamp(NMath.Ease.OutCubic(s.Lifetime * 1.4f) * 155, 0, 155));
			levelSelector.PaintOverride += (s, w, h) => {
				s.Paint(w, h);
				var length = framesOverTime.Capacity;
				Vector2F[] lineparts = new Vector2F[length];
				for (int i = 0; i < framesOverTime.Capacity; i++) {
					float sample = framesOverTime[i];
					var x = (i / (float)framesOverTime.Capacity * w);
					var y = (h / 2) + (h * .15f * sample);
					lineparts[i] = new(x, y);
				}
				Graphics2D.SetDrawColor(50, 50, 50, (int)(Math.Clamp(s.Lifetime * .6f, 0, 1) * 140));
				Graphics2D.DrawLineStrip(lineparts.ToArray());
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
				Graphics2D.SetDrawColor(25, 25, 25, 255);
				Graphics2D.DrawCircle(size / 2, (size.W / 2) + 12);
				Graphics2D.SetDrawColor(255, 255, 255, 255);
				Graphics2D.DrawImage(new(0, 0), size);
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
					var xOffset = NMath.Ease.InQuart(1 - Math.Clamp(life * 2f, 0, 1)) * 256;

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
}
