using CloneDash.Game.Events;

namespace CloneDash.Game;

public enum EventTriggerType
{

	AtTimeMinusLength,
	AtTime
}
public class CD_BaseEvent
{
	public CD_GameLevel Game;
	public CD_BaseEvent(CD_GameLevel game) {
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
	public static CD_BaseEvent CreateFromType(CD_GameLevel game, EventType type) {
		switch (type) {
			case EventType.BossIn: return new BossInEvent(game);
			case EventType.BossOut: return new BossOutEvent(game);
			case EventType.BossSingleHit: return new BossSingleHit(game);
			case EventType.BossMasher: return new BossMasher(game);
			case EventType.BossFar1Start: return new BossFar1Start(game);
			case EventType.BossFar1End: return new BossFar1End(game);
			case EventType.BossFar1To2: return new BossFar1To2(game);
			case EventType.BossFar2Start: return new BossFar2Start(game);
			case EventType.BossFar2End: return new BossFar2End(game);
			case EventType.BossFar2To1: return new BossFar2To1(game);
			case EventType.BossHide: return new BossHide(game);
			default: throw new Exception();
		}
	}
}
