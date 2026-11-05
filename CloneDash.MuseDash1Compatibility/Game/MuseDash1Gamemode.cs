using CloneDash.Characters;
using CloneDash.Common;
using CloneDash.Common.Compatibility;
using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.Common.Scenes;
using CloneDash.Common.Songs;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Game.Entities;
using CloneDash.Game.Logic;
using CloneDash.Game.Statistics;
using CloneDash.MD1_Compat.Compatibility;
using CloneDash.MD1_Compat.Game.Events;
using CloneDash.Scenes;
using Microsoft.Extensions.DependencyInjection;
using Nucleus;
using Nucleus.Commands;
using Nucleus.Common.Commands;

namespace CloneDash.Game;

public class MuseDash1GamemodeProvider : ModProvider<IGamemodeDescriptor>, IGamemodeProvider
{
	readonly MuseDash1Gamemode[] descriptors;

	public MuseDash1GamemodeProvider() : base("Muse Dash 1", "musedash1") {
		descriptors = [
			new MuseDash1Gamemode(this),
			new MuseDash1TouhouGamemode(this),
		];
	}

	public override IGamemodeDescriptor? FindByID(ReadOnlySpan<char> id) {
		foreach (var descriptor in descriptors)
			if (id.Equals(descriptor.GetID(), StringComparison.InvariantCultureIgnoreCase))
				return descriptor;
		return null;
	}

	public override IEnumerable<string> GetAvailableIDs() {
		foreach (var descriptor in descriptors)
			yield return descriptor.GetID();
	}
}

[Nucleus.MarkForStaticConstruction]
public partial class MuseDash1Gamemode(ICloneDashModProvider provider) : RhythmGamemode(provider)
{
	public const string UUID = "gamemode/musedash1/standard";

	public static ConCommand md1level = new(nameof(md1level), clonedash_openmdlevel_execute, clonedash_openmdlevel_autocomplete, "Opens a Muse Dash 1 level.");

	public static float GlobalScale => 1 / 200f;

	public override string GetName() => "Muse Dash 1";
	public override string GetID() => "standard";

	protected override void Compose(RhythmGameLevel level, ServiceCollection parts, ISongChart chart) {
		var data = (MD1_GamemodeData)chart.GetGamemodeData();

		parts.AddStandardParts(level);
		AddQuirks(parts);
		AddSetup(parts, data);
		AddRules(level, parts);
		AddContent(parts, data);
		AddBehaviour(level, parts);
		AddPresentation(level, parts, data);
		AddTimeline(parts, data);
	}

	static void AddBehaviour(RhythmGameLevel level, ServiceCollection parts) {
		parts.AddPart<MashComponent>();
		parts.AddPart<MuseDash1CharacterComponent>();
		parts.AddPart<ISustainManager, StackBasedSustainManager>();
		parts.AddPart<MuseDash1JudgeComponent>();
		parts.AddEntityPart<AutoPlayer>(autoPlayer => {
			autoPlayer.Enabled = level.GetParameters().Get<bool>(RhythmGameLoadParameters.AUTOPLAY);
			level.Component<InputComponent>().Receivers.Add(autoPlayer);
		});
	}

	protected virtual void AddChart(ServiceCollection parts, MD1_GamemodeData data) => parts.AddPartWith<MuseDash1ChartComponent>(data);

	static void AddContent(ServiceCollection parts, MD1_GamemodeData data) {
		parts.AddPart<MuseDash1AssetsComponent>();
		parts.AddSlot<IMuseDash1SceneInstance>(slot => slot
			.From(() => SceneMod.Instance.GetActive())
			.From(() => SceneMod.Instance.GetDescriptorByUID(data.InitialScene))
			.Required());
		parts.AddPartWith<MuseDash1ScenesComponent>(data);
		parts.AddSlot<IMuseDash1CharacterInstance>(slot => slot
			.From(() => CharacterMod.Instance.GetActive())
			.Required());
	}

	static void AddPresentation(RhythmGameLevel level, ServiceCollection parts, MD1_GamemodeData data) {
		parts.AddPart<ScreenEffectsComponent>(PrepareShaders);
		parts.AddPart(sp => {
			var hitsounds = new HitsoundComponent(() => level.IsSeeking);
			hitsounds.RegisterEnum<SceneSound>("musedash1");
			hitsounds.AddOutput(new MuseDash1SceneHitsoundOutput(level.Component<MuseDash1ScenesComponent>()));
			return hitsounds;
		});
		parts.AddPart<ICameraRig, MuseDash1CameraRig>();
		parts.AddPartWith<MuseDash1BackgroundComponent>(data);
		parts.AddPart<MuseDash1HudComponent>();
		parts.AddSlot<IMuseDash1FeverRuntime>(slot => slot
			.From(() => FeverMod.Instance.GetActive()));
		parts.AddPart<MuseDash1FeverFXComponent>();
		parts.AddPart<MuseDash1PlayfieldComponent>();
	}

	static void AddQuirks(ServiceCollection parts) {
		parts.AddPart<MuseDash1GameplayQuirksComponent>(quirks => {
			var charData = CharacterMod.Instance.GetActive();
			if (charData == null) throw new ArgumentNullException(nameof(charData));
			charData.ApplyQuirks(ref quirks.Value);

			// Before loading the scene, load quirks, since some quirks might affect entity data
			// TODO: Elfin quirk mods
			MuseDash1GameplayQuirks.ApplyMods(ref quirks.Value);
		});
	}

	static void AddRules(RhythmGameLevel level, ServiceCollection parts) {
		parts.AddPart<ComboComponent>(combo => {
			ref var quirks = ref level.Component<MuseDash1GameplayQuirksComponent>().Value;
			combo.MissForgiveness = quirks.MissComboForgiveness;
		});
		parts.AddPart<HealthComponent>(health => {
			ref var quirks = ref level.Component<MuseDash1GameplayQuirksComponent>().Value;
			health.Max = quirks.MaxHP;
			health.IFrameLength = quirks.IFrameLength;
			health.BonusGranted = quirks.HealthBonusGranted;
			health.BonusThreshold = quirks.HealthBonusThreshold;
			health.BonusTimes = quirks.HealthBonusTimes;
			health.DyingSeconds = quirks.DyingSeconds;
			health.Reset();
		});
		parts.AddPart<FeverComponent>(fever => {
			ref var quirks = ref level.Component<MuseDash1GameplayQuirksComponent>().Value;
			fever.Max = quirks.MaxFever;
			fever.Duration = quirks.FeverDuration;
			fever.DismissalTime = quirks.PostFeverDismissalTime;
		});
		parts.AddPart<ScoreComponent>();
		parts.AddPart(sp => new StatisticsData(level.GetSongChart()));
		parts.AddPart<MuseDash1RulesComponent>();
	}

	static void AddSetup(ServiceCollection parts, MD1_GamemodeData data) {
		parts.AddPart(sp => new PathwaysComponent(data.Layout));
		parts.AddPart<ScrollComponent>(scroll => scroll.Add(new LayerRemapScrollModifier(data.LayerRemaps)));
	}

	void AddTimeline(ServiceCollection parts, MD1_GamemodeData data) {
		parts.AddPart<INoteRules<DashEnemy>, MuseDash1NoteRules>();
		parts.AddPart<DashEnemyComponents>();
		parts.AddEntityPart<Boss>(boss => boss.RendersItself = false);
		AddChart(parts, data);
		parts.AddPart<MuseDash1SeekComponent>();
		parts.AddPart<DashEventComponents>();
	}

	static void PrepareShaders(ScreenEffectsComponent effects) {
		// TODO: Verify effect order...
		effects.Add(new SepiaScreenEffect());
		effects.Add(new ChromaticAberrationScreenEffect());
		effects.Add(new MosaicScreenEffect());
		effects.Add(new ScanlinesScreenEffect());
		effects.Add(new FilmGrainScreenEffect());
		effects.Add(new VignetteScreenEffect());
		effects.Add(new ScreenScrollScreenEffect());
		effects.Add(new TVStaticScreenEffect());
		effects.Add(new FlashbangScreenEffect());
		effects.Add(new NoteFreezeScreenEffect());
		effects.Add(new BgFreezeScreenEffect());
		effects.Add(new FocusLinesScreenEffect());
		effects.Add(new NoteVisibilityScreenEffect());
		effects.Add(new BackgroundVisibilityScreenEffect());
		effects.Initialize();
	}

	private static void clonedash_openmdlevel_autocomplete(ConCommandBase cmd, string argsStr, TokenizedCommand args, int curArgPos, ref string[] returns, ref string[]? returnHelp) {
		if (curArgPos == 1) {
			var songs = MuseDash1Compatibility.FindSongsStartingWith(args.Arg(1));
			returns = [.. songs.Select(s => s.BaseName)];
			returnHelp = [.. songs.Select(s => $" '{s.FetchMetadata().Name}'")];
		}
		else if (curArgPos == 2) {
			var values = Enum.GetValues<MuseDashDifficulty>();
			returns = [.. values.Select(d => ((int)d).ToString())];
			returnHelp = [.. values.Select(d => d.ToString())];
		}
		else if (curArgPos == 3) {
			returns = ["0", "1"];
			returnHelp = ["Autoplay Off", "Autoplay On"];
		}
	}

	private static void clonedash_openmdlevel_execute(ConCommand cmd, in TokenizedCommand args) {
		var md_level = args.Arg(1);
		if (md_level.IsEmpty) {
			Logs.Warn("Provide a name.");
			return;
		}

		var map = args.Arg(2, -1);
		if (map <= 0) {
			Logs.Warn("Provide a difficulty.");
			return;
		}

		MD1_Song? song = MuseDash1Compatibility.FindSong(md_level);
		if (song == null) {
			Logs.Warn("Can't find that song.");
			Logs.Print("Here are some similar names:");
			foreach (var s in MuseDash1Compatibility.FindSimilarSongs(md_level))
				Logs.Print($"    {s.FetchMetadata().Name} ({s.BaseName})");
			return;
		}

		LevelTransitions.LoadSongChart($"Loading '{song.FetchMetadata().Name}'...", song.GetSheet(map), new RhythmGameLoadParameters()
			.Set(RhythmGameLoadParameters.AUTOPLAY, args.Arg(3, 0) == 1)
		);
	}
}