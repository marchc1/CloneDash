using CloneDash.Common.Compatibility;
using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes;
using CloneDash.Common.Songs;
using CloneDash.Unbeatable.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public class UnbeatableWhiteLabelGamemodeProvider : ModProvider<IGamemodeDescriptor>, IGamemodeProvider
{
	readonly UnbeatableWhiteLabelGamemode[] descriptors;

	public UnbeatableWhiteLabelGamemodeProvider() : base("UNBEATABLE [white label]", "unbeatable_whitelabel") {
		descriptors = [
			new UnbeatableWhiteLabelGamemode(this),
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

public class UnbeatableWhiteLabelGamemode(ICloneDashModProvider provider) : RhythmGamemode(provider)
{
	public const string UUID = "gamemode/unbeatable_whitelabel/standard";

	public override string GetName() => "UNBEATABLE [white label]";
	public override string GetID() => "standard";

	public override IEnumerable<GamemodeInput> GetInputs() {
		List<GamemodeInput> inputs = [];
		foreach (var definition in UnbeatableWhiteLabelPathways.Layout.Definitions)
			inputs.Add(new(definition.Name, UnbeatableWhiteLabelActions.SET, definition.Action, definition.Color));
		return inputs;
	}

	protected override void Compose(RhythmGameLevel level, ServiceCollection parts, ISongChart chart) {
		var beatmap = (Beatmap)chart.GetGamemodeData();

		parts.AddStandardParts(level, UnbeatableWhiteLabelActions.Binds);
		AddRules(level, parts, beatmap);
		AddBehaviour(level, parts);
		AddPresentation(parts);
		AddTimeline(parts, beatmap);
	}

	static void AddBehaviour(RhythmGameLevel level, ServiceCollection parts) {
		parts.AddPart<UnbeatableWhiteLabelPlayerComponent>();
		parts.AddPart<UnbeatableWhiteLabelJudgeComponent>();
		parts.AddPart<UnbeatableWhiteLabelAutoPlayer>(autoPlayer => {
			autoPlayer.Enabled = level.GetParameters().Get<bool>(RhythmGameLoadParameters.AUTOPLAY);
			level.Component<InputComponent>().Receivers.Add(autoPlayer);
		});
	}

	static void AddPresentation(ServiceCollection parts) {
		parts.AddPart<ICameraRig, UnbeatableWhiteLabelCameraRig>();
		parts.AddPart<UnbeatableWhiteLabelStageComponent>();
		parts.AddPart<UnbeatableWhiteLabelPlayerVisualsComponent>();
		parts.AddPart<UnbeatableWhiteLabelPlayfieldComponent>();
		parts.AddPart<UnbeatableWhiteLabelHudComponent>();
	}

	static void AddRules(RhythmGameLevel level, ServiceCollection parts, Beatmap beatmap) {
		parts.AddPart<ComboComponent>();
		parts.AddPart<HealthComponent>(health => {
			health.Max = 100;
			health.IFrameLength = 0;
			health.Reset();
			level.Component<InputComponent>().CanPoll = () => !health.Dead;
			level.Component<ResultComponent>().IsFailed = () => health.Dead;
		});
		parts.AddPartWith<UnbeatableWhiteLabelScoreComponent>(beatmap);
		parts.AddPart<INoteRules<UnbeatableWhiteLabelNote>, UnbeatableWhiteLabelNoteRules>();
	}

	static void AddTimeline(ServiceCollection parts, Beatmap beatmap) {
		parts.AddPart<ScrollComponent>();
		parts.AddPart<UnbeatableWhiteLabelNoteComponents>();
		parts.AddPartWith<UnbeatableWhiteLabelChartComponent>(beatmap);
	}
}
