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
	public DashEventComponents Components;
	public DashEvent(DashEventComponents components) {
		Components = components;
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
	public static DashEvent CreateFromType(DashEventComponents components, EventType type) {
		DashEvent ret;
		switch (type) {
			case EventType.BossIn: ret = new BossInEvent(components); break;
			case EventType.BossOut: ret = new BossOutEvent(components); break;
			case EventType.BossShow: ret = new BossVisibilityEvent(components, true); break;
			case EventType.BossHide: ret = new BossVisibilityEvent(components, false); break;
			case EventType.BossSingleHit: ret = new BossSingleHit(components); break;
			case EventType.BossMasher: ret = new BossMasher(components, 1); break;
			case EventType.BossMasherEnd: ret = new BossMasher(components, 2); break;
			case EventType.BossFar1Start: ret = new BossFar1Start(components); break;
			case EventType.BossFar1End: ret = new BossFar1End(components); break;
			case EventType.BossFar1To2: ret = new BossFar1To2(components); break;
			case EventType.BossFar2Start: ret = new BossFar2Start(components); break;
			case EventType.BossFar2End: ret = new BossFar2End(components); break;
			case EventType.BossFar2To1: ret = new BossFar2To1(components); break;

			case EventType.AirSpeed1: ret = new SpeedChange(components, components.Pathways.AirSidePathways, 1); break;
			case EventType.AirSpeed2: ret = new SpeedChange(components, components.Pathways.AirSidePathways, 2); break;
			case EventType.AirSpeed3: ret = new SpeedChange(components, components.Pathways.AirSidePathways, 3); break;

			case EventType.GroundSpeed1: ret = new SpeedChange(components, components.Pathways.GroundSidePathways, 1); break;
			case EventType.GroundSpeed2: ret = new SpeedChange(components, components.Pathways.GroundSidePathways, 2); break;
			case EventType.GroundSpeed3: ret = new SpeedChange(components, components.Pathways.GroundSidePathways, 3); break;

			case EventType.DoubleSpeed1: ret = new SpeedChange(components, components.Pathways.Layout.All, 1); break;
			case EventType.DoubleSpeed2: ret = new SpeedChange(components, components.Pathways.Layout.All, 2); break;
			case EventType.DoubleSpeed3: ret = new SpeedChange(components, components.Pathways.Layout.All, 3); break;

			case EventType.ScreenScrollUp: ret = new ScreenScrollEffect(components, ScreenScrollDirection.Up); break;
			case EventType.ScreenScrollDown: ret = new ScreenScrollEffect(components, ScreenScrollDirection.Down); break;
			case EventType.ScreenScrollEnd: ret = new ScreenScrollEffect(components, ScreenScrollDirection.NoScroll); break;
			case EventType.ScanlinesOn: ret = new ScanlinesEffect(components, true); break;
			case EventType.ScanlinesOff: ret = new ScanlinesEffect(components, false); break;
			case EventType.ChromaticAberrationOn: ret = new ChromaticAberrationEffect(components, true); break;
			case EventType.ChromaticAberrationOff: ret = new ChromaticAberrationEffect(components, false); break;
			case EventType.VignetteOn: ret = new VignetteEffect(components, true); break;
			case EventType.VignetteOff: ret = new VignetteEffect(components, false); break;
			case EventType.TVStaticOn: ret = new TVStaticEffect(components, true); break;
			case EventType.TVStaticOff: ret = new TVStaticEffect(components, false); break;
			case EventType.FlashbangStart: ret = new FlashbangEffect(components, FlashbangParam.Start); break;
			case EventType.FlashbangHigh: ret = new FlashbangEffect(components, FlashbangParam.High); break;
			case EventType.FlashbangEnd: ret = new FlashbangEffect(components, FlashbangParam.End); break;
			case EventType.NoteFreeze: ret = new NoteFreezeEvent(components, true); break;
			case EventType.NoteUnfreeze: ret = new NoteFreezeEvent(components, false); break;
			case EventType.BgFreeze: ret = new BgFreezeEvent(components, true); break;
			case EventType.BgUnfreeze: ret = new BgFreezeEvent(components, false); break;
			case EventType.MosaicStart: ret = new MosaicEffect(components, true); break;
			case EventType.MosaicEnd: ret = new MosaicEffect(components, false); break;
			case EventType.SepiaStart: ret = new SepiaEffect(components, true); break;
			case EventType.SepiaEnd: ret = new SepiaEffect(components, false); break;
			case EventType.FocusLinesBlack: ret = new FocusLinesEffect(components, FocusLineMode.Black); break;
			case EventType.FocusLinesWhite: ret = new FocusLinesEffect(components, FocusLineMode.White); break;
			case EventType.FocusLinesOff: ret = new FocusLinesEffect(components, FocusLineMode.Off); break;
			case EventType.FilmGrainOn: ret = new FilmGrainEffect(components, true); break;
			case EventType.FilmGrainOff: ret = new FilmGrainEffect(components, false); break;

			case EventType.AutoPlayOn: ret = new AutoPlayEvent(components, true); break;
			case EventType.AutoPlayOff: ret = new AutoPlayEvent(components, false); break;

			case EventType.ShowBackground: ret = new BackgroundVisibilityEvent(components, true); break;
			case EventType.HideBackground: ret = new BackgroundVisibilityEvent(components, false); break;

			case EventType.ShowNotes: ret = new NoteVisibilityEvent(components, true); break;
			case EventType.HideNotes : ret = new NoteVisibilityEvent(components, false); break;

			case EventType.FlashbangColorWhite: ret = new FlashBangEffectColorChange(components, FlashbangColor.White); break;
			case EventType.FlashbangColorBlack: ret = new FlashBangEffectColorChange(components, FlashbangColor.Black); break;
			case EventType.FlashbangColorRed: ret = new FlashBangEffectColorChange(components, FlashbangColor.Red); break;
			case EventType.FlashbangColorGreen: ret = new FlashBangEffectColorChange(components, FlashbangColor.Green); break;
			case EventType.FlashbangColorBlue: ret = new FlashBangEffectColorChange(components, FlashbangColor.Blue); break;
			case EventType.FlashbangColorCyan: ret = new FlashBangEffectColorChange(components, FlashbangColor.Cyan); break;
			case EventType.FlashbangColorMagenta: ret = new FlashBangEffectColorChange(components, FlashbangColor.Magenta); break;
			case EventType.FlashbangColorYellow: ret = new FlashBangEffectColorChange(components, FlashbangColor.Yellow); break;

			default: throw new Exception();
		}

		ret.SortIndex = components.Chart.EventSortIndexCounter++;
		return ret;
	}

	public double GetChunkableTime() => Time;
	public double GetChunkablePostLength() => Length;
	public bool IsChunkableNow() => true; // Always chunkable!
	public int GetChunkableSortIndex() => SortIndex;
}
