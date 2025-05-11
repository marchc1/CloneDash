using CloneDash.Modding;
using Newtonsoft.Json;
using Nucleus.Files;

namespace CloneDash.Fevers;

public class FeverDescriptor : CloneDashDescriptor
{
	public FeverDescriptor() : base(CloneDashDescriptorType.Fever, "fevers", "fever", "fever", "2025-05-06-01") { }

	public static FeverDescriptor? ParseFever(string filename) => Filesystem.ReadAllText("fevers", filename, out var text) ? ParseFile<FeverDescriptor>(text, filename) : null;

#nullable disable
	[JsonRequired][JsonProperty("name")] public string Name;
	[JsonRequired][JsonProperty("author")] public string Author;
	[JsonRequired][JsonProperty("background_controller")] public string PathToBackgroundController;
#nullable enable
}
