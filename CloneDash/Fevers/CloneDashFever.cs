using CloneDash.Game;
using CloneDash.Modding;

using Newtonsoft.Json;

using Nucleus.Files;

namespace CloneDash.Fevers;

public class CloneDashFever : CloneDashDescriptor, IFeverDescriptor
{
	public CloneDashFever() : base(CloneDashDescriptorType.Fever, "fevers", "fever", "fever", "2025-05-06-01") { }

	public static CloneDashFever? ParseFever(string filename) => filesystem.ReadAllText("fevers", filename, out var text) ? ParseFile<CloneDashFever>(text, filename) : null;

	public void Initialize(DashGameLevel game) {

	}

	public void Start(DashGameLevel game) {

	}

	public void Think(DashGameLevel game) {

	}

	public void Render(DashGameLevel game) {

	}

#nullable disable
	[JsonRequired][JsonProperty("name")] public string Name;
	[JsonRequired][JsonProperty("author")] public string Author;
	[JsonRequired][JsonProperty("background_controller")] public string PathToBackgroundController;
#nullable enable
}
