using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Nucleus.Platform;

namespace CloneDash.Modding.Descriptors
{
	public enum CloneDashDescriptorType {
		Character = 1
	}
	[Nucleus.MarkForStaticConstruction]
	public abstract class CloneDashDescriptor
	{
		[JsonConverter(typeof(StringEnumConverter))] 
		public CloneDashDescriptorType Type { get; private set; }
		public CloneDashDescriptor(CloneDashDescriptorType type, int version = 1) {
			Type = type;
			Version = version;
		}
		public string? Filepath;
		public int Version;
		public static T ParseFile<T>(string filepath) where T : CloneDashDescriptor {
			var ret = JsonConvert.DeserializeObject<T>(File.ReadAllText(filepath)) ?? throw new Exception("Could not parse the file.");
			ret.Filepath = filepath;
			return ret;
		}
	
		static CloneDashDescriptor() {
			FileAssoc.Register(".cdd", "CloneDash.Descriptor", "Clone Dash Descriptor File");
		}
	}
}


