using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Game.Events;
using CloneDash.MD1_Compat.Game.Events;
using FMOD;

namespace CloneDash.Game;

public enum EventTriggerType
{

	AtTimeMinusLength,
	AtTime
}
public class DashEvent : IDashChunkable
{
	public MuseDash1Game Game;
	public DashEvent(MuseDash1Game game) {
		Game = game;
	}
	public override string ToString() {
		return GetType().Name;
	}
	public virtual EventTriggerType TriggerType => EventTriggerType.AtTime;
	public double Time { get; set; }
	public double Length { get; set; }

	public int? Score { get; set; }
	public int? Fever { get; set; }
	public int? Damage { get; set; }

	public int SortIndex;

	public string? BossAction { get; set; }

	public void Build() {
		OnBuild();
	}

	/// <summary>
	/// Called by the game level
	/// </summary>
	public virtual void Activate() {

	}

	public virtual bool ShouldDebug() => true;

	public virtual void Deactivate() {

	}

	public virtual void OnBuild() { }
	public static DashEvent CreateFromType(MuseDash1Game game, EventType type) {
		DashEvent ret;
		switch (type) {
			case EventType.BossIn: ret = new BossInEvent(game); break;
			case EventType.BossOut: ret = new BossOutEvent(game); break;
			case EventType.BossShow: ret = new BossVisibilityEvent(game, true); break;
			case EventType.BossHide: ret = new BossVisibilityEvent(game, false); break;
			case EventType.BossSingleHit: ret = new BossSingleHit(game); break;
			case EventType.BossMasher: ret = new BossMasher(game, 1); break;
			case EventType.BossMasherEnd: ret = new BossMasher(game, 2); break;
			case EventType.BossFar1Start: ret = new BossFar1Start(game); break;
			case EventType.BossFar1End: ret = new BossFar1End(game); break;
			case EventType.BossFar1To2: ret = new BossFar1To2(game); break;
			case EventType.BossFar2Start: ret = new BossFar2Start(game); break;
			case EventType.BossFar2End: ret = new BossFar2End(game); break;
			case EventType.BossFar2To1: ret = new BossFar2To1(game); break;

			case EventType.AirSpeed1: ret = new SpeedChange(game, PathwaySide.Top, 1); break;
			case EventType.AirSpeed2: ret = new SpeedChange(game, PathwaySide.Top, 2); break;
			case EventType.AirSpeed3: ret = new SpeedChange(game, PathwaySide.Top, 3); break;

			case EventType.GroundSpeed1: ret = new SpeedChange(game, PathwaySide.Bottom, 1); break;
			case EventType.GroundSpeed2: ret = new SpeedChange(game, PathwaySide.Bottom, 2); break;
			case EventType.GroundSpeed3: ret = new SpeedChange(game, PathwaySide.Bottom, 3); break;

			case EventType.DoubleSpeed1: ret = new SpeedChange(game, PathwaySide.Both, 1); break;
			case EventType.DoubleSpeed2: ret = new SpeedChange(game, PathwaySide.Both, 2); break;
			case EventType.DoubleSpeed3: ret = new SpeedChange(game, PathwaySide.Both, 3); break;

			case EventType.ScreenScrollUp: ret = new ScreenScrollEffect(game, ScreenScrollDirection.Up); break;
			case EventType.ScreenScrollDown: ret = new ScreenScrollEffect(game, ScreenScrollDirection.Down); break;
			case EventType.ScreenScrollEnd: ret = new ScreenScrollEffect(game, ScreenScrollDirection.NoScroll); break;
			case EventType.ScanlinesOn: ret = new ScanlinesEffect(game, true); break;
			case EventType.ScanlinesOff: ret = new ScanlinesEffect(game, false); break;
			case EventType.ChromaticAberrationOn: ret = new ChromaticAberrationEffect(game, true); break;
			case EventType.ChromaticAberrationOff: ret = new ChromaticAberrationEffect(game, false); break;
			case EventType.VignetteOn: ret = new VignetteEffect(game, true); break;
			case EventType.VignetteOff: ret = new VignetteEffect(game, false); break;
			case EventType.TVStaticOn: ret = new TVStaticEffect(game, true); break;
			case EventType.TVStaticOff: ret = new TVStaticEffect(game, false); break;
			case EventType.FlashbangStart: ret = new FlashbangEffect(game, FlashbangParam.Start); break;
			case EventType.FlashbangHigh: ret = new FlashbangEffect(game, FlashbangParam.High); break;
			case EventType.FlashbangEnd: ret = new FlashbangEffect(game, FlashbangParam.End); break;
			case EventType.NoteFreeze: ret = new NoteFreezeEvent(game, true); break;
			case EventType.NoteUnfreeze: ret = new NoteFreezeEvent(game, false); break;
			case EventType.BgFreeze: ret = new BgFreezeEvent(game, true); break;
			case EventType.BgUnfreeze: ret = new BgFreezeEvent(game, false); break;
			case EventType.MosaicStart: ret = new MosaicEffect(game, true); break;
			case EventType.MosaicEnd: ret = new MosaicEffect(game, false); break;
			case EventType.SepiaStart: ret = new SepiaEffect(game, true); break;
			case EventType.SepiaEnd: ret = new SepiaEffect(game, false); break;
			case EventType.FocusLinesBlack: ret = new FocusLinesEffect(game, FocusLineMode.Black); break;
			case EventType.FocusLinesWhite: ret = new FocusLinesEffect(game, FocusLineMode.White); break;
			case EventType.FocusLinesOff: ret = new FocusLinesEffect(game, FocusLineMode.Off); break;
			case EventType.FilmGrainOn: ret = new FilmGrainEffect(game, true); break;
			case EventType.FilmGrainOff: ret = new FilmGrainEffect(game, false); break;

			case EventType.AutoPlayOn: ret = new AutoPlayEvent(game, true); break;
			case EventType.AutoPlayOff: ret = new AutoPlayEvent(game, false); break;

			case EventType.ShowBackground: ret = new BackgroundVisibilityEvent(game, true); break;
			case EventType.HideBackground: ret = new BackgroundVisibilityEvent(game, false); break;

			case EventType.ShowNotes: ret = new NoteVisibilityEvent(game, true); break;
			case EventType.HideNotes : ret = new NoteVisibilityEvent(game, false); break;

			case EventType.FlashbangColorWhite: ret = new FlashBangEffectColorChange(game, FlashbangColor.White); break;
			case EventType.FlashbangColorBlack: ret = new FlashBangEffectColorChange(game, FlashbangColor.Black); break;
			case EventType.FlashbangColorRed: ret = new FlashBangEffectColorChange(game, FlashbangColor.Red); break;
			case EventType.FlashbangColorGreen: ret = new FlashBangEffectColorChange(game, FlashbangColor.Green); break;
			case EventType.FlashbangColorBlue: ret = new FlashBangEffectColorChange(game, FlashbangColor.Blue); break;
			case EventType.FlashbangColorCyan: ret = new FlashBangEffectColorChange(game, FlashbangColor.Cyan); break;
			case EventType.FlashbangColorMagenta: ret = new FlashBangEffectColorChange(game, FlashbangColor.Magenta); break;
			case EventType.FlashbangColorYellow: ret = new FlashBangEffectColorChange(game, FlashbangColor.Yellow); break;

			default: throw new Exception();
		}

		ret.SortIndex = game.EventSortIndexCounter++;
		return ret;
	}

	public double GetChunkableTime() => Time;
	public double GetChunkablePostLength() => Length;
	public bool IsChunkableNow() => true; // Always chunkable!
	public int GetChunkableSortIndex() => SortIndex;
}
