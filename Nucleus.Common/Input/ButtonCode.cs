namespace Nucleus.Common.Input;

/// <summary>
/// Enumeration for generic key types. Can be used as a bitfield, if necessary.
/// </summary>
public enum CharacterType
{
	NoAction = 0,
	VisibleCharacter = 1 << 0,
	Delete = 1 << 1,
	DeleteBackwards = 1 << 2,
	DeleteForwards = 1 << 3,
	Enter = 1 << 4,
	Arrow = 1 << 5,
	Tab = 1 << 6,
	Control = 1 << 7,
	Shift = 1 << 8,
	Alt = 1 << 9,
	FunctionNumber = 1 << 10,
	Function = 1 << 11,
	Super = 1 << 12
}

public record struct ButtonAction(CharacterType Type, string? Extra = null)
{
	public static readonly ButtonAction Empty = new(0, null);

	public static implicit operator ButtonAction(CharacterType t) => new(t);
	public static implicit operator ButtonAction(string s) => new(CharacterType.VisibleCharacter, s);
	public static implicit operator string(ButtonAction s) => s.Extra ?? "?";
}

public enum ButtonCode : short
{
	Invalid = -1,
	None = 0,

	KeyFirst = 0,

	KeyNone = KeyFirst,
	Key0,
	Key1,
	Key2,
	Key3,
	Key4,
	Key5,
	Key6,
	Key7,
	Key8,
	Key9,
	KeyA,
	KeyB,
	KeyC,
	KeyD,
	KeyE,
	KeyF,
	KeyG,
	KeyH,
	KeyI,
	KeyJ,
	KeyK,
	KeyL,
	KeyM,
	KeyN,
	KeyO,
	KeyP,
	KeyQ,
	KeyR,
	KeyS,
	KeyT,
	KeyU,
	KeyV,
	KeyW,
	KeyX,
	KeyY,
	KeyZ,
	KeyPad0,
	KeyPad1,
	KeyPad2,
	KeyPad3,
	KeyPad4,
	KeyPad5,
	KeyPad6,
	KeyPad7,
	KeyPad8,
	KeyPad9,
	KeyPadDivide,
	KeyPadMultiply,
	KeyPadMinus,
	KeyPadPlus,
	KeyPadEnter,
	KeyPadEqual,
	KeyPadDecimal,
	KeyLeftBracket,
	KeyRightBracket,
	KeySemicolon,
	KeyApostrophe,
	KeyBackquote,
	KeyComma,
	KeyPeriod,
	KeySlash,
	KeyBackslash,
	KeyMinus,
	KeyEqual,
	KeyEnter,
	KeySpace,
	KeyBackspace,
	KeyTab,
	KeyCapsLock,
	KeyNumLock,
	KeyEscape,
	KeyPrintScreen,
	KeyScrollLock,
	KeyPause,
	KeyInsert,
	KeyDelete,
	KeyHome,
	KeyEnd,
	KeyPageUp,
	KeyPageDown,
	KeyBreak,
	KeyLeftShift,
	KeyRightShift,
	KeyLeftAlt,
	KeyRightAlt,
	KeyLeftControl,
	KeyRightControl,
	KeyLeftSuper,
	KeyRightSuper,
	KeyApp,
	KeyUp,
	KeyLeft,
	KeyDown,
	KeyRight,
	KeyF1,
	KeyF2,
	KeyF3,
	KeyF4,
	KeyF5,
	KeyF6,
	KeyF7,
	KeyF8,
	KeyF9,
	KeyF10,
	KeyF11,
	KeyF12,
	KeyCapsLockToggle,
	KeyNumLockToggle,
	KeyScrollLockToggle,

	KeyLast = KeyScrollLockToggle,
	KeyCount = KeyLast - KeyFirst + 1,

	// Mouse
	MouseFirst = KeyLast + 1,

	MouseLeft = MouseFirst,
	Mouse1 = MouseLeft,
	MouseRight,
	Mouse2 = MouseRight,
	MouseMiddle,
	Mouse3 = MouseMiddle,
	Mouse4,
	Mouse5,
	MouseWheelUp,
	MouseWheelDown,

	MouseLast = MouseWheelDown,
	MouseCount = MouseLast - MouseFirst + 1,
}

public static class ButtonCodeExtensions
{
	extension(int integer){
		public ButtonCode ToButtonCode() => (ButtonCode)integer;
	}

	extension(ButtonCode code)
	{
		public bool IsAlpha() => code >= ButtonCode.KeyA && code <= ButtonCode.KeyZ;
		public bool IsAlphaNumeric() => code >= ButtonCode.Key0 && code <= ButtonCode.KeyZ;
		public bool IsSpace() => code == ButtonCode.KeyEnter || code == ButtonCode.KeyTab || code == ButtonCode.KeySpace;
		public bool IsKeypad() => code >= ButtonCode.KeyPad0 && code <= ButtonCode.KeyPadDecimal;
		public bool IsPunctuation() => code >= ButtonCode.Key0 && code <= ButtonCode.KeySpace && !IsAlphaNumeric(code) && !IsSpace(code) && !IsKeypad(code);
		public bool IsKeyCode() => code >= ButtonCode.KeyFirst && code <= ButtonCode.KeyLast;
		public bool IsMouseCode() => code >= ButtonCode.MouseFirst && code <= ButtonCode.MouseLast;

		public string GetString() {
			return code switch {
				ButtonCode.Invalid => "Invalid",
				ButtonCode.None => "None",
				ButtonCode.Key0 => "0",
				ButtonCode.Key1 => "1",
				ButtonCode.Key2 => "2",
				ButtonCode.Key3 => "3",
				ButtonCode.Key4 => "4",
				ButtonCode.Key5 => "5",
				ButtonCode.Key6 => "6",
				ButtonCode.Key7 => "7",
				ButtonCode.Key8 => "8",
				ButtonCode.Key9 => "9",
				ButtonCode.KeyA => "A",
				ButtonCode.KeyB => "B",
				ButtonCode.KeyC => "C",
				ButtonCode.KeyD => "D",
				ButtonCode.KeyE => "E",
				ButtonCode.KeyF => "F",
				ButtonCode.KeyG => "G",
				ButtonCode.KeyH => "H",
				ButtonCode.KeyI => "I",
				ButtonCode.KeyJ => "J",
				ButtonCode.KeyK => "K",
				ButtonCode.KeyL => "L",
				ButtonCode.KeyM => "M",
				ButtonCode.KeyN => "N",
				ButtonCode.KeyO => "O",
				ButtonCode.KeyP => "P",
				ButtonCode.KeyQ => "Q",
				ButtonCode.KeyR => "R",
				ButtonCode.KeyS => "S",
				ButtonCode.KeyT => "T",
				ButtonCode.KeyU => "U",
				ButtonCode.KeyV => "V",
				ButtonCode.KeyW => "W",
				ButtonCode.KeyX => "X",
				ButtonCode.KeyY => "Y",
				ButtonCode.KeyZ => "Z",
				ButtonCode.KeyPad0 => "KeyPad0",
				ButtonCode.KeyPad1 => "KeyPad1",
				ButtonCode.KeyPad2 => "KeyPad2",
				ButtonCode.KeyPad3 => "KeyPad3",
				ButtonCode.KeyPad4 => "KeyPad4",
				ButtonCode.KeyPad5 => "KeyPad5",
				ButtonCode.KeyPad6 => "KeyPad6",
				ButtonCode.KeyPad7 => "KeyPad7",
				ButtonCode.KeyPad8 => "KeyPad8",
				ButtonCode.KeyPad9 => "KeyPad9",
				ButtonCode.KeyPadDivide => "KeyPadDivide",
				ButtonCode.KeyPadMultiply => "KeyPadMultiply",
				ButtonCode.KeyPadMinus => "KeyPadMinus",
				ButtonCode.KeyPadPlus => "KeyPadPlus",
				ButtonCode.KeyPadEnter => "KeyPadEnter",
				ButtonCode.KeyPadDecimal => "KeyPadDecimal",
				ButtonCode.KeyLeftBracket => "KeyLeftBracket",
				ButtonCode.KeyRightBracket => "KeyRightBracket",
				ButtonCode.KeySemicolon => "Semicolon",
				ButtonCode.KeyApostrophe => "Apostrophe",
				ButtonCode.KeyBackquote => "Backquote",
				ButtonCode.KeyComma => "Comma",
				ButtonCode.KeyPeriod => "Period",
				ButtonCode.KeySlash => "Slash",
				ButtonCode.KeyBackslash => "Backslash",
				ButtonCode.KeyMinus => "Minus",
				ButtonCode.KeyEqual => "Equal",
				ButtonCode.KeyEnter => "Enter",
				ButtonCode.KeySpace => "Space",
				ButtonCode.KeyBackspace => "Backspace",
				ButtonCode.KeyTab => "Tab",
				ButtonCode.KeyCapsLock => "CapsLock",
				ButtonCode.KeyNumLock => "NumLock",
				ButtonCode.KeyEscape => "Escape",
				ButtonCode.KeyScrollLock => "ScrollLock",
				ButtonCode.KeyInsert => "Insert",
				ButtonCode.KeyDelete => "Delete",
				ButtonCode.KeyHome => "Home",
				ButtonCode.KeyEnd => "End",
				ButtonCode.KeyPageUp => "PageUp",
				ButtonCode.KeyPageDown => "PageDown",
				ButtonCode.KeyBreak => "Break",
				ButtonCode.KeyLeftShift => "LeftShift",
				ButtonCode.KeyRightShift => "RightShift",
				ButtonCode.KeyLeftAlt => "LeftAlt",
				ButtonCode.KeyRightAlt => "RightAlt",
				ButtonCode.KeyLeftControl => "LeftControl",
				ButtonCode.KeyRightControl => "RightControl",
				ButtonCode.KeyLeftSuper => "LeftSuper",
				ButtonCode.KeyRightSuper => "RightSuper",
				ButtonCode.KeyApp => "App",
				ButtonCode.KeyUp => "Up",
				ButtonCode.KeyLeft => "Left",
				ButtonCode.KeyDown => "Down",
				ButtonCode.KeyRight => "Right",
				ButtonCode.KeyF1 => "F1",
				ButtonCode.KeyF2 => "F2",
				ButtonCode.KeyF3 => "F3",
				ButtonCode.KeyF4 => "F4",
				ButtonCode.KeyF5 => "F5",
				ButtonCode.KeyF6 => "F6",
				ButtonCode.KeyF7 => "F7",
				ButtonCode.KeyF8 => "F8",
				ButtonCode.KeyF9 => "F9",
				ButtonCode.KeyF10 => "F10",
				ButtonCode.KeyF11 => "F11",
				ButtonCode.KeyF12 => "F12",
				ButtonCode.KeyCapsLockToggle => "CapsLockToggle",
				ButtonCode.KeyNumLockToggle => "NumLockToggle",
				ButtonCode.KeyScrollLockToggle => "ScrollLockToggle",

				ButtonCode.MouseLeft => "MouseLeft",
				ButtonCode.MouseRight => "MouseRight",
				ButtonCode.MouseMiddle => "MouseMiddle",
				ButtonCode.Mouse4 => "Mouse4",
				ButtonCode.Mouse5 => "Mouse5",
				ButtonCode.MouseWheelUp => "MouseWheelUp",
				ButtonCode.MouseWheelDown => "MouseWheelDown",
				_ => "Unknown"
			};
		}

		public ButtonAction GetAction(bool ctrl = false, bool alt = false, bool shift = false, bool caps = false, bool numpad = false) {
			switch (code) {
				case ButtonCode.KeyApostrophe: return !(shift || caps) ? "'" : "\""; // Apostrophe
				case ButtonCode.KeyComma: return !(shift || caps) ? "," : "<"; // Comma
				case ButtonCode.KeyMinus: return !(shift || caps) ? "-" : "_"; // Minus
				case ButtonCode.KeyPeriod: return !(shift || caps) ? "." : ">"; // Period
				case ButtonCode.KeySlash: return !(shift || caps) ? "/" : "?"; // Slash
				case ButtonCode.Key0: return !(shift || caps) ? "0" : ")"; // Zero
				case ButtonCode.Key1: return !(shift || caps) ? "1" : "!"; // One
				case ButtonCode.Key2: return !(shift || caps) ? "2" : "@"; // Two
				case ButtonCode.Key3: return !(shift || caps) ? "3" : "#"; // Three
				case ButtonCode.Key4: return !(shift || caps) ? "4" : "$"; // Four
				case ButtonCode.Key5: return !(shift || caps) ? "5" : "%"; // Five
				case ButtonCode.Key6: return !(shift || caps) ? "6" : "^"; // Six
				case ButtonCode.Key7: return !(shift || caps) ? "7" : "&"; // Seven
				case ButtonCode.Key8: return !(shift || caps) ? "8" : "*"; // Eight
				case ButtonCode.Key9: return !(shift || caps) ? "9" : "("; // Nine
				case ButtonCode.KeySemicolon: return !(shift || caps) ? ";" : ":"; // Semicolon
				case ButtonCode.KeyEqual: return !(shift || caps) ? "=" : "+"; // Equal
				case ButtonCode.KeyA: return !(shift || caps) ? "a" : "A"; // A
				case ButtonCode.KeyB: return !(shift || caps) ? "b" : "B"; // B
				case ButtonCode.KeyC: return !(shift || caps) ? "c" : "C"; // C
				case ButtonCode.KeyD: return !(shift || caps) ? "d" : "D"; // D
				case ButtonCode.KeyE: return !(shift || caps) ? "e" : "E"; // E
				case ButtonCode.KeyF: return !(shift || caps) ? "f" : "F"; // F
				case ButtonCode.KeyG: return !(shift || caps) ? "g" : "G"; // G
				case ButtonCode.KeyH: return !(shift || caps) ? "h" : "H"; // H
				case ButtonCode.KeyI: return !(shift || caps) ? "i" : "I"; // I
				case ButtonCode.KeyJ: return !(shift || caps) ? "j" : "J"; // J
				case ButtonCode.KeyK: return !(shift || caps) ? "k" : "K"; // K
				case ButtonCode.KeyL: return !(shift || caps) ? "l" : "L"; // L
				case ButtonCode.KeyM: return !(shift || caps) ? "m" : "M"; // M
				case ButtonCode.KeyN: return !(shift || caps) ? "n" : "N"; // N
				case ButtonCode.KeyO: return !(shift || caps) ? "o" : "O"; // O
				case ButtonCode.KeyP: return !(shift || caps) ? "p" : "P"; // P
				case ButtonCode.KeyQ: return !(shift || caps) ? "q" : "Q"; // Q
				case ButtonCode.KeyR: return !(shift || caps) ? "r" : "R"; // R
				case ButtonCode.KeyS: return !(shift || caps) ? "s" : "S"; // S
				case ButtonCode.KeyT: return !(shift || caps) ? "t" : "T"; // T
				case ButtonCode.KeyU: return !(shift || caps) ? "u" : "U"; // U
				case ButtonCode.KeyV: return !(shift || caps) ? "v" : "V"; // V
				case ButtonCode.KeyW: return !(shift || caps) ? "w" : "W"; // W
				case ButtonCode.KeyX: return !(shift || caps) ? "x" : "X"; // X
				case ButtonCode.KeyY: return !(shift || caps) ? "y" : "Y"; // Y
				case ButtonCode.KeyZ: return !(shift || caps) ? "z" : "Z"; // Z
				case ButtonCode.KeySpace: return " ";
				case ButtonCode.KeyLeftBracket: return !(shift || caps) ? "[" : "{"; // LeftBracket
				case ButtonCode.KeyBackslash: return !(shift || caps) ? "\\" : "|"; // Backslash
				case ButtonCode.KeyRightBracket: return !(shift || caps) ? "]" : "}"; // RightBracket
				case ButtonCode.KeyBackquote: return !(shift || caps) ? "`" : "~"; // Grave

				case ButtonCode.KeyEnter: return new(CharacterType.Enter, "Enter");
				case ButtonCode.KeyTab: return new(CharacterType.Tab, "Tab");
				case ButtonCode.KeyBackspace: return CharacterType.DeleteBackwards; // Numpad0
				case ButtonCode.KeyDelete: return CharacterType.DeleteForwards; // Numpad0

				case ButtonCode.KeyRight: return new(CharacterType.Arrow, "RIGHT");
				case ButtonCode.KeyLeft: return new(CharacterType.Arrow, "LEFT");
				case ButtonCode.KeyDown: return new(CharacterType.Arrow, "DOWN");
				case ButtonCode.KeyUp: return new(CharacterType.Arrow, "UP");

				case ButtonCode.KeyLeftShift: return new(CharacterType.Shift, "LEFT");
				case ButtonCode.KeyLeftControl: return new(CharacterType.Control, "LEFT");
				case ButtonCode.KeyLeftAlt: return new(CharacterType.Alt, "LEFT");
				case ButtonCode.KeyLeftSuper: return new(CharacterType.Super, "LEFT");

				case ButtonCode.KeyRightShift: return new(CharacterType.Shift, "RIGHT");
				case ButtonCode.KeyRightControl: return new(CharacterType.Control, "RIGHT");
				case ButtonCode.KeyRightAlt: return new(CharacterType.Alt, "RIGHT");
				case ButtonCode.KeyRightSuper: return new(CharacterType.Super, "RIGHT");

				case ButtonCode.KeyPad0: return numpad ? "0" : ButtonAction.Empty; // Numpad0
				case ButtonCode.KeyPad1: return numpad ? "1" : ButtonAction.Empty; // Numpad1
				case ButtonCode.KeyPad2: return numpad ? "2" : ButtonAction.Empty; // Numpad2
				case ButtonCode.KeyPad3: return numpad ? "3" : ButtonAction.Empty; // Numpad3
				case ButtonCode.KeyPad4: return numpad ? "4" : ButtonAction.Empty; // Numpad4
				case ButtonCode.KeyPad5: return numpad ? "5" : ButtonAction.Empty; // Numpad5
				case ButtonCode.KeyPad6: return numpad ? "6" : ButtonAction.Empty; // Numpad6
				case ButtonCode.KeyPad7: return numpad ? "7" : ButtonAction.Empty; // Numpad7
				case ButtonCode.KeyPad8: return numpad ? "8" : ButtonAction.Empty; // Numpad8
				case ButtonCode.KeyPad9: return numpad ? "9" : ButtonAction.Empty; // Numpad9
				case ButtonCode.KeyPadDecimal: return numpad ? "." : ButtonAction.Empty; // NumpadDecimal
				case ButtonCode.KeyPadDivide: return numpad ? "/" : ButtonAction.Empty; // NumpadDivide
				case ButtonCode.KeyPadMultiply: return numpad ? "*" : ButtonAction.Empty; // NumpadMultiply
				case ButtonCode.KeyPadMinus: return numpad ? "-" : ButtonAction.Empty; // NumpadSubtract
				case ButtonCode.KeyPadPlus: return numpad ? "+" : ButtonAction.Empty; // NumpadAdd
				case ButtonCode.KeyPadEqual: return numpad ? "=" : ButtonAction.Empty; // NumpadEqual
			}

			return CharacterType.NoAction;
		}

	}
}