using Nucleus.Types;
using Raylib_cs;
using System.Diagnostics;

namespace Nucleus.Input
{
	public struct MouseState
	{
		// Pressed this frame
		public bool Mouse1Clicked = false;
		public bool Mouse2Clicked = false;
		public bool Mouse3Clicked = false;
		public bool Mouse4Clicked = false;
		public bool Mouse5Clicked = false;

		// Held/down in general
		public bool Mouse1Held = false;
		public bool Mouse2Held = false;
		public bool Mouse3Held = false;
		public bool Mouse4Held = false;
		public bool Mouse5Held = false;

		// Released this frame
		public bool Mouse1Released = false;
		public bool Mouse2Released = false;
		public bool Mouse3Released = false;
		public bool Mouse4Released = false;
		public bool Mouse5Released = false;

		public bool MouseClicked => Mouse1Clicked || Mouse2Clicked || Mouse3Clicked || Mouse4Clicked || Mouse5Clicked;
		public bool MouseHeld => Mouse1Held || Mouse2Held || Mouse3Held || Mouse4Held || Mouse5Held;
		public bool MouseReleased => Mouse1Released || Mouse2Released || Mouse3Released || Mouse4Released || Mouse5Released;

		public bool Clicked(MouseButton button) {
			switch (button.Button) {
				case 1: return Mouse1Clicked;
				case 2: return Mouse2Clicked;
				case 3: return Mouse3Clicked;
				case 4: return Mouse4Clicked;
				case 5: return Mouse5Clicked;
			}
			throw new NotImplementedException("No Clicked handler for MouseButton " + button.Button);
		}

		public bool Held(MouseButton button) {
			switch (button.Button) {
				case 1: return Mouse1Held;
				case 2: return Mouse2Held;
				case 3: return Mouse3Held;
				case 4: return Mouse4Held;
				case 5: return Mouse5Held;
			}
			throw new NotImplementedException("No Held handler for MouseButton " + button.Button);
		}

		public bool Released(MouseButton button) {
			switch (button.Button) {
				case 1: return Mouse1Released;
				case 2: return Mouse2Released;
				case 3: return Mouse3Released;
				case 4: return Mouse4Released;
				case 5: return Mouse5Released;
			}
			throw new NotImplementedException("No Released handler for MouseButton " + button.Button);
		}

		/// <summary>
		/// Mouse position, localized to the window.
		/// </summary>
		public Vector2F MousePos = new(0);
		public Vector2F MouseDelta  = new(0);
		public Vector2F MouseScroll  = new(0);

		public MouseState() { }

		static readonly char[] ros_state = new char[1024];
		static bool writeToROSState(ref int i, ReadOnlySpan<char> text) {
			return text.TryCopyTo(ros_state.AsSpan()[(i += text.Length)..]);
		}

		static bool writeToROSState(ref int i, in Vector2F vec2f) {
			int written;
			if (!vec2f.X.TryFormat(ros_state.AsSpan()[i..], out written))
				return false;
			i += written;
			if (!writeToROSState(ref i, " x "))
				return false;
			if (!vec2f.Y.TryFormat(ros_state.AsSpan()[i..], out written))
				return false;
			i += written;
			return true;
		}
		static bool writeBooleanROS(ref int i, bool value) => writeToROSState(ref i, value ? "^" : "_");

		public ReadOnlySpan<char> ToReadOnlySpan() {
			int i = 0;
			writeToROSState(ref i, "C [");
			writeBooleanROS(ref i, Mouse1Clicked);
			writeBooleanROS(ref i, Mouse2Clicked);
			writeBooleanROS(ref i, Mouse3Clicked);
			writeBooleanROS(ref i, Mouse4Clicked);
			writeBooleanROS(ref i, Mouse5Clicked);
			writeToROSState(ref i, "] ");

			writeToROSState(ref i, "H [");
			writeBooleanROS(ref i, Mouse1Held);
			writeBooleanROS(ref i, Mouse2Held);
			writeBooleanROS(ref i, Mouse3Held);
			writeBooleanROS(ref i, Mouse4Held);
			writeBooleanROS(ref i, Mouse5Held);
			writeToROSState(ref i, "] ");

			writeToROSState(ref i, "R [");
			writeBooleanROS(ref i, Mouse1Released);
			writeBooleanROS(ref i, Mouse2Released);
			writeBooleanROS(ref i, Mouse3Released);
			writeBooleanROS(ref i, Mouse4Released);
			writeBooleanROS(ref i, Mouse5Released);
			writeToROSState(ref i, "] ");

			writeToROSState(ref i, "P [");
			writeToROSState(ref i, in MousePos);
			writeToROSState(ref i, "] D [");
			writeToROSState(ref i, in MouseDelta);
			writeToROSState(ref i, "] S [");
			writeToROSState(ref i, in MouseScroll);
			writeToROSState(ref i, "]");

			return ros_state.AsSpan()[..i];
			
		}
	}
}
