using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloneDash.Modding.Descriptors;

public class SceneDescriptor : CloneDashDescriptor
{
	public SceneDescriptor() : base(CloneDashDescriptorType.Scene, "2") { }

	[JsonProperty("name")] public string Name;
	[JsonProperty("author")] public string Author;

	public static SceneDescriptor ParseFile(string filepath) => ParseFile<SceneDescriptor>(filepath);
}
