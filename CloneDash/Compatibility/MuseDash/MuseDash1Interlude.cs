using AssetStudio;
using CloneDash.Compatibility.Unity;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Text;
using Texture2D = Raylib_cs.Texture2D;

namespace CloneDash.Compatibility.MuseDash;

public class MuseDash1Interlude
{
	public string? path;
	public Texture2D? LoadTexture() {
		if (path == null) throw new NullReferenceException("Wtf?");
		AssetStudio.Texture2D tex2d = UnityAssetUtils.InternalLoadAsset<AssetStudio.Texture2D>(MuseDash1Compatibility.StreamingFiles, Path.GetFileNameWithoutExtension(path));

		if (tex2d.m_TextureFormat == TextureFormat.RGBA32)
			return null;

		var img = tex2d.ToRaylib();
		var tex = Raylib.LoadTextureFromImage(img);
		Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
		Raylib.UnloadImage(img);
		return tex;
	}
}
