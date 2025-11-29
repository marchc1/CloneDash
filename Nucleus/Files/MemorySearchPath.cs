using Nucleus.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Files;


public class MemoryFile : Stream
{
	MemoryStream backingStream = new();
	public MemoryStream BaseStream => backingStream;

	public override bool CanRead => true;
	public override bool CanSeek => true;
	public override bool CanWrite => true;
	public override long Length => backingStream.Length;

	public override long Position { get => backingStream.Position; set => backingStream.Position = value; }
	public override void Flush() => backingStream.Flush();
	public override int Read(byte[] buffer, int offset, int count) => backingStream.Read(buffer, offset, count);
	public override long Seek(long offset, SeekOrigin origin) => backingStream.Seek(offset, origin);
	public override void SetLength(long value) => backingStream.SetLength(value);
	public override void Write(byte[] buffer, int offset, int count) => backingStream.Write(buffer, offset, count);

	protected override void Dispose(bool disposing) {
		Position = 0; // ?
	}
}
public class MemorySearchPath : SearchPath
{
	public Dictionary<UtlSymId_t, MemoryFile> __encoded = [];

	public override bool CheckFile(ReadOnlySpan<char> path, FileAccess? specificAccess, FileMode? specificMode) {
		switch (specificMode) {
			case FileMode.Open:
				return __encoded.ContainsKey(path.Hash());
			case FileMode.CreateNew:
				return !__encoded.ContainsKey(path.Hash());
			case FileMode.Create:
			case FileMode.OpenOrCreate:
			default:
				return true;
		}
	}
	public override IEnumerable<string> FindDirectories(ReadOnlySpan<char> path, ReadOnlySpan<char> searchQuery, SearchOption options) {
		yield break; // unimplemented, but i don't want things to die on it
	}
	public override IEnumerable<string> FindFiles(ReadOnlySpan<char> path, ReadOnlySpan<char> searchQuery, SearchOption options) {
		yield break; // unimplemented, but i don't want things to die on it
	}
	protected override bool CheckDirectory(ReadOnlySpan<char> path, FileAccess? specificAccess = null, FileMode? specificMode = null) {
		return false; // todo
	}
	protected override Stream? OnOpen(ReadOnlySpan<char> path, FileAccess access, FileMode open) {
		ulong hash = path.Hash();
		if (!__encoded.TryGetValue(hash, out MemoryFile? data)){
			if (access == FileAccess.Read) 
				return null;

			MemoryFile writeStream = new();
			__encoded[hash] = writeStream;
			return writeStream;
		}

		return data;
	}
}
