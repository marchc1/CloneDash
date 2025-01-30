using Newtonsoft.Json;
using Nucleus.Platform;

namespace CloneDash.Modding.Descriptors
{
	[Nucleus.MarkForStaticConstruction]
	public abstract class CloneDashDescriptor
	{
		public int Version;
		public static T ParseFile<T>(string filepath) where T : CloneDashDescriptor => JsonConvert.DeserializeObject<T>(File.ReadAllText(filepath)) ?? throw new Exception("Could not parse the file.");
	
		static CloneDashDescriptor() {
			FileAssoc.Register(".cdd", "CloneDash.Descriptor", "Clone Dash Descriptor File");
		}
	}
}
