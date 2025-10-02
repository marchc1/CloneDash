using AssetStudio;

using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;
using CloneDash.Game;

using Nucleus;
using Nucleus.Commands;
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

// todo: finish these
/// <summary>
/// An interlude texture provider.
/// </summary>
public abstract class InterludeTextureProvider
{
	public abstract int Count { get; }
	public abstract bool ShouldFlipTexture { get; }
	public bool Empty => Count == 0;
	public int RandomIndex() => Random.Shared.Next(0, Count);
	public abstract bool Pick(int index, out Texture2D tex);
}

public class MuseDashInterlude
{
	public string? path;
	public Texture2D? LoadTexture() {
		if (path == null) throw new NullReferenceException("Wtf?");
		AssetStudio.Texture2D tex2d = UnityAssetUtils.InternalLoadAsset<AssetStudio.Texture2D>(MuseDashCompatibility.StreamingFiles, Path.GetFileNameWithoutExtension(path));

		if (tex2d.m_TextureFormat == TextureFormat.RGBA32)
			return null;

		var img = tex2d.ToRaylib();
		var tex = Raylib.LoadTextureFromImage(img);
		Raylib.SetTextureFilter(tex, TextureFilter.TEXTURE_FILTER_BILINEAR);
		Raylib.UnloadImage(img);
		return tex;
	}
}

/// <summary>
/// Provides interlude textures from Muse Dash.
/// </summary>
public class MuseDashInterludeProvider : InterludeTextureProvider
{
	static MuseDashInterlude[]? interludes;
	public override bool ShouldFlipTexture => true;
	private static bool ready = false;
	private static int setup() {
		if (ready && interludes != null) return interludes.Length;

		if (MuseDashCompatibility.WhereIsMuseDashInstalled == null) {
			return 0;
		}

		var interludesRaw = UnityAssetUtils.GetAllFiles(MuseDashCompatibility.StreamingFiles, "loadinginterlude_assets_interlude_", regex: true);
		interludes = new MuseDashInterlude[interludesRaw.Length];
		for (int i = 0; i < interludesRaw.Length; i++) {
			interludes[i] = new() {
				path = interludesRaw[i]
			};
		}
		ready = true;

		return interludes.Length;
	}

	public override int Count => setup();

	// Texture 2D used here because itll be loaded when Interlude is initialized.
	// And it will be destroyed immediately after
	public override bool Pick(int index, out Texture2D tex) {
		setup();
		if (!ready) {
			tex = default;
			return false;
		}
		var ttex = interludes?[index]?.LoadTexture();

		if (ttex.HasValue) {
			tex = ttex.Value;
			return true;
		}

		tex = default;
		return false;
	}
}
/// <summary>
/// Provides interlude textures from Clone Dash.
/// </summary>
public class CloneDashInterludeProvider : InterludeTextureProvider
{
	string[] files;
	public override bool ShouldFlipTexture => false;
	public CloneDashInterludeProvider() {
		files = Filesystem.FindFiles("interludes", "*.png").ToArray();
	}
	public override int Count => files.Length;

	public override bool Pick(int index, out Texture2D tex) {
		tex = Raylib.LoadTexture(files[index]);
		Raylib.SetTextureFilter(tex, TextureFilter.TEXTURE_FILTER_BILINEAR);
		return true;
	}
}
/// <summary>
/// Called during level loads. Adds a little something during loading operations, while
/// running on the main thread.
/// </summary>
[Nucleus.MarkForStaticConstruction]
public static class Interlude
{
	public static ConVar usemdinterludes = ConVar.Register(nameof(usemdinterludes), "1", ConsoleFlags.Saved, "If Muse Dash is installed, adds the interludes from the base game.", 0, 1);
	private static Stopwatch limiter = new();
	private static double lastFrame = -100;
	private static bool inInterlude;
	private static bool hasTex;
	private static bool flipTex;
	private static string? loadMsg;
	private static string? loadSubMsg;
	private static Texture2D interludeTexture;

	private static bool _should = false;
	public static bool ShouldSelectInterludeTexture {
		get => _should;
		set {
			if (!_should && value) {
				_should = value;
				if (!hasTex) {
					determineInterludeTexture(); // load interlude texture now. Only really used for the loading screen when starting the game
				}
			}

			_should = value;
		}
	}

	private static void determineInterludeTexture() {
		if (!_should) return;

		var providers = ReflectionTools.InstantiateAllInheritorsOfAbstractType<InterludeTextureProvider>().ToList();
		while (providers.Count > 0) {
			var provider = providers.Random();
			if (provider.Empty || (provider is MuseDashInterludeProvider && !usemdinterludes.GetBool())) {
				providers.Remove(provider);
				continue;
			}

			// The provider isn't empty
			if (provider.Pick(Random.Shared.Next(0, provider.Count), out interludeTexture)) {
				hasTex = Raylib.IsTextureReady(interludeTexture); // make sure the texture is valid, just in case
				if (!hasTex) {
					Logs.Warn("Failed to load the interlude texture, despite a provider giving us one!");
				}

				flipTex = provider.ShouldFlipTexture;

				return;
			}

			providers.Remove(provider);
		}
	}

	private static void reset() {
		if (hasTex)
			Raylib.UnloadTexture(interludeTexture);

		limiter.Reset();
		lastFrame = -100;
		hasTex = false;
		flipTex = false;
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

		using (StaticSequentialProfiler.AccumulateTime("Interlude.Spin")) {
			var msNow = limiter.Elapsed.TotalSeconds;
			if (lastFrame < 0 || (msNow - lastFrame) >= (1d / 30d)) {
				lastFrame = msNow;
				// Render cycle
				Rlgl.LoadIdentity();
				Surface.Clear(0);
				var windowSize = EngineCore.GetWindowSize();

				Graphics2D.ResetDrawingOffset(); // Interlude directly takes main-thread control, so the level frame state would never clear this like it usually would
				if (hasTex) {
					var tex = interludeTexture;
					// unity moment; need to flip sometimes
					Raylib.DrawTexturePro(tex, new(0, 0, tex.Width, flipTex ? -tex.Height : tex.Height), new(0, 0, windowSize.W, windowSize.H), new(0, 0), 0, Raylib_cs.Color.White);
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
	}

	static readonly Regex boldRegex = new("^(.+)<b>(.+)<\\/b>(.+)$");
	private static void DrawLoadMsg(Vector2F position, string loadMsg, float fontSize, Anchor fontAnchor) {
		// Strawberry Godzilla from Muse Dash
		// TODO: More accurate Regex?
		Match boldRegexMatch = boldRegex.Match(loadMsg);
		if (boldRegexMatch.Success) {
			Graphics2D.DrawText(position, [
									new(boldRegexMatch.Groups[1].Value, Graphics2D.UI_CN_JP_FONT_NAME),
									new(boldRegexMatch.Groups[2].Value, Graphics2D.UI_MONO_BOLD_FONT_NAME),
									new(boldRegexMatch.Groups[3].Value, Graphics2D.UI_CN_JP_FONT_NAME)
								], 3, fontSize, fontAnchor);
		}
		else
			Graphics2D.DrawText(position, loadMsg, Graphics2D.UI_CN_JP_FONT_NAME, fontSize, fontAnchor);
	}

	public static void End() {
		limiter.Stop();
		reset();
	}
}
