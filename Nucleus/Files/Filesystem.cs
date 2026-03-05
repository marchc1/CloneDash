using Newtonsoft.Json;
using Nucleus.Core;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nucleus.Files;

/// <summary>
/// Temporary backwards compatibility for Raylib specific methods until we move this junk out of here
/// </summary>
public static class Filesystem
{
	public static string GetExtension(string path) => System.IO.Path.GetExtension(path) ?? "";
	private static FileNotFoundException NotFound(ReadOnlySpan<char> pathID, ReadOnlySpan<char> path) => new FileNotFoundException($"Cannot find '{path}' in '{pathID}'!");

	// Extra Raylib macros.

	// We use this scratch buffer to write files to before uploading to Raylib.
	// The strategy is to allocate 8mb buffers in a local thread context. 
	// The buffer is incremented when overflows would occur and never shrinks.
	internal static ThreadLocal<byte[]> ScratchBuffer = new(() => new byte[1024 * 1024 * 8]);

	public static T ReadJSON<T>(string pathID, string path) {
		return JSON.Deserialize<T>(filesystem.ReadAllText(pathID, path)!) ?? throw new Exception("Cannot deserialize.");
	}

	/// <summary>
	/// Prepares a file into memory for use with the scratch buffer system.
	/// </summary>
	/// <param name="pathID"></param>
	/// <param name="path"></param>
	/// <param name="scratchBuffer"></param>
	static unsafe Span<byte> ScratchUpload(string pathID, string path) {
		using (var stream = filesystem.Open(pathID, path, FileAccess.Read, FileMode.Open)) {
			if (stream == null)
				throw NotFound(pathID, path);

			var scratchBuffer = ScratchBuffer.Value!;
			if (scratchBuffer.Length < stream.Length) {
				int oldLength = scratchBuffer.Length;
				int newLength = oldLength;
				while (newLength < stream.Length)
					newLength = newLength * 2;
				scratchBuffer = new byte[newLength];
				ScratchBuffer.Value = scratchBuffer;
				Logs.Info($"ScratchUpload: incremented thread #{Thread.CurrentThread.ManagedThreadId}'s scratchbuffer from {oldLength} -> {newLength} bytes");
			}

			int size = stream.Read(scratchBuffer);
			return scratchBuffer.AsSpan()[..size];
		}
	}

	public static unsafe Image ReadImage(string pathID, string path) {
		var buffer = ScratchUpload(pathID, path);
		fixed (byte* data = buffer)
			return Raylib.LoadImageFromMemory(new Utf8Buffer(GetExtension(path)).AsPointer(), data, buffer.Length);
	}

	public static Texture2D ReadTexture(string pathID, string path, TextureFilter filter = TextureFilter.Bilinear) {
		using (Raylib.ImageRef img = new(ReadImage(pathID, path))) {
			var tex = Raylib.LoadTextureFromImage(img);
			Raylib.SetTextureFilter(tex, filter);
			return tex;
		}
	}

	public static unsafe Font ReadFont(string pathID, string path, int fontSize, int[] codepoints, int codepointCount) {
		var buffer = ScratchUpload(pathID, path);
		fixed (int* codepointsPtr = codepoints)
		fixed (byte* data = buffer) {
			var font = Raylib.LoadFontFromMemory(new Utf8Buffer(GetExtension(path)).AsPointer(), data, buffer.Length, fontSize, codepointsPtr, codepointCount);
			return font;
		}
	}

	public static Shader ReadVertexShader(string pathID, string vertexShader) {
		string? data = filesystem.ReadAllText(pathID, vertexShader);
		if (data == null) throw NotFound(pathID, vertexShader);

		var shader = Raylib.LoadShaderFromMemory(data, null);
		return shader;
	}

	public static Shader ReadFragmentShader(string pathID, string fragmentShader) {
		string? data = filesystem.ReadAllText(pathID, fragmentShader);
		if (data == null) throw NotFound(pathID, fragmentShader);

		var shader = Raylib.LoadShaderFromMemory(null, data);
		return shader;
	}

	public static Shader ReadShader(string pathID, string vertexShader, string fragmentShader) {
		string? vertexData = filesystem.ReadAllText(pathID, vertexShader);
		if (vertexData == null) throw NotFound(pathID, vertexShader);

		string? fragmentData = filesystem.ReadAllText(pathID, fragmentShader);
		if (fragmentData == null) throw NotFound(pathID, fragmentShader);

		var shader = Raylib.LoadShaderFromMemory(vertexData, fragmentData);
		return shader;
	}
}
