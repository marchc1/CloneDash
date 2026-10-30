using Nucleus.Common.Input;
using Nucleus.Core;
using Nucleus.Input;

using System.Diagnostics;

namespace Nucleus.UI;

/// <summary>
/// Caret blinking, solid right after anything happens
/// </summary>
public class CaretBlink
{
	long stamp = Stopwatch.GetTimestamp();
	public static double BlinkTime = 0.53;

	public void Reset() => stamp = Stopwatch.GetTimestamp();
	public bool Visible => Stopwatch.GetElapsedTime(stamp).TotalSeconds % (BlinkTime * 2) < BlinkTime;
}

public class TextEditingInput(TextEditingCore core)
{
	public TextEditingCore Core => core;
	public readonly CaretBlink Blink = new();

	// Alt/Ctrl shortcuts can still produce text input on some setups, eat it for the frame.
	bool swallowText;

	/// <summary>
	/// Has to run once a frame
	/// </summary>
	public void EndFrame() => swallowText = false;

	public bool KeyPressed(in KeyboardState state, ButtonCode key) {
		bool handled = HandleKey(in state, key);
		if (handled) {
			Blink.Reset();
			if (state.ControlDown || state.AltDown)
				swallowText = true;
		}
		return handled;
	}

	public bool TextInput(in KeyboardState state, string text) {
		if (swallowText)
			return true;
		core.Type(text);
		Blink.Reset();
		return true;
	}

	private static TextMovement? ArrowMovement(ButtonCode key, bool ctrl) => key switch {
		ButtonCode.KeyLeft => ctrl ? TextMovement.WordLeft : TextMovement.Left,
		ButtonCode.KeyRight => ctrl ? TextMovement.WordRight : TextMovement.Right,
		ButtonCode.KeyUp => TextMovement.Up,
		ButtonCode.KeyDown => TextMovement.Down,
		_ => null
	};

	private bool HandleKey(in KeyboardState state, ButtonCode key) {
		bool ctrl = state.ControlDown;
		bool shift = state.ShiftDown;
		bool alt = state.AltDown;
		bool vertical = key is ButtonCode.KeyUp or ButtonCode.KeyDown;

		switch (key) {
			case ButtonCode.KeyLeft:
			case ButtonCode.KeyRight:
			case ButtonCode.KeyUp:
			case ButtonCode.KeyDown: {
					if (alt && shift && !ctrl) {
						core.ExtendBox(ArrowMovement(key, false)!.Value);
						return true;
					}
					if (ctrl && alt && vertical) {
						core.AddCaretVertical(key == ButtonCode.KeyUp ? -1 : 1);
						return true;
					}
					if (alt && !ctrl && vertical) {
						if (!core.MultiLine) return false;
						core.MoveLines(key == ButtonCode.KeyUp ? -1 : 1);
						return true;
					}
					core.Move(ArrowMovement(key, ctrl)!.Value, shift);
					return true;
				}
			case ButtonCode.KeyHome: core.Move(ctrl ? TextMovement.DocumentStart : TextMovement.Home, shift); return true;
			case ButtonCode.KeyEnd: core.Move(ctrl ? TextMovement.DocumentEnd : TextMovement.End, shift); return true;
			case ButtonCode.KeyPageUp: core.Move(TextMovement.PageUp, shift); return true;
			case ButtonCode.KeyPageDown: core.Move(TextMovement.PageDown, shift); return true;

			case ButtonCode.KeyBackspace: core.Backspace(ctrl); return true;
			case ButtonCode.KeyDelete:
				if (shift && !ctrl) {
					// Shift+Del cuts, like VS
					var cut = core.Cut();
					if (cut != null)
						Clipboard.Text = cut;
					return true;
				}
				core.Delete(ctrl);
				return true;

			case ButtonCode.KeyEnter:
			case ButtonCode.KeyPadEnter:
				if (!core.MultiLine) return false;
				if (ctrl)
					core.InsertLine(below: shift);
				else
					core.NewLine();
				return true;

			case ButtonCode.KeyTab:
				if (!core.MultiLine || ctrl || alt) return false;
				core.Tab(shift);
				return true;

			case ButtonCode.KeyEscape:
				return core.CollapseCarets();
		}

		if (ctrl && !alt) {
			switch (key) {
				case ButtonCode.KeyA: core.SelectAll(); return true;
				case ButtonCode.KeyC: {
						var text = core.Copy();
						if (text != null)
							Clipboard.Text = text;
						return true;
					}
				case ButtonCode.KeyX: {
						var text = core.Cut();
						if (text != null)
							Clipboard.Text = text;
						return true;
					}
				case ButtonCode.KeyV: core.Paste(Clipboard.Text); return true;
				case ButtonCode.KeyZ: if (shift) core.Redo(); else core.Undo(); return true;
				case ButtonCode.KeyY: core.Redo(); return true;
				case ButtonCode.KeyD: core.Duplicate(); return true;
				case ButtonCode.KeyL when shift: core.DeleteLines(); return true;
			}
		}

		if (alt && shift && !ctrl) {
			switch (key) {
				case ButtonCode.KeyPeriod: core.AddNextOccurrence(); return true;
				case ButtonCode.KeySemicolon: core.SelectAllOccurrences(); return true;
			}
		}

		return false;
	}

	// Mouse

	enum DragUnit { Character, Word, Line }

	public static double DoubleClickTime = 0.5;

	int clickCount;
	long lastClickStamp;
	TextPosition lastClickPos;
	DragUnit unit;
	TextPosition rangeStart, rangeEnd;
	bool boxDrag;
	bool addingCaret;
	int boxAnchorLine;
	float boxAnchorX;

	public int ClickCount => clickCount;

	/// <summary>
	/// Mouse down at <paramref name="position"/>, handles alt/ctrl/shift and double/triple clicks
	/// </summary>
	public void PointerDown(TextPosition position, bool shift, bool ctrl, bool alt) {
		Blink.Reset();
		position = core.Document.Clamp(position);

		bool quick = Stopwatch.GetElapsedTime(lastClickStamp).TotalSeconds < DoubleClickTime
			&& position.Line == lastClickPos.Line && Math.Abs(position.Column - lastClickPos.Column) <= 1;
		clickCount = quick ? Math.Min(clickCount + 1, 3) : 1;
		lastClickStamp = Stopwatch.GetTimestamp();
		lastClickPos = position;

		boxDrag = false;
		addingCaret = false;
		unit = DragUnit.Character;

		if (alt && !ctrl && core.Layout != null) {
			// Shift+Alt boxes from where the caret already was
			if (shift)
				StartBox(core.Primary.Anchor, position, core.Layout.GetX(position));
			else {
				StartBox(position, position, core.Layout.GetX(position));
				core.SetCaret(position);
			}
			return;
		}
		if (ctrl && alt) {
			addingCaret = true;
			core.AddCaret(position);
			return;
		}
		if (shift) {
			core.SetSelection(new(core.Primary.Anchor, position));
			return;
		}

		switch (clickCount) {
			case 2:
				unit = DragUnit.Word;
				var word = core.WordRangeAt(position);
				rangeStart = word.Start;
				rangeEnd = word.End;
				core.SetSelection(new(rangeStart, rangeEnd));
				break;
			case 3:
				unit = DragUnit.Line;
				var line = core.LineRangeAt(position);
				rangeStart = line.Start;
				rangeEnd = line.End;
				core.SetSelection(new(rangeStart, rangeEnd));
				break;
			default:
				core.SetCaret(position);
				break;
		}
	}

	private void StartBox(TextPosition anchor, TextPosition caret, float caretX) {
		boxDrag = true;
		boxAnchorLine = anchor.Line;
		boxAnchorX = core.Layout!.GetX(anchor);
		if (anchor != caret)
			core.BoxSelect(boxAnchorLine, boxAnchorX, caret.Line, caretX);
	}

	/// <summary>
	/// Mouse dragged to <paramref name="position"/>, <paramref name="x"/> is only for box selection
	/// </summary>
	public void PointerDrag(TextPosition position, float x, bool alt = false) {
		Blink.Reset();
		position = core.Document.Clamp(position);

		if (!boxDrag && !addingCaret && alt && unit == DragUnit.Character && core.Layout != null)
			StartBox(core.Primary.Anchor, position, x);

		if (boxDrag) {
			core.BoxSelect(boxAnchorLine, boxAnchorX, position.Line, x);
			return;
		}

		var selection = core.Primary;
		switch (unit) {
			case DragUnit.Character:
				core.SetPrimary(new(selection.Anchor, position));
				break;
			case DragUnit.Word:
			case DragUnit.Line: {
					var range = unit == DragUnit.Word ? core.WordRangeAt(position) : core.LineRangeAt(position);
					if (range.Start < rangeStart)
						core.SetPrimary(new(rangeEnd, range.Start));
					else
						core.SetPrimary(new(rangeStart, TextPosition.Max(range.End, rangeEnd)));
					break;
				}
		}
	}
}
