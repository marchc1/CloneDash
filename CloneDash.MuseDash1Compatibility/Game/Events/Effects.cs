using CloneDash.Common.Game;
using CloneDash.Game;
using Nucleus;
using System;
using System.Collections.Generic;
using System.Text;

namespace CloneDash.MD1_Compat.Game.Events;

public enum FlashbangColor : byte
{
	White,
	Black,
	Red,
	Green,
	Blue,
	Cyan,
	Magenta,
	Yellow
}

public enum ScreenScrollDirection : sbyte
{
	NoScroll = 0,
	Up = 1,
	Down = -1,
}

public enum FlashbangParam : sbyte
{
	Start = 0,
	High = 1,
	End = 2
}

public enum FocusLineMode : sbyte
{
	Off = 0,
	Black = -1,
	White = 1
}




public class ScreenspaceEffectEvent<TEffect>(DashEventComponents components, double targetValue) : DashEvent(components) where TEffect : ScreenEffect
{
	public double TargetValue = targetValue;
	public virtual double? GetLengthOfEffect() => null;
	public virtual Func<double, double>? GetEasing() => null;
	public override void Activate() {
		Components.Effects.Get<TEffect>().Trigger(TargetValue, GetLengthOfEffect() ?? Length, GetEasing());
	}
}

public class ScreenScrollEffect(DashEventComponents components, ScreenScrollDirection direction) : ScreenspaceEffectEvent<ScreenScrollScreenEffect>(components, (double)direction){

	public override void Activate() {
		Components.Effects.Get<ScreenScrollScreenEffect>().Trigger((int)direction, 0);
		Components.Effects.Get<ScreenScrollScreenEffect>().ResetScreenScroll();
	}
	public override double? GetLengthOfEffect() => 0;
}
public class ScanlinesEffect(DashEventComponents components, bool active) : ScreenspaceEffectEvent<ScanlinesScreenEffect>(components, active ? 1 : 0);
public class ChromaticAberrationEffect(DashEventComponents components, bool active) : ScreenspaceEffectEvent<ChromaticAberrationScreenEffect>(components, active ? 1 : 0)
{
	public override double? GetLengthOfEffect() => 0.4;
}
public class VignetteEffect(DashEventComponents components, bool active) : ScreenspaceEffectEvent<VignetteScreenEffect>(components, active ? 1 : 0){
	public override double? GetLengthOfEffect() => 0.4;
	public override Func<double, double>? GetEasing() => active ? null : NMath.Ease.OutCubic;
}
public class TVStaticEffect(DashEventComponents components, bool active) : ScreenspaceEffectEvent<TVStaticScreenEffect>(components, active ? 1 : 0);
public class FlashbangEffect(DashEventComponents components, FlashbangParam parameter) : ScreenspaceEffectEvent<FlashbangScreenEffect>(components, parameter switch {
	FlashbangParam.Start => 0,
	FlashbangParam.High => 1,
	FlashbangParam.End => 0,
}) {
	public readonly FlashbangParam Type = parameter;
}
public class NoteFreezeEvent(DashEventComponents components, bool freeze) : ScreenspaceEffectEvent<NoteFreezeScreenEffect>(components, freeze ? 1 : 0);
public class BgFreezeEvent(DashEventComponents components, bool freeze) : ScreenspaceEffectEvent<BgFreezeScreenEffect>(components, freeze ? 1 : 0);
public class MosaicEffect(DashEventComponents components, bool active) : ScreenspaceEffectEvent<MosaicScreenEffect>(components, active ? 1 : 0){
	public override double? GetLengthOfEffect() {
		return 0;
	}
}
public class SepiaEffect(DashEventComponents components, bool active) : ScreenspaceEffectEvent<SepiaScreenEffect>(components, active ? 1 : 0){
	public override double? GetLengthOfEffect() => 0.4;
}
public class FocusLinesEffect(DashEventComponents components, FocusLineMode mode) : ScreenspaceEffectEvent<FocusLinesScreenEffect>(components, (double)mode);
public class FilmGrainEffect(DashEventComponents components, bool active) : ScreenspaceEffectEvent<FilmGrainScreenEffect>(components, active ? 1 : 0)
{
	public override double? GetLengthOfEffect() => 0.2;
}
public class FlashBangEffectColorChange(DashEventComponents components, FlashbangColor color) : ScreenspaceEffectEvent<FlashbangScreenEffect>(components, (double)color){
	public override void Activate() {
		components.Effects.Get<FlashbangScreenEffect>().SetFlashbangColor(color);
	}
}
public class AutoPlayEvent(DashEventComponents components, bool active) : DashEvent(components)
{
	public bool Active = active;

}

public class NoteVisibilityEvent(DashEventComponents components, bool visible) : DashEvent(components)
{
	public bool Visible = visible;
	public override void Activate() {
		Components.Effects.Get<NoteVisibilityScreenEffect>().Visible = Visible;
	}
}
public class BackgroundVisibilityEvent(DashEventComponents components, bool visible) : DashEvent(components)
{
	public bool Visible = visible;
	public override void Activate() {
		Components.Effects.Get<BackgroundVisibilityScreenEffect>().Visible = Visible;
	}
}