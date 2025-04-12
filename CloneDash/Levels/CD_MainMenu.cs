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
		private void LoadMDM(string filename) => LoadSongSelector(new CustomChartsSong(filename));
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

					if (song.Name.ToLower().Contains(txt.Text.ToLower())) item.ShowLVItem = true;
					else if (song.BaseName.ToLower().Contains(txt.Text.ToLower())) item.ShowLVItem = true;
					else if (song.Author.ToLower().Contains(txt.Text.ToLower())) item.ShowLVItem = true;
					else item.ShowLVItem = false;
				}
				list.InvalidateChildren(self: true, recursive: true);
			};

			list.Dock = Dock.Fill;

			foreach (var item in Songs) {
				var lvitem = list.Add<ListViewItem>();

				lvitem.SetTag("musedash_song", item);
				lvitem.Text = $"\"{item.Name}\" by {item.Author}";
				lvitem.MouseReleaseEvent += Lvitem_MouseReleaseEvent;
			}
		}

		private void LoadSongSelector(ChartSong song) {
			// Load all slow-to-get info now before the Window loads
			MusicTrack? track = song.GetDemoTrack();
			song.GetInfo();
			song.GetCover();

			ConstantLengthNumericalQueue<float> framesOverTime = new(480);
			Window levelSelector = UI.Add<Window>();
			levelSelector.HideNonCloseButtons();
			levelSelector.MakePopup();
			levelSelector.Title = $"\"{song.Name}\" by {song.Author} - Level Selection";
			//test.Title = "Non-Rendertexture Window";
			levelSelector.Size = new Vector2F(650, 320);
			levelSelector.DockPadding = RectangleF.TLRB(8);
			levelSelector.Center();

			levelSelector.AddParent.PaintOverride += (self, w, h) => {
				var length = framesOverTime.Length;
				Vector2F[] lineparts = new Vector2F[length];
				for (int i = 0; i < length; i++) {
					float sample = framesOverTime[i];
					var x = (i / (float)framesOverTime.Capacity) * w;
					var y = (h / 2) + (h * .25f * sample);
					lineparts[i] = new(x, y);
				}
				Graphics2D.SetDrawColor(50, 50, 50);
				Graphics2D.DrawLineStrip(lineparts.ToArray());
			};

			float currentAvgVolume = 0;
			SecondOrderSystem animationSmoother = new SecondOrderSystem(6, 0.98f, 1f, 0);


			Panel imageCanvas = levelSelector.Add<Panel>();
			imageCanvas.Dock = Dock.Right;
			imageCanvas.Size = new Vector2F(320 - 36);
			imageCanvas.PaintOverride += delegate (Element self, float width, float height) {
				var c = song.GetCover();
				if (c == null) return;
				Graphics2D.SetDrawColor(255, 255, 255, 255);
				Graphics2D.SetTexture(c.Texture);
				var distance = 16;
				var size = new Vector2F(width - (distance * 2) - Math.Clamp(Math.Abs(animationSmoother.Update(currentAvgVolume) * 80), 0, 16));
				var offset = Graphics2D.Offset;
				Graphics2D.ResetDrawingOffset();
				Rlgl.PushMatrix();
				Rlgl.Translatef(
					(offset.X + (width / 2)) + (float)(NMath.Remap(1 - NMath.Ease.OutCubic(self.Lifetime * 2), 0, 1, 0, 1, false, true) * width), 
					offset.Y + (height / 2), 0);
				Rlgl.Rotatef(self.Lifetime * 90, 0, 0, 1);
				Rlgl.Translatef(-size.X / 2, -size.Y / 2, 0);
				Graphics2D.DrawImage(new(0, 0), size);
				Graphics2D.OffsetDrawing(offset);
				Rlgl.PopMatrix();
			};

			if (track != null) {
				track.Playhead = 0;
				track.Volume = 0.4f;
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

			var info = song.GetInfo();


			CreateDifficulty(levelSelector, song, MuseDashDifficulty.Touhou, song.Difficulty5);
			CreateDifficulty(levelSelector, song, MuseDashDifficulty.Hidden, song.Difficulty4);
			CreateDifficulty(levelSelector, song, MuseDashDifficulty.Hard, song.Difficulty3);
			CreateDifficulty(levelSelector, song, MuseDashDifficulty.Normal, song.Difficulty2);
			CreateDifficulty(levelSelector, song, MuseDashDifficulty.Easy, song.Difficulty1);

			LevelSelectWindow?.AttachWindowAndLockInput(levelSelector);
		}
		private void Lvitem_MouseReleaseEvent(Element self, FrameState state, MouseButton button) {
			var song = self.GetTag<MuseDashSong>("musedash_song");

			LoadSongSelector(song);
		}

		private static void CreateDifficulty(Window levelSelector, ChartSong song, MuseDashDifficulty difficulty, string difficultyLevel)
			=> CreateDifficulty(levelSelector, (mapID, state) => {
				var sheet = song.GetSheet(mapID);
				var lvl = new CD_GameLevel(sheet);
				EngineCore.LoadLevel(lvl, state.KeyboardState.AltDown);
			}, difficulty, song.GetInfo()?.Designer((int)difficulty - 1) ?? "", difficultyLevel);
		private static void CreateDifficulty(Window levelSelector, Action<int, FrameState> onClick, MuseDashDifficulty difficulty, string designer, string difficultyLevel) {
			if (difficultyLevel == "") return;
			if (difficultyLevel == "0") return;

			Button play = levelSelector.Add<Button>();
			play.AutoSize = true;
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
		}

		public override void PreRenderBackground(FrameState frameState) {
			base.PreRenderBackground(frameState);
		}
	}
}
