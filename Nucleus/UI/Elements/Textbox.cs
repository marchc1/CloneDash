using Nucleus.Core;
using Nucleus.Types;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
		protected override void OnThink(FrameState frameState) {
			if (Hovered)
				EngineCore.SetMouseCursor(MouseCursor.MOUSE_CURSOR_IBEAM);

		}

		public Caret Caret { get; private set; } = new Caret();
		protected override void Initialize() {
			base.Initialize();
			Text = "";

			KeyboardInputMarshal = new HoldingKeyboardInputMarshal();
		}
		public void SetText(string text) {
			Text = text;
			Caret.Set(Text, Text.Length);
		}
		public DateTime LastKeyboardInteraction { get; private set; } = DateTime.Now;

		public void DeleteSelection() {
			Text = Caret.RemoveStringSelection(Text);
			Caret.Pointer = Math.Clamp(Caret.End ?? Text.Length - 1, 0, Text.Length - 1);
			Caret.ClearSelection();
		}
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


			if (Caret.HasSelection) {
				var textSize = Graphics2D.GetTextSize(Text, Font, TextSize);
				var selectionStart = Graphics2D.GetTextSize(Text.Substring(0, Caret.Start ?? 0), Font, TextSize).X;
				var selectionSize = Graphics2D.GetTextSize(Text.Substring(Caret.Start ?? 0, Caret.End ?? 0), Font, TextSize);

				var selectionStartX = (width / 2) - (textSize.X / 2) + selectionStart;
				var selectionStartY = (height / 2) - (textSize.Y / 2);

				var padding = 2;
				selectionStartX -= padding;
				selectionStartY -= padding;

				var selectionSizeX = selectionSize.X + (padding * 2);
				var selectionSizeY = selectionSize.Y + (padding * 2);

				Graphics2D.SetDrawColor(170, 200, 255, 80);
				Graphics2D.DrawRectangle(selectionStartX, selectionStartY, selectionSizeX, selectionSizeY);
			}

			base.Paint(width, height);

			if (replaceTextForDrawing) {
				TextNocall = "";
				TextColor = colorStore;
			}

			if ((DateTime.Now - LastKeyboardInteraction).TotalSeconds % .666666 < 0.33333 && KeyboardFocused) {
				var textAtPointer = "";
				if (Text.Length > 0)
					textAtPointer = Text.Substring(0, Math.Min(Caret.Pointer, Text.Length));


				var textSize = Graphics2D.GetTextSize(Text, Font, TextSize);
				var textSizeAtCaret = Graphics2D.GetTextSize(textAtPointer, Font, TextSize);
				float x = 0;

				switch (TextAlignment.ToTextAlignment().horizontal.Alignment) {
					case 0: x = textSize.X + 4; break;
					case 1: x = ((width / 2) - (textSize.X / 2)) + textSizeAtCaret.X; break;
					case 2: x = width - textSize.X - 4; break;
				}

				x += 2;
				Graphics2D.SetDrawColor(240, 248, 255);
				Graphics2D.DrawLine(x, 4, x, height - 4);
			}
		}

		public override void MouseRelease(Element self, FrameState state, MouseButton button) {
			if (!ReadOnly) {
				DemandKeyboardFocus();
				var textSize = Graphics2D.GetTextSize(Text, Font, TextSize);
				var xStart = (self.RenderBounds.Width / 2) - (textSize.X / 2);
				var xWhere = self.GetMousePos().X - xStart;
				for (int i = 0; i < (Text ?? "").Length; i++) {
					var subtext = Text.Substring(0, i + 1);
					var subTextSize = Graphics2D.GetTextSize(subtext, Font, TextSize);
					if (subTextSize.X >= xWhere || i == Text.Length - 1) {
						Caret = new Caret() { Pointer = i + 1 };
						break;
					}
				}
			}
		}

		public override void KeyboardFocusLost(Element self, bool demanded) {
			base.KeyboardFocusLost(self, demanded);
			Caret.ClearSelection();
		}
		public override void KeyPressed(KeyboardState state, KeyboardKey key) {
			var vischar = state.GetKeyActionFromKey(key);
			if (vischar.Type == CharacterType.NoAction)
				return;

			if (Caret.HasSelection) {
				DeleteSelection();
			}

			LastKeyboardInteraction = DateTime.Now;
			if (vischar.Type == CharacterType.VisibleCharacter) {
				if (state.ControlDown) {
					switch (key.Key) {
						case 65:
							LastKeyboardInteraction = DateTime.Now;
							Caret.Set(Text, Text.Length, 0, Text.Length);
							break;
						case 67:
							if (!Caret.HasSelection) return;
							Clipboard.Text = Text.Substring(Caret.Start ?? 0, Caret.End ?? 0);
							Logs.Info("Copied to clipboard!");
							break;
						case 86:
							string txt = Clipboard.Text;
							if (Caret.HasSelection) {
								Text = Caret.RemoveStringSelection(Text);
								Caret.ClearSelection(Caret.Start);
							}

							Text = Text.Substring(0, Caret.Pointer) + txt + Text.Substring(Caret.Pointer);
							Caret = new Caret() { Pointer = Caret.Pointer + txt.Length };
							Logs.Info("Pasted from clipboard!");
							break;
					}
				}
				else {
					Text = Text.Substring(0, Caret.Pointer) + vischar + Text.Substring(Caret.Pointer);
					Caret.IncrementPointer(Text);
				}
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
						if (Caret.Pointer == 0) break;
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
					case CharacterType.Enter:
						KeyboardUnfocus();
						break;
				}
			}
		}
	}
}