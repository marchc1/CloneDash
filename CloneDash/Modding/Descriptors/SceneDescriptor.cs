using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloneDash.Modding.Descriptors;

public class SceneDescriptor : CloneDashDescriptor, IDisposable
{
	public class SceneDescriptor_Announcer {
		[JsonProperty("begin")] public string Begin;
		[JsonProperty("fever")] public string Fever;
		[JsonProperty("unpause")] public string Unpause;
		[JsonProperty("fullcombo")] public string FullCombo;
	}
	public SceneDescriptor() : base(CloneDashDescriptorType.Scene, "2") { }

	[JsonProperty("name")] public string Name;
	[JsonProperty("author")] public string Author;
	
	[JsonProperty("announcer")] public SceneDescriptor_Announcer AnnouncerLines;
	private bool disposedValue;

	public static SceneDescriptor ParseFile(string filepath) => ParseFile<SceneDescriptor>(filepath);

	protected virtual void Dispose(bool disposing) {
		if (!disposedValue) {
			if (disposing) {
				// TODO: dispose managed state (managed objects)
			}

			// TODO: free unmanaged resources (unmanaged objects) and override finalizer
			// TODO: set large fields to null
			disposedValue = true;
		}
	}

	~SceneDescriptor()
	{
	    // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
	    Dispose(disposing: false);
	}

	public void Dispose() {
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
