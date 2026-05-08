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
	NoScroll,
	Up,
	Down,
}

public enum FlashbangParam
{
	Start,
	High,
	End
}


public struct ScreenspaceEffectParams {
	public bool Active;
	public ScreenScrollDirection ScreenScrollDirection;
	public FlashbangParam FlashbangParam;
}

public class ScreenspaceEffectEvent(MuseDash1Game game, ScreenspaceEffectType type, bool active = false, ScreenScrollDirection dir = 0, FlashbangParam flashparam = 0) : DashEvent(game)
{
	public ScreenspaceEffectType Type = type;
	public ScreenspaceEffectParams Params = new() {
		Active = active,
		ScreenScrollDirection = dir,
		FlashbangParam = flashparam
	};
	public override void Activate() {
		Game.SetScreenspaceEffectStart(Type, in Params, Length);
	}
}

public class ScreenScrollEffect(MuseDash1Game game, ScreenScrollDirection direction) : ScreenspaceEffectEvent(game, ScreenspaceEffectType.ScreenScroll, dir: direction);
public class ScanlinesEffect(MuseDash1Game game, bool active) : ScreenspaceEffectEvent(game, ScreenspaceEffectType.Scanlines, active: active);
public class ChromaticAberrationEffect(MuseDash1Game game, bool active) : ScreenspaceEffectEvent(game, ScreenspaceEffectType.ChromaticAberration, active: active);
public class VignetteEffect(MuseDash1Game game, bool active) : ScreenspaceEffectEvent(game, ScreenspaceEffectType.Vignette, active: active);
public class TVStaticEffect(MuseDash1Game game, bool active) : ScreenspaceEffectEvent(game, ScreenspaceEffectType.TVStatic, active: active);
public class FlashbangEffect(MuseDash1Game game, FlashbangParam parameter) : ScreenspaceEffectEvent(game, ScreenspaceEffectType.Flashbang, flashparam: parameter);
public class NoteFreezeEvent(MuseDash1Game game, bool freeze) : ScreenspaceEffectEvent(game, ScreenspaceEffectType.NoteFreeze, active: freeze);
public class BgFreezeEvent(MuseDash1Game game, bool freeze) : ScreenspaceEffectEvent(game, ScreenspaceEffectType.BgFreeze, active: freeze);
public class MosaicEffect(MuseDash1Game game, bool active) : ScreenspaceEffectEvent(game, ScreenspaceEffectType.Mosaic, active: active);
public class SepiaEffect(MuseDash1Game game, bool active) : ScreenspaceEffectEvent(game, ScreenspaceEffectType.Sepia, active: active);