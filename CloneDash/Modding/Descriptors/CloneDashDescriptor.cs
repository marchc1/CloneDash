using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Nucleus.Platform;

namespace CloneDash.Modding.Descriptors
{
	public enum CloneDashDescriptorType {
		Character = 1,
		Scene = 2
	}
	[Nucleus.MarkForStaticConstruction]
	public abstract class CloneDashDescriptor
	{
		[JsonConverter(typeof(StringEnumConverter))] 
		public CloneDashDescriptorType Type { get; private set; }
		public CloneDashDescriptor(CloneDashDescriptorType type, string version = "1") {
			Type = type;
			Version = version;
		}
		public string? Filename;
		public string Version;
		public static T ParseFile<T>(string data, string filename) where T : CloneDashDescriptor {
			var ret = JsonConvert.DeserializeObject<T>(data) ?? throw new Exception("Could not parse the file.");
			ret.Filename = filename;
			return ret;
		}
	
		static CloneDashDescriptor() {
			FileAssoc.Register(".cdd", "CloneDash.Descriptor", "Clone Dash Descriptor File");
		}
	}
}