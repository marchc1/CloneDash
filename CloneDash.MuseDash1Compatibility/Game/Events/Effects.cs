using CloneDash.Game;
using System;
using System.Collections.Generic;
using System.Text;

namespace CloneDash.MD1_Compat.Game.Events;

public enum ScreenspaceEffectType
{
	ScreenScroll,
	Scanlines,
	ChromaticAberration,
	Vignette,
	TVStatic,
	Flashbang,
	NoteFreeze,
	BgFreeze,
	Mosaic,
	Sepia,

	Count
}


public enum ScreenScrollDirection 
{
	NoScroll = 0,
	Up = 1,
	Down = -1,
}

public enum FlashbangParam 
{
	Start = 0,
	High = 1,
	End = 0
}

public class ScreenspaceEffectEvent(MuseDash1Game game, ScreenspaceEffectType type, double targetValue) : DashEvent(game)
{
	public ScreenspaceEffectType Type = type;
	public double TargetValue = targetValue;
	public virtual double? GetLengthOfEffect() => null;
	public override void Activate() {
		Game.TriggerScreenspaceEffectStart(Type, TargetValue, GetLengthOfEffect() ?? Length);
	}
}

public class ScreenScrollEffect(MuseDash1Game game, ScreenScrollDirection direction) : ScreenspaceEffectEvent(game, ScreenspaceEffectType.ScreenScroll, (double)direction);
public class ScanlinesEffect(MuseDash1Game game, bool active) : ScreenspaceEffectEvent(game, ScreenspaceEffectType.Scanlines, active ? 1 : 0);
public class ChromaticAberrationEffect(MuseDash1Game game, bool active) : ScreenspaceEffectEvent(game, ScreenspaceEffectType.ChromaticAberration, active ? 1 : 0){
	public override double? GetLengthOfEffect() => 1;
}
public class VignetteEffect(MuseDash1Game game, bool active) : ScreenspaceEffectEvent(game, ScreenspaceEffectType.Vignette, active ? 1 : 0);
public class TVStaticEffect(MuseDash1Game game, bool active) : ScreenspaceEffectEvent(game, ScreenspaceEffectType.TVStatic, active ? 1 : 0);
public class FlashbangEffect(MuseDash1Game game, FlashbangParam parameter) : ScreenspaceEffectEvent(game, ScreenspaceEffectType.Flashbang, (double)parameter);
public class NoteFreezeEvent(MuseDash1Game game, bool freeze) : ScreenspaceEffectEvent(game, ScreenspaceEffectType.NoteFreeze, freeze ? 1 : 0);
public class BgFreezeEvent(MuseDash1Game game, bool freeze) : ScreenspaceEffectEvent(game, ScreenspaceEffectType.BgFreeze, freeze ? 1 : 0);
public class MosaicEffect(MuseDash1Game game, bool active) : ScreenspaceEffectEvent(game, ScreenspaceEffectType.Mosaic, active ? 1 : 0);
public class SepiaEffect(MuseDash1Game game, bool active) : ScreenspaceEffectEvent(game, ScreenspaceEffectType.Sepia, active ? 1 : 0);