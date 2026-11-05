using CloneDash.Common.Game;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;
using CloneDash.Game;
using Nucleus;
using Nucleus.Common.Graphics;
using Nucleus.Core;
using Nucleus.ManagedMemory;
using Nucleus.Models;
using Nucleus.Types;
using Nucleus.Util;
using Raylib_cs;

using Color = Nucleus.Common.Types.Color;

namespace CloneDash.MD1_Compat.Game.Events;

public class SepiaScreenEffect() : ShaderScreenEffect("sepia")
{
	protected override bool SetUniforms(IShader shader) {
		double value = Value;
		if (value <= 0.0) return false;

		shader.SetUniform("uStrength", (float)value * 1f);

		return true;
	}
}

public class ChromaticAberrationScreenEffect() : ShaderScreenEffect("chromatic_aberration")
{
	protected override bool SetUniforms(IShader shader) {
		double value = Value;
		if (value <= 0.0) return false;

		float widthPx = Effects.RenderTexture?.GetWidth() ?? 1920;
		shader.SetUniform("uOffset", (float)value * 4.0f / widthPx);

		return true;
	}
}

public class MosaicScreenEffect() : ShaderScreenEffect("mosaic")
{
	protected override bool SetUniforms(IShader shader) {
		double value = Value;
		if (value <= 0.0) return false;

		shader.SetUniform("uStrength", (float)value * 0.05f);
		shader.SetUniform("uResolution", new System.Numerics.Vector2(Effects.RenderTexture?.GetWidth() ?? 1, Effects.RenderTexture?.GetHeight() ?? 1));

		return true;
	}
}

public class ScanlinesScreenEffect() : ShaderScreenEffect("scanlines")
{
	protected override bool SetUniforms(IShader shader) {
		double value = Value;
		if (value <= 0.0) return false;

		shader.SetUniform("uTime", (float)Conductor.GetTime());
		shader.SetUniform("uStrength", 0.3f);

		return true;
	}
}

public class FilmGrainScreenEffect() : ShaderScreenEffect("filmgrain")
{
	ITexture? OldFilmDustTex;
	ITexture? OldFilmScratchesTex;

	public override void Initialize() {
		base.Initialize();

		OldFilmScratchesTex = LoadOldFilmTexture("ScratchesTex");
		OldFilmDustTex = LoadOldFilmTexture("DustTex");
	}

	protected override void OnShaderActivated(IShader shader) {
		if (OldFilmScratchesTex != null) shader.SetTexture("uScratchesTex", OldFilmScratchesTex);
		if (OldFilmDustTex != null) shader.SetTexture("uDustTex", OldFilmDustTex);
	}

	protected override bool SetUniforms(IShader shader) {
		double value = Value;
		if (value <= 0.0) return false;

		shader.SetUniform("uTime", (float)Conductor.GetTime());
		shader.SetUniform("uStrength", (float)value * 1f);

		return true;
	}

	private ITexture? LoadOldFilmTexture(string name) {
		var tex2d = MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.Texture2D>(name);
		if (tex2d == null) return null;
		var tex = UnityAssetUtils.ConvertTexture(EngineCore.Level, tex2d);
		tex.SetWrap(TextureWrap.Repeat); // so the scratches/dust tile as they scroll
		return tex;
	}
}

public class VignetteScreenEffect() : ShaderScreenEffect("vignette")
{
	protected override bool SetUniforms(IShader shader) {
		double value = Value;
		if (value <= 0.0) return false;

		shader.SetUniform("uStrength", (float)value);

		return true;
	}
}

public class ScreenScrollScreenEffect : ScreenEffect
{
	double ScreenScrollLastTime;
	double ScreenScrollProgress;
	double ScreenScrollRate;

	public override bool Present(FrameState frameState, RenderTexture read) {
		if (ScreenScrollRate == 0)
			return false;

		float offset = (float)(ScreenScrollProgress % frameState.WindowHeight);
		float gap = frameState.WindowHeight * 0.02f;
		if (ScreenScrollRate > 0) {
			read.Draw(new(0, 0, frameState.WindowWidth, -frameState.WindowHeight), new(0, offset + gap), Color.White);
			read.Draw(new(0, 0, frameState.WindowWidth, -frameState.WindowHeight), new(0, offset - frameState.WindowHeight - gap), Color.White);
		}
		else {
			read.Draw(new(0, 0, frameState.WindowWidth, -frameState.WindowHeight), new(0, offset - gap), Color.White);
			read.Draw(new(0, 0, frameState.WindowWidth, -frameState.WindowHeight), new(0, offset + frameState.WindowHeight + gap), Color.White);
		}

		return true;
	}

	public override void Render(FrameState frameState, ref RenderTexture read, ref RenderTexture write) {
		// this draws the screen scroll effect, its done as a texture shift instead here
		double screenScrollDirection = Target;
		if (screenScrollDirection == -1)
			ScreenScrollRate = -0.7;
		else if (screenScrollDirection == 1)
			ScreenScrollRate = 1;

		if (ScreenScrollRate != 0 && !Level.Paused) {
			double deltaTime = Conductor.GetTime() - ScreenScrollLastTime;
			ScreenScrollLastTime = Conductor.GetTime();
			ScreenScrollProgress += ScreenScrollRate * deltaTime * frameState.WindowHeight * 8 * RhythmGameLevel.musicspeed.GetDouble();

			double windowH = frameState.WindowHeight;

			if (ScreenScrollRate > 0 && ScreenScrollProgress >= windowH) {
				ScreenScrollProgress = 0;
				if (screenScrollDirection == 0) ScreenScrollRate = 0;
			}
			else if (ScreenScrollRate < 0 && ScreenScrollProgress <= -windowH) {
				ScreenScrollProgress = 0;
				if (screenScrollDirection == 0) ScreenScrollRate = 0;
			}
		}
	}

	public void ResetScreenScroll() {
		ScreenScrollProgress = 0;
		ScreenScrollRate = 0;
		ScreenScrollLastTime = Conductor.GetTime();
	}

	protected override void OnReset() {
		ScreenScrollLastTime = 0;
		ScreenScrollRate = 0;
		ScreenScrollProgress = 0;
	}
}

public class TVStaticScreenEffect() : ShaderScreenEffect("tvstatic")
{
	protected override bool SetUniforms(IShader shader) {
		double value = Value;
		if (value <= 0.0) return false;

		shader.SetUniform("uTime", (float)Conductor.GetTime());
		shader.SetUniform("uResolution", new System.Numerics.Vector2(Effects.RenderTexture?.GetWidth() ?? 1, Effects.RenderTexture?.GetHeight() ?? 1));

		return true;
	}
}

public class FlashbangScreenEffect : ScreenEffect
{
	readonly FCurve<float> flashbangIntensity = new();

	Color flashbangColor = new(255, 255, 255, 255);
	FlashbangEffect? lastFlashbangEffect;

	public void AddFlashbang(FlashbangEffect flash) {
		// This is a hack on top of a hack! This fixes spontaneous middle flashbangs
		// I want to use constant interpolation, but I think I messed up my fcurve implementation...
		KeyframeInterpolation interpolation = KeyframeInterpolation.Linear;
		if ((lastFlashbangEffect == null || lastFlashbangEffect.Type == FlashbangParam.End) && flash.Type == FlashbangParam.High){
			flashbangIntensity.AddKeyframe(new() { Time = flash.Time - 0.001, Value = 0, Interpolation = KeyframeInterpolation.Linear });
			flashbangIntensity.AddKeyframe(new() { Time = flash.Time, Value = 1, Interpolation = KeyframeInterpolation.Linear });
		}
		else
			flashbangIntensity.AddKeyframe(new() { Time = flash.Time, Value = (float)flash.TargetValue, Interpolation = KeyframeInterpolation.Linear });
		lastFlashbangEffect = flash;
	}

	public override void RenderOverlay(FrameState frameState) {
		// this draws flashbangs
		double flashbangBrightness = flashbangIntensity.DetermineValueAtTime(Conductor.GetTime());
		if (flashbangBrightness > 0) {
			Rlgl.DrawRenderBatchActive();
			Graphics2D.SetDrawColor(flashbangColor.R, flashbangColor.G, flashbangColor.B, (int)(float)(255 * flashbangBrightness)); // todo: flashbang color interp
			Graphics2D.DrawRectangle(0, 0, frameState.WindowWidth, frameState.WindowHeight);
			Rlgl.DrawRenderBatchActive();
		}
	}

	public void SetFlashbangColor(FlashbangColor color) {
		switch (color) {
			case FlashbangColor.White: flashbangColor = new(255, 255, 255, 255); break;
			case FlashbangColor.Black: flashbangColor = new(0, 0, 0, 255); break;
			case FlashbangColor.Red: flashbangColor = new(255, 0, 0, 255); break;
			case FlashbangColor.Green: flashbangColor = new(0, 255, 0, 255); break;
			case FlashbangColor.Blue: flashbangColor = new(0, 0, 255, 255); break;
			case FlashbangColor.Cyan: flashbangColor = new(0, 255, 255, 255); break;
			case FlashbangColor.Magenta: flashbangColor = new(255, 0, 255, 255); break;
			case FlashbangColor.Yellow: flashbangColor = new(255, 255, 0, 255); break;
		}
	}

	protected override void OnReset() {
		flashbangColor = new(255, 255, 255, 255);
	}
}

public class NoteFreezeScreenEffect : ScreenEffect;

public class BgFreezeScreenEffect : ScreenEffect;

public class FocusLinesScreenEffect : ScreenEffect;

public class NoteVisibilityScreenEffect : ScreenEffect
{
	public bool Visible = true;

	protected override void OnReset() {
		Visible = true;
	}
}

public class BackgroundVisibilityScreenEffect : ScreenEffect
{
	public bool Visible = true;

	protected override void OnReset() {
		Visible = true;
	}
}
