using AssetStudio;
using CloneDash.Systems;
using Nucleus;
using Nucleus.Core;
using Nucleus.Rendering;
using Nucleus.Types;
using Raylib_cs;
using System.Diagnostics;
using static CloneDash.MuseDashCompatibility;
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
	public abstract Texture2D Pick(int index);
}

public class MuseDashInterlude
{
	public string path;
	public Texture2D LoadTexture() {
		AssetStudio.Texture2D tex2d = UnityAssetUtils.InternalLoadAsset<AssetStudio.Texture2D>(StreamingFiles, Path.GetFileNameWithoutExtension(path));

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
	static MuseDashInterlude[] interludes;
	public override bool ShouldFlipTexture => true;
	static MuseDashInterludeProvider() {
		if (MuseDashCompatibility.InitializeCompatibilityLayer() != MuseDashCompatibility.MDCompatLayerInitResult.OK) {
			interludes = [];
			return;
		}

		var interludesRaw = UnityAssetUtils.GetAllFiles(StreamingFiles, "loadinginterlude_assets_interlude_", regex: true);
		interludes = new MuseDashInterlude[interludesRaw.Length]; 
		for (int i = 0; i < interludesRaw.Length; i++) {
			interludes[i] = new() {
				path = interludesRaw[i]
			};
		}
	}
	public override int Count => interludes.Length;

	// Texture 2D used here because itll be loaded when Interlude is initialized.
	// And it will be destroyed immediately after
	public override Texture2D Pick(int index) => interludes[index].LoadTexture();
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

	public override Texture2D Pick(int index) {
		var tex = Raylib.LoadTexture(files[index]);
		Raylib.SetTextureFilter(tex, TextureFilter.TEXTURE_FILTER_BILINEAR);
		return tex;
	}
}
/// <summary>
/// Called during level loads. Adds a little something during loading operations, while
/// running on the main thread.
/// </summary>
[Nucleus.MarkForStaticConstruction]
public static class Interlude
{
	public static ConVar clonedash_usemdinterludes = ConVar.Register("clonedash_usemdinterludes", "1", ConsoleFlags.Saved, "If Muse Dash is installed, adds the interludes from the base game.", 0, 1);
	private static Stopwatch limiter = new();
	private static double lastFrame = -100;
	private static bool inInterlude;
	private static bool hasTex;
	private static bool flipTex;
	private static string? loadMsg;
	private static Raylib_cs.Texture2D interludeTexture;

	private static void determineInterludeTexture() {
		var providers = ReflectionTools.InstantiateAllInheritorsOfAbstractType<InterludeTextureProvider>().ToList();
		while (providers.Count > 0) {
			var provider = providers.Random();
			if (provider.Empty || (provider is MuseDashInterludeProvider && !clonedash_usemdinterludes.GetBool())) {
				providers.Remove(provider);
				continue;
			}

			// The provider isn't empty
			interludeTexture = provider.Pick(Random.Shared.Next(0, provider.Count));
			hasTex = Raylib.IsTextureReady(interludeTexture); // make sure the texture is valid, just in case
			if (!hasTex) {
				Logs.Warn("Failed to load the interlude texture, despite a provider giving us one!");
			}
			
			flipTex = provider.ShouldFlipTexture;

			return;
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
	public static void Spin() {
		if (!inInterlude)
			return;

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

			var midBottom = (windowSize.H - bottomSize) + (bottomSize / 2);
			Graphics2D.SetDrawColor(255, 255, 255);
			Graphics2D.DrawText(new(windowSize.W - 42 - 8, midBottom), loadMsg ?? "Loading...", "Noto Sans", texSize, Anchor.CenterRight);

			Graphics2D.DrawLoader(windowSize.W - 24, midBottom, time: msNow, inner: 8, outer: 12);
			Surface.Spin();
		}
	}

	public static void End() {
		limiter.Stop();
		reset();
	}
}