using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// All of the stuff to support "custom keyboard layouts" is probably organized horribly and needs work
// Help would be appreciated from someone with more experience with this kind of thing

namespace Nucleus.Types
{
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
    public record KeyAction(CharacterType Type, string? Extra = null)
    {
        public static implicit operator KeyAction(CharacterType t) => new KeyAction(t);
        public static implicit operator KeyAction(string s) => new KeyAction(CharacterType.VisibleCharacter, s);
        public static implicit operator string(KeyAction s) => s.Extra ?? "?";
    }
    public record KeyboardKey(string Name, int Key);
    public abstract class KeyboardLayout
    {
        public static USKeyboard USA { get; private set; } = new();

        private Dictionary<string, int> KeyStrToKey = new();
        private Dictionary<int, string> KeyToKeyStr = new();
        private Dictionary<string, KeyboardKey> KeyStrToNamedKey = new();
        private Dictionary<int, KeyboardKey> KeyToNamedKey = new();

        private List<int> __keys = [];
        private List<KeyboardKey> __namedKeys = [];

        public int[] Keys => __keys.ToArray();
        public KeyboardKey[] NamedKeys => __namedKeys.ToArray();

        public KeyboardKey AssignKey(string name, int key) {
            if (KeyToKeyStr.ContainsKey(key))
                throw new Exception($"This key already exists. {key}");
            KeyStrToKey[name] = key;
            KeyToKeyStr[key] = name;
            var ret = new KeyboardKey(name, key);
            KeyStrToNamedKey[name] = ret;
            KeyToNamedKey[key] = ret;
            __keys.Add(key);
            __namedKeys.Add(ret);
            return ret;
        }

        public KeyboardKey FromInt(int key) => KeyToNamedKey[key];
        public KeyboardKey FromKeyStr(string key) => KeyStrToNamedKey[key];

        public abstract KeyAction GetKeyAction(KeyboardState state, KeyboardKey key);
        public abstract bool ControlDown(KeyboardState state);
        public abstract bool AltDown(KeyboardState state);
        public abstract bool ShiftDown(KeyboardState state);
    }

    public class USKeyboard : KeyboardLayout
    {
        public KeyboardKey Apostrophe { get; private set; }
        public KeyboardKey Comma { get; private set; }
        public KeyboardKey Minus { get; private set; }
        public KeyboardKey Period { get; private set; }
        public KeyboardKey Slash { get; private set; }
        public KeyboardKey Zero { get; private set; }
        public KeyboardKey One { get; private set; }
        public KeyboardKey Two { get; private set; }
        public KeyboardKey Three { get; private set; }
        public KeyboardKey Four { get; private set; }
        public KeyboardKey Five { get; private set; }
        public KeyboardKey Six { get; private set; }
        public KeyboardKey Seven { get; private set; }
        public KeyboardKey Eight { get; private set; }
        public KeyboardKey Nine { get; private set; }
        public KeyboardKey Semicolon { get; private set; }
        public KeyboardKey Equal { get; private set; }
        public KeyboardKey A { get; private set; }
        public KeyboardKey B { get; private set; }
        public KeyboardKey C { get; private set; }
        public KeyboardKey D { get; private set; }
        public KeyboardKey E { get; private set; }
        public KeyboardKey F { get; private set; }
        public KeyboardKey G { get; private set; }
        public KeyboardKey H { get; private set; }
        public KeyboardKey I { get; private set; }
        public KeyboardKey J { get; private set; }
        public KeyboardKey K { get; private set; }
        public KeyboardKey L { get; private set; }
        public KeyboardKey M { get; private set; }
        public KeyboardKey N { get; private set; }
        public KeyboardKey O { get; private set; }
        public KeyboardKey P { get; private set; }
        public KeyboardKey Q { get; private set; }
        public KeyboardKey R { get; private set; }
        public KeyboardKey S { get; private set; }
        public KeyboardKey T { get; private set; }
        public KeyboardKey U { get; private set; }
        public KeyboardKey V { get; private set; }
        public KeyboardKey W { get; private set; }
        public KeyboardKey X { get; private set; }
        public KeyboardKey Y { get; private set; }
        public KeyboardKey Z { get; private set; }
        public KeyboardKey Space { get; private set; }
        public KeyboardKey Escape { get; private set; }
        public KeyboardKey Enter { get; private set; }
        public KeyboardKey Tab { get; private set; }
        public KeyboardKey Backspace { get; private set; }
        public KeyboardKey Insert { get; private set; }
        public KeyboardKey Delete { get; private set; }
        public KeyboardKey Right { get; private set; }
        public KeyboardKey Left { get; private set; }
        public KeyboardKey Down { get; private set; }
        public KeyboardKey Up { get; private set; }
        public KeyboardKey PageUp { get; private set; }
        public KeyboardKey PageDown { get; private set; }
        public KeyboardKey Home { get; private set; }
        public KeyboardKey End { get; private set; }
        public KeyboardKey CapsLock { get; private set; }
        public KeyboardKey ScrollLock { get; private set; }
        public KeyboardKey NumLock { get; private set; }
        public KeyboardKey PrintScreen { get; private set; }
        public KeyboardKey Pause { get; private set; }
        public KeyboardKey F1 { get; private set; }
        public KeyboardKey F2 { get; private set; }
        public KeyboardKey F3 { get; private set; }
        public KeyboardKey F4 { get; private set; }
        public KeyboardKey F5 { get; private set; }
        public KeyboardKey F6 { get; private set; }
        public KeyboardKey F7 { get; private set; }
        public KeyboardKey F8 { get; private set; }
        public KeyboardKey F9 { get; private set; }
        public KeyboardKey F10 { get; private set; }
        public KeyboardKey F11 { get; private set; }
        public KeyboardKey F12 { get; private set; }
        public KeyboardKey LeftShift { get; private set; }
        public KeyboardKey LeftControl { get; private set; }
        public KeyboardKey LeftAlt { get; private set; }
        public KeyboardKey LeftSuper { get; private set; }
        public KeyboardKey RightShift { get; private set; }
        public KeyboardKey RightControl { get; private set; }
        public KeyboardKey RightAlt { get; private set; }
        public KeyboardKey RightSuper { get; private set; }
        public KeyboardKey KB_Menu { get; private set; }
        public KeyboardKey LeftBracket { get; private set; }
        public KeyboardKey Backslash { get; private set; }
        public KeyboardKey RightBracket { get; private set; }
        public KeyboardKey Tilda { get; private set; }
        public KeyboardKey Numpad0 { get; private set; }
        public KeyboardKey Numpad1 { get; private set; }
        public KeyboardKey Numpad2 { get; private set; }
        public KeyboardKey Numpad3 { get; private set; }
        public KeyboardKey Numpad4 { get; private set; }
        public KeyboardKey Numpad5 { get; private set; }
        public KeyboardKey Numpad6 { get; private set; }
        public KeyboardKey Numpad7 { get; private set; }
        public KeyboardKey Numpad8 { get; private set; }
        public KeyboardKey Numpad9 { get; private set; }
        public KeyboardKey NumpadDecimal { get; private set; }
        public KeyboardKey NumpadDivide { get; private set; }
        public KeyboardKey NumpadMultiply { get; private set; }
        public KeyboardKey NumpadSubtract { get; private set; }
        public KeyboardKey NumpadAdd { get; private set; }
        public KeyboardKey NumpadEnter { get; private set; }
        public KeyboardKey NumpadEqual { get; private set; }
        public KeyboardKey Back { get; private set; }
        public KeyboardKey Menu { get; private set; }
        public KeyboardKey VolumeUp { get; private set; }
        public KeyboardKey VolumeDown { get; private set; }

        public USKeyboard() {
            Apostrophe = AssignKey("APOSTROPHE", 39);
            Comma = AssignKey("COMMA", 44);
            Minus = AssignKey("MINUS", 45);
            Period = AssignKey("PERIOD", 46);
            Slash = AssignKey("SLASH", 47);
            Zero = AssignKey("ZERO", 48);
            One = AssignKey("ONE", 49);
            Two = AssignKey("TWO", 50);
            Three = AssignKey("THREE", 51);
            Four = AssignKey("FOUR", 52);
            Five = AssignKey("FIVE", 53);
            Six = AssignKey("SIX", 54);
            Seven = AssignKey("SEVEN", 55);
            Eight = AssignKey("EIGHT", 56);
            Nine = AssignKey("NINE", 57);
            Semicolon = AssignKey("SEMICOLON", 59);
            Equal = AssignKey("EQUAL", 61);
            A = AssignKey("A", 65);
            B = AssignKey("B", 66);
            C = AssignKey("C", 67);
            D = AssignKey("D", 68);
            E = AssignKey("E", 69);
            F = AssignKey("F", 70);
            G = AssignKey("G", 71);
            H = AssignKey("H", 72);
            I = AssignKey("I", 73);
            J = AssignKey("J", 74);
            K = AssignKey("K", 75);
            L = AssignKey("L", 76);
            M = AssignKey("M", 77);
            N = AssignKey("N", 78);
            O = AssignKey("O", 79);
            P = AssignKey("P", 80);
            Q = AssignKey("Q", 81);
            R = AssignKey("R", 82);
            S = AssignKey("S", 83);
            T = AssignKey("T", 84);
            U = AssignKey("U", 85);
            V = AssignKey("V", 86);
            W = AssignKey("W", 87);
            X = AssignKey("X", 88);
            Y = AssignKey("Y", 89);
            Z = AssignKey("Z", 90);
            Space = AssignKey("SPACE", 32);
            Escape = AssignKey("ESCAPE", 256);
            Enter = AssignKey("ENTER", 257);
            Tab = AssignKey("TAB", 258);
            Backspace = AssignKey("BACKSPACE", 259);
            Insert = AssignKey("INSERT", 260);
            Delete = AssignKey("DELETE", 261);
            Right = AssignKey("RIGHT", 262);
            Left = AssignKey("LEFT", 263);
            Down = AssignKey("DOWN", 264);
            Up = AssignKey("UP", 265);
            PageUp = AssignKey("PAGE_UP", 266);
            PageDown = AssignKey("PAGE_DOWN", 267);
            Home = AssignKey("HOME", 268);
            End = AssignKey("END", 269);
            CapsLock = AssignKey("CAPS_LOCK", 280);
            ScrollLock = AssignKey("SCROLL_LOCK", 281);
            NumLock = AssignKey("NUM_LOCK", 282);
            PrintScreen = AssignKey("PRINT_SCREEN", 283);
            Pause = AssignKey("PAUSE", 284);
            F1 = AssignKey("F1", 290);
            F2 = AssignKey("F2", 291);
            F3 = AssignKey("F3", 292);
            F4 = AssignKey("F4", 293);
            F5 = AssignKey("F5", 294);
            F6 = AssignKey("F6", 295);
            F7 = AssignKey("F7", 296);
            F8 = AssignKey("F8", 297);
            F9 = AssignKey("F9", 298);
            F10 = AssignKey("F10", 299);
            F11 = AssignKey("F11", 300);
            F12 = AssignKey("F12", 301);
            LeftShift = AssignKey("LEFT_SHIFT", 340);
            LeftControl = AssignKey("LEFT_CONTROL", 341);
            LeftAlt = AssignKey("LEFT_ALT", 342);
            LeftSuper = AssignKey("LEFT_SUPER", 343);
            RightShift = AssignKey("RIGHT_SHIFT", 344);
            RightControl = AssignKey("RIGHT_CONTROL", 345);
            RightAlt = AssignKey("RIGHT_ALT", 346);
            RightSuper = AssignKey("RIGHT_SUPER", 347);
            KB_Menu = AssignKey("KB_MENU", 348);
            LeftBracket = AssignKey("LEFT_BRACKET", 91);
            Backslash = AssignKey("BACKSLASH", 92);
            RightBracket = AssignKey("RIGHT_BRACKET", 93);
            Tilda = AssignKey("GRAVE", 96);
            Numpad0 = AssignKey("KP_0", 320);
            Numpad1 = AssignKey("KP_1", 321);
            Numpad2 = AssignKey("KP_2", 322);
            Numpad3 = AssignKey("KP_3", 323);
            Numpad4 = AssignKey("KP_4", 324);
            Numpad5 = AssignKey("KP_5", 325);
            Numpad6 = AssignKey("KP_6", 326);
            Numpad7 = AssignKey("KP_7", 327);
            Numpad8 = AssignKey("KP_8", 328);
            Numpad9 = AssignKey("KP_9", 329);
            NumpadDecimal = AssignKey("KP_DECIMAL", 330);
            NumpadDivide = AssignKey("KP_DIVIDE", 331);
            NumpadMultiply = AssignKey("KP_MULTIPLY", 332);
            NumpadSubtract = AssignKey("KP_SUBTRACT", 333);
            NumpadAdd = AssignKey("KP_ADD", 334);
            NumpadEnter = AssignKey("KP_ENTER", 335);
            NumpadEqual = AssignKey("KP_EQUAL", 336);
            Back = AssignKey("BACK", 4);
            VolumeUp = AssignKey("VOLUME_UP", 24);
            VolumeDown = AssignKey("VOLUME_DOWN", 25);
        }

        public override KeyAction GetKeyAction(KeyboardState state, KeyboardKey key) {
            bool ctrl = state.ControlDown, alt = state.AltDown, shift = state.ShiftDown;
            bool caps = false;
            bool numpad = false;

            switch (key.Key) {
                case 39: return !(shift || caps) ? "'" : "\""; // Apostrophe
                case 44: return !(shift || caps) ? "," : "<"; // Comma
                case 45: return !(shift || caps) ? "-" : "_"; // Minus
                case 46: return !(shift || caps) ? "." : ">"; // Period
                case 47: return !(shift || caps) ? "/" : "?"; // Slash
                case 48: return !(shift || caps) ? "0" : ")"; // Zero
                case 49: return !(shift || caps) ? "1" : "!"; // One
                case 50: return !(shift || caps) ? "2" : "@"; // Two
                case 51: return !(shift || caps) ? "3" : "#"; // Three
                case 52: return !(shift || caps) ? "4" : "$"; // Four
                case 53: return !(shift || caps) ? "5" : "%"; // Five
                case 54: return !(shift || caps) ? "6" : "^"; // Six
                case 55: return !(shift || caps) ? "7" : "&"; // Seven
                case 56: return !(shift || caps) ? "8" : "*"; // Eight
                case 57: return !(shift || caps) ? "9" : "("; // Nine
                case 59: return !(shift || caps) ? ";" : ":"; // Semicolon
                case 61: return !(shift || caps) ? "=" : "+"; // Equal
                case 65: return !(shift || caps) ? "a" : "A"; // A
                case 66: return !(shift || caps) ? "b" : "B"; // B
                case 67: return !(shift || caps) ? "c" : "C"; // C
                case 68: return !(shift || caps) ? "d" : "D"; // D
                case 69: return !(shift || caps) ? "e" : "E"; // E
                case 70: return !(shift || caps) ? "f" : "F"; // F
                case 71: return !(shift || caps) ? "g" : "G"; // G
                case 72: return !(shift || caps) ? "h" : "H"; // H
                case 73: return !(shift || caps) ? "i" : "I"; // I
                case 74: return !(shift || caps) ? "j" : "J"; // J
                case 75: return !(shift || caps) ? "k" : "K"; // K
                case 76: return !(shift || caps) ? "l" : "L"; // L
                case 77: return !(shift || caps) ? "m" : "M"; // M
                case 78: return !(shift || caps) ? "n" : "N"; // N
                case 79: return !(shift || caps) ? "o" : "O"; // O
                case 80: return !(shift || caps) ? "p" : "P"; // P
                case 81: return !(shift || caps) ? "q" : "Q"; // Q
                case 82: return !(shift || caps) ? "r" : "R"; // R
                case 83: return !(shift || caps) ? "s" : "S"; // S
                case 84: return !(shift || caps) ? "t" : "T"; // T
                case 85: return !(shift || caps) ? "u" : "U"; // U
                case 86: return !(shift || caps) ? "v" : "V"; // V
                case 87: return !(shift || caps) ? "w" : "W"; // W
                case 88: return !(shift || caps) ? "x" : "X"; // X
                case 89: return !(shift || caps) ? "y" : "Y"; // Y
                case 90: return !(shift || caps) ? "z" : "Z"; // Z
                case 32: return " ";
                case 91: return !(shift || caps) ? "[" : "{"; // LeftBracket
                case 92: return !(shift || caps) ? "\\" : "|"; // Backslash
                case 93: return !(shift || caps) ? "]" : "}"; // RightBracket
                case 96: return !(shift || caps) ? "`" : "~"; // Grave
                case 259: return CharacterType.DeleteBackwards; // Numpad0
                case 262: return new(CharacterType.Arrow, "RIGHT");
                case 263: return new(CharacterType.Arrow, "LEFT");
                case 264: return new(CharacterType.Arrow, "DOWN");
                case 265: return new(CharacterType.Arrow, "UP");

                case 340: return new(CharacterType.Shift, "LEFT");
                case 341: return new(CharacterType.Control, "LEFT");
                case 342: return new(CharacterType.Alt, "LEFT");
                case 343: return new(CharacterType.Super, "LEFT");
                
				case 344: return new(CharacterType.Shift, "RIGHT");
                case 345: return new(CharacterType.Control, "RIGHT");
                case 346: return new(CharacterType.Alt, "RIGHT");
                case 347: return new(CharacterType.Super, "RIGHT");


                case 320: return numpad ? "0" : null; // Numpad0
                case 321: return numpad ? "1" : null; // Numpad1
                case 322: return numpad ? "2" : null; // Numpad2
                case 323: return numpad ? "3" : null; // Numpad3
                case 324: return numpad ? "4" : null; // Numpad4
                case 325: return numpad ? "5" : null; // Numpad5
                case 326: return numpad ? "6" : null; // Numpad6
                case 327: return numpad ? "7" : null; // Numpad7
                case 328: return numpad ? "8" : null; // Numpad8
                case 329: return numpad ? "9" : null; // Numpad9
                case 330: return numpad ? "." : null; // NumpadDecimal
                case 331: return numpad ? "/" : null; // NumpadDivide
                case 332: return numpad ? "*" : null; // NumpadMultiply
                case 333: return numpad ? "-" : null; // NumpadSubtract
                case 334: return numpad ? "+" : null; // NumpadAdd
                case 336: return numpad ? "=" : null; // NumpadEqual
            }

            return CharacterType.NoAction;
        }

        public override bool ControlDown(KeyboardState state) => state.KeyDown(LeftControl) || state.KeyDown(RightControl);
        public override bool AltDown(KeyboardState state) => state.KeyDown(LeftAlt) || state.KeyDown(RightAlt);
        public override bool ShiftDown(KeyboardState state) => state.KeyDown(LeftShift) || state.KeyDown(RightShift);
    }

    public struct KeyboardState()
    {
        public List<int> KeyOrder = [];
        public HashSet<int> KeysPressed = [];
        public Dictionary<int, int> KeyPressCounts = new();

        public List<int> KeysHeld = new List<int>();
        public List<int> KeysReleased = new List<int>();

        public bool KeyDown(KeyboardKey key) => Raylib.IsKeyDown((Raylib_cs.KeyboardKey)key.Key);
        public bool KeyPressed(KeyboardKey key) => Raylib.IsKeyPressed((Raylib_cs.KeyboardKey)key.Key);
        public bool KeyReleased(KeyboardKey key) => Raylib.IsKeyReleased((Raylib_cs.KeyboardKey)key.Key);

        public override string ToString() {
            List<string> keys = [];
            foreach (var key in KeysHeld) {
                keys.Add(KeyboardLayout.USA.FromInt(key).Name);
            }
            return $"Held [{string.Join(", ", keys)}]";
        }

        public KeyAction GetKeyActionFromKey(KeyboardKey key) {
            return KeyboardLayout.USA.GetKeyAction(this, key);
        }

        public bool ShiftDown => KeyboardLayout.USA.ShiftDown(this);
        public bool ControlDown => KeyboardLayout.USA.ControlDown(this);
        public bool AltDown => KeyboardLayout.USA.AltDown(this);
    }
}
