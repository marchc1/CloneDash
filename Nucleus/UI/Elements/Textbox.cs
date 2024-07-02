using Nucleus.Core;
using Nucleus.Types;
using Raylib_cs;
using KeyboardKey = Nucleus.Types.KeyboardKey;
using MouseButton = Nucleus.Types.MouseButton;

namespace Nucleus.UI
{
    public class Caret
    {
        public int Pointer { get; set; } = 0;
        public int? Start { get; set; } = null;
        public int? End { get; set; } = null;


        public void IncrementPointer(string text) {
            Pointer++;
            clamp(text);
        }
        public void DecrementPointer(string text) {
            Pointer--;
            clamp(text);
        }

        private void clamp(string text) {
            Pointer = Math.Clamp(Pointer, 0, text.Length);
        }

        public string GetStringSelection(string text) {
            if (!Start.HasValue || !End.HasValue) return "";

            return text.Substring(Start.Value, End.Value);
        }

        public string RemoveStringSelection(string text) {
            if (!Start.HasValue || !End.HasValue) return text;

            string subL = text.Substring(0, Start.Value);
            string subR = text.Substring(Start.Value + End.Value, text.Length - (Start.Value + End.Value));
            return subL + subR;
        }

        public void Set(string text, int? pointer = null, int? start = null, int? end = null) {
            Pointer = pointer ?? Pointer;
            Start = start ?? Start;
            End = end ?? End;
            clamp(text);
        }

        public bool HasSelection => Start.HasValue && End.HasValue;
        public void ClearSelection(int? newPointer = null) {
            Start = null;
            End = null;
            Pointer = newPointer ?? Pointer;
        }
    }
    /// <summary>
    /// Needs a lot of work but it (kind of) gets the job done right now
    /// </summary>
    public class Textbox : Label
    {
        public string HelperText { get; set; } = "Textbox...";
        public bool MultiLine { get; set; } = false;
        private bool __readOnly = false;
        public bool ReadOnly {
            get => __readOnly;
            set {
                __readOnly = value;
                KeyboardUnfocus(); // not a forced unfocus so itll only unfocus when this is the selected element
            }
        }

        public Caret Caret { get; private set; } = new Caret();
        protected override void Initialize() {
            base.Initialize();
            Text = "";

            Keybinds.AddKeybind([KeyboardLayout.USA.LeftControl, KeyboardLayout.USA.A], delegate () {
                LastKeyboardInteraction = DateTime.Now;
                Caret.Set(Text, Text.Length, 0, Text.Length);
            });
        }

        public DateTime LastKeyboardInteraction { get; private set; } = DateTime.Now;

        public override void Paint(float width, float height) {
            BackgroundColor = KeyboardFocused ? new(20, 32, 25, 127) : new(20, 25, 32, 127);
            ForegroundColor = KeyboardFocused ? new(85, 110, 95, 255) : new(85, 95, 110, 255);

            Color back;
            Color fore;

            if (!ReadOnly) {
                back = MixColorBasedOnMouseState(this, BackgroundColor, new(0, 1.1f, 2.3f, 1f), new(0, 1.2f, 0.6f, 1f));
                fore = MixColorBasedOnMouseState(this, ForegroundColor, new(0, 1.1f, 1.3f, 1f), new(0, 1.2f, 0.6f, 1f));
            }
            else {
                back = BackgroundColor;
                fore = ForegroundColor;
            }
            Graphics2D.SetDrawColor(back);
            Graphics2D.DrawRectangle(0, 0, width, height);
            Graphics2D.SetDrawColor(fore);
            Graphics2D.DrawRectangleOutline(0, 0, width, height, 2);

            bool replaceTextForDrawing = false;
            if (Text == "")
                replaceTextForDrawing = true;

            var colorStore = TextColor;
            if (replaceTextForDrawing) {
                TextNocall = HelperText;
                TextColor = TextColor.Adjust(0, -0.1, -0.4);
            }

            base.Paint(width, height);

            if (replaceTextForDrawing) {
                TextNocall = "";
                TextColor = colorStore;
            }

            if ((DateTime.Now - LastKeyboardInteraction).TotalSeconds % 1 < 0.5 && KeyboardFocused) {
                var textAtPointer = "";
                if (Text.Length > 0)
                    textAtPointer = Text.Substring(0, Caret.Pointer);

                var textSize = Graphics2D.GetTextSize(Text, Font, TextSize);
                var textSizeAtCaret = Graphics2D.GetTextSize(textAtPointer, Font, TextSize);
                float x = 0;
                switch (TextAlignment.ToTextAlignment().horizontal.Alignment) {
                    case 0: x = textSize.X; break;
                    case 1: x = ((width / 2) - (textSize.X / 2)) + textSizeAtCaret.X; break;
                    case 2: x = width - textSize.X; break;
                }
                x += 1;
                Graphics2D.SetDrawColor(240, 248, 255);
                Graphics2D.DrawLine(x, 4, x, height - 4);

                if (Caret.HasSelection) {

                }
            }
        }

        public override void MouseRelease(Element self, FrameState state, MouseButton button) {
            if (!ReadOnly)
                DemandKeyboardFocus();
        }

        public override void KeyPressed(KeyboardState state, KeyboardKey key) {
            var vischar = state.GetKeyActionFromKey(key);
            if (vischar.Type == CharacterType.NoAction)
                return;

            LastKeyboardInteraction = DateTime.Now;
            if (vischar.Type == CharacterType.VisibleCharacter) {
                Text += vischar;
                Caret.IncrementPointer(Text);
            }
            else {
                switch (vischar.Type) {
                    case CharacterType.DeleteBackwards:
                        if (Text.Length == 0) return;
                        if (Caret.HasSelection) {
                            Text = Caret.RemoveStringSelection(Text);
                            Caret.ClearSelection(Caret.Start);
                            return;
                        }

                        var piece1 = Text.Substring(0, Caret.Pointer - 1);
                        var piece2 = Text.Substring(Caret.Pointer, Text.Length - Caret.Pointer);
                        Text = piece1 + piece2;
                        Caret.DecrementPointer(Text);

                        break;
                    case CharacterType.Arrow:
                        switch (vischar.Extra) {
                            case "LEFT":
                                Caret.DecrementPointer(Text);
                                break;
                            case "RIGHT":
                                Caret.IncrementPointer(Text);
                                break;
                        }
                        break;
                }
            }
        }
    }
}