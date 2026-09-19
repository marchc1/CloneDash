namespace Nucleus.AudioSystem.Raylib;

internal static unsafe partial class Raylib
{
	/// <summary>C++ style memory allocator</summary>
	internal static T* New<T>(int count) where T : unmanaged {
		return (T*)MemAlloc(count * sizeof(T));
	}

	/// <summary>Convert wave data to desired format</summary>
	internal static void WaveFormat(ref Wave wave, int sampleRate, int sampleSize, int channels) {
		fixed (Wave* p = &wave) {
			WaveFormat(p, sampleRate, sampleSize, channels);
		}
	}

	/// <summary>Crop a wave to defined samples range</summary>
	internal static void WaveCrop(ref Wave wave, int initSample, int finalSample) {
		fixed (Wave* p = &wave) {
			WaveCrop(p, initSample, finalSample);
		}
	}

	/// <summary>Load wave data from file</summary>
	internal static Wave LoadWave(string fileName) {
		using var str1 = fileName.ToAnsiBuffer();
		return LoadWave(str1.AsPointer());
	}

	/// <summary>
	/// Load wave from managed memory, fileType refers to extension: i.e. "wav"
	/// </summary>
	internal static Wave LoadWaveFromMemory(
		string fileType,
		byte[] fileData
	) {
		using var fileTypeNative = fileType.ToAnsiBuffer();

		fixed (byte* fileDataNative = fileData) {
			Wave wave = LoadWaveFromMemory(
				fileTypeNative.AsPointer(),
				fileDataNative,
				fileData.Length
			);

			return wave;
		}
	}

	/// <summary>Load sound from file</summary>
	internal static Sound LoadSound(string fileName) {
		using var str1 = fileName.ToAnsiBuffer();
		return LoadSound(str1.AsPointer());
	}

	/// <summary>Export wave data to file</summary>
	internal static CBool ExportWave(Wave wave, string fileName) {
		using var str1 = fileName.ToAnsiBuffer();
		return ExportWave(wave, str1.AsPointer());
	}

	/// <summary>Export wave sample data to code (.h)</summary>
	internal static CBool ExportWaveAsCode(Wave wave, string fileName) {
		using var str1 = fileName.ToAnsiBuffer();
		return ExportWaveAsCode(wave, str1.AsPointer());
	}

	/// <summary>Load music stream from file</summary>
	internal static Music LoadMusicStream(string fileName) {
		using var str1 = fileName.ToAnsiBuffer();
		return LoadMusicStream(str1.AsPointer());
	}

	/// <summary>
	/// Load music stream from managed memory, fileType refers to extension: i.e. ".wav"
	/// </summary>
	internal static Music LoadMusicStreamFromMemory(
		string fileType,
		byte[] fileData
	) {
		using var fileTypeNative = fileType.ToAnsiBuffer();

		fixed (byte* fileDataNative = fileData) {
			Music music = LoadMusicStreamFromMemory(
				fileTypeNative.AsPointer(),
				fileDataNative,
				fileData.Length
			);

			return music;
		}
	}
}
