using CloneDash.Common.Compatibility;
using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes;
using CloneDash.Common.Songs;
using Microsoft.Extensions.DependencyInjection;

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

	public override string GetName() => "Muse Dash 1";
	public override string GetID() => "standard";

	protected override void Compose(RhythmGameLevel level, ServiceCollection parts, ISongChart chart) {
	}
}
