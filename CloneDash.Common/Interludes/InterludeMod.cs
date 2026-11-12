using CloneDash.Common.Compatibility;
using Nucleus;
using Nucleus.Commands;

namespace CloneDash.Common.Interludes;

[MarkForStaticConstruction]
public sealed class InterludeMod : ModStore<IInterludeProvider, IInterludeDescriptor>
{
	public static InterludeMod Instance => ModStores.Get<InterludeMod>();

	public static ConCommand interludes = new(nameof(interludes), (_, in _) => Instance.PrintAll(), "Prints all available interludes");

	static InterludeMod() {
		_ = Instance;
	}

	public InterludeMod() : base("Interludes", "interlude") { }
}
