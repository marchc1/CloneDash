using Nucleus;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Raylib_cs;
using static AssetStudio.BundleFile;
using static System.Net.Mime.MediaTypeNames;
using MouseButton = Nucleus.Types.MouseButton;
using CloneDash.Game.Sheets;
using CloneDash.Systems;
using CloneDash.Levels;

namespace CloneDash.Game
{
    public class CD_MainMenu : Level
    {
        public override void Initialize(params object[] args) {
            var header = UI.Add<Panel>();
            header.Position = new Vector2F(0);
            header.Size = new Vector2F(256, 48);
            header.Dock = Dock.Top;

            var loadMDLevel = header.Add<Button>();
            loadMDLevel.AutoSize = true;
            loadMDLevel.Dock = Dock.Right;
            loadMDLevel.Text = "Load Muse Dash Level";
            loadMDLevel.TextSize = 21;
            loadMDLevel.DockMargin = RectangleF.TLRB(4);
            loadMDLevel.MouseReleaseEvent += LoadMDLevel_MouseReleaseEvent;

            var loadModelViewer = header.Add<Button>();
            loadModelViewer.AutoSize = true;
            loadModelViewer.Dock = Dock.Right;
            loadModelViewer.Text = "Load Model Editor";
            loadModelViewer.TextSize = 21;
            loadModelViewer.DockMargin = RectangleF.TLRB(4);
            loadModelViewer.MouseReleaseEvent += LoadModelViewer_MouseReleaseEvent;

            var test2 = header.Add<Label>();
            test2.Size = new Vector2F(158, 32);
            test2.Dock = Dock.Left;
            test2.Text = "Clone Dash [Alpha]";
            test2.TextSize = 30;
            test2.AutoSize = true;
            test2.DockMargin = RectangleF.TLRB(4);
        }

        private void LoadModelViewer_MouseReleaseEvent(Element self, FrameState state, MouseButton button) {
            EngineCore.LoadLevel(new CD_ModelEditor());
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
            MDLevelWindow.Size = new Vector2F(360, 600);
            MDLevelWindow.DockPadding = RectangleF.TLRB(4);
            MDLevelWindow.HideNonCloseButtons();
            MDLevelWindow.Center();

            var txt = MDLevelWindow.Add<Textbox>();
            var list = MDLevelWindow.Add<ListView>();
            
            txt.Dock = Dock.Top;
            txt.HelperText = "Filter by Level Name...";
            txt.TextChangedEvent += delegate (Element self, string oldT, string newT) {
                foreach(Element e in list.MainPanel.GetChildren()) {
                    ListViewItem item = e as ListViewItem;
                    var name = item.GetTag<MuseDashCompatibility.MuseDashSong>("musedash_song").Name;
                    if (name.ToLower().Contains(txt.Text.ToLower()))
                        item.ShowLVItem = true;
                    else
                        item.ShowLVItem = false;
                }
                list.InvalidateChildren(self: true, recursive: true);
            };

            list.Dock = Dock.Fill;

            foreach (var item in MuseDashCompatibility.Songs) {
                var lvitem = list.Add<ListViewItem>();

                lvitem.SetTag("musedash_song", item);
                lvitem.Text = $"\"{item.Name}\" by {item.Author}";
                lvitem.MouseReleaseEvent += Lvitem_MouseReleaseEvent;
            }
        }

        private void Lvitem_MouseReleaseEvent(Element self, FrameState state, MouseButton button) {
            var song = self.GetTag<MuseDashCompatibility.MuseDashSong>("musedash_song");

            // Load all slow-to-get info now before the Window loads
            MusicTrack track = song.GetDemoMusic();
            song.GetCover();

            Window levelSelector = UI.Add<Window>();
            levelSelector.HideNonCloseButtons();
            levelSelector.MakePopup();
            levelSelector.Title = $"\"{song.Name}\" by {song.Author} - Level Selection";
            //test.Title = "Non-Rendertexture Window";
            levelSelector.Size = new Vector2F(650, 320);
            levelSelector.DockPadding = RectangleF.TLRB(8);
            levelSelector.Center();
            
            track.Playhead = 0;
            track.Volume = 0.4f;
            track.Playing = true;
            levelSelector.Thinking += delegate (Element self) {
                track.Update();
            };

            levelSelector.Removed += delegate (Element self) {
                track.Paused = true;
            };

            Panel imageCanvas = levelSelector.Add<Panel>();
            imageCanvas.Dock = Dock.Right;
            imageCanvas.Size = new Vector2F(320 - 36);
            imageCanvas.PaintOverride += delegate (Element self, float width, float height) {
                Graphics2D.SetDrawColor(255, 255, 255, 255);
                Graphics2D.SetTexture(song.GetCover());
                var distance = 16;
                Graphics2D.DrawImage(new(distance, distance), new(width - (distance * 2), height - (distance * 2)));
            };

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

            MDLevelWindow.AttachWindowAndLockInput(levelSelector);
            levelSelector.Birth = DateTime.Now;
        }

        private static void CreateDifficulty(Window levelSelector, MuseDashCompatibility.MuseDashSong song, int difficulty, string difficultyLevel) {
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
            play.Text = $"Play on {difficultyName} Mode [difficulty: {difficultyLevel}]";

            play.Thinking += delegate (Element self) {
                if (EngineCore.CurrentFrameState.KeyboardState.AltDown)
                    play.Text = $"[AUTOPLAY] Play on {difficultyName} Mode [difficulty: {difficultyLevel}]";
                else
                    play.Text = $"Play on {difficultyName} Mode [difficulty: {difficultyLevel}]";
            };

            play.MouseReleaseEvent += delegate (Element self, FrameState state, MouseButton button) {
                var sheet = song.GetDashSheet(difficulty);
                var lvl = new CD_GameLevel(sheet);
                EngineCore.LoadLevel(lvl, state.KeyboardState.AltDown);
            };
        }

        public override void PreRenderBackground(FrameState frameState) {
            base.PreRenderBackground(frameState);
        }
    }
}
