using Nucleus.Core;
using Nucleus.Types;
using Raylib_cs;
using SDL;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Nucleus.Engine;

public class WindowKeyboardState(OSWindow window)
{
	public const int MAX_KEYBOARD_KEYS = 512;
	public const int MAX_KEY_PRESSED_QUEUE = 32;
	public const int MAX_CHAR_PRESSED_QUEUE = 32;

	public bool ExitKey;

	public byte[] CurrentKeyState = new byte[MAX_KEYBOARD_KEYS];
	public byte[] PreviousKeyState = new byte[MAX_KEYBOARD_KEYS];

	public byte[] KeyRepeatInFrame = new byte[MAX_KEYBOARD_KEYS];

	public double[] KeyPressTimeQueue = new double[MAX_KEY_PRESSED_QUEUE];
	public KeyboardKey[] KeyPressQueue = new KeyboardKey[MAX_KEY_PRESSED_QUEUE];
	public int KeyPressQueueCount = 0;

	internal void Reset() {
		KeyPressQueueCount = 0;
		for (int i = 0; i < MAX_KEY_PRESSED_QUEUE; i++) {
			KeyPressQueue[i] = KeyboardKey.KEY_NULL;
			KeyPressTimeQueue[i] = 0;
		}
		for (int i = 0; i < MAX_KEYBOARD_KEYS; i++) {
			PreviousKeyState[i] = CurrentKeyState[i];
			KeyRepeatInFrame[i] = 0;
		}
	}

	public void EnqueueKeyPress(ref SDL_Event ev) {
		if (KeyPressQueueCount >= MAX_KEY_PRESSED_QUEUE) {
			Logs.Error($"Somehow; the user typed > {MAX_KEY_PRESSED_QUEUE} in a single frame. Preventing a crash.");
			return;
		}

		KeyPressTimeQueue[KeyPressQueueCount] = OS.TicksToTime(ev.key.timestamp);
		KeyPressQueue[KeyPressQueueCount] = OSWindow.TranslateKeyboardKey(ev.key.scancode);
		KeyPressQueueCount++;
	}
}

public class WindowMouseState(OSWindow window)
{
	public const int MAX_MOUSE_BUTTONS = 8;

	public Vector2F MouseOffset;
	public Vector2F MouseScale;
	public Vector2F CurrentMousePosition;
	public Vector2F PreviousMousePosition;

	public byte[] CurrentMouseButtonState = new byte[MAX_MOUSE_BUTTONS];
	public byte[] PreviousMouseButtonState = new byte[MAX_MOUSE_BUTTONS];
	public Vector2F CurrentMouseScroll;
	public Vector2F PreviousMouseScroll;

	internal void Reset() {
		PreviousMouseScroll.X = CurrentMouseScroll.X;
		PreviousMouseScroll.Y = CurrentMouseScroll.Y;

		CurrentMouseScroll.X = 0;
		CurrentMouseScroll.Y = 0;

		PreviousMousePosition = CurrentMousePosition;

		for (int i = 0; i < MAX_MOUSE_BUTTONS; i++)
			PreviousMouseButtonState[i] = CurrentMouseButtonState[i];
	}
}

public unsafe class OSWindow
{
	internal Vector2F ScreenSize;
	internal Vector2F PreviousScreenSize;
	internal Vector2F RenderSize;
	internal Vector2F RenderOffset;
	internal Vector2F CurrentFbo;
	public Matrix4x4 ScreenScale;

	internal bool ResizedLastFrame;
	internal bool UserWantsToClose;
	internal bool UsingFbo;

	public WindowKeyboardState Keyboard;
	public WindowMouseState Mouse;

	private SDL_Window* handle;
	private SDL_WindowID windowID;
	private SDL_GLContextState* glctx;

	private static Dictionary<OSWindow, SDL_WindowID> windowLookup_window2id = [];
	private static Dictionary<SDL_WindowID, OSWindow> windowLookup_id2window = [];

	private OSWindow() {
		Keyboard = new(this);
		Mouse = new(this);
	}

	public static OSWindow Create(int width, int height, string title = "Nucleus Engine - Window", ConfigFlags confFlags = 0) {
		if (!OS.InitPlatform()) {
			throw Util.Util.MessageBoxException("Cannot initialize SDL.");
		}

		OSWindow window = new OSWindow();
		SDL_WindowFlags flags = SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS | SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS | SDL_WindowFlags.SDL_WINDOW_MOUSE_CAPTURE;

		if (confFlags.HasFlag(ConfigFlags.FLAG_MSAA_4X_HINT)) {
			SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_MULTISAMPLEBUFFERS, 1);
			SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_MULTISAMPLESAMPLES, 4);
		}

		window.handle = SDL3.SDL_CreateWindow(title, width, height, flags);
		window.glctx = SDL3.SDL_GL_CreateContext(window.handle);
		SDL3.SDL_GL_SetSwapInterval(0);

		Rlgl.LoadExtensions(&OS.OpenGL_GetProcAddress);

		window.ActivateGL();
		Rlgl.GlInit(width, height);

		window.SetupViewport(width, height);

		Texture2D tex = new() { Id = Rlgl.GetTextureIdDefault(), Width = 1, Height = 1, Mipmaps = 1, Format = PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8 };
		Raylib.SetShapesTexture(tex, new(0, 0, 1, 1));

		if (window.handle == null) throw Util.Util.MessageBoxException("SDL could not create a window.");

		window.windowID = SDL3.SDL_GetWindowID(window.handle);
		windowLookup_id2window[window.windowID] = window;
		windowLookup_window2id[window] = window.windowID;

		window.ScreenSize.X = width;
		window.ScreenSize.Y = height;
		window.ScreenScale = Raymath.MatrixIdentity();

		// SDL3.SDL_SetEventEnabled(SDL_EventType.SDL_EVENT_DROP_FILE, true);

		window.Resizable = true;

		// This fixes a Windows issue where the window becomes unresponsive while moving/resizing
		SDL3.SDL_AddEventWatch(&HandleWin32Resize, (nint)window.handle);

		return window;
	}
	private static double lastUpdate;
	[UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
	private static SDL.SDLBool HandleWin32Resize(nint data, SDL_Event* ev) {
		var type = ev->Type;
		if(ev->Type == SDL_EventType.SDL_EVENT_WINDOW_EXPOSED && !EngineCore.InLevelFrame) {
			var now = OS.GetTime();
			if ((now - lastUpdate) > (1 / 60d)) {
				var winObj = windowLookup_id2window[ev->window.windowID];
				winObj.PushEvent(ref *ev);
				EngineCore.ProcessFrame();
				lastUpdate = now;
			}
		}
		return false;
	}

	public bool Resizable {
		get => SDL3.SDL_GetWindowFlags(handle).HasFlag(SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
		set => SDL3.SDL_SetWindowResizable(handle, value);
	}

	public bool Undecorated {
		get => SDL3.SDL_GetWindowFlags(handle).HasFlag(SDL_WindowFlags.SDL_WINDOW_BORDERLESS);
		set => SDL3.SDL_SetWindowResizable(handle, value);
	}

	public void SwapScreenBuffer() => SDL3.SDL_GL_SwapWindow(handle);

	public void SetupViewport(float width, float height) => SetupViewport((int)width, (int)height);
	public void SetupViewport(int width, int height) {
		RenderSize.W = width;
		RenderSize.H = height;

#if COMPILED_OSX
		Vector2F scale = GetWindowScaleDPI();
		Rlgl.Viewport((int)(RenderOffset.x/2*scale.x), (int)(RenderOffset.y/2*scale.y), (int)((RenderSize.W)*scale.x), (int)((RenderSize.H)*scale.y));
#else
		Rlgl.Viewport((int)(RenderOffset.x / 2), ((int)RenderOffset.y / 2), (int)RenderSize.W, (int)RenderSize.H);
#endif

		Rlgl.MatrixMode(MatrixMode.Projection);
		Rlgl.LoadIdentity();
		Rlgl.Ortho(0, RenderSize.W, RenderSize.H, 0, 0, 1);
		Rlgl.MatrixMode(MatrixMode.ModelView);
		Rlgl.LoadIdentity();
	}

	public void ActivateGL() {
		SDL3.SDL_GL_MakeCurrent(handle, glctx);
	}

	public const int SCANCODE_MAPPED_NUM = 232;
	public static KeyboardKey[] ScancodeToKey = new KeyboardKey[SCANCODE_MAPPED_NUM] {
		KeyboardKey.KEY_NULL,           // SDL_SCANCODE_UNKNOWN
		0,
		0,
		0,
		KeyboardKey.KEY_A,              // SDL_SCANCODE_A
		KeyboardKey.KEY_B,              // SDL_SCANCODE_B
		KeyboardKey.KEY_C,              // SDL_SCANCODE_C
		KeyboardKey.KEY_D,              // SDL_SCANCODE_D
		KeyboardKey.KEY_E,              // SDL_SCANCODE_E
		KeyboardKey.KEY_F,              // SDL_SCANCODE_F
		KeyboardKey.KEY_G,              // SDL_SCANCODE_G
		KeyboardKey.KEY_H,              // SDL_SCANCODE_H
		KeyboardKey.KEY_I,              // SDL_SCANCODE_I
		KeyboardKey.KEY_J,              // SDL_SCANCODE_J
		KeyboardKey.KEY_K,              // SDL_SCANCODE_K
		KeyboardKey.KEY_L,              // SDL_SCANCODE_L
		KeyboardKey.KEY_M,              // SDL_SCANCODE_M
		KeyboardKey.KEY_N,              // SDL_SCANCODE_N
		KeyboardKey.KEY_O,              // SDL_SCANCODE_O
		KeyboardKey.KEY_P,              // SDL_SCANCODE_P
		KeyboardKey.KEY_Q,              // SDL_SCANCODE_Q
		KeyboardKey.KEY_R,              // SDL_SCANCODE_R
		KeyboardKey.KEY_S,              // SDL_SCANCODE_S
		KeyboardKey.KEY_T,              // SDL_SCANCODE_T
		KeyboardKey.KEY_U,              // SDL_SCANCODE_U
		KeyboardKey.KEY_V,              // SDL_SCANCODE_V
		KeyboardKey.KEY_W,              // SDL_SCANCODE_W
		KeyboardKey.KEY_X,              // SDL_SCANCODE_X
		KeyboardKey.KEY_Y,              // SDL_SCANCODE_Y
		KeyboardKey.KEY_Z,              // SDL_SCANCODE_Z
		KeyboardKey.KEY_ONE,            // SDL_SCANCODE_1
		KeyboardKey.KEY_TWO,            // SDL_SCANCODE_2
		KeyboardKey.KEY_THREE,          // SDL_SCANCODE_3
		KeyboardKey.KEY_FOUR,           // SDL_SCANCODE_4
		KeyboardKey.KEY_FIVE,           // SDL_SCANCODE_5
		KeyboardKey.KEY_SIX,            // SDL_SCANCODE_6
		KeyboardKey.KEY_SEVEN,          // SDL_SCANCODE_7
		KeyboardKey.KEY_EIGHT,          // SDL_SCANCODE_8
		KeyboardKey.KEY_NINE,           // SDL_SCANCODE_9
		KeyboardKey.KEY_ZERO,           // SDL_SCANCODE_0
		KeyboardKey.KEY_ENTER,          // SDL_SCANCODE_RETURN
		KeyboardKey.KEY_ESCAPE,         // SDL_SCANCODE_ESCAPE
		KeyboardKey.KEY_BACKSPACE,      // SDL_SCANCODE_BACKSPACE
		KeyboardKey.KEY_TAB,            // SDL_SCANCODE_TAB
		KeyboardKey.KEY_SPACE,          // SDL_SCANCODE_SPACE
		KeyboardKey.KEY_MINUS,          // SDL_SCANCODE_MINUS
		KeyboardKey.KEY_EQUAL,          // SDL_SCANCODE_EQUALS
		KeyboardKey.KEY_LEFT_BRACKET,   // SDL_SCANCODE_LEFTBRACKET
		KeyboardKey.KEY_RIGHT_BRACKET,  // SDL_SCANCODE_RIGHTBRACKET
		KeyboardKey.KEY_BACKSLASH,      // SDL_SCANCODE_BACKSLASH
		0,                  // SDL_SCANCODE_NONUSHASH
		KeyboardKey.KEY_SEMICOLON,      // SDL_SCANCODE_SEMICOLON
		KeyboardKey.KEY_APOSTROPHE,     // SDL_SCANCODE_APOSTROPHE
		KeyboardKey.KEY_GRAVE,          // SDL_SCANCODE_GRAVE
		KeyboardKey.KEY_COMMA,          // SDL_SCANCODE_COMMA
		KeyboardKey.KEY_PERIOD,         // SDL_SCANCODE_PERIOD
		KeyboardKey.KEY_SLASH,          // SDL_SCANCODE_SLASH
		KeyboardKey.KEY_CAPS_LOCK,      // SDL_SCANCODE_CAPSLOCK
		KeyboardKey.KEY_F1,             // SDL_SCANCODE_F1
		KeyboardKey.KEY_F2,             // SDL_SCANCODE_F2
		KeyboardKey.KEY_F3,             // SDL_SCANCODE_F3
		KeyboardKey.KEY_F4,             // SDL_SCANCODE_F4
		KeyboardKey.KEY_F5,             // SDL_SCANCODE_F5
		KeyboardKey.KEY_F6,             // SDL_SCANCODE_F6
		KeyboardKey.KEY_F7,             // SDL_SCANCODE_F7
		KeyboardKey.KEY_F8,             // SDL_SCANCODE_F8
		KeyboardKey.KEY_F9,             // SDL_SCANCODE_F9
		KeyboardKey.KEY_F10,            // SDL_SCANCODE_F10
		KeyboardKey.KEY_F11,            // SDL_SCANCODE_F11
		KeyboardKey.KEY_F12,            // SDL_SCANCODE_F12
		KeyboardKey.KEY_PRINT_SCREEN,   // SDL_SCANCODE_PRINTSCREEN
		KeyboardKey.KEY_SCROLL_LOCK,    // SDL_SCANCODE_SCROLLLOCK
		KeyboardKey.KEY_PAUSE,          // SDL_SCANCODE_PAUSE
		KeyboardKey.KEY_INSERT,         // SDL_SCANCODE_INSERT
		KeyboardKey.KEY_HOME,           // SDL_SCANCODE_HOME
		KeyboardKey.KEY_PAGE_UP,        // SDL_SCANCODE_PAGEUP
		KeyboardKey.KEY_DELETE,         // SDL_SCANCODE_DELETE
		KeyboardKey.KEY_END,            // SDL_SCANCODE_END
		KeyboardKey.KEY_PAGE_DOWN,      // SDL_SCANCODE_PAGEDOWN
		KeyboardKey.KEY_RIGHT,          // SDL_SCANCODE_RIGHT
		KeyboardKey.KEY_LEFT,           // SDL_SCANCODE_LEFT
		KeyboardKey.KEY_DOWN,           // SDL_SCANCODE_DOWN
		KeyboardKey.KEY_UP,             // SDL_SCANCODE_UP
		KeyboardKey.KEY_NUM_LOCK,       // SDL_SCANCODE_NUMLOCKCLEAR
		KeyboardKey.KEY_KP_DIVIDE,      // SDL_SCANCODE_KP_DIVIDE
		KeyboardKey.KEY_KP_MULTIPLY,    // SDL_SCANCODE_KP_MULTIPLY
		KeyboardKey.KEY_KP_SUBTRACT,    // SDL_SCANCODE_KP_MINUS
		KeyboardKey.KEY_KP_ADD,         // SDL_SCANCODE_KP_PLUS
		KeyboardKey.KEY_KP_ENTER,       // SDL_SCANCODE_KP_ENTER
		KeyboardKey.KEY_KP_1,           // SDL_SCANCODE_KP_1
		KeyboardKey.KEY_KP_2,           // SDL_SCANCODE_KP_2
		KeyboardKey.KEY_KP_3,           // SDL_SCANCODE_KP_3
		KeyboardKey.KEY_KP_4,           // SDL_SCANCODE_KP_4
		KeyboardKey.KEY_KP_5,           // SDL_SCANCODE_KP_5
		KeyboardKey.KEY_KP_6,           // SDL_SCANCODE_KP_6
		KeyboardKey.KEY_KP_7,           // SDL_SCANCODE_KP_7
		KeyboardKey.KEY_KP_8,           // SDL_SCANCODE_KP_8
		KeyboardKey.KEY_KP_9,           // SDL_SCANCODE_KP_9
		KeyboardKey.KEY_KP_0,           // SDL_SCANCODE_KP_0
		KeyboardKey.KEY_KP_DECIMAL,     // SDL_SCANCODE_KP_PERIOD
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0,
		KeyboardKey.KEY_LEFT_CONTROL,   //SDL_SCANCODE_LCTRL
		KeyboardKey.KEY_LEFT_SHIFT,     //SDL_SCANCODE_LSHIFT
		KeyboardKey.KEY_LEFT_ALT,       //SDL_SCANCODE_LALT
		KeyboardKey.KEY_LEFT_SUPER,     //SDL_SCANCODE_LGUI
		KeyboardKey.KEY_RIGHT_CONTROL,  //SDL_SCANCODE_RCTRL
		KeyboardKey.KEY_RIGHT_SHIFT,    //SDL_SCANCODE_RSHIFT
		KeyboardKey.KEY_RIGHT_ALT,      //SDL_SCANCODE_RALT
		KeyboardKey.KEY_RIGHT_SUPER     //SDL_SCANCODE_RGUI
	};
	public static KeyboardKey TranslateKeyboardKey(SDL_Scancode scancode) {
		if (scancode >= 0 && (int)scancode < SCANCODE_MAPPED_NUM)
			return ScancodeToKey[(int)scancode];

		return KeyboardKey.KEY_NULL;
	}

	public bool KeyAvailable(ref int i, out KeyboardKey key, out double time) {
		if (i < Keyboard.KeyPressQueueCount) {
			key = Keyboard.KeyPressQueue[i];
			time = Keyboard.KeyPressTimeQueue[i];

			i++;
			return true;
		}

		key = KeyboardKey.KEY_NULL;
		time = 0;
		return false;
	}

	public void PushEvent(ref SDL_Event ev) {
		switch (ev.Type) {
			case SDL_EventType.SDL_EVENT_KEY_DOWN: {
					KeyboardKey key = TranslateKeyboardKey(ev.key.scancode);
					if (key != KeyboardKey.KEY_NULL)
						Keyboard.CurrentKeyState[(int)key] = 1;

					Keyboard.EnqueueKeyPress(ref ev);
				}
				break;
			case SDL_EventType.SDL_EVENT_KEY_UP: {
					KeyboardKey key = TranslateKeyboardKey(ev.key.scancode);
					if (key != KeyboardKey.KEY_NULL)
						Keyboard.CurrentKeyState[(int)key] = 0;
				}
				break;
			case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN: {
					int btn = ev.button.button - 1;
					if (btn == 2) btn = 1;
					else if (btn == 1) btn = 2;

					Mouse.CurrentMouseButtonState[btn] = 1;
				}
				break;
			case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP: {
					int btn = ev.button.button - 1;
					if (btn == 2) btn = 1;
					else if (btn == 1) btn = 2;

					Mouse.CurrentMouseButtonState[btn] = 0;
				}
				break;
			case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
				Mouse.CurrentMouseScroll.X = ev.wheel.x;
				Mouse.CurrentMouseScroll.Y = ev.wheel.y;
				break;
			case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
				Mouse.CurrentMousePosition.X = ev.motion.x;
				Mouse.CurrentMousePosition.Y = ev.motion.y;
				break;
			case SDL_EventType.SDL_EVENT_WINDOW_RESIZED:
			case SDL_EventType.SDL_EVENT_WINDOW_PIXEL_SIZE_CHANGED: {
					int width = ev.window.data1;
					int height = ev.window.data2;
					SetupViewport(width, height);
					ScreenSize.W = width;
					ScreenSize.H = height;
					CurrentFbo.W = width;
					CurrentFbo.H = height;
					ResizedLastFrame = true;
				}
				break;
			case SDL_EventType.SDL_EVENT_WINDOW_EXPOSED: {
					int width, height;
					SDL3.SDL_GetWindowSize(SDL3.SDL_GetWindowFromID(ev.window.windowID), &width, &height);
					SetupViewport(width, height);
					ScreenSize.W = width;
					ScreenSize.H = height;
					CurrentFbo.W = width;
					CurrentFbo.H = height;
					ResizedLastFrame = true;
				}
				break;
			case SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED: UserWantsToClose = true; break;
		}
	}

	public static void PollInputEvents() {
		SDL_Event ev;
		unsafe {
			while (SDL3.SDL_PollEvent(&ev)) {
				switch (ev.Type) {
					case SDL_EventType.SDL_EVENT_TERMINATING: break;
					case SDL_EventType.SDL_EVENT_LOW_MEMORY: break;
					case SDL_EventType.SDL_EVENT_LOCALE_CHANGED: break;
					case SDL_EventType.SDL_EVENT_SYSTEM_THEME_CHANGED: break;
					case SDL_EventType.SDL_EVENT_CLIPBOARD_UPDATE: break;
					// Todo: a way to intercept this
					case SDL_EventType.SDL_EVENT_QUIT: Environment.Exit(0); break;
					default:
						var window = SDL3.SDL_GetWindowFromEvent(&ev);
						var windowID = SDL3.SDL_GetWindowID(window);

						if (windowLookup_id2window.TryGetValue(windowID, out var osWindow))
							osWindow.PushEvent(ref ev);

						break;
				}
			}
		}
	}

	public bool Fullscreen {
		get => false;
		set { }
	}

	public bool Maximized {
		get => SDL3.SDL_GetWindowFlags(handle).HasFlag(SDL_WindowFlags.SDL_WINDOW_MAXIMIZED);
		set => SDL3.SDL_MaximizeWindow(handle);
	}

	public bool Minimized {
		get => SDL3.SDL_GetWindowFlags(handle).HasFlag(SDL_WindowFlags.SDL_WINDOW_MINIMIZED);
		set => SDL3.SDL_MinimizeWindow(handle);
	}

	public bool Visible {
		get => !SDL3.SDL_GetWindowFlags(handle).HasFlag(SDL_WindowFlags.SDL_WINDOW_HIDDEN);
		set {
			if (value) SDL3.SDL_ShowWindow(handle);
			else SDL3.SDL_HideWindow(handle);
		}
	}

	public bool InputFocused {
		get => SDL3.SDL_GetWindowFlags(handle).HasFlag(SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS);
	}

	public bool MouseFocused {
		get => SDL3.SDL_GetWindowFlags(handle).HasFlag(SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS);
	}

	public bool Focusable {
		get => !SDL3.SDL_GetWindowFlags(handle).HasFlag(SDL_WindowFlags.SDL_WINDOW_NOT_FOCUSABLE);
		set => SDL3.SDL_SetWindowFocusable(handle, value);
	}

	public bool Topmost {
		get => !SDL3.SDL_GetWindowFlags(handle).HasFlag(SDL_WindowFlags.SDL_WINDOW_ALWAYS_ON_TOP);
		set => SDL3.SDL_SetWindowAlwaysOnTop(handle, value);
	}

	public bool AlwaysRun {
		get => false;
		set => Logs.Warn("AlwaysRun is unsupported on a SDL backend.");
	}

	public bool Transparent {
		get => false;
		set => Logs.Warn("Transparency is unsupported on a SDL backend.");
	}

	public bool HighDPI {
		get => false;
		set => Logs.Warn("Setting HighDPI is unsupported on a SDL backend.");
	}

	public bool MousePassthru {
		get => false;
		set => Logs.Warn("Mouse passthrough is unsupported on a SDL backend.");
	}

	public void Close() {
		SDL3.SDL_DestroyCursor(cursor);
		SDL3.SDL_GL_DestroyContext(glctx);
		SDL3.SDL_DestroyWindow(handle);
	}

	public void SetIcon(Image image) {
		SDL_Surface* iconSurface = null;

		uint rmask, gmask, bmask, amask;
		int depth = 0, pitch = 0;

		switch (image.Format) {
			case PixelFormat.PIXELFORMAT_UNCOMPRESSED_GRAYSCALE:
				rmask = 0xFF; gmask = 0;
				bmask = 0; amask = 0;
				depth = 8; pitch = image.Width;
				break;
			case PixelFormat.PIXELFORMAT_UNCOMPRESSED_GRAY_ALPHA:
				rmask = 0xFF; gmask = 0xFF00;
				bmask = 0; amask = 0;
				depth = 16; pitch = image.Width * 2;
				break;
			case PixelFormat.PIXELFORMAT_UNCOMPRESSED_R5G6B5:
				rmask = 0xF800; gmask = 0x07E0;
				bmask = 0x001F; amask = 0;
				depth = 16; pitch = image.Width * 2;
				break;
			case PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8:
				rmask = 0xFF0000; gmask = 0x00FF00;
				bmask = 0x0000FF; amask = 0;
				depth = 24; pitch = image.Width * 3;
				break;
			case PixelFormat.PIXELFORMAT_UNCOMPRESSED_R5G5B5A1:
				rmask = 0xF800; gmask = 0x07C0;
				bmask = 0x003E; amask = 0x0001;
				depth = 16; pitch = image.Width * 2;
				break;
			case PixelFormat.PIXELFORMAT_UNCOMPRESSED_R4G4B4A4:
				rmask = 0xF000; gmask = 0x0F00;
				bmask = 0x00F0; amask = 0x000F;
				depth = 16; pitch = image.Width * 2;
				break;
			case PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8:
				rmask = 0xFF000000; gmask = 0x00FF0000;
				bmask = 0x0000FF00; amask = 0x000000FF;
				depth = 32; pitch = image.Width * 4;
				break;
			case PixelFormat.PIXELFORMAT_UNCOMPRESSED_R32:
				rmask = 0xFFFFFFFF; gmask = 0;
				bmask = 0; amask = 0;
				depth = 32; pitch = image.Width * 4;
				break;
			case PixelFormat.PIXELFORMAT_UNCOMPRESSED_R32G32B32:
				rmask = 0xFFFFFFFF; gmask = 0xFFFFFFFF;
				bmask = 0xFFFFFFFF; amask = 0;
				depth = 96; pitch = image.Width * 12;
				break;
			case PixelFormat.PIXELFORMAT_UNCOMPRESSED_R32G32B32A32:
				rmask = 0xFFFFFFFF; gmask = 0xFFFFFFFF;
				bmask = 0xFFFFFFFF; amask = 0xFFFFFFFF;
				depth = 128; pitch = image.Width * 16;
				break;
			case PixelFormat.PIXELFORMAT_UNCOMPRESSED_R16:
				rmask = 0xFFFF; gmask = 0;
				bmask = 0; amask = 0;
				depth = 16; pitch = image.Width * 2;
				break;
			case PixelFormat.PIXELFORMAT_UNCOMPRESSED_R16G16B16:
				rmask = 0xFFFF; gmask = 0xFFFF;
				bmask = 0xFFFF; amask = 0;
				depth = 48; pitch = image.Width * 6;
				break;
			case PixelFormat.PIXELFORMAT_UNCOMPRESSED_R16G16B16A16:
				rmask = 0xFFFF; gmask = 0xFFFF;
				bmask = 0xFFFF; amask = 0xFFFF;
				depth = 64; pitch = image.Width * 8;
				break;
			default:
				// Compressed formats are not supported
				return;
		}

		iconSurface = SDL3.SDL_CreateSurfaceFrom(image.Width, image.Height, SDL3.SDL_GetPixelFormatForMasks(depth, rmask, gmask, bmask, amask), (nint)image.Data, pitch);

		if (iconSurface != null) {
			SDL3.SDL_SetWindowIcon(handle, iconSurface);
			SDL3.SDL_DestroySurface(iconSurface);
		}
	}

	public Vector2F Position {
		get {
			int _x, _y;
			SDL3.SDL_GetWindowPosition(handle, &_x, &_y);
			return new(_x, _y);
		}
		set => SDL3.SDL_SetWindowPosition(handle, (int)value.X, (int)value.Y);
	}

	public Vector2F Size {
		get {
			int _x, _y;
			SDL3.SDL_GetWindowSize(handle, &_x, &_y);
			return new(_x, _y);
		}
		set => SDL3.SDL_SetWindowSize(handle, (int)value.X, (int)value.Y);
	}

	public Vector2F MinSize {
		get {
			int _x, _y;
			SDL3.SDL_GetWindowMinimumSize(handle, &_x, &_y);
			return new(_x, _y);
		}
		set => SDL3.SDL_SetWindowMinimumSize(handle, (int)value.X, (int)value.Y);
	}

	public Vector2F MaxSize {
		get {
			int _x, _y;
			SDL3.SDL_GetWindowMaximumSize(handle, &_x, &_y);
			return new(_x, _y);
		}
		set => SDL3.SDL_SetWindowMaximumSize(handle, (int)value.X, (int)value.Y);
	}

	public string Title {
		get => SDL3.SDL_GetWindowTitle(handle) ?? "";
		set => SDL3.SDL_SetWindowTitle(handle, value);
	}

	public int Monitor {
		get => (int)SDL3.SDL_GetDisplayForWindow(handle);
	}

	public float Opacity {
		get => SDL3.SDL_GetWindowOpacity(handle);
		set => SDL3.SDL_SetWindowOpacity(handle, Math.Clamp(value, 0, 1));
	}

	public void FocusWindow() => SDL3.SDL_RaiseWindow(handle);

	public void* Handle => handle;

	public int GetMonitorPhysicalWidth(int monitor) {
		if (!OS.IsMonitorIDValid(monitor)) { Logs.Warn("(SDL) Failed to find the monitor."); return 0; }
		var dpi = SDL3.SDL_GetWindowDisplayScale(handle) * 96;
		return (int)((SDL3.SDL_GetCurrentDisplayMode((SDL_DisplayID)monitor)->w / dpi) * 25.4f);
	}

	public int GetMonitorPhysicalHeight(int monitor) {
		if (!OS.IsMonitorIDValid(monitor)) { Logs.Warn("(SDL) Failed to find the monitor."); return 0; }
		var dpi = SDL3.SDL_GetWindowDisplayScale(handle) * 96;
		return (int)((SDL3.SDL_GetCurrentDisplayMode((SDL_DisplayID)monitor)->h / dpi) * 25.4f);
	}

	public Vector2F GetWindowScaleDPI() {
		return new(SDL3.SDL_GetWindowDisplayScale(handle));
	}

	public void EnableCursor() {
		SDL3.SDL_SetWindowRelativeMouseMode(handle, false);
		OS.ShowCursor();
	}
	public void DisableCursor() {
		SDL3.SDL_SetWindowRelativeMouseMode(handle, true);
		OS.HideCursor();
	}

	SDL_Cursor* cursor;

	public void SetMouseCursor(MouseCursor cursor) {
		var lastCursor = this.cursor;
		this.cursor = SDL3.SDL_CreateSystemCursor(cursor switch {
			MouseCursor.MOUSE_CURSOR_DEFAULT => SDL_SystemCursor.SDL_SYSTEM_CURSOR_DEFAULT,
			MouseCursor.MOUSE_CURSOR_ARROW => SDL_SystemCursor.SDL_SYSTEM_CURSOR_DEFAULT,
			MouseCursor.MOUSE_CURSOR_IBEAM => SDL_SystemCursor.SDL_SYSTEM_CURSOR_TEXT,
			MouseCursor.MOUSE_CURSOR_CROSSHAIR => SDL_SystemCursor.SDL_SYSTEM_CURSOR_CROSSHAIR,
			MouseCursor.MOUSE_CURSOR_POINTING_HAND => SDL_SystemCursor.SDL_SYSTEM_CURSOR_POINTER,
			MouseCursor.MOUSE_CURSOR_RESIZE_EW => SDL_SystemCursor.SDL_SYSTEM_CURSOR_EW_RESIZE,
			MouseCursor.MOUSE_CURSOR_RESIZE_NS => SDL_SystemCursor.SDL_SYSTEM_CURSOR_NS_RESIZE,
			MouseCursor.MOUSE_CURSOR_RESIZE_NWSE => SDL_SystemCursor.SDL_SYSTEM_CURSOR_NWSE_RESIZE,
			MouseCursor.MOUSE_CURSOR_RESIZE_NESW => SDL_SystemCursor.SDL_SYSTEM_CURSOR_NESW_RESIZE,
			MouseCursor.MOUSE_CURSOR_RESIZE_ALL => SDL_SystemCursor.SDL_SYSTEM_CURSOR_MOVE,
			MouseCursor.MOUSE_CURSOR_NOT_ALLOWED => SDL_SystemCursor.SDL_SYSTEM_CURSOR_NOT_ALLOWED,
			_ => SDL_SystemCursor.SDL_SYSTEM_CURSOR_DEFAULT
		});
		SDL3.SDL_SetCursor(this.cursor);
		if (lastCursor != null)
			SDL3.SDL_DestroyCursor(lastCursor);
	}

	public bool UserClosed() {
		if (UserWantsToClose) {
			UserWantsToClose = false;
			return true;
		}

		return false;
	}

	public const float RL_CULL_DISTANCE_NEAR = 0.01f;
	public const float RL_CULL_DISTANCE_FAR = 1000f;

	public void ClearBackground(int r, int g, int b, int a) => ClearBackground((byte)r, (byte)g, (byte)b, (byte)a);
	public void ClearBackground(byte r, byte g, byte b, byte a) {
		Rlgl.ClearColor(r, g, b, a);
		Rlgl.ClearScreenBuffers();
	}
	public void ClearBackground(int r, int g, int b) => ClearBackground(r, g, b, 255);
	public void ClearBackground(Color c) => ClearBackground(c.R, c.G, c.B, c.A);

	public void BeginScissorMode(int x, int y, int width, int height) {
		Rlgl.DrawRenderBatchActive();
		Rlgl.EnableScissorTest();

		if (!UsingFbo) {
			Vector2F scale = GetWindowScaleDPI();
			Rlgl.Scissor((int)(x * scale.x), (int)(Size.H * scale.y - (((y + height) * scale.y))), (int)(width * scale.x), (int)(height * scale.y));
		}
		else {
			Rlgl.Scissor(x, (int)CurrentFbo.H - (y + height), width, height);
		}
	}

	public void EndScissorMode() {
		Rlgl.DrawRenderBatchActive();
		Rlgl.DisableScissorTest();
	}

	public void BeginMode2D(Camera2D camera) {
		Rlgl.DrawRenderBatchActive();
		Rlgl.LoadIdentity();
		Rlgl.MultMatrixf(Raymath.MatrixToFloatV(Raylib.GetCameraMatrix2D(camera)));
		Rlgl.MultMatrixf(Raymath.MatrixToFloatV(ScreenScale));
	}

	public void EndMode2D() {
		Rlgl.DrawRenderBatchActive();
		Rlgl.LoadIdentity();
		Rlgl.MultMatrixf(Raymath.MatrixToFloatV(ScreenScale));
	}

	public void BeginMode3D(Camera3D camera) {
		Rlgl.DrawRenderBatchActive();

		Rlgl.MatrixMode(MatrixMode.Projection);
		Rlgl.PushMatrix();
		Rlgl.LoadIdentity();

		float aspect = (float)Rlgl.GetFramebufferWidth() / (float)Rlgl.GetFramebufferHeight();

		if (camera.Projection == CameraProjection.Perspective) {
			double top = RL_CULL_DISTANCE_NEAR * MathF.Tan(camera.FovY * 0.5f * (MathF.PI / 180f));
			double right = top * aspect;

			Rlgl.Frustum(-right, right, -top, top, RL_CULL_DISTANCE_NEAR, RL_CULL_DISTANCE_FAR);
		}
		else if (camera.Projection == CameraProjection.Orthographic) {
			double top = camera.FovY / 2.0;
			double right = top * aspect;

			Rlgl.Ortho(-right, right, -top, top, RL_CULL_DISTANCE_NEAR, RL_CULL_DISTANCE_FAR);
		}

		Rlgl.MatrixMode(MatrixMode.ModelView);
		Rlgl.LoadIdentity();

		// Setup Camera view
		Matrix4x4 matView = Raymath.MatrixLookAt(camera.Position, camera.Target, camera.Up);
		Rlgl.MultMatrixf(Raymath.MatrixToFloatV(matView));

		Rlgl.EnableDepthTest();
	}

	public void EndMode3D() {
		Rlgl.DrawRenderBatchActive();
		Rlgl.MatrixMode(MatrixMode.Projection);
		Rlgl.PopMatrix();
		Rlgl.MatrixMode(MatrixMode.ModelView);
		Rlgl.LoadIdentity();
		Rlgl.MultMatrixf(Raymath.MatrixToFloatV(ScreenScale));
		Rlgl.DisableDepthTest();
	}

	// Initializes render texture for drawing
	public void BeginTextureMode(RenderTexture2D target) {
		Rlgl.DrawRenderBatchActive();      // Update and draw internal render batch

		Rlgl.EnableFramebuffer(target.Id); // Enable render target

		// Set viewport and RLGL internal framebuffer size
		Rlgl.Viewport(0, 0, target.Texture.Width, target.Texture.Height);
		Rlgl.SetFramebufferWidth(target.Texture.Width);
		Rlgl.SetFramebufferHeight(target.Texture.Height);

		Rlgl.MatrixMode(MatrixMode.Projection);    // Switch to projection matrix
		Rlgl.LoadIdentity();               // Reset current matrix (projection)

		// Set orthographic projection to current framebuffer size
		// NOTE: Configured top-left corner as (0, 0)
		Rlgl.Ortho(0, target.Texture.Width, target.Texture.Height, 0, 0.0f, 1.0f);

		Rlgl.MatrixMode(MatrixMode.ModelView);     // Switch back to modelview matrix
		Rlgl.LoadIdentity();               // Reset current matrix (modelview)

		//rlScalef(0.0f, -1.0f, 0.0f);  // Flip Y-drawing (?)

		// Setup current width/height for proper aspect ratio
		// calculation when using BeginMode3D()
		CurrentFbo.X = target.Texture.Width;
		CurrentFbo.Y = target.Texture.Height;
		UsingFbo = true;
	}

	// Ends drawing to render texture
	public void EndTextureMode() {
		Rlgl.DrawRenderBatchActive();      // Update and draw internal render batch

		Rlgl.DisableFramebuffer();         // Disable render target (fbo)

		// Set viewport to default framebuffer size
		SetupViewport(RenderSize.W, RenderSize.H);

		// Reset current fbo to screen size
		CurrentFbo.X = RenderSize.W;
		CurrentFbo.Y = RenderSize.H;
		UsingFbo = false;
	}

	/// <summary>
	/// Writes the <see cref="WindowMouseState"/> into a <see cref="Input.MouseState"/> structure, then resets the <see cref="WindowMouseState"/> and prepares it for the next time this method is called.
	/// <br/>This adds a layer of abstraction over how the engine windows handle input vs. how the game code handles input.
	/// </summary>
	/// <param name="keyboardState"></param>
	internal void FlushMouseStateInto(ref Input.MouseState ms) {
		int m1 = (int)MouseButton.MOUSE_LEFT_BUTTON, m2 = (int)MouseButton.MOUSE_RIGHT_BUTTON, m3 = (int)MouseButton.MOUSE_BUTTON_MIDDLE, m4 = (int)MouseButton.MOUSE_BUTTON_FORWARD, m5 = (int)MouseButton.MOUSE_BUTTON_BACK;

		ms.MousePos = Mouse.CurrentMousePosition;
		ms.MouseDelta = Mouse.CurrentMousePosition - Mouse.PreviousMousePosition;
		ms.MouseScroll = Mouse.CurrentMouseScroll;

		int pm1 = Mouse.PreviousMouseButtonState[m1], pm2 = Mouse.PreviousMouseButtonState[m2], pm3 = Mouse.PreviousMouseButtonState[m3], pm4 = Mouse.PreviousMouseButtonState[m4], pm5 = Mouse.PreviousMouseButtonState[m5];
		int cm1 = Mouse.CurrentMouseButtonState[m1], cm2 = Mouse.CurrentMouseButtonState[m2], cm3 = Mouse.CurrentMouseButtonState[m3], cm4 = Mouse.CurrentMouseButtonState[m4], cm5 = Mouse.CurrentMouseButtonState[m5];

		ms.Mouse1Clicked = pm1 == 0 && cm1 == 1;
		ms.Mouse2Clicked = pm2 == 0 && cm2 == 1;
		ms.Mouse3Clicked = pm3 == 0 && cm3 == 1;
		ms.Mouse4Clicked = pm4 == 0 && cm4 == 1;
		ms.Mouse5Clicked = pm5 == 0 && cm5 == 1;

		ms.Mouse1Released = pm1 == 1 && cm1 == 0;
		ms.Mouse2Released = pm2 == 1 && cm2 == 0;
		ms.Mouse3Released = pm3 == 1 && cm3 == 0;
		ms.Mouse4Released = pm4 == 1 && cm4 == 0;
		ms.Mouse5Released = pm5 == 1 && cm5 == 0;

		ms.Mouse1Held = cm1 == 1;
		ms.Mouse2Held = cm2 == 1;
		ms.Mouse3Held = cm3 == 1;
		ms.Mouse4Held = cm4 == 1;
		ms.Mouse5Held = cm5 == 1;

		Mouse.Reset();
	}

	/// <summary>
	/// Writes the <see cref="WindowKeyboardState"/> into a <see cref="Input.KeyboardState"/> structure, then resets the <see cref="WindowKeyboardState"/> and prepares it for the next time this method is called.
	/// <br/>This adds a layer of abstraction over how the engine windows handle input vs. how the game code handles input.
	/// </summary>
	/// <param name="keyboardState"></param>
	internal void FlushKeyboardStateInto(ref Input.KeyboardState keyboardState) {
		int i = 0;

		while (KeyAvailable(ref i, out KeyboardKey key, out double timePressed)) {
			int keyPressed = (int)key;
			keyboardState.PushKeyPress((int)key, timePressed);
		}

		for (int j = 0; j < WindowKeyboardState.MAX_KEYBOARD_KEYS; j++) {
			var prev = Keyboard.PreviousKeyState[j];
			var curr = Keyboard.CurrentKeyState[j];

			keyboardState.KeysDown[j] = curr == 1;
			keyboardState.KeysReleased[j] = prev > 0 && curr == 0;
		}

		Keyboard.Reset();
	}

	public void SetMousePosition(Vector2F dragStart) {

	}
}

public static unsafe class OS
{
	private static bool initialized = false;
	public static bool InitPlatform() {
		if (initialized) return true;

		if (!SDL3.SDL_Init(
			SDL_InitFlags.SDL_INIT_AUDIO |
			SDL_InitFlags.SDL_INIT_CAMERA |
			SDL_InitFlags.SDL_INIT_EVENTS |
			SDL_InitFlags.SDL_INIT_GAMEPAD |
			SDL_InitFlags.SDL_INIT_HAPTIC |
			SDL_InitFlags.SDL_INIT_JOYSTICK |
			SDL_InitFlags.SDL_INIT_SENSOR |
			SDL_InitFlags.SDL_INIT_VIDEO
		))
			return false;

		var version = Rlgl.GetVersion();
		switch (version) {
			case GlVersion.OPENGL_21:
				if (!SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MAJOR_VERSION, 2)) return false;
				if (!SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MINOR_VERSION, 1)) return false;
				break;
			case GlVersion.OPENGL_33:
				if (!SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MAJOR_VERSION, 3)) return false;
				if (!SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MINOR_VERSION, 3)) return false;
#if COMPILED_OSX
				if (!SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_FLAGS, (int)SDL_GLContextFlag.SDL_GL_CONTEXT_FORWARD_COMPATIBLE_FLAG)) return false;
#else
				if (!SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL_GLProfile.SDL_GL_CONTEXT_PROFILE_CORE)) return false;
#endif
				break;
			case GlVersion.OPENGL_43:
				if (!SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MAJOR_VERSION, 4)) return false;
				if (!SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MINOR_VERSION, 3)) return false;
				if (!SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL_GLProfile.SDL_GL_CONTEXT_PROFILE_CORE)) return false;
				break;
			case GlVersion.OPENGL_ES_20:
				if (!SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MAJOR_VERSION, 2)) return false;
				if (!SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MINOR_VERSION, 0)) return false;
				if (!SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL_GLProfile.SDL_GL_CONTEXT_PROFILE_ES)) return false;
				break;
		}

		initialized = true;
		return true;
	}
	[UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
	public static void* OpenGL_GetProcAddress(byte* name) {
		return (void*)SDL3.SDL_GL_GetProcAddress(name);
	}

	public static bool IsMonitorIDValid(int idx) => idx > 0 && idx < GetMonitorCount();
	public static int GetMonitorCount() => SDL3.SDL_GetDisplays()?.Count ?? 0;
	public static int GetMonitorWidth(int monitor) {
		if (!IsMonitorIDValid(monitor)) { Logs.Warn("(SDL) Failed to find the monitor."); return 0; }
		return SDL3.SDL_GetCurrentDisplayMode((SDL_DisplayID)monitor)->w;
	}
	public static int GetMonitorHeight(int monitor) {
		if (!IsMonitorIDValid(monitor)) { Logs.Warn("(SDL) Failed to find the monitor."); return 0; }
		return SDL3.SDL_GetCurrentDisplayMode((SDL_DisplayID)monitor)->h;
	}
	public static Vector2F GetMonitorPosition(int monitor) {
		if (!IsMonitorIDValid(monitor)) { Logs.Warn("(SDL) Failed to find the monitor."); return new(0); }

		SDL_Rect rect;
		SDL3.SDL_GetDisplayUsableBounds((SDL_DisplayID)monitor, &rect);

		return new(rect.x, rect.y);
	}
	public static Vector2F GetMonitorSize(int monitor) {
		if (!IsMonitorIDValid(monitor)) { Logs.Warn("(SDL) Failed to find the monitor."); return new(0); }

		SDL_Rect rect;
		SDL3.SDL_GetDisplayUsableBounds((SDL_DisplayID)monitor, &rect);

		return new(rect.w, rect.h);
	}

	public static float GetMonitorRefreshRate(int monitor) {
		if (!IsMonitorIDValid(monitor)) { Logs.Warn("(SDL) Failed to find the monitor."); return 0; }
		return SDL3.SDL_GetCurrentDisplayMode((SDL_DisplayID)monitor)->refresh_rate;
	}

	public static string GetDisplayName(int monitor) {
		if (!IsMonitorIDValid(monitor)) { Logs.Warn("(SDL) Failed to find the monitor."); return "<no monitor>"; }
		return SDL3.SDL_GetDisplayName((SDL_DisplayID)monitor) ?? "<null>";
	}

	public static string GetClipboardText() => SDL3.SDL_GetClipboardText() ?? "";
	public static void SetClipboardText(string text) => SDL3.SDL_SetClipboardText(text);

	public static bool HasClipboardText() => SDL3.SDL_HasClipboardText();

	public static void ShowCursor() {
		SDL3.SDL_ShowCursor();
	}

	public static void HideCursor() {
		SDL3.SDL_HideCursor();
	}

	public static double TicksToTime(ulong ticks) => (double)ticks / 1_000_000_000d;
	public static double GetTime() => TicksToTime(SDL3.SDL_GetTicksNS());

	/// <summary>
	/// Performs thread sleeping, but at the end, busy-loops to ensure tight frame timing.
	/// </summary>
	/// <param name="seconds">How long, in seconds, should the thread sleep/busy wait for</param>
	public static void Wait(double seconds) {
		double start = GetTime();
		double sleepFor = seconds - (seconds * 0.05);
		Thread.Sleep((int)(sleepFor * 1000));
		double left = GetTime() - start;
		if (left > 0) {
			while ((GetTime() - start) < seconds) { }
		}
	}
}