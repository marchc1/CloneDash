using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using Raylib_cs;
using System.Text.RegularExpressions;

namespace Nucleus.UI
{

	public class TextEditorCaret
	{
		public int StartCol = 0;
		public int EndCol = 0;
		public int StartRow = 0;
		public int EndRow = 0;

		public TextEditorCaret() { }
		public TextEditorCaret Copy() => new(StartRow, StartCol, EndRow, EndCol);
		public TextEditorCaret(int row, int col) {
			StartRow = row;
			StartCol = col;
			EndRow = row;
			EndCol = col;
		}
		public TextEditorCaret(int row, int col, int eRow, int eCol) {
			StartRow = row;
			StartCol = col;
			EndRow = eRow;
			EndCol = eCol;
		}

		// Swizzling
		public TextEditorCaret SR_SC => new(StartRow, StartCol);
		public TextEditorCaret SR_EC => new(StartRow, EndCol);
		public TextEditorCaret SR_ER => new(StartRow, EndRow);
		public TextEditorCaret ER_SC => new(EndRow, StartCol);
		public TextEditorCaret ER_EC => new(EndRow, EndCol);
		public TextEditorCaret ER_SR => new(EndRow, StartRow);

		public int Row => StartRow;
		public int Column => StartCol;
	}

	public class TextEditor : Panel
	{
		public Panel? Editor { get; set; }
		public Panel? Gutter { get; set; }
		public Scrollbar? VScrollbar { get; set; }
		public Scrollbar? HScrollbar { get; set; }
		public TextEditorCaret Caret { get; set; } = new(0, 0);
		public SafeArray<string> Rows { get; set; } = [""];

		private bool _showGutter = true, _showDetails = true;

		public bool Multiline { get; set; } = true;
		public bool TriggerExecuteOnEnter { get; set; } = false;
		public bool Readonly { get; set; } = false;
		public bool ShowGutter {
			get => _showGutter;
			set {
				_showGutter = value;
				RefreshFromParams();
			}
		}
		public bool ShowDetails {
			get => _showDetails;
			set {
				_showDetails = value;
				RefreshFromParams();
			}
		}
		private void RefreshFromParams() {
			if (Gutter != null) {
				Gutter.Enabled = _showGutter;
				Gutter.Visible = _showGutter;
			}
			InvalidateLayout(true);
			InvalidateChildren(true, true);
		}


		public float TopRow { get; set; } = 0;
		public float LeftCol { get; set; } = 0;
		public float MaxVisibleRows { get; set; } = 0;
		public float MaxVisibleCols { get; set; } = 0;
		public float FontWidth { get; set; } = 0;
		public float FontHeight { get; set; } = 0;
		public float Width { get; set; } = 0;
		public float Height { get; set; } = 0;
		public float PaddingTop { get; set; } = 0;
		public float PaddingLeft { get; set; } = 0;
		public float CaretWidth { get; set; } = 0;
		public float GutterLength { get; set; } = 6;
		public float GutterPaddingRight { get; set; } = 2;

		public int CaretLeftHold { get; set; } = 0;
		public void UpdateCaretLeftHold() {
			CaretLeftHold = Caret.Column;
		}
		protected override void PerformLayout(float width, float height) {
			base.PerformLayout(width, height);
			this.Width = width;
			this.Height = height;

			var s = Graphics2D.GetTextSize("W", Font, TextSize);
			FontWidth = s.X;
			FontHeight = s.Y + 2;

			MaxVisibleRows = MathF.Floor(Height / FontHeight);
			MaxVisibleCols = MathF.Floor(Width / FontWidth);
			PaddingTop = FontHeight * 0.3f;
			PaddingLeft = FontWidth * 1f;
			CaretWidth = FontWidth * 0.2f;
			GutterPaddingRight = FontWidth * 1f;

			if (this.SetScrollS.HasValue) {
				this.SetScroll(SetScrollS.Value);
				this.SetScrollS = null;
			}

			//Logs.Debug($"MaxVisibleRows: {MaxVisibleRows}");
			//Logs.Debug($"MaxVisibleCols: {MaxVisibleCols}");
		}

		private SyntaxHighlighter __highlighter = SyntaxHighlighter.Blank;
		public SyntaxHighlighter Highlighter {
			get => __highlighter;
			set {
				__highlighter = value;
				OnEdit();
			}
		}

		protected override void Initialize() {
			Gutter = Add<Panel>();
			VScrollbar = Add<Scrollbar>();
			Editor = Add<Panel>();
			RefreshFromParams();
			//Editor.DockMargin = RectangleF.TLRB(6);
			//Gutter.DockMargin = RectangleF.TLRB(6);

			Gutter.Dock = Dock.Left;
			Gutter.Size = new(64, 0);
			Editor.Dock = Dock.Fill;

			VScrollbar.Dock = Dock.Right;
			VScrollbar.OnScrolled += Scrollbar_OnScrolled;

			Gutter.PaintOverride += Gutter_PaintOverride;

			Editor.MouseClickEvent += Editor_MouseClickEvent;
			Editor.MouseReleaseEvent += Editor_MouseReleaseEvent;
			Editor.MouseDragEvent += Editor_MouseDragEvent;
			Editor.MouseScrollEvent += Editor_MouseScrollEvent;
			Editor.KeyboardInputMarshal = new HoldingKeyboardInputMarshal();
			Editor.OnKeyPressed += Editor_OnKeyPressed;

			Editor.Keybinds.AddKeybind([KeyboardLayout.USA.LeftControl, KeyboardLayout.USA.A], () => {
				SetCaret(0, 0, Rows[Rows.Count - 1].Length, Rows.Count - 1);
			});
			Editor.Keybinds.AddKeybind([KeyboardLayout.USA.LeftControl, KeyboardLayout.USA.C], () => {
				Clipboard.Text = GetSelection();
			});
			Editor.Keybinds.AddKeybind([KeyboardLayout.USA.LeftControl, KeyboardLayout.USA.X], () => {
				if (Readonly) return;
				Clipboard.Text = GetSelection();
				DeleteSelection();
			});
			Editor.Keybinds.AddKeybind([KeyboardLayout.USA.LeftControl, KeyboardLayout.USA.V], () => {
				if (Readonly) return;
				InsertText(Clipboard.Text);
				OnEdit();
			});

			Editor.Keybinds.AddKeybind([KeyboardLayout.USA.LeftControl, KeyboardLayout.USA.Z], () => {
				if (Readonly) return;
				UndoBackwards();
			}); Editor.Keybinds.AddKeybind([KeyboardLayout.USA.LeftControl, KeyboardLayout.USA.Y], () => {
				if (Readonly) return;
				UndoForwards();
			});

			Editor.PaintOverride += Editor_PaintOverride;

			Editor.Thinking += Editor_Thinking;

			SetFont("Consolas", 16);
		}

		private void Scrollbar_OnScrolled(float value) {
			TopRow = Math.Clamp(value, 0, 1200000);
			ConsumeScrollEvent();
		}

		bool wasHovered = false;
		private void Editor_Thinking(Element self) {
			if (self.Hovered)
				EngineCore.SetMouseCursor(MouseCursor.MOUSE_CURSOR_IBEAM);
			else if (wasHovered)
				EngineCore.ResetMouseCursor();

			wasHovered = self.Hovered;
		}

		private void Editor_PaintOverride(Element self, float width, float height) {
			int i = 0;
			int row = (int)TopRow;

			BackgroundColor = self.KeyboardFocused ? new(20, 32, 25, 127) : new(20, 25, 32, 127);
			ForegroundColor = self.KeyboardFocused ? new(85, 110, 95, 255) : new(85, 95, 110, 255);

			while (i < MaxVisibleRows) {
				if (row >= Rows.Count)
					break;

				var content = Rows[row];

				var y = PaddingTop + ((i) * FontHeight);
				var x = PaddingLeft;

				TextEditorCaret t = GetCaretTopLeft(), b = GetCaretBottomRight();
				var inRange = NMath.InRange(row, t.Row, b.Row);

				Graphics2D.SetDrawColor((inRange && this.Editor.KeyboardFocused) ? new Color(42, 45, 50, 155) : i % 2 == 1 ? new Color(2, 5, 10, 155) : new Color(13, 16, 22, 155));
				Graphics2D.DrawRectangle(0, y, width, FontHeight);

				row += 1;
				i += 1;
			}

			i = 0;
			row = (int)TopRow;

			while (i < MaxVisibleRows) {
				if (row >= Rows.Count)
					break;

				var content = Rows[row];

				var y = PaddingTop + ((i) * FontHeight);
				var x = PaddingLeft;

				var textPos = new Vector2F(x, y);
				var xOffset = 0;
				string txt = "";
				foreach (var item in Highlighter.Rows[row] ?? []) {
					Graphics2D.SetDrawColor(item.Color);
					Graphics2D.DrawText(x + (xOffset * FontWidth), y + 2, item.Text, Font, TextSize);
					xOffset += item.Text.Length;
					txt += item.Text;
				}

				var whitespace = 0;
				for (int wc = 0; wc < txt.Length; wc++) {
					if (!char.IsWhiteSpace(txt[wc]))
						break;
					else
						whitespace += 1;
				}

				var whitespaceCounter = MathF.Ceiling((whitespace - 1f) / 4);

				Graphics2D.SetDrawColor(130, 135, 140, 60);
				for (int wi = 0; wi < whitespaceCounter; wi++) {
					var xV = (FontWidth * (wi) * 4) + (FontWidth * 1.5f);
					Graphics2D.DrawLine(new(xV, y), new(xV, y + FontHeight), 1);
				}

				row += 1;
				i += 1;
			}

			float aF = (1 - ((EngineCore.Level.CurtimeF * 3) % 1)) * 255.5f;
			byte a = (byte)(int)aF;
			if (this.Editor.KeyboardFocused) {
				Graphics2D.SetDrawColor(255, 255, 255, a);
				Graphics2D.DrawRectangle(PaddingLeft + (Caret.EndCol) * FontWidth, PaddingTop + (Caret.EndRow - TopRow) * FontHeight, CaretWidth, FontHeight);
				Graphics2D.SetDrawColor(255, 255, 255, a / 2);
				Graphics2D.DrawRectangle((PaddingLeft + (Caret.EndCol) * FontWidth) + CaretWidth, PaddingTop + (Caret.EndRow - TopRow) * FontHeight, CaretWidth, FontHeight);
			}
			if (HasSelection() && this.Editor.KeyboardFocused) {
				Graphics2D.SetDrawColor(97, 137, 200, 140);
				if (Caret.StartRow == (int)MathF.Max(Caret.EndRow, 0)) {
					int leftmost = (int)MathF.Min(Caret.StartCol, Caret.EndCol);
					int rightmost = (int)MathF.Max(Caret.StartCol, Caret.EndCol);

					Graphics2D.DrawRectangle(PaddingLeft + leftmost * FontWidth, PaddingTop + (Caret.EndRow - TopRow) * FontHeight, FontWidth * (rightmost - leftmost), FontHeight);
				}
				else {
					TextEditorCaret top = GetCaretTop(), bottom = GetCaretBottom();

					Graphics2D.DrawRectangle(PaddingLeft + FontWidth * (top.Column), PaddingTop + (top.Row - TopRow) * FontHeight, FontWidth * (Rows[top.Row].Length - top.Column), FontHeight);
					Graphics2D.DrawRectangle(PaddingLeft, PaddingTop + (bottom.Row - TopRow) * FontHeight, (bottom.Column) * FontWidth, FontHeight);

					for (int i2 = top.Row + 1; i2 < bottom.Row; i2++) {
						var absi = i2 - TopRow;
						var rowContent = Rows[i2];
						if (rowContent != null) {
							Graphics2D.DrawRectangle(PaddingLeft, PaddingTop + absi * FontHeight, FontWidth * rowContent.Length, FontHeight);
						}
					}
				}
			}

			if (_showDetails) {
				Graphics2D.SetDrawColor(Highlighter.Color);
				string linePart;
				if (Caret.StartRow != Caret.EndRow)
					linePart = $"selected: {Math.Min(Caret.StartRow, Caret.EndRow)} - {Math.Max(Caret.StartRow, Caret.EndRow)}";
				else
					linePart = $"line {Caret.StartRow} / {Rows.Count}";
				Graphics2D.DrawText(width - 4, height - 2, $"{StringExtensions.FormatNumberByThousands(chars)} chars - {linePart} - {Highlighter.Name}", Font, 14, Anchor.BottomRight);
			}

			if (BorderSize > 0) {
				Graphics2D.SetDrawColor(ForegroundColor);
				Graphics2D.DrawRectangleOutline(0, 0, width, height);
			}
		}

		private void Editor_MouseDragEvent(Element self, FrameState state, Vector2F delta) {
			if (doubleClicked) return;

			var pos = CalculatePos(state.MouseState.MousePos - self.GetGlobalPosition());
			Caret.EndCol = pos.Column;
			Caret.EndRow = pos.Row;
		}
		public TextEditorCaret CalculatePos(Vector2F p) => CalculatePos(p.X, p.Y);
		public TextEditorCaret CalculatePos(float Fx, float Fy) {
			//int x = (int)Fx, y = (int)Fy;

			Fx = MathF.Max(0, Fx - PaddingLeft) - 2;
			Fy = MathF.Max(0, Fy - PaddingTop);

			int col = (int)MathF.Round(Fx / FontWidth), row = (int)(MathF.Floor(Fy / FontHeight) + TopRow);
			if (row < Rows.Count) {
				var content = Rows[row];
				col = (int)MathF.Min(content.Length, col);

				return new(row, col);
			}
			else {
				for (int i = row; i < TopRow; i--) {
					if (i < Rows.Count) {
						return new(row, (int)MathF.Min(Rows[i].Length, col));
					}
				}

				return new(Rows.Count - 1, (Rows[Rows.Count - 1] ?? "").Length);
			}
		}

		public bool DoesLineOverflowTop(int line) {
			int currentRows = Rows.Count;
			int maxRows = (int)MaxVisibleRows;

			// cannot scroll anyway
			if (currentRows <= maxRows) {
				return false;
			}

			return line < TopRow;
		}

		public bool DoesLineOverflowBottom(int line) {
			int currentRows = Rows.Count;
			int maxRows = (int)MaxVisibleRows;

			// cannot scroll anyway
			if (currentRows <= maxRows) {
				return false;
			}

			return line > (TopRow + (maxRows - 1));
		}

		public void ScrollToLine(int line, float ratio) {
			int currentRows = Rows.Count;
			int maxRows = (int)MaxVisibleRows;

			// cannot scroll anyway
			if (currentRows <= maxRows) {
				TopRow = 0;
				return;
			}

			TopRow = Math.Max(0, line - (int)((maxRows - 1) * ratio));
		}
		public void MoveTopToLine(int line) => ScrollToLine(line, 0);
		public void MoveCenterToLine(int line) => ScrollToLine(line, 0.5f);
		public void MoveBottomToLine(int line) => ScrollToLine(line, 1);

		public enum LineOverflow {
			Above = -1,
			Within = 0,
			Below = 1
		}
		public enum SideOverflow {
			Left = -1,
			Within = 0,
			Right = 1
		}
		public LineOverflow GetLineOverflow(int targLine) {
			int topRow = (int)TopRow;
			int bottomRow = (int)(TopRow + MaxVisibleRows);

			if (NMath.InRange(targLine, topRow, bottomRow - 1))
				return LineOverflow.Within;

			return targLine < topRow ? LineOverflow.Above : LineOverflow.Below;
		}

		private void Editor_OnKeyPressed(Element self, KeyboardState state, Nucleus.Types.KeyboardKey key) {
			TextEditorCaret c = Caret;
			bool caretPointerOnTop = MathF.Min(c.StartRow, c.EndRow) == c.EndRow;
			int eRow = c.EndRow, eCol = c.EndCol;

			if (key == KeyboardLayout.USA.Right) {
				var bottom = GetCaretBottomRight();
				if (state.ShiftDown) {
					if (eCol <= Rows[eRow].Length - 1) {
						if (state.ControlDown) {
							string r = Rows[bottom.Row];
							int rlen = r.Length;
							Caret.EndCol = GetEndGroupRight(r, bottom.Column) ?? rlen + 1;
						}
						else {
							Caret.EndCol = Caret.EndCol + 1;
						}
					}
					else if (eRow < MaxVisibleRows && Rows[eRow + 1] != null) {
						Caret.EndCol = 1;
						Caret.EndRow += 1;
					}
				}
				else {
					if (bottom.Column < Rows[bottom.Row].Length) {
						if (state.ControlDown) {
							string r = Rows[bottom.Row];
							int rlen = r.Length;

							SetCaret(GetEndGroupRight(r, bottom.Column) ?? rlen + 1, bottom.Row);
						}
						else if (HasSelection()) SetCaret(bottom.Column, bottom.Row);
						else SetCaret(bottom.Column + 1, bottom.Row);
					}
					else if (bottom.Row < MaxVisibleRows && bottom.Row < Rows.Count - 1) SetCaret(0, bottom.Row + 1);

					else SetCaret(Rows[bottom.Row].Length, bottom.Row);
				}
				OnEdit();
				UpdateCaretLeftHold();
			}
			else if (key == KeyboardLayout.USA.Left) {
				var top = GetCaretTopLeft();
				if (state.ShiftDown) {
					if (eCol > 0) {
						if (state.ControlDown)
							Caret.EndCol = GetDeletionGroupLeft(Rows[top.Row] ?? "", top.Column) ?? 0;
						else
							Caret.EndCol -= 1;
					}
					else if (top.Row > 0) {
						if (top.Row <= TopRow)
							TopRow -= 1;

						Caret.EndCol = Rows[eRow - 1].Length;
						Caret.EndRow -= 1;
					}
				}
				else {
					if (top.Column > 0) {
						if (state.ControlDown) SetCaret(GetDeletionGroupLeft(Rows[top.Row], top.Column) ?? 0, top.Row);
						else if (HasSelection()) SetCaret(top.Column, top.Row);
						else SetCaret(top.Column - 1, top.Row);
					}
					else if (top.Row > 0) SetCaret(Rows[top.Row - 1].Length, top.Row - 1);
					else SetCaret(0, 0);
				}
				OnEdit();
				UpdateCaretLeftHold();
			}
			else if (key == KeyboardLayout.USA.Up) {
				var top = GetCaretTopLeft();
				if (state.ShiftDown) {
					if (eRow > 0) {
						Caret.EndCol = (int)MathF.Min(eCol, Rows[eRow - 1].Length);
						Caret.EndRow -= 1;
					}
					else {
						Caret.EndCol = 0;
						Caret.EndRow = 0;
					}

					if (DoesLineOverflowTop(Caret.EndRow)) MoveTopToLine(Caret.EndRow);
				}
				else {
					if (top.Row > 0)
						SetCaret((int)MathF.Min(top.Column, Rows[top.Row - 1].Length), top.Row - 1);
					else
						SetCaret(0, 0);

					if (DoesLineOverflowTop(Caret.Row)) MoveTopToLine(Caret.Row);
				}

				OnEdit();
			}
			else if (key == KeyboardLayout.USA.Down) {
				var bottom = GetCaretBottom();
				if (state.ShiftDown) {
					if (eRow < Rows.Count) {
						if (Rows[eRow + 1] != null) {
							Caret.EndCol = (int)MathF.Min(eCol, Rows[eRow + 1].Length);
							Caret.EndRow += 1;
						}
						else Caret.EndCol = Rows[eRow].Length;
					}

					if (DoesLineOverflowBottom(Caret.EndRow)) MoveBottomToLine(Caret.EndRow);
				}
				else {
					if (Rows[bottom.Row + 1] != null)
						SetCaret((int)MathF.Min(bottom.Column, Rows[bottom.Row + 1].Length), bottom.Row + 1);
					else
						SetCaret(Rows[bottom.Row].Length, bottom.Row);

					if (DoesLineOverflowBottom(Caret.Row)) MoveBottomToLine(Caret.Row);
				}
				OnEdit();
			}
			else if (key == KeyboardLayout.USA.Backspace && !DeleteSelection()) {
				if (Readonly) return;

				int col = Caret.EndCol, row = Caret.EndRow;
				string rowContent = Rows[row] ?? "";
				col = Math.Clamp(col, 0, rowContent.Length);
				if (col == 0) {
					if (row != 0) {
						var rest = Rows[row];
						if (rest == null) return;
						Rows.RemoveAt(row);
						SetCaret(Rows[row - 1].Length, row - 1);
						Rows[row - 1] = Rows[row - 1] + rest;
					}
				}
				else if (state.ControlDown) {
					int endColC = GetDeletionGroupLeft(rowContent, col) ?? throw new Exception();
					Rows[row] = rowContent.Substring(0, Math.Clamp(endColC, 0, rowContent.Length)) + rowContent.Substring(Math.Clamp(col, 0, rowContent.Length));
					SetCaret(endColC, row);
				}
				else {
					if (col > 3 && rowContent.Substring(col - 4, 4) == "    ") {
						Rows[row] = rowContent.Substring(0, col - 4) + rowContent.Substring(col);
						SetCaret(col - 4, row);
					}
					else {
						Rows[row] = rowContent.Substring(0, col - 1) + rowContent.Substring(col);
						SetCaret(col - 1, row);
					}
				}
				OnEdit();
			}
			else if (key == KeyboardLayout.USA.S && state.ControlDown) {
				OnSave?.Invoke(this);
				return;
			}
			else if (key == KeyboardLayout.USA.D && state.ControlDown) {
				if (Readonly) return;

				if (!HasSelection()) {
					int col = Caret.EndCol, row = Caret.EndRow;
					var rowContent = Rows[row] ?? "";
					Rows.Insert(row + 1, rowContent);
					SetCaret(col, row + 1);
					OnEdit();
				}
				return;
			}
			else if (key == KeyboardLayout.USA.P && state.AltDown) {
				OnExecute?.Invoke(this);
				return;
			}
			else if (key == KeyboardLayout.USA.Enter || key == KeyboardLayout.USA.NumpadEnter) {
				if (Readonly) return;

				if (Multiline) {
					bool deleted = DeleteSelection();

					int col = Caret.EndCol, row = Caret.EndRow;
					string? rowContent = Rows[row];

					Rows[row] = rowContent == null ? "" : rowContent.Substring(0, Math.Clamp(col, 0, rowContent.Length));

					if (!deleted) {
						string lastSpace = rowContent?.TrimStart(' ') ?? "";
						lastSpace = rowContent?.Substring(0, (rowContent?.Length ?? 0) - lastSpace.Length) ?? "";
						Rows.Insert(row + 1, lastSpace + rowContent?.Substring(Math.Clamp(col, 0, rowContent.Length)) ?? "");
						SetCaret(lastSpace.Length, row + 1);
					}
					else {
						Rows.Insert(row + 1, rowContent == null ? "" : rowContent.Substring(Math.Clamp(col, 0, rowContent.Length)));
						SetCaret(0, row + 1);
					}
					OnEdit();
				}
				else if (TriggerExecuteOnEnter) {
					OnExecute?.Invoke(this);
					SetText("");
					SetCaret(0, 0);
					return;
				}
				else {
					// We release focus instead.
					KeyboardUnfocus();
					return;
				}
			}
			else if (key == KeyboardLayout.USA.Delete && !DeleteSelection()) {
				if (Readonly) return;

				OnEdit();
			}
			var t = KeyboardLayout.USA.GetKeyAction(state, key);
			if (t.Type != CharacterType.VisibleCharacter && t.Type != CharacterType.Tab)
				return;

			if (Readonly) return;

			int endCol = 0, endRow = 0;
			string? add = null;

			if (t.Type == CharacterType.VisibleCharacter) {
				DeleteSelection();

				endCol = Caret.EndCol;
				endRow = Caret.EndRow;
				add = t.Extra;
			}
			else if (t.Type == CharacterType.Tab) {
				if (OnTab != null) {
					OnTab.Invoke(this);
					return;
				}
				if (!HasSelection()) {
					endCol = Caret.EndCol;
					endRow = Caret.EndRow;
					add = "    ";
				}
				else {
					var tl = GetCaretTopLeft();
					var br = GetCaretBottomRight();

					if (state.ShiftDown) {
						for (int i = tl.Row; i <= br.Row; i++) {
							for (int i2 = 0; i2 < 4; i2++) {
								if ((Rows[i]?.Length ?? 0) > 0 && Rows[i][0] == ' ')
									Rows[i] = Rows[i]?.Substring(1) ?? null;
								else break;
							}
						}

						SetCaret(Caret.StartCol - 4, Caret.StartRow, Caret.EndCol - 4, Caret.EndRow);
					}
					else {
						for (int i = tl.Row; i <= br.Row; i++)
							Rows[i] = "    " + Rows[i];

						SetCaret(Caret.StartCol + 4, Caret.StartRow, Caret.EndCol + 4, Caret.EndRow);
					}

					OnEdit();
					return;
				}
			}

			if (endRow >= Rows.Count) {
				if (add != null)
					Rows.Add(add);
			}
			else {
				var rowContent = Rows[endRow];
				var ec = Math.Clamp(endCol, 0, rowContent?.Length ?? 0);
				Rows[endRow] = rowContent.Substring(0, ec) + (add ?? "") + rowContent.Substring(ec);
			}

			SetCaret(endCol + (add != null ? add.Length : 0), endRow);
			OnEdit();
		}

		public delegate void OnVoid(TextEditor self);
		public event OnVoid? OnTextEdited;
		public event OnVoid? OnSave;
		public event OnVoid? OnExecute;
		public event OnVoid? OnTab;

		protected override void OnThink(FrameState frameState) {
			base.OnThink(frameState);
			VScrollbar.Scroll = TopRow;
			VScrollbar.Update(new(0, Rows.Count), new(0, MaxVisibleRows));
			VScrollbar.ScrollDelta = 3;
		}
		private void Editor_MouseScrollEvent(Element self, FrameState state, Vector2F delta) {
			var diff = TopRow - (delta.Y * 4);

			if (diff >= 0 && diff <= Rows.Count - MaxVisibleRows) {
				TopRow = diff;
			}
			else if (diff <= 0) {
				TopRow = 0;
			}
			else {
				TopRow = Rows.Count - MaxVisibleRows;
			}
			self.ConsumeScrollEvent();
		}

		private void Editor_MouseReleaseEvent(Element self, FrameState state, Nucleus.Types.MouseButton button) {

		}

		double LastPress;
		bool doubleClicked = false;
		private void Editor_MouseClickEvent(Element self, FrameState state, Nucleus.Types.MouseButton button) {
			if (button == Nucleus.Types.MouseButton.Mouse1) {
				self.DemandKeyboardFocus();

				Vector2F xy = self.CursorPos();
				TextEditorCaret pos = CalculatePos(xy.X, xy.Y);

				var now = EngineCore.Level.Curtime;
				doubleClicked = now - LastPress < 0.2;

				if (doubleClicked) {
					var content = Rows[pos.Row];

					var right = GetEndGroupRight(content, pos.Column) ?? pos.Column;
					var left = GetDeletionGroupLeft(content, pos.Column) ?? pos.Column;

					SetCaret(left, pos.Row, right, pos.Row);
				}
				else {
					SetCaret(pos.Column, pos.Row);
				}

				UpdateCaretLeftHold();

				LastPress = now;
			}
		}

		private int chars;

		public MaxStack<UndoFrame> Undos = new MaxStack<UndoFrame>(100);
		public Stack<UndoFrame> Redos = new Stack<UndoFrame>();

		public void UndoBackwards() {
			if (Undos.Count > 1) {
				Redos.Push(Undos.Pop());
				var t = Undos.Peek();
				Rows = new SafeArray<string>(t.Rows);
				Caret = t.Caret;
				OnEdit(false);
			}
		}
		public void UndoForwards() {
			if (Redos.Count == 0)
				return;

			var t = Redos.Pop();
			Undos.Push(t);
			Rows = new SafeArray<string>(t.Rows);
			Caret = t.Caret;
			OnEdit(false);
		}

		public void OnEdit(bool modifyUndoStack = true) {
			Highlighter.Rebuild(Rows);
			chars = 0;

			foreach (var row in Rows)
				chars += row.Length;

			if (modifyUndoStack) {
				Redos.Clear();
				Undos.Push(new() { Caret = Caret.Copy(), Rows = Rows.ToArray() });
			}
			OnTextEdited?.Invoke(this);


			if ((Caret.Row + 1) >= Rows.Count)
				MoveBottomToLine(Caret.Row);

			switch (GetLineOverflow(Caret.Row)) {
				case LineOverflow.Above:
					MoveTopToLine(Caret.Row);
					break;
				case LineOverflow.Below:
					MoveBottomToLine(Caret.Row);
					break;
			}
		}

		public void ResetUndoStack() {
			if (Redos.Count > 0)
				Redos.Clear();
			if (Undos.Count > 0)
				Undos.Clear();
			Undos.Push(new() { Caret = Caret.Copy(), Rows = Rows.ToArray() });
		}

		public void SetText(string? text) {
			text = text ?? "";
			Rows = new SafeArray<string>(text.Replace("\r", "").Replace("\t", "    ").Split("\n"));
			OnEdit();
		}
		public void RemoveLine(int index = 0) {
			Rows.RemoveAt(index);
			OnEdit();
		}
		public void AppendLine(string text) {
			if (Rows.Count > 0 && (Rows[Rows.Count - 1]?.Length ?? 0) <= 0)
				Rows[Rows.Count - 1] = text;
			else
				Rows.Add(text);
			OnEdit();
		}
		public string GetText() {
			return string.Join("\n", Rows);
		}
		public void SetCaret(int col, int row, int? endcolI = null, int? endrowI = null) {
			col = (int)MathF.Max(0, col);
			row = (int)MathF.Max(0, row);

			if (endcolI.HasValue) endcolI = (int)MathF.Max(0, endcolI.Value);
			if (endrowI.HasValue) endrowI = (int)MathF.Max(0, endrowI.Value);

			Caret.StartCol = col;
			Caret.EndCol = endcolI ?? col;
			Caret.StartRow = row;
			Caret.EndRow = endrowI ?? row;
		}
		private float? SetScrollS = null;
		public void SetScroll(float percent) {
			if (MaxVisibleRows == -1) {
				this.SetScrollS = percent;
				return;
			}
			if (percent <= 0)
				this.TopRow = 0;
			else if (percent >= 1)
				this.TopRow = (Rows.Count - MaxVisibleRows);
			else
				this.TopRow = (Rows.Count - MaxVisibleRows) * percent;
		}

		private static bool isDigit(char c) => c >= '0' && c <= '9';
		private static bool isHexDigit(char c) => isDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
		private static bool isIdentifierStartSymbol(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';

		public static int? GetDeletionGroupLeft(string row, int column) {
			char firstC = '\0';
			int i;
			for (i = column - 1; i > 0; i--) {
				char c = row[i];
				if (firstC == '\0') {
					if (!char.IsWhiteSpace(c) && c != ',')
						firstC = row[i];
				}
				else {
					bool isWhitespace = char.IsWhiteSpace(c);

					switch (firstC) {
						case ')':
						case '}':
						case ']':
							if (!isWhitespace)
								return i + 1;
							break;
						case '"':
							if (c == '"' && i > 0 && row[i - 1] != '\\')
								return i;
							break;
						case '(':
						case '{':
						case '[':
							return i + 1;
							break;
						default:
							// Read characters until reaching something else.
							if (isDigit(c) || isHexDigit(c) || isIdentifierStartSymbol(c)) {

							}
							else {
								// determine digit or identifier here
								var str = row.Substring(i + 1, column - i - 1);
								var isDigit = Regex.IsMatch(str, "(0[xX][0-9a-fA-F]+|(\\d+(\\.\\d*)?|\\.\\d+)([eE][+-]?\\d+)?|\\d+\\.\\d*)\\b");
								if (isDigit) {
									return i + 1;
								}
								else {
									switch (c) {
										case '[':
										case '(':
										case '{':
											return i + 1;
										case '"':
											return i;
										default:
											return i + 1;
									}
								}
							}
							break;
					}
				}
			}

			return i;
		}
		public static bool CharInRange(char input, char min, char max) => input >= min && input <= max;
		public static int? GetEndGroupRight(string row, int column) {
			int len = row.Length, ty = -1;

			for (int i = column; i < len; i++) {
				var c = row[i];
				if (c == ' ' || c == '(' || c == ',' || c == ')' || c == ':' || (c == '.' && i <= len - 1 && !char.IsDigit(row[i + 1]))) {
					return i;
				}
			}

			return len;
		}

		public TextEditorCaret GetCaretBottom() {
			if (Caret.EndRow > Caret.StartRow)
				return Caret.ER_EC;
			else
				return Caret.SR_SC;
		}
		public TextEditorCaret GetCaretBottomRight() {
			if (Caret.EndRow > Caret.StartRow) {
				return Caret.ER_EC;
			}
			else if (Caret.EndRow == Caret.StartRow) {
				return new(Caret.StartRow, (int)MathF.Max(Caret.StartCol, Caret.EndCol));
			}
			else {
				return Caret.SR_SC;
			}
		}
		public TextEditorCaret GetCaretTop() {
			if (Caret.EndRow < Caret.StartRow) {
				return Caret.ER_EC;
			}
			else {
				return Caret.SR_SC;
			}
		}
		public TextEditorCaret GetCaretTopLeft() {
			if (Caret.EndRow < Caret.StartRow)
				return Caret.ER_EC;
			else if (Caret.EndRow == Caret.StartRow)
				return new(Caret.StartRow, (int)MathF.Min(Caret.StartCol, Caret.EndCol));
			else
				return Caret.SR_SC;
		}
		public bool HasSelection() => Caret.StartCol != Caret.EndCol || Caret.StartRow != Caret.EndRow;

		public string GetSelection() {
			if (!HasSelection())
				return "";

			TextEditorCaret top = GetCaretTopLeft();
			TextEditorCaret bottom = GetCaretBottomRight();

			List<string> buf = [Rows[top.Row].Substring(top.Column)];

			if (top.Row != bottom.Row) {
				for (int i = top.Row + 1; i < bottom.Row; i++) {
					buf.Add(Rows[i]);
				}
			}
			else {
				return Rows[top.Row].Substring(top.Column, bottom.Column - top.Column);
			}

			buf.Add(Rows[bottom.Row].Substring(0, bottom.Column));
			OnEdit();
			return string.Join("\n", buf);
		}

		public bool InsertText(string txt) {
			if (txt == null)
				return false;

			if (HasSelection()) {
				var top = GetCaretTopLeft();
				var bottom = GetCaretBottomRight();

				var topPiece = Rows[top.Row].Substring(0, top.Column);
				var bottomPiece = Rows[bottom.Row].Substring(bottom.Column);

				var newLines = txt.Replace("\r", "").Replace("\t", "    ").Split('\n');

				Rows[top.Row] = topPiece;
				for (int i = top.Row + 1; i < bottom.Row + 1; i++) {
					Rows.RemoveAt(top.Row + 1);
				}
				int row = top.Row;
				for (int i = 0; i < newLines.Length - 1; i++) {
					Rows[row] += newLines[i];
					Rows.Insert(row + 1, "");
					row++;
				}
				Rows[row] += newLines.Last() + bottomPiece;

				SetCaret(Rows[row].Length - bottomPiece.Length, row);
			}
			else {
				var top = GetCaretTopLeft();
				var bottom = GetCaretBottomRight();

				var topPiece = Rows[Caret.Row].Substring(0, (int)MathF.Min(Rows[Caret.Row].Length, Caret.Column));
				var bottomPiece = Rows[Caret.Row].Substring((int)MathF.Min(Rows[Caret.Row].Length, Caret.Column));

				var newLines = txt.Replace("\r", "").Split('\n');

				Rows[top.Row] = topPiece;

				int row = top.Row;
				for (int i = 0; i < newLines.Length - 1; i++) {
					Rows[row] += newLines[i];
					Rows.Insert(row + 1, "");
					row++;
				}

				Rows[row] += newLines.Last() + bottomPiece;

				SetCaret(Caret.Column + newLines.Last().Length, row);
			}
			return true;
		}
		public bool DeleteSelection() {
			if (!HasSelection()) return false;

			var top = GetCaretTopLeft();
			var bottom = GetCaretBottomRight();

			Rows[top.Row] = Rows[top.Row].Substring(0, top.Column) + Rows[bottom.Row].Substring(bottom.Column);
			for (int i = top.Row + 1; i < bottom.Row + 1; i++) {
				Rows.RemoveAt(top.Row + 1);
			}

			SetCaret(top.Column, top.Row);
			OnEdit();
			return true;
		}

		public override void RequestKeyboardFocus() => Editor?.RequestKeyboardFocus();
		public override void DemandKeyboardFocus() => Editor?.DemandKeyboardFocus();
		public override void KeyboardUnfocus() => Editor?.KeyboardUnfocus();

		private void Gutter_PaintOverride(Element self, float width, float height) {
			Graphics2D.SetDrawColor(40, 50, 55, 155);
			Graphics2D.DrawRectangle(0, 0, width, height);

			int i = 0, row = (int)TopRow;
			while (i < MaxVisibleRows) {
				if (Rows[row] == null)
					break;

				var str = (row + 1).ToString();

				float y = PaddingTop + i * FontHeight;
				float x = ((width / FontWidth) - str.Length) * FontWidth - GutterPaddingRight;

				TextEditorCaret t = GetCaretTopLeft(), b = GetCaretBottomRight();
				var inRange = NMath.InRange(row, t.Row, b.Row);

				Graphics2D.SetDrawColor((inRange && this.Editor.KeyboardFocused) ? new Color(90, 100, 110, 155) : i % 2 == 1 ? new Color(4, 7, 10, 125) : new Color(24, 30, 40, 125));
				Graphics2D.DrawRectangle(0, y, width, FontHeight);

				row += 1;
				i += 1;
			}

			i = 0;
			row = (int)TopRow;

			while (i < MaxVisibleRows) {
				if (Rows[row] == null)
					break;

				var str = (row + 1).ToString();

				float y = PaddingTop + i * FontHeight;
				float x = ((width / FontWidth) - str.Length) * FontWidth - GutterPaddingRight;

				TextEditorCaret t = GetCaretTopLeft(), b = GetCaretBottomRight();
				var inRange = NMath.InRange(row, t.Row, b.Row);

				Graphics2D.SetDrawColor(220, 220, 220, 255);
				Graphics2D.DrawText(x, y + 2, str, Font, TextSize);

				row += 1;
				i += 1;
			}
		}

		public void SetFont(string fontName, int fontSize) {
			this.Font = fontName;
			this.TextSize = fontSize;

			var s = Graphics2D.GetTextSize("W", fontName, fontSize);
			FontWidth = s.X;
			FontHeight = s.Y;

			MaxVisibleRows = MathF.Floor(Height / FontHeight - 1);
			PaddingTop = FontHeight * 0.3f;
			PaddingLeft = FontWidth * 2f;
			CaretWidth = FontWidth * 0.2f;
			GutterPaddingRight = FontWidth * 1f;
		}

		public override void Paint(float width, float height) {
			Graphics2D.SetDrawColor(10, 15, 20, 190);
			Graphics2D.DrawRectangle(0, 0, width, height);
		}
	}
}
