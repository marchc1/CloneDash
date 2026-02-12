namespace Nucleus.Common.FileSystem;

public interface IFileHandle {
	public bool CanRead { get; }
	public bool CanSeek { get; }
	public bool CanWrite { get; }
	public long Length { get; }
	public long Position { get; }
	public void Flush();
	public int Read(Span<byte> output);
	public long Seek(long offset, SeekOrigin origin);
	public int Write(ReadOnlySpan<byte> output);
}
