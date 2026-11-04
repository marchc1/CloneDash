using Nucleus;
using Nucleus.Commands;

namespace CloneDash.Common.Game;

[MarkForStaticConstruction]
public sealed class GameEventManager
{
	public static readonly ConVar gameevents_debug = new(nameof(gameevents_debug), "0", FCvar.None, "Prints every fired game event to the console.");

	static int nextTypeId;

	object?[] channels = [];

	public void Clear() => channels = [];

	public void Fire<T>(T ev) where T : struct, IGameEvent => Fire(ref ev);

	public void Fire<T>(ref T ev) where T : struct, IGameEvent {
		if (gameevents_debug.GetBool())
			Logs.Debug($"{typeof(T).Name}: {ev}");

		var channel = GetChannel<T>(false);
		if (channel == null)
			return;

		var handlers = channel.Handlers;
		for (int i = 0; i < handlers.Length; i++)
			handlers[i](ref ev);
	}

	public void Listen<T>(GameEventHandler<T> handler) where T : struct, IGameEvent {
		var channel = GetChannel<T>(true)!;
		channel.Handlers = [.. channel.Handlers, handler];
	}

	public void Listen<T>(IGameEventListener<T> listener) where T : struct, IGameEvent => Listen<T>(listener.FireGameEvent);

	public void Unlisten<T>(GameEventHandler<T> handler) where T : struct, IGameEvent {
		var channel = GetChannel<T>(false);
		if (channel == null)
			return;

		int index = Array.IndexOf(channel.Handlers, handler);
		if (index < 0)
			return;

		channel.Handlers = [.. channel.Handlers.AsSpan(0, index), .. channel.Handlers.AsSpan(index + 1)];
	}

	public void Unlisten<T>(IGameEventListener<T> listener) where T : struct, IGameEvent => Unlisten<T>(listener.FireGameEvent);

	Channel<T>? GetChannel<T>(bool create) where T : struct, IGameEvent {
		int id = TypeId<T>.Value;
		if (id >= channels.Length) {
			if (!create)
				return null;

			Array.Resize(ref channels, id + 1);
		}

		var channel = channels[id];
		if (channel == null) {
			if (!create)
				return null;

			channels[id] = channel = new Channel<T>();
		}

		return (Channel<T>)channel;
	}

	sealed class Channel<T> where T : struct, IGameEvent
	{
		public GameEventHandler<T>[] Handlers = [];
	}

	static class TypeId<T> where T : struct, IGameEvent
	{
		public static readonly int Value = Interlocked.Increment(ref nextTypeId) - 1;
	}
}
