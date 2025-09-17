using Nucleus.Types;

using Raylib_cs;

using SDL;

using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;

namespace Nucleus.Engine;

public struct OSMonitor : IValidatable
{
	public int DisplayID;
	public OSMonitor(int id) => DisplayID = id;
	public static implicit operator OSMonitor(int id) => new(id);
	public static implicit operator OSMonitor(SDL_DisplayID id) => new((int)id);

	public bool IsValid() {
		if (!OS.IsMonitorIDValid(DisplayID)) {
			Logs.Warn("OSDisplay: Failed to find the monitor.");
			return false;
		}

		return true;
	}

	public unsafe int X => (int)Position.X;
	public unsafe int Y => (int)Position.Y;
	public unsafe int Width => (int)Size.W;
	public unsafe int Height => (int)Size.H;
	public unsafe Vector2F Position {
		get {
			if (!IsValid())
				return Vector2F.Zero;

			SDL_Rect rect;
			SDL3.SDL_GetDisplayUsableBounds((SDL_DisplayID)DisplayID, &rect);

			return new(rect.x, rect.y);
		}
	}
	public unsafe Vector2F Size {
		get {
			if (!IsValid())
				return Vector2F.Zero;
			var dpm = SDL3.SDL_GetCurrentDisplayMode((SDL_DisplayID)DisplayID);
			return new(dpm->w, dpm->h);
		}
	}

	public unsafe Vector2F Bounds {
		get {
			if (!IsValid())
				return Vector2F.Zero;
			SDL_Rect bounds;
			SDL3.SDL_GetDisplayUsableBounds((SDL_DisplayID)DisplayID, &bounds);
			return new(bounds.w, bounds.h);
		}
	}

	public unsafe float RefreshRate {
		get {
			if (!IsValid())
				return 0;
			return SDL3.SDL_GetCurrentDisplayMode((SDL_DisplayID)DisplayID)->refresh_rate;
		}
	}

	public unsafe string DisplayName {
		get {
			if (!IsValid())
				return "<no-monitor>";
			return SDL3.SDL_GetDisplayName((SDL_DisplayID)DisplayID) ?? "<null>";
		}
	}
}

public struct SDL3AppMetaData
{
	public readonly string appname;
	public readonly string appversion;
	public readonly string appidentifier;

	public SDL3AppMetaData(string appName, string appVersion, string appIdentifier) =>
		(appname, appversion, appidentifier) = (appName, appVersion, appIdentifier);
}

public static unsafe class OS
{
	private static bool initialized = false;
	private static SDL3AppMetaData MetaData;
	public static void GetAppMetaData(out SDL3AppMetaData metadata) {
		if (!initialized)
			throw new InvalidOperationException("Cannot get app metadata until OS static class initialized!");
		metadata = MetaData;
	}
	public static bool InitSDL(SDL3AppMetaData metadata) {
		if (initialized) return true;

		if (!SDL3.SDL_SetAppMetadata(metadata.appname, metadata.appversion, metadata.appidentifier))
			Logs.Warn("Failed to set app metadata for SDL3");

		if (!SDL3.SDL_Init(
			SDL_InitFlags.SDL_INIT_AUDIO |
			//SDL_InitFlags.SDL_INIT_CAMERA |
			SDL_InitFlags.SDL_INIT_EVENTS |
			SDL_InitFlags.SDL_INIT_GAMEPAD |
			SDL_InitFlags.SDL_INIT_HAPTIC |
			SDL_InitFlags.SDL_INIT_JOYSTICK |
			SDL_InitFlags.SDL_INIT_SENSOR |
			SDL_InitFlags.SDL_INIT_VIDEO
		))
			return false;

		if (!SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_STENCIL_SIZE, 8)) return false;
		var version = Rlgl.GetVersion();
		switch (version) {
			case GlVersion.OPENGL_21:
				if (!SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MAJOR_VERSION, 2)) return false;
				if (!SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MINOR_VERSION, 1)) return false;
				break;
			case GlVersion.OPENGL_33:
				if (!SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MAJOR_VERSION, 3)) return false;
				if (!SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MINOR_VERSION, 3)) return false;
				if (!SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL_GLProfile.SDL_GL_CONTEXT_PROFILE_CORE)) return false;
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

		MetaData = metadata;
		initialized = true;
		return true;
	}
	[UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
	public static void* OpenGL_GetProcAddress(byte* name) {
		return (void*)SDL3.SDL_GL_GetProcAddress(name);
	}

	public static bool IsMonitorIDValid(int idx) => idx > 0 && idx < GetMonitorCount();
	public static int GetMonitorCount() => SDL3.SDL_GetDisplays()?.Count ?? 0;
	public static OSMonitor GetPrimaryMonitor() => SDL3.SDL_GetPrimaryDisplay();

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
			while ((GetTime() - start) < seconds) ;
		}
	}
}
