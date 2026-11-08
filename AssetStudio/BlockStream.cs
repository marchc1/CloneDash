using System;
using System.Buffers;
using System.IO;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace AssetStudio
{
    public class BlockStream : Stream
    {
        private readonly SafeFileHandle _handle;
        private readonly BundleFile.StorageBlock[] _blocks;
        private readonly long[] _compressedOffsets;
        private readonly long[] _uncompressedOffsets;
        private readonly byte[][] _data;
        private readonly object _sync = new object();
        private readonly long _length;
        private long _position;
        private bool _disposed;

        public BlockStream(string path, long dataOffset, BundleFile.StorageBlock[] blocks)
        {
            _blocks = blocks;
            _compressedOffsets = new long[blocks.Length];
            _uncompressedOffsets = new long[blocks.Length + 1];
            _data = new byte[blocks.Length][];

            long compressed = dataOffset, uncompressed = 0;
            for (var i = 0; i < blocks.Length; i++)
            {
                _compressedOffsets[i] = compressed;
                _uncompressedOffsets[i] = uncompressed;
                compressed += blocks[i].compressedSize;
                uncompressed += blocks[i].uncompressedSize;
            }
            _uncompressedOffsets[blocks.Length] = uncompressed;
            _length = uncompressed;

            _handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, FileOptions.RandomAccess);
        }

        public static bool Supports(CompressionType type) => type is CompressionType.None or CompressionType.Lz4 or CompressionType.Lz4HC or CompressionType.Zstd or CompressionType.Oodle;

        public Stream CreateNodeStream(long offset, long size) => new NodeStream(this, offset, size, true);

        public static Stream View(Stream stream) => stream is NodeStream node ? node.CreateView() : stream;

        public override bool CanRead => !_disposed;
        public override bool CanSeek => !_disposed;
        public override bool CanWrite => false;
        public override long Length => _length;

        public override long Position
        {
            get => _position;
            set => _position = value;
        }

        public byte[] GetBlock(long position, out long blockStart)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var index = BlockAt(position);
            blockStart = _uncompressedOffsets[index];
            return EnsureBlock(index);
        }

        public int ReadAt(long position, Span<byte> buffer)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (position >= _length || buffer.Length == 0)
                return 0;

            var count = (int)Math.Min(buffer.Length, _length - position);
            var index = BlockAt(position);
            var done = 0;
            while (done < count)
            {
                var block = EnsureBlock(index);
                var inBlock = (int)(position + done - _uncompressedOffsets[index]);
                var take = Math.Min(count - done, block.Length - inBlock);
                block.AsSpan(inBlock, take).CopyTo(buffer.Slice(done));
                done += take;
                index++;
            }
            return done;
        }

        private int BlockAt(long position)
        {
            var index = Array.BinarySearch(_uncompressedOffsets, position);
            if (index < 0)
                index = ~index - 1;
            while (index < _blocks.Length - 1 && _blocks[index].uncompressedSize == 0)
                index++;
            return index;
        }

        private byte[] EnsureBlock(int index)
        {
            var existing = Volatile.Read(ref _data[index]);
            if (existing != null)
                return existing;

            var info = _blocks[index];
            var block = new byte[info.uncompressedSize];
            var compressionType = (CompressionType)(info.flags & StorageBlockFlags.CompressionTypeMask);
            if (compressionType == CompressionType.None)
            {
                ReadExact(block, _compressedOffsets[index]);
            }
            else
            {
                var compressed = ArrayPool<byte>.Shared.Rent((int)info.compressedSize);
                try
                {
                    var compressedSpan = compressed.AsSpan(0, (int)info.compressedSize);
                    ReadExact(compressedSpan, _compressedOffsets[index]);

                    var errorMsg = string.Empty;
                    var numWrite = BundleDecompressionHelper.DecompressBlock(compressionType, compressedSpan, block, ref errorMsg);
                    if (numWrite != info.uncompressedSize)
                        throw new IOException($"{compressionType} block decompression error. {errorMsg}\nWrite {numWrite} bytes but expected {info.uncompressedSize} bytes.\nBlock data might be encrypted.\n");
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(compressed);
                }
            }
            return Interlocked.CompareExchange(ref _data[index], block, null) ?? block;
        }

        private void ReadExact(Span<byte> buffer, long fileOffset)
        {
            var read = 0;
            while (read < buffer.Length)
            {
                var n = RandomAccess.Read(_handle, buffer.Slice(read), fileOffset + read);
                if (n == 0)
                    throw new EndOfStreamException("Bundle block data is truncated.");
                read += n;
            }
        }

        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            lock (_sync)
            {
                var n = ReadAt(_position, buffer);
                _position += n;
                return n;
            }
        }

        public override int ReadByte()
        {
            Span<byte> one = stackalloc byte[1];
            return Read(one) == 1 ? one[0] : -1;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            _position = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => _position + offset,
                SeekOrigin.End => _length + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            };
            return _position;
        }

        public override void Flush() { }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_sync)
                {
                    if (!_disposed)
                    {
                        _disposed = true;
                        _handle.Dispose();
                        Array.Clear(_data);
                    }
                }
            }
            base.Dispose(disposing);
        }

        private class NodeStream : Stream
        {
            private readonly BlockStream _owner;
            private readonly long _offset;
            private readonly long _length;
            private readonly bool _ownsOwner;
            private long _position;
            private byte[] _block;
            private long _blockStart;

            public NodeStream(BlockStream owner, long offset, long length, bool ownsOwner)
            {
                _owner = owner;
                _offset = offset;
                _length = length;
                _ownsOwner = ownsOwner;
            }

            public Stream CreateView() => new NodeStream(_owner, _offset, _length, false);

            public override bool CanRead => _owner.CanRead;
            public override bool CanSeek => _owner.CanSeek;
            public override bool CanWrite => false;
            public override long Length => _length;

            public override long Position
            {
                get => _position;
                set => _position = value;
            }

            public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

            public override int Read(Span<byte> buffer)
            {
                var position = _position;
                if (position >= _length)
                    return 0;
                if (buffer.Length > _length - position)
                    buffer = buffer.Slice(0, (int)(_length - position));

                ObjectDisposedException.ThrowIf(_owner._disposed, this);
                if (buffer.Length == 0)
                    return 0;

                var absolute = _offset + position;
                var block = _block;
                if (block == null || absolute < _blockStart || absolute >= _blockStart + block.Length)
                {
                    block = _owner.GetBlock(absolute, out _blockStart);
                    _block = block;
                }

                var inBlock = (int)(absolute - _blockStart);
                int n;
                if (buffer.Length <= block.Length - inBlock)
                {
                    block.AsSpan(inBlock, buffer.Length).CopyTo(buffer);
                    n = buffer.Length;
                }
                else
                {
                    n = _owner.ReadAt(absolute, buffer);
                }
                _position = position + n;
                return n;
            }

            public override int ReadByte()
            {
                Span<byte> one = stackalloc byte[1];
                return Read(one) == 1 ? one[0] : -1;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                _position = origin switch
                {
                    SeekOrigin.Begin => offset,
                    SeekOrigin.Current => _position + offset,
                    SeekOrigin.End => _length + offset,
                    _ => throw new ArgumentOutOfRangeException(nameof(origin))
                };
                return _position;
            }

            public override void Flush() { }

            public override void SetLength(long value) => throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if (disposing && _ownsOwner)
                    _owner.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}
