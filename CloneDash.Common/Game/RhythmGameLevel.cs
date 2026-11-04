using CloneDash.Common.Compatibility;
using CloneDash.Common.Gamemodes;
using CloneDash.Common.Songs;
using CloneDash.Game;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Engine;
using Nucleus.Util;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CloneDash.Common.Game;

public class RhythmGameLoadParameters
{
	public const string AUTOPLAY = "Autoplay";
	public const string START_MEASURE = "StartMeasure";

	readonly Dictionary<string, object?> Parameters = [];

	public T? Get<T>(ReadOnlySpan<char> key)
		=> Parameters.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(key, out object? o) && o is T t ? t : default;

	public RhythmGameLoadParameters Set<T>(ReadOnlySpan<char> key, T? value) {
		if (value == null)
			Parameters.GetAlternateLookup<ReadOnlySpan<char>>().Remove(key);
		else
			Parameters.GetAlternateLookup<ReadOnlySpan<char>>()[key] = value;

		return this;
	}
}

public abstract class RhythmGamemode(ICloneDashModProvider provider) : IGamemodeDescriptor
{
	protected internal abstract void Compose(RhythmGameLevel level, ServiceCollection parts, ISongChart chart);

	public RhythmGameLevel Load(ISongChart chart, RhythmGameLoadParameters parms) => new(this, chart, parms);

	public ICloneDashModProvider GetProvider() => provider; 
	public abstract string GetName();
	public abstract string GetID();
}

public class RhythmGameLevel : Level, IServiceProvider
{
	readonly RhythmGamemode Gamemode;
	readonly ServiceProvider Services;
	readonly ISongChart Chart;
	readonly RhythmGameLoadParameters Parameters;

	internal RhythmGameLevel(RhythmGamemode gamemode, ISongChart chart, RhythmGameLoadParameters parms) {
		Gamemode = gamemode;
		Chart = chart;
		Parameters = parms;

		ServiceCollection parts = new();
		parts.AddSingleton<Level>(this);
		parts.AddSingleton(this);
		parts.AddSingleton<GameEventManager>();
		parts.AddSingleton<MusicComponent>();
		parts.AddPart(sp => {
			var conductor = Add<Conductor>();
			conductor.Music = sp.GetRequiredService<MusicComponent>();
			conductor.SeekTo = SeekTo;
			return conductor;
		});
		parts.AddSingleton<IConductor>(sp => sp.GetRequiredService<Conductor>());
		gamemode.Compose(this, parts, chart);
		Services = parts.BuildServiceProvider();
	}

	public bool IsSeeking { get; private set; }

	public IGamemodeDescriptor GetGamemode() => Gamemode;

	public object? GetService(Type serviceType) => Services.GetService(serviceType);

	public ISongChart GetSongChart() => Chart;

	public void Restart() => SeekTo(0);

	public void SeekTo(double time) {
		var music = this.GetRequiredService<MusicComponent>();
		time = Math.Clamp(time, 0, music.Duration);
		IsSeeking = true;

		foreach (var part in this.GetServices<IRhythmGameComponent>())
			part.Reset();

		music.SeekTo(time);

		foreach (var part in this.GetServices<IRhythmGameSeekable>())
			part.SeekTo(time);

		IsSeeking = false;
		this.GetRequiredService<Conductor>().InvalidateTime();
	}
}

public static class RhythmGamePartExts
{
	public static IServiceCollection AddPart<T>(this IServiceCollection parts) where T : class
		=> parts.AddSingleton<T>().ForwardPart<T>();

	public static IServiceCollection AddPart<T>(this IServiceCollection parts, Func<IServiceProvider, T> factory) where T : class
		=> parts.AddSingleton(factory).ForwardPart<T>();

	static IServiceCollection ForwardPart<T>(this IServiceCollection parts) where T : class {
		if (typeof(T).IsAssignableTo(typeof(IRhythmGameComponent)))
			parts.AddSingleton<IRhythmGameComponent>(sp => (IRhythmGameComponent)sp.GetRequiredService<T>());
		if (typeof(T).IsAssignableTo(typeof(IRhythmGameSeekable)))
			parts.AddSingleton<IRhythmGameSeekable>(sp => (IRhythmGameSeekable)sp.GetRequiredService<T>());
		return parts;
	}
}