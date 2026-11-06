using CloneDash.Common.Compatibility;
using CloneDash.Common.Systems;
using CloneDash.Common.Systems.Discord;
using CloneDash.Common.Gamemodes;
using CloneDash.Common.Songs;
using CloneDash.Game;
using Microsoft.Extensions.DependencyInjection;
using Nucleus;
using Nucleus.Commands;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Entities;
using Nucleus.Types;
using Nucleus.Util;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Raylib_cs;

using Color = Nucleus.Common.Types.Color;

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

[MarkForStaticConstruction]
public class RhythmGameLevel : Level, IServiceProvider
{
	public static readonly ConVar musicspeed = new(nameof(musicspeed), "1", FCvar.None, "Sets the music speed for the game.", 0.1, 4, (cv, _, _) => {
		if (EngineCore.Level is RhythmGameLevel game)
			game.InitSpeedFromCvar();
	});

	public static ConCommand cdrestest = new(nameof(cdrestest), (_, in args) => {
		Vector2F winSize;
		switch (args.ArgS(1)) {
			case "16:9": winSize = new(1600, 900); break;
			case "19.5:9": winSize = new(1950, 900); break;
			case "4:3": winSize = new(1600, 1200); break;
			default:
				Logs.Warn($"Expected 16:9, 19.5:9, or 4:3.");
				return;

		}

		OSMonitor monitor = EngineCore.Window.Monitor;
		Vector2F monPos = monitor.Position;
		Vector2F monSize = monitor.Size;
		Vector2F winPos = (monPos + (monSize / 2)) - (winSize / 2);
		EngineCore.Window.Position = new((int)winPos.X, (int)winPos.Y);
		EngineCore.Window.Size = new((int)winSize.X, (int)winSize.Y);
	});

	public static ConCommand musicseek = new(nameof(musicseek), (_, in args) => {
		var level = EngineCore.Level.AsNullable<RhythmGameLevel>();
		if (level == null) {
			Logs.Warn("Not in game context!");
			return;
		}

		double d = args.Arg(1, -1d);
		if (d == -1) Logs.Warn("Did not specify a time!");
		else level.SeekTo(d);
	});

	public static ConVar profilegameload = new(nameof(profilegameload), "0", FCvar.None, "Profiles the game during loading, then triggers an engine interrupt afterwards to tell you how long each individual component took.");
	readonly RhythmGamemode Gamemode;
	readonly ServiceProvider Services;
	readonly ISongChart Chart;
	readonly RhythmGameLoadParameters Parameters;

	IRhythmGameBackgroundPreRenderer[] backgroundPreRenderers = [];
	ICameraRig? cameraRig;
	IRhythmGameComponent[] components = [];
	ScreenEffectsComponent? effects;
	bool endingFrame;
	IRhythmGameEntityPostRenderer[] entityPostRenderers = [];
	IRhythmGamePostRenderer[] postRenderers = [];
	IRhythmGamePreRenderer[] preRenderers = [];
	IRhythmGameRenderer[] renderers = [];
	IRhythmGameThinker[] thinkers = [];

	internal RhythmGameLevel(RhythmGamemode gamemode, ISongChart chart, RhythmGameLoadParameters parms) {
		Gamemode = gamemode;
		Chart = chart;
		Parameters = parms;

		ServiceCollection parts = new();
		parts.AddSingleton<Level>(this);
		parts.AddSingleton(this);
		parts.AddPart<GameEventManager>();
		parts.AddPart<MusicComponent>();
		parts.AddEntityPart<Conductor>(conductor => {
			conductor.Music = Component<MusicComponent>();
			conductor.SeekTo = SeekTo;
		});
		parts.AddSingleton<IConductor>(sp => sp.GetRequiredService<Conductor>());
		gamemode.Compose(this, parts, chart);
		Services = parts.BuildServiceProvider();

		foreach (var check in Services.GetServices<ContentSlotCheck>()) {
			if (check.Run(this) is not string error)
				continue;

			Services.Dispose();
			throw new ContentSlotException(error);
		}
	}

	public override bool IsInGame => true;

	public bool IsSeeking { get; private set; }

	public override void CalcView2D(FrameState frameState, ref Camera2D cam) => cameraRig?.CalcView2D(frameState, ref cam);

	public T Component<T>() where T : notnull => Services.GetRequiredService<T>();

	public IGamemodeDescriptor GetGamemode() => Gamemode;

	public RhythmGameLoadParameters GetParameters() => Parameters;

	public object? GetService(Type serviceType) => Services.GetService(serviceType);

	public ISongChart GetSongChart() => Chart;

	public void EndFrame() => endingFrame = true;

	public override void Initialize(params object[] args) {
		using (StaticSequentialProfiler.StartStackFrame("CD_GameLevel.RichPresenceUpdate")) {
			RichPresenceSystem.SetPresence(new() {
				Details = "In Game",
				State = $"{Gamemode.GetName()} - '{Chart.GetSong().FetchMetadata().Name}'"
			});
		}
		Render3D = false;

		foreach (var part in Services.GetServices<RhythmGamePartEntry>())
			Services.GetRequiredService(part.Type);

		backgroundPreRenderers = [.. Services.GetServices<IRhythmGameBackgroundPreRenderer>()];
		cameraRig = Services.GetService<ICameraRig>();
		effects = Services.GetService<ScreenEffectsComponent>();
		components = [.. Services.GetServices<IRhythmGameComponent>()];
		entityPostRenderers = [.. Services.GetServices<IRhythmGameEntityPostRenderer>()];
		postRenderers = [.. Services.GetServices<IRhythmGamePostRenderer>()];
		preRenderers = [.. Services.GetServices<IRhythmGamePreRenderer>()];
		renderers = [.. Services.GetServices<IRhythmGameRenderer>()];
		thinkers = [.. Services.GetServices<IRhythmGameThinker>().OrderBy(thinker => thinker.Phase)];

		foreach (var part in components)
			part.Load();

		var conductor = Component<Conductor>();
		var timing = Chart.GetTiming();
		foreach (var bpmChange in timing.TempoChanges)
			conductor.AddTempoChange(bpmChange.Time, bpmChange.Beat, bpmChange.BPM);

		foreach (var timeSigChange in timing.TimeSignatureChanges)
			conductor.AddTimeSignatureChange(timeSigChange.Beat, timeSigChange.Percentage);

		if (timing.TimeSignatureChanges.Count == 0)
			conductor.AddTimeSignatureChange(0, 1);

		Component<MusicComponent>().Load(Chart.GetAudioTrack());
		InitSpeedFromCvar();

		var measure = Parameters.Get<int>(RhythmGameLoadParameters.START_MEASURE);
		if (measure != 0)
			SeekTo(conductor.MeasureToSeconds(measure));

		if (StaticSequentialProfiler.Profiling) {
			StaticSequentialProfiler.End(out var stack, out var accumulators);
			PanicSystem.Interrupt(() => {
				Graphics2D.SetDrawColor(255, 255, 255);
				var lines = stack.ToStringArray();
				int y = 0;

				Graphics2D.DrawText(8, 8 + (y++ * 16), "Accumulators:", "Consolas", 15);
				for (int i = 0; i < accumulators.Count; i++, y++)
					Graphics2D.DrawText(8, 8 + (y * 16), $"  {accumulators[i].Key}: {accumulators[i].Value.Timer.Elapsed.TotalMilliseconds:F4} ms", "Consolas", 15);
				y++;
				Graphics2D.DrawText(8, 8 + (y++ * 16), "Stack:", "Consolas", 15);
				for (int i = 0; i < lines.Length; i++, y++)
					Graphics2D.DrawText(8, 8 + (y * 16), $"  {lines[i]}", "Consolas", 15);

			}, false);
		}
		Component<GameEventManager>().Fire(new LevelLoadedGameEvent());
		MainThread.RunASAP(Interlude.End, ThreadExecutionTime.AfterFrame);
	}

	public void InitSpeedFromCvar() {
		Component<MusicComponent>().SetSpeed(musicspeed.GetDouble());
	}

	public override void OnUnload() {
		Component<GameEventManager>().Clear();
		Services.Dispose();
	}

	public override void PostRender(FrameState frameState) {
		foreach (var part in postRenderers)
			part.PostRender(frameState);

		effects?.End();
		effects?.Draw(frameState);
	}

	public override void PostRenderEntities(FrameState frameState) {
		foreach (var part in entityPostRenderers)
			part.PostRenderEntities(frameState);
	}

	public override void PreRender(FrameState frameState) {
		float width = EngineCore.GetWindowWidth(), height = EngineCore.GetWindowHeight();
		if (effects != null) {
			effects.Begin(width, height);
			EngineCore.Window.ClearBackground(Color.Blank);
		}

		foreach (var part in preRenderers)
			part.PreRender(frameState);
	}

	public override void PreRenderBackground(FrameState frameState) {
		foreach (var part in backgroundPreRenderers)
			part.PreRenderBackground(frameState);
	}

	public override void Render(FrameState frameState) {
		foreach (var part in renderers)
			part.Render(frameState);
	}

	public void Restart() => SeekTo(0);

	public void SeekTo(double time) {
		var music = Component<MusicComponent>();
		time = Math.Clamp(time, 0, music.Duration);
		IsSeeking = true;

		foreach (var part in components)
			part.Reset();

		music.SeekTo(time);

		foreach (var part in components)
			part.SeekTo(time);

		IsSeeking = false;
		Component<Conductor>().InvalidateTime();
		Component<GameEventManager>().Fire(new SeekFinishedGameEvent(time));
	}

	public override void Think(FrameState frameState) {
		endingFrame = false;
		foreach (var part in thinkers) {
			part.Think(frameState);
			if (endingFrame)
				return;
		}
	}
}

public static class RhythmGamePartExts
{
	public static IServiceCollection AddEntityPart<T>(this IServiceCollection parts, Action<T>? configure = null) where T : Entity
		=> parts.AddPart(sp => {
			var entity = sp.GetRequiredService<Level>().Add(ActivatorUtilities.CreateInstance<T>(sp));
			configure?.Invoke(entity);
			return entity;
		});

	public static IServiceCollection AddPart<T>(this IServiceCollection parts) where T : class
		=> parts.AddSingleton<T>().ForwardPart<T>();

	public static IServiceCollection AddPart<T>(this IServiceCollection parts, Action<T> configure) where T : class
		=> parts.AddPart(sp => {
			var part = ActivatorUtilities.CreateInstance<T>(sp);
			configure(part);
			return part;
		});

	public static IServiceCollection AddPart<T>(this IServiceCollection parts, Func<IServiceProvider, T> factory) where T : class
		=> parts.AddSingleton(factory).ForwardPart<T>();

	public static IServiceCollection AddPart<TService, TImpl>(this IServiceCollection parts) where TService : class where TImpl : class, TService
		=> parts.AddPart<TImpl>().AddSingleton<TService>(sp => sp.GetRequiredService<TImpl>());

	public static IServiceCollection AddPartWith<T>(this IServiceCollection parts, params object[] arguments) where T : class
		=> parts.AddPart(sp => ActivatorUtilities.CreateInstance<T>(sp, arguments));

	public static IServiceCollection AddPartWith<TService, TImpl>(this IServiceCollection parts, params object[] arguments) where TService : class where TImpl : class, TService
		=> parts.AddPartWith<TImpl>(arguments).AddSingleton<TService>(sp => sp.GetRequiredService<TImpl>());

	static IServiceCollection Forward<T, TInterface>(this IServiceCollection parts) where T : class where TInterface : class {
		if (typeof(T).IsAssignableTo(typeof(TInterface)))
			parts.AddSingleton(sp => (TInterface)(object)sp.GetRequiredService<T>());
		return parts;
	}

	static IServiceCollection ForwardPart<T>(this IServiceCollection parts) where T : class {
		parts.AddSingleton(new RhythmGamePartEntry(typeof(T)));
		return parts
			.Forward<T, IRhythmGameComponent>()
			.Forward<T, IRhythmGameThinker>()
			.Forward<T, IRhythmGameBackgroundPreRenderer>()
			.Forward<T, IRhythmGamePreRenderer>()
			.Forward<T, IRhythmGameRenderer>()
			.Forward<T, IRhythmGameEntityPostRenderer>()
			.Forward<T, IRhythmGamePostRenderer>();
	}
}

sealed record RhythmGamePartEntry(Type Type);