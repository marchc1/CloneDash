using Nucleus.Common.Input;
using Nucleus.Util;

namespace Nucleus.Input;

public struct KeyboardState()
{
	public const int MAXIMUM_KEY_ARRAY_LENGTH = 512;
	public const int MAX_TEXT_INPUTS = 256;
	public const int MAXIMUM_FRAME_ORDERED_KEYS_LENGTH = 64;

	public InlineArray64<double> KeyTimesThisFrame;
	public InlineArray64<int> KeysThisFrame;

	public int TotalKeysThisFrame = 0;

	public InlineArray512<bool> KeysDown;
	public InlineArray512<byte> KeysPressed;
	public InlineArray512<bool> KeysReleased;

	public InlineArray256<string?> TextInputs;

	public int GetTextInputsThisFrame() {
		int len = 0;
		for (len = 0; len < 256; len++) {
			if (TextInputs[len] == null)
				break;
		}
		return len;
	}

	public string GetTextInputThisFrameAtIndex(int i) {
		return TextInputs[i]!;
	}

	public IEnumerable<int> GetKeysThisFrame() {
		for (int i = 0; i < TotalKeysThisFrame; i++) {
			yield return KeysThisFrame[i];
		}
	}

	public IEnumerable<int> GetKeysHeld() {
		for (int i = 0; i < MAXIMUM_KEY_ARRAY_LENGTH; i++) {
			if (KeysDown[i])
				yield return i;
		}
	}

	public bool KeyAvailable(ref int i, out ButtonCode key, out double time) {
		if (i > TotalKeysThisFrame) {
			key = 0;
			time = 0;
			return false;
		}

		key = (ButtonCode)KeysThisFrame[i];
		time = KeyTimesThisFrame[i];
		i++;
		return true;
	}

	public void PushKeyPress(int key, double time) {
		KeysThisFrame[TotalKeysThisFrame] = key;
		KeyTimesThisFrame[TotalKeysThisFrame] = time;
		TotalKeysThisFrame++;
		KeysPressed[key]++;
	}

	public readonly bool IsKeyDown(int key) => KeysDown[key];
	public readonly bool IsKeyDown(ButtonCode key) => KeysDown[(int)key];
	public readonly bool WasKeyPressed(int key) => KeysPressed[key] > 0;
	public readonly bool WasKeyPressed(ButtonCode key) => KeysPressed[(int)key] > 0;
	public readonly int KeyPressCount(int key) => KeysPressed[key];
	public readonly int KeyPressCount(ButtonCode key) => KeysPressed[(int)key];
	public readonly bool WasKeyReleased(int key) => KeysReleased[key];
	public readonly bool WasKeyReleased(ButtonCode key) => KeysReleased[(int)key];

	static readonly char[] ros_state = new char[1024];
	static bool writeToROSState(ref int i, ReadOnlySpan<char> text){
		return text.TryCopyTo(ros_state.AsSpan()[(i += text.Length)..]);
	} 
	public ReadOnlySpan<char> ToReadOnlySpan() {
		int i = 0;

		writeToROSState(ref i, "Pressed [");
		bool wroteOne = false;

		foreach (var key in GetKeysHeld()) {
			wroteOne = writeToROSState(ref i, key.ToButtonCode().GetString());
			writeToROSState(ref i, ", ");
		}
		if (wroteOne) i -= 2; // go back
		writeToROSState(ref i, "] Held [");

		wroteOne = false;
		foreach(var key in GetKeysThisFrame()) {
			wroteOne = writeToROSState(ref i, key.ToButtonCode().GetString());
			writeToROSState(ref i, ", ");
		}
		if (wroteOne) i -= 2; // go back
		writeToROSState(ref i, "]");

		return ros_state.AsSpan()[..i];
		// return $"Pressed [{string.Join(", ", pressed)}] Held [{string.Join(", ", keys)}]";
	}

	public readonly bool ShiftDown => IsKeyDown(ButtonCode.KeyLeftShift) || IsKeyDown(ButtonCode.KeyRightShift);
	public readonly bool ControlDown => IsKeyDown(ButtonCode.KeyLeftControl) || IsKeyDown(ButtonCode.KeyRightControl);
	public readonly bool AltDown => IsKeyDown(ButtonCode.KeyLeftAlt) || IsKeyDown(ButtonCode.KeyRightAlt);

	public void Clear() {
		for (int i = 0; i < MAXIMUM_FRAME_ORDERED_KEYS_LENGTH; i++) {
			KeysThisFrame[i] = 0;
			KeyTimesThisFrame[i] = 0;
		}
		TotalKeysThisFrame = 0;
		for (int i = 0; i < MAXIMUM_KEY_ARRAY_LENGTH; i++) {
			KeysDown[i] = false;
			KeysPressed[i] = 0;
			KeysReleased[i] = false;
		}
	}
}
