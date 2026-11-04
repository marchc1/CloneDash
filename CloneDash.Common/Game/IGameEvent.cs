namespace CloneDash.Common.Game;

public interface IGameEvent;

public delegate void GameEventHandler<T>(ref T ev) where T : struct, IGameEvent;

public interface IGameEventListener<T> where T : struct, IGameEvent
{
	void FireGameEvent(ref T ev);
}
