using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Nucleus;
using Nucleus.Files;

namespace CloneDash.Modding
{
	public enum CloneDashDescriptorType
	{
		Character = 1,
		Scene = 2,
		Fever = 3
	}

	[MarkForStaticConstruction]
	public abstract class CloneDashDescriptor
	{
		[JsonConverter(typeof(StringEnumConverter))]
		public CloneDashDescriptorType Type { get; private set; }

		/// <summary>
		/// The search path, used to look for this descriptor type. Ex: "chars"
		/// </summary>
		public string SearchPathID { get; private set; }
		/// <summary>
		/// The mount path, where the loaded descriptor is mounted to the filesystem. Ex: "character"
		/// </summary>
		public string MountPathID { get; private set; }
		/// <summary>
		/// The file name of the .cdd file (without the extension). Ex: "character"
		/// </summary>
		public string DescriptorFileName { get; private set; }

		public CloneDashDescriptor(CloneDashDescriptorType type, string searchPathID, string cddFileName, string mountPathID, string version = "1") {
			Type = type;
			SearchPathID = searchPathID;
			MountPathID = mountPathID;
			DescriptorFileName = cddFileName;
			Version = version;
		}

		public string? Filename;
		[JsonRequired][JsonProperty("version")] public string Version;

		public static T ParseFile<T>(string data, string filename) where T : CloneDashDescriptor {
			var ret = JsonConvert.DeserializeObject<T>(data) ?? throw new Exception("Could not parse the file.");
			ret.Filename = filename;
			return ret;
		}

		static CloneDashDescriptor() {
			Platform.RegisterFileAssociation(".cdd", "CloneDash.Descriptor", "Clone Dash Descriptor File");
		}

		public void MountToFilesystem() {
			if (Filename == null) throw new FileNotFoundException("FeverDescriptor.MountToFilesystem: Cannot mount the file, because Filename == null!");
			Filesystem.RemoveSearchPath(MountPathID);

			// Find the search path that contains the scene descriptor.
			// TODO: Need to redo this! It doesn't really support zip files (which was the whole
			// point of the filesystem restructure!)
			var searchPath = Filesystem.FindSearchPath(SearchPathID, $"{Filename}/{DescriptorFileName}.cdd");
			switch (searchPath) {
				case DiskSearchPath diskPath:
					Filesystem.AddTemporarySearchPath(MountPathID, DiskSearchPath.Combine(searchPath, Filename));
					break;
			}
		}
	}
}