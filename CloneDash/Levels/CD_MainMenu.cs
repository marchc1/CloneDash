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
			loadMDLevel.Image = Textures.LoadTextureFromFile("ui\\mainmenu_play.png");
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
			loadMDCC.Image = Textures.LoadTextureFromFile("ui\\mainmenu_play.png");
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

			CloneDashConsole.HookToLevel(this);
		}

		private void LoadMDCC_MouseReleaseEvent(Element self, FrameState state, MouseButton button) {
			var result = TinyFileDialogs.OpenFileDialog(".mdm file", "", ["*.mdm"], "Muse Dash Custom Album Chart", false);
			if (!result.Cancelled) {
				LoadSongSelector(new CustomChartsSong(result.Result));
			}
		}

		public record MuseDashMap(string map_first, List<string> maps);

		public Window MDLevelWindow { get; set; }
		private void LoadMDLevel_MouseReleaseEvent(Element self, FrameState state, MouseButton button) {
			if (IValidatable.IsValid(MDLevelWindow))
				return;
			/*
            var testingImages = UI.Add<Window>();
            testingImages.Title = "FlexPanel & ImageOrientation Test";
            testingImages.Size = new(1500, 200); 
            testingImages.Center();

            var flexTest = testingImages.Add<FlexPanel>();
            flexTest.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
            flexTest.Dock = Dock.Fill;

            for (int i = 0; i < 5; i++) {
                var img = flexTest.Add<Button>();
                img.TextAlignment = Anchor.TopLeft;
                img.Text = Enum.GetName(typeof(ImageOrientation), i);
                img.Image = TextureSystem.LoadTexture("ui/pause_play.png");
                img.ImageOrientation = (ImageOrientation)i;
            }

            return;*/

			MDLevelWindow = UI.Add<Window>();
			MDLevelWindow.Title = "Open Muse Dash Level";
			//test.Title = "Non-Rendertexture Window";
			MDLevelWindow.Size = new Vector2F(600, 600);
			MDLevelWindow.DockPadding = RectangleF.TLRB(4);
			MDLevelWindow.HideNonCloseButtons();
			MDLevelWindow.Center();

			var txt = MDLevelWindow.Add<Textbox>();
			var list = MDLevelWindow.Add<ListView>();

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
			SecondOrderSystem animationSmoother = new SecondOrderSystem(4, 0.98f, 1f, 0);


			Panel imageCanvas = levelSelector.Add<Panel>();
			imageCanvas.Dock = Dock.Right;
			imageCanvas.Size = new Vector2F(320 - 36);
			imageCanvas.PaintOverride += delegate (Element self, float width, float height) {
				var c = song.GetCover();
				if (c == null) return;
				Graphics2D.SetDrawColor(255, 255, 255, 255);
				Graphics2D.SetTexture(c.Texture);
				var distance = 16;
				var size = new Vector2F(width - (distance * 2) - Math.Abs(animationSmoother.Update(currentAvgVolume) * 90));
				var offset = Graphics2D.Offset;
				Graphics2D.ResetDrawingOffset();
				Rlgl.PushMatrix();
				Rlgl.Translatef(offset.X + (width / 2), offset.Y + (height / 2), 0);
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

			Label bpm = levelSelector.Add<Label>();
			bpm.AutoSize = true;
			bpm.Text = $"BPM: {song.BPM}";
			bpm.TextSize = 18;
			bpm.Dock = Dock.Top;
			bpm.TextAlignment = Anchor.TopLeft;

			Label mapper = levelSelector.Add<Label>();
			mapper.AutoSize = true;
			mapper.Text = ""; // $"Level Designer: {song.LevelDesigner}";
			mapper.TextSize = 18;
			mapper.Dock = Dock.Top;
			mapper.TextAlignment = Anchor.TopLeft;

			CreateDifficulty(levelSelector, song, 4, song.Difficulty4);
			CreateDifficulty(levelSelector, song, 3, song.Difficulty3);
			CreateDifficulty(levelSelector, song, 2, song.Difficulty2);
			CreateDifficulty(levelSelector, song, 1, song.Difficulty1);

			MDLevelWindow?.AttachWindowAndLockInput(levelSelector);
		}
		private void Lvitem_MouseReleaseEvent(Element self, FrameState state, MouseButton button) {
			var song = self.GetTag<MuseDashSong>("musedash_song");

			LoadSongSelector(song);
		}

		private static void CreateDifficulty(Window levelSelector, ChartSong song, int difficulty, string difficultyLevel) {
			if (difficultyLevel == "") return;
			if (difficultyLevel == "0") return;

			Button play = levelSelector.Add<Button>();
			play.AutoSize = true;
			play.Dock = Dock.Bottom;

			var difficultyName = "";
			Color buttonColor = play.BackgroundColor;
			switch (difficulty) {
				case 1:
					difficultyName = "Easy";
					buttonColor = new Color(88, 199, 76, 60);
					break;
				case 2:
					difficultyName = "Normal";
					buttonColor = new Color(109, 196, 199, 60);
					break;
				case 3:
					difficultyName = "Hard";
					buttonColor = new Color(188, 95, 184, 60);
					break;
				case 4:
					difficultyName = "Hidden";
					buttonColor = new Color(199, 35, 35, 60);
					break;
			}
			play.BackgroundColor = buttonColor;
			play.ForegroundColor = buttonColor.Adjust(hue: 0, saturation: -0.5f, value: -0.4f);
			//play.Text = $"Play on {difficultyName} Mode [difficulty: {difficultyLevel}]";
			play.Text = "";
			play.TextAlignment = Anchor.CenterLeft;
			play.TextPadding = new(8, 0);
			play.TextSize = 28;

			play.BorderSize = 2;
			play.PaintOverride += delegate (Element self, float w, float h) {
				var btn = self as Button;
				btn.Paint(w, h);


				Vector2F textDrawingPosition = Anchor.GetPositionGivenAlignment(Anchor.CenterRight, btn.RenderBounds.Size, btn.TextPadding);
				Graphics2D.SetDrawColor(btn.TextColor);
				Graphics2D.DrawText(textDrawingPosition, $"{difficultyLevel}", btn.Font, btn.TextSize, Anchor.CenterRight);
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
				var sheet = song.GetSheet(difficulty);
				var lvl = new CD_GameLevel(sheet);
				EngineCore.LoadLevel(lvl, state.KeyboardState.AltDown);
			};
		}

		public override void PreRenderBackground(FrameState frameState) {
			base.PreRenderBackground(frameState);
		}
	}
}
