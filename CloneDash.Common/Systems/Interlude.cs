using AssetStudio;
using CloneDash.Common.Interludes;
using CloneDash.Game;
using Nucleus;
using Nucleus.Commands;
using Nucleus.Common.Graphics;
using Nucleus.Common.Images;
using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Files;
using Nucleus.Rendering;
using Nucleus.Types;
using Nucleus.Util;

using Raylib_cs;

using System.Diagnostics;
using System.Text.RegularExpressions;

using Texture2D = Raylib_cs.Texture2D;

namespace CloneDash;

/// <summary>
/// Called during level loads. Adds a little something during loading operations, while
/// running on the main thread.
/// </summary>
[Nucleus.MarkForStaticConstruction]
public static class Interlude
{
	private static Stopwatch limiter = new();
	private static double lastFrame = -100;
	private static bool inInterlude;
	private static string? loadMsg;
	private static string? loadSubMsg;
	private static ITexture? interludeTexture;

	private static bool _should = false;
	public static bool ShouldSelectInterludeTexture {
		get => _should;
		set {
			if (!_should && value) {
				_should = value;
				if (interludeTexture == null && pendingImage == null) {
					determineInterludeTexture(); // load interlude texture now. Only really used for the loading screen when starting the game
				}
			}

			_should = value;
		}
	}

	static Task<(IInterludeDescriptor Descriptor, Image Image)?>? pendingImage;
	private static void determineInterludeTexture() {
		if (!_should) return;

		IInterludeDescriptor[] descriptors = [.. InterludeMod.Instance.GetAllDescriptors()];
		if (descriptors.Length == 0) return;

		Random.Shared.Shuffle(descriptors);
		pendingImage = Task.Run<(IInterludeDescriptor Descriptor, Image Image)?>(() => {
			foreach (var descriptor in descriptors)
				if (descriptor.LoadImage() is Image image)
					return (descriptor, image);
			return null;
		});
	}

	private static void uploadPendingImage() {
		if (pendingImage == null || !pendingImage.IsCompleted)
			return;

		if (pendingImage.IsFaulted)
			Logs.Warn($"Failed to load the interlude texture: {pendingImage.Exception?.InnerException?.Message}");
		else if (pendingImage.Result is { } loaded) {
			interludeTexture = loaded.Descriptor.CreateTexture(loaded.Image);
			Image.UnloadImage(loaded.Image);
		}

		pendingImage = null;
	}

	private static void reset() {
		pendingImage?.ContinueWith(static task => {
			if (task.IsCompletedSuccessfully && task.Result is { } loaded)
				Image.UnloadImage(loaded.Image);
		});
		pendingImage = null;

		if (interludeTexture != null)
			interludeTexture.Dispose();

		limiter.Reset();
		lastFrame = -100;
		interludeTexture = null;
		loadMsg = null;
		loadSubMsg = null;
		inInterlude = false;
	}

	public static void Begin(string? loadMsg = null) {
		reset(); limiter.Start();     // Set up stopwatch for fps limiting
		EngineCore.StopSound();       // Tell engine to stop playing sounds please
		determineInterludeTexture();  // pick an interlude texture
		Interlude.loadMsg = loadMsg;
		inInterlude = true; Spin();   // render one interlude frame now
	}

	public static string? GetCurrentMessage() => loadMsg;
	public static string? GetCurrentSubmessage() => loadSubMsg;

	/// <summary>
	/// Renders the interlude texture, progress, etc, and swaps the frame buffer.
	/// It is automatically limited to 30 FPS updates; so you can call this repeatedly with minimal performance loss
	/// </summary>
	public static void Spin(string? message = null, string? submessage = null) {
		if (!inInterlude)
			return;

		if (message != null)
			loadMsg = message; // changes the title message
		if (submessage != null)
			loadSubMsg = submessage; // changes the subtitle message

		if (MainThread.GameThreadSet && Thread.CurrentThread != MainThread.GameThread)
			return;

		var msNow = limiter.Elapsed.TotalSeconds;
		if (lastFrame >= 0 && (msNow - lastFrame) < (1d / 30d))
			return;

		using (StaticSequentialProfiler.AccumulateTime("Interlude.Spin")) {
			lastFrame = msNow;
			// Render cycle
			Rlgl.LoadIdentity();
			Surface.Clear(0);
			var windowSize = EngineCore.GetWindowSize();

			Graphics2D.ResetDrawingOffset(); // Interlude directly takes main-thread control, so the level frame state would never clear this like it usually would
			uploadPendingImage();
			if (interludeTexture != null) {
				Graphics2D.SetTexture(interludeTexture);
				Graphics2D.SetDrawColor(255, 255, 255, 255);
				Graphics2D.DrawTexturedRectangle(0, 0, windowSize.W, windowSize.H);
			}

			var originalBottomSize = 48f;
			var originalTextSize = 28f;
			var originalDesignedRes = 900f;

			var bottomSize = windowSize.H / (originalDesignedRes / originalBottomSize);
			var texSize = windowSize.H / (originalDesignedRes / originalTextSize);
			Graphics2D.SetDrawColor(0, 0, 0);
			Graphics2D.DrawRectangle(0, windowSize.H - bottomSize, windowSize.W, bottomSize);

			Graphics2D.SetDrawColor(new(55)); Graphics2D.DrawRectangle(0, windowSize.H - bottomSize - 2, windowSize.W, 1);
			Graphics2D.SetDrawColor(new(155)); Graphics2D.DrawRectangle(0, windowSize.H - bottomSize - 1, windowSize.W, 1);

			var midBottom = (windowSize.H - bottomSize) + (bottomSize / 2);
			Graphics2D.SetDrawColor(255, 255, 255);
			if (loadSubMsg == null)
				DrawLoadMsg(new(windowSize.W - 42 - 8, midBottom), loadMsg ?? "Loading...", texSize, Anchor.CenterRight);
			else {
				DrawLoadMsg(new(windowSize.W - 42 - 8, midBottom - 6), loadMsg ?? "Loading...", texSize * 0.9f, Anchor.CenterRight);
				Graphics2D.DrawText(new(windowSize.W - 42 - 2, midBottom + 12), loadSubMsg, Graphics2D.UI_FONT_NAME, texSize * 0.6f, Anchor.CenterRight);
			}

			Graphics2D.DrawLoader(windowSize.W - 24, midBottom, time: msNow, inner: 8, outer: 12);
			Surface.Spin();
		}
	}

	private static void DrawLoadMsg(Vector2F position, string loadMsg, float fontSize, Anchor fontAnchor) {
		Match boldRegexMatch = Util.BoldRegex.Match(loadMsg);
		if (boldRegexMatch.Success) {
			ReadOnlySpan<char> before = boldRegexMatch.Groups[1].ValueSpan;
			ReadOnlySpan<char> bold = boldRegexMatch.Groups[2].ValueSpan;
			ReadOnlySpan<char> after = boldRegexMatch.Groups[3].ValueSpan;
			int length = before.Length + bold.Length + after.Length + Graphics2D.UI_CN_JP_FONT_NAME.Length * 2 + Graphics2D.UI_MONO_BOLD_FONT_NAME.Length;

			Graphics2D.TextChunksWriter writer = new(length <= 512 ? stackalloc char[512] : new char[length], stackalloc int[6]);
			writer.Add(before, Graphics2D.UI_CN_JP_FONT_NAME);
			writer.Add(bold, Graphics2D.UI_MONO_BOLD_FONT_NAME);
			writer.Add(after, Graphics2D.UI_CN_JP_FONT_NAME);
			Graphics2D.DrawText(position, writer.ToChunks(), 3, fontSize, fontAnchor);
		}
		else
			Graphics2D.DrawText(position, loadMsg, Graphics2D.UI_CN_JP_FONT_NAME, fontSize, fontAnchor);
	}

	public static void SpinUntilComplete(Task task) {
		while (!((IAsyncResult)task).AsyncWaitHandle.WaitOne(1000 / 30))
			Spin();
	}

	public static void End() {
		limiter.Stop();
		reset();
	}
}
