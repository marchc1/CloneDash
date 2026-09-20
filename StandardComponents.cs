using Nucleus.Audio;
using Nucleus.Common.Audio;
using Nucleus.Common.Engine;
using Nucleus.Common.FileSystem;
using Nucleus.Common.Graphics;
using Nucleus.Common.Localization;
using Nucleus.Engine;
using Nucleus.Files;
using Nucleus.ManagedMemory;
using Nucleus.NewEngine;

namespace Nucleus;

/// <summary>
/// This is a class to load standard components from stage 3.
/// 
/// </summary>
public static class StandardComponents {
	extension(EngineBuilder api){
		public EngineBuilder WithStandardComponents(){
			return api
					.WithComponent<IAudioSystem, RaylibAudioSystem>()
					.WithComponent<IFileSystem, FileSystem>()
					.WithComponent<ILocalize, EngineLocalize>()
					.WithComponent<ITextureManager, TextureManager>()
					;
		}
	}
}