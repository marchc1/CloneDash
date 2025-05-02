using Nucleus.Types;
using Raylib_cs;
using SDL;
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

	public double[] KeyTimeQueue = new double[MAX_KEY_PRESSED_QUEUE];
	public int[] KeyPressedQueue = new int[MAX_KEY_PRESSED_QUEUE];
	public int KeyPressedQueueCount = 0;

	public double[] CharTimeQueue = new double[MAX_CHAR_PRESSED_QUEUE];
	public int[] CharPressedQueue = new int[MAX_CHAR_PRESSED_QUEUE];
	public int CharPressedQueueCount = 0;

	internal void Reset() {
		for (int i = 0; i < MAX_KEYBOARD_KEYS; i++) {
			PreviousKeyState[i] = CurrentKeyState[i];
			KeyRepeatInFrame[i] = 0;
		}
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
	public Vector2F CurrentMouseWheelMove;
	public Vector2F PreviousMouseWheelMove;

	internal void Reset() {
		PreviousMouseWheelMove.X = CurrentMouseWheelMove.X;
		PreviousMouseWheelMove.Y = CurrentMouseWheelMove.Y;

		CurrentMouseWheelMove.X = 0;
		CurrentMouseWheelMove.Y = 0;

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

		if (window.handle == null) throw Util.Util.MessageBoxException("SDL could not create a window.");

		window.windowID = SDL3.SDL_GetWindowID(window.handle);
		windowLookup_id2window[window.windowID] = window;
		windowLookup_window2id[window] = window.windowID;

		window.ScreenSize.X = width;
		window.ScreenSize.Y = height;
		window.ScreenScale = Raymath.MatrixIdentity();

		SDL3.SDL_SetEventEnabled(SDL_EventType.SDL_EVENT_DROP_FILE, true);

		return window;
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

	public void Event(ref SDL_Event ev) {

	}

	public static void PollInputEvents() {
		SDL_Event ev;
		unsafe {
			while (SDL3.SDL_PollEvent(&ev)) {
				switch (ev.Type) {
					case SDL_EventType.SDL_EVENT_QUIT: break;
					case SDL_EventType.SDL_EVENT_TERMINATING: break;
					case SDL_EventType.SDL_EVENT_LOW_MEMORY: break;
					case SDL_EventType.SDL_EVENT_LOCALE_CHANGED: break;
					case SDL_EventType.SDL_EVENT_SYSTEM_THEME_CHANGED: break;
					case SDL_EventType.SDL_EVENT_CLIPBOARD_UPDATE: break;

					default:
						var window = SDL3.SDL_GetWindowFromEvent(&ev);
						var windowID = SDL3.SDL_GetWindowID(window);

						if (windowLookup_id2window.TryGetValue(windowID, out var osWindow))
							osWindow.Event(ref ev);

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
		this.cursor = SDL3.SDL_CreateSystemCursor(cursor switch {
			MouseCursor.MOUSE_CURSOR_DEFAULT => SDL_SystemCursor.SDL_SYSTEM_CURSOR_DEFAULT,
			_ => SDL_SystemCursor.SDL_SYSTEM_CURSOR_DEFAULT
		});
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

#if COMPILED_OSX
			// todo
#else
		if (!UsingFbo && false) {
			Vector2F scale = GetWindowScaleDPI();
			Rlgl.Scissor((int)(x * scale.x), (int)(CurrentFbo.H - (y + height) * scale.y), (int)(width * scale.x), (int)(height * scale.y));
		}
#endif
		else {
			Rlgl.Scissor(x, Rlgl.GetFramebufferHeight() - (y + height), width, height);
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

	public static double GetTime() => (double)SDL3.SDL_GetTicksNS() / 1_000_000_000d;

	/// <summary>
	/// Performs thread sleeping, but at the end, busy-loops to ensure tight frame timing.
	/// </summary>
	/// <param name="seconds">How long, in seconds, should the thread sleep/busy wait for</param>
	public static void Wait(double seconds) {
		double start = GetTime();
		double sleepFor = seconds - (seconds * 0.05);
		Thread.Sleep((int)(sleepFor * 1000));
		double left = GetTime() - start;
		if(left > 0) {
			while((GetTime() - start) < seconds) {}
		}
	}
}