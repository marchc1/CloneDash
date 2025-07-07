using AssetStudio;

using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;

using Lua;

using Nucleus.Engine;
using Nucleus.ManagedMemory;

using Raylib_cs;

using SixLabors.Fonts;

namespace CloneDash.Scripting;

[LuaObject]
public partial class LuaTextures
{
	private Level Level;
	private TextureManagement Textures;
	private Dictionary<long, Raylib.ImageRef> imageCache = [];
	private Dictionary<string, LuaTexture> texCache = [];
	private Dictionary<string, LuaTexture> spriteCache = [];

	public LuaTextures(Level level, TextureManagement textures) {
		this.Level = level;
		this.Textures = textures;

		level.AddFinalizer(Dispose);
	}

	[LuaMember("loadTextureFromFile")]
	public LuaTexture LoadTextureFromFile(string pathID, string path) {
		return new(Level, Textures, Textures.LoadTextureFromFile(pathID, path));
	}
	[LuaMember("loadMuseDashSprite")]
	public LuaTexture LoadMuseDashSprite(string path) {
		if (spriteCache.TryGetValue(path, out LuaTexture? luaTex))
			return luaTex;
		// Loads a Muse Dash sprite from a named sprite asset
		// Find the sprite
		Sprite sprite = MuseDashCompatibility.StreamingAssets.FindAssetByName<Sprite>(path!)!;

		// Load the sprite atlas image or get the cached reference to it
		var img = cacheimage(sprite);
		using Raylib.ImageRef cutout = img.Crop(sprite.m_RD.textureRect.x, img.Height - sprite.m_RD.textureRect.y - sprite.m_RD.textureRect.height, sprite.m_RD.textureRect.width, sprite.m_RD.textureRect.height);

		Nucleus.ManagedMemory.Texture tex = new Nucleus.ManagedMemory.Texture(Textures, Raylib.LoadTextureFromImage(cutout), true);
		luaTex = new LuaTexture(Level, Textures, tex);
		spriteCache[path] = luaTex;
		return luaTex;
	}
	[LuaMember("loadMuseDashTexture")]
	public LuaTexture LoadMuseDashTexture(string path) {
		if (texCache.TryGetValue(path, out LuaTexture? luaTex))
			return luaTex;
		// Loads a Muse Dash sprite from a named sprite asset
		// Find the sprite
		AssetStudio.Texture2D tex = MuseDashCompatibility.StreamingAssets.FindAssetByName<AssetStudio.Texture2D>(path!)!;
		using Raylib.ImageRef img = new Raylib.ImageRef(tex.ToRaylib(), flipV: true);
		Nucleus.ManagedMemory.Texture ntex = new Nucleus.ManagedMemory.Texture(Textures, Raylib.LoadTextureFromImage(img), true);
		luaTex = new LuaTexture(Level, Textures, ntex);
		return luaTex;
	}

	private Raylib.ImageRef cacheimage(Sprite sprite) {
		var texture = sprite.m_RD.texture;
		if (imageCache.TryGetValue(texture.m_PathID, out var imgCache)) return imgCache;

		if (!texture.TryGet(out var unityTex)) throw new Exception();
		imgCache = new Raylib.ImageRef(unityTex.ToRaylib(), flipV: true);
		imageCache[texture.m_PathID] = imgCache;
		return imgCache;
	}

	public void Dispose(Level lvl) {
		foreach (var image in imageCache.Values)
			image.Dispose();
	}
}
