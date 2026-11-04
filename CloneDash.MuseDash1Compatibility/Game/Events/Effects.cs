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




public class ScreenspaceEffectEvent<TEffect>(MuseDash1Game game, double targetValue) : DashEvent(game) where TEffect : ScreenEffect
{
	public double TargetValue = targetValue;
	public virtual double? GetLengthOfEffect() => null;
	public virtual Func<double, double>? GetEasing() => null;
	public override void Activate() {
		Game.Effects.Get<TEffect>().Trigger(TargetValue, GetLengthOfEffect() ?? Length, GetEasing());
	}
}

public class ScreenScrollEffect(MuseDash1Game game, ScreenScrollDirection direction) : ScreenspaceEffectEvent<ScreenScrollScreenEffect>(game, (double)direction){

	public override void Activate() {
		Game.Effects.Get<ScreenScrollScreenEffect>().Trigger((int)direction, 0);
		Game.Effects.Get<ScreenScrollScreenEffect>().ResetScreenScroll();
	}
	public override double? GetLengthOfEffect() => 0;
}
public class ScanlinesEffect(MuseDash1Game game, bool active) : ScreenspaceEffectEvent<ScanlinesScreenEffect>(game, active ? 1 : 0);
public class ChromaticAberrationEffect(MuseDash1Game game, bool active) : ScreenspaceEffectEvent<ChromaticAberrationScreenEffect>(game, active ? 1 : 0)
{
	public override double? GetLengthOfEffect() => 0.4;
}
public class VignetteEffect(MuseDash1Game game, bool active) : ScreenspaceEffectEvent<VignetteScreenEffect>(game, active ? 1 : 0){
	public override double? GetLengthOfEffect() => 0.4;
	public override Func<double, double>? GetEasing() => active ? null : NMath.Ease.OutCubic;
}
public class TVStaticEffect(MuseDash1Game game, bool active) : ScreenspaceEffectEvent<TVStaticScreenEffect>(game, active ? 1 : 0);
public class FlashbangEffect(MuseDash1Game game, FlashbangParam parameter) : ScreenspaceEffectEvent<FlashbangScreenEffect>(game, parameter switch {
	FlashbangParam.Start => 0,
	FlashbangParam.High => 1,
	FlashbangParam.End => 0,
}) {
	public readonly FlashbangParam Type = parameter;
}
public class NoteFreezeEvent(MuseDash1Game game, bool freeze) : ScreenspaceEffectEvent<NoteFreezeScreenEffect>(game, freeze ? 1 : 0);
public class BgFreezeEvent(MuseDash1Game game, bool freeze) : ScreenspaceEffectEvent<BgFreezeScreenEffect>(game, freeze ? 1 : 0);
public class MosaicEffect(MuseDash1Game game, bool active) : ScreenspaceEffectEvent<MosaicScreenEffect>(game, active ? 1 : 0){
	public override double? GetLengthOfEffect() {
		return 0;
	}
}
public class SepiaEffect(MuseDash1Game game, bool active) : ScreenspaceEffectEvent<SepiaScreenEffect>(game, active ? 1 : 0){
	public override double? GetLengthOfEffect() => 0.4;
}
public class FocusLinesEffect(MuseDash1Game game, FocusLineMode mode) : ScreenspaceEffectEvent<FocusLinesScreenEffect>(game, (double)mode);
public class FilmGrainEffect(MuseDash1Game game, bool active) : ScreenspaceEffectEvent<FilmGrainScreenEffect>(game, active ? 1 : 0)
{
	public override double? GetLengthOfEffect() => 0.2;
}
public class FlashBangEffectColorChange(MuseDash1Game game, FlashbangColor color) : ScreenspaceEffectEvent<FlashbangScreenEffect>(game, (double)color){
	public override void Activate() {
		game.Effects.Get<FlashbangScreenEffect>().SetFlashbangColor(color);
	}
}
public class AutoPlayEvent(MuseDash1Game game, bool active) : DashEvent(game)
{
	public bool Active = active;

}

public class NoteVisibilityEvent(MuseDash1Game game, bool visible) : DashEvent(game)
{
	public bool Visible = visible;
	public override void Activate() {
		Game.Effects.Get<NoteVisibilityScreenEffect>().Visible = Visible;
	}
}
public class BackgroundVisibilityEvent(MuseDash1Game game, bool visible) : DashEvent(game)
{
	public bool Visible = visible;
	public override void Activate() {
		Game.Effects.Get<BackgroundVisibilityScreenEffect>().Visible = Visible;
	}
}