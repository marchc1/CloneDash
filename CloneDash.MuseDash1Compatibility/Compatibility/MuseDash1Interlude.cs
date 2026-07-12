using AssetStudio;
using CloneDash.Compatibility.Unity;
using Nucleus;
using Nucleus.Common.Graphics;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Text;
using Texture2D = Raylib_cs.Texture2D;

namespace CloneDash.Compatibility.MuseDash;


public class MuseDash1Interlude
{
	public string? path;
	public ITexture? LoadTexture() {
		if (path == null) throw new NullReferenceException("Wtf?");
		AssetStudio.Texture2D tex2d = UnityAssetUtils.InternalLoadAsset<AssetStudio.Texture2D>(MuseDash1Compatibility.StreamingFiles, Path.GetFileNameWithoutExtension(path));

		if (tex2d.m_TextureFormat == TextureFormat.RGBA32)
			return null;

		var img = tex2d.ToRaylib();
		var tex = Raylib.LoadTextureFromImage(img);
		Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
		Raylib.UnloadImage(img);
		var texObj = new Nucleus.ManagedMemory.Texture(null, tex);
		texObj.AddPublicFlags(PublicTextureFlags.RequiresFlippedV);
		return texObj;
	}
}
