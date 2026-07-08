using Nucleus.Audio;
using Nucleus.Common.Audio;
using Nucleus.Common.Engine;
using Nucleus.Common.FileSystem;
using Nucleus.Files;
using Nucleus.NewEngine;

[assembly: NucleusAssembly]

namespace Nucleus;

/// <summary>
/// This is a class to load standard components from stage 3.
/// </summary>
public static class StandardComponents {
	extension(EngineBuilder api){
		public EngineBuilder WithStandardComponents(){
			return api
					.WithComponent<IFileSystem, FileSystem>()
					.WithComponent<IAudioSystem, RaylibAudioSystem>()
					;
		}
	}
}