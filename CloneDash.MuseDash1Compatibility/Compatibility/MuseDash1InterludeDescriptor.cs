using AssetStudio;
using CloneDash.Common.Compatibility;
using CloneDash.Common.Interludes;
using CloneDash.Compatibility.Unity;
using Nucleus.Common.Graphics;
using Nucleus.Common.Images;
using Raylib_cs;

namespace CloneDash.Compatibility.MuseDash;

public class MuseDash1InterludeDescriptor(string id, string path, ICloneDashModProvider provider) : SimpleModDescriptor(id, id, provider), IInterludeDescriptor
{
	public Image? LoadImage() {
		AssetStudio.Texture2D tex2d = UnityAssetUtils.InternalLoadAsset<AssetStudio.Texture2D>(MuseDash1Compatibility.StreamingFiles, Path.GetFileNameWithoutExtension(path));

		if (tex2d.m_TextureFormat == TextureFormat.RGBA32)
			return null;

		return tex2d.ToRaylib();
	}

	// Texture 2D used here because itll be loaded when Interlude is initialized.
	// And it will be destroyed immediately after
	public ITexture CreateTexture(Image image) {
		var texObj = textures.CreateTexture(image);
		texObj.SetFilter(TextureFilter.Bilinear);
		texObj.AddPublicFlags(PublicTextureFlags.RequiresFlippedV);
		return texObj;
	}
}
