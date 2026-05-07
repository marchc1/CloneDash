using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Game.Events;

namespace CloneDash.Game;

public enum EventTriggerType
{

	AtTimeMinusLength,
	AtTime
}
public class DashEvent
{
	public MuseDash1Game Game;
	public DashEvent(MuseDash1Game game) {
		Game = game;
	}

	public virtual EventTriggerType TriggerType => EventTriggerType.AtTime;
	public double Time { get; set; }
	public double Length { get; set; }

	public int? Score { get; set; }
	public int? Fever { get; set; }
	public int? Damage { get; set; }

	public string? BossAction { get; set; }

	public void Build() {
		OnBuild();
	}

	/// <summary>
	/// Called by the game level
	/// </summary>
	public virtual void Activate() {

	}

	public virtual void Deactivate() {

	}

	public virtual void OnBuild() { }
	public static DashEvent CreateFromType(MuseDash1Game game, EventType type) {
		switch (type) {
			case EventType.BossIn: return new BossInEvent(game);
			case EventType.BossOut: return new BossOutEvent(game);
			case EventType.BossSingleHit: return new BossSingleHit(game);
			case EventType.BossMasher: return new BossMasher(game, 1);
			case EventType.BossMasherEnd: return new BossMasher(game, 2);
			case EventType.BossFar1Start: return new BossFar1Start(game);
			case EventType.BossFar1End: return new BossFar1End(game);
			case EventType.BossFar1To2: return new BossFar1To2(game);
			case EventType.BossFar2Start: return new BossFar2Start(game);
			case EventType.BossFar2End: return new BossFar2End(game);
			case EventType.BossFar2To1: return new BossFar2To1(game);
			case EventType.BossHide: return new BossHide(game);

			case EventType.AirSpeed1: return new SpeedChange(game, PathwaySide.Top, 1);
			case EventType.AirSpeed2: return new SpeedChange(game, PathwaySide.Top, 2);
			case EventType.AirSpeed3: return new SpeedChange(game, PathwaySide.Top, 3);

			case EventType.GroundSpeed1: return new SpeedChange(game, PathwaySide.Bottom, 1);
			case EventType.GroundSpeed2: return new SpeedChange(game, PathwaySide.Bottom, 2);
			case EventType.GroundSpeed3: return new SpeedChange(game, PathwaySide.Bottom, 3);

			case EventType.DoubleSpeed1: return new SpeedChange(game, PathwaySide.Both, 1);
			case EventType.DoubleSpeed2: return new SpeedChange(game, PathwaySide.Both, 2);
			case EventType.DoubleSpeed3: return new SpeedChange(game, PathwaySide.Both, 3);

			default: throw new Exception();
		}
	}
}
