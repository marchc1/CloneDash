using SDL;

using System.Runtime.InteropServices;

namespace Nucleus;

[Flags]
public enum MessageBoxButtonFlags : uint
{
	None,
	ReturnKeyDefault = 1u,
	EscapeKeyDefault = 2u
}

public enum MessageBoxIcon
{
	Error,
	Warning,
	Information
}

public static partial class Platform
{
	public unsafe struct MessageBoxBuilder()
	{
		public const int MAX_BUTTONS = 16;
		public const int MAX_STRINGS = 128;
		private Action?[] actions = new Action?[MAX_BUTTONS];
		private nint buttonPtr = 0;
		private SDL_MessageBoxButtonData[] buttons = new SDL_MessageBoxButtonData[MAX_BUTTONS];
		private byte*[] cleanup = new byte*[MAX_STRINGS];
		private nint cleanupPtr = 0;

		private SDL_MessageBoxColor color;

		private SDL_MessageBoxData data;

		private SDL_MessageBoxFlags flags;

		public bool Show() {
			fixed (SDL_MessageBoxButtonData* btns = buttons) {
				data.buttons = btns;
				data.numbuttons = (int)buttonPtr;
				int buttonID;
				SDL_MessageBoxData dataLocal = data; // :(
				bool ret = SDL3.SDL_ShowMessageBox(&dataLocal, &buttonID);

				deallocAllStrs(); // message box done, garbage collect
				if (buttonID >= 0)
					actions[buttonID]?.Invoke();

				return ret;
			}
		}

		public MessageBoxBuilder WithButton(string text, Action? action = null, MessageBoxButtonFlags flags = MessageBoxButtonFlags.None) {
			int buttonPtrThisBtn = (int)buttonPtr++;
			ref SDL_MessageBoxButtonData btnData = ref buttons[buttonPtrThisBtn];
			btnData.buttonID = buttonPtrThisBtn;
			btnData.text = allocStr(text);
			btnData.flags = (SDL_MessageBoxButtonFlags)flags;
			actions[buttonPtrThisBtn] = action;
			return this;
		}

		public MessageBoxBuilder WithIcon(MessageBoxIcon icon) {
			switch (icon) {
				case MessageBoxIcon.Information:
					data.flags |= SDL_MessageBoxFlags.SDL_MESSAGEBOX_INFORMATION;
					data.flags &= ~SDL_MessageBoxFlags.SDL_MESSAGEBOX_WARNING;
					data.flags &= ~SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR;
					break;

				case MessageBoxIcon.Warning:
					data.flags &= ~SDL_MessageBoxFlags.SDL_MESSAGEBOX_INFORMATION;
					data.flags |= SDL_MessageBoxFlags.SDL_MESSAGEBOX_WARNING;
					data.flags &= ~SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR;
					break;

				case MessageBoxIcon.Error:
					data.flags &= ~SDL_MessageBoxFlags.SDL_MESSAGEBOX_INFORMATION;
					data.flags &= ~SDL_MessageBoxFlags.SDL_MESSAGEBOX_WARNING;
					data.flags |= SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR;
					break;
			}
			return this;
		}

		public MessageBoxBuilder WithMessage(string str) {
			data.message = allocStr(str);
			return this;
		}

		public MessageBoxBuilder WithTitle(string str) {
			data.title = allocStr(str);
			return this;
		}

		private byte* allocStr(string str) {
			if (cleanupPtr >= MAX_STRINGS)
				throw new OverflowException();

			byte* ret = (byte*)Marshal.StringToHGlobalAnsi(str);
			cleanup[cleanupPtr++] = ret;
			return ret;
		}

		private void deallocAllStrs() {
			for (int i = 0; i < cleanupPtr; i++)
				Marshal.FreeHGlobal((nint)cleanup[i]);
		}
	}
}