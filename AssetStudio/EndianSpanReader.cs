using System;
using System.Buffers.Binary;
using System.Text;

namespace AssetStudio
{
    public static class EndianSpanReader
    {
        public static uint ReadUInt32(this Span<byte> data, int start, bool isBigEndian)
        {
            return ReadUInt32((ReadOnlySpan<byte>)data, start, isBigEndian);
        }

        public static uint ReadUInt32(this ReadOnlySpan<byte> data, int start, bool isBigEndian)
        {
            return isBigEndian
                ? BinaryPrimitives.ReadUInt32BigEndian(data.Slice(start))
                : BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(start));
        }

        public static long ReadUInt16(this Span<byte> data, int start, bool isBigEndian)
        {
            return ReadUInt16((ReadOnlySpan<byte>)data, start, isBigEndian);
        }

        public static uint ReadUInt16(this ReadOnlySpan<byte> data, int start, bool isBigEndian)
        {
            return isBigEndian
                ? BinaryPrimitives.ReadUInt16BigEndian(data.Slice(start))
                : BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(start));
        }

        public static long ReadInt64(this Span<byte> data, int start, bool isBigEndian)
        {
            return ReadInt64((ReadOnlySpan<byte>)data, start, isBigEndian);
        }

        public static long ReadInt64(this ReadOnlySpan<byte> data, int start, bool isBigEndian)
        {
            return isBigEndian
                ? BinaryPrimitives.ReadInt64BigEndian(data.Slice(start))
                : BinaryPrimitives.ReadInt64LittleEndian(data.Slice(start));
        }

        public static float ReadSingle(this Span<byte> data, int start, bool isBigEndian)
        {
            return ReadSingle((ReadOnlySpan<byte>)data, start, isBigEndian);
        }

#if NETFRAMEWORK
        public static float ReadSingle(this ReadOnlySpan<byte> data, int start, bool isBigEndian)
        {
            var bytes = data.Slice(start, 4).ToArray();
            if ((isBigEndian && BitConverter.IsLittleEndian) || (!isBigEndian && !BitConverter.IsLittleEndian))
                bytes.AsSpan().Reverse();

            return BitConverter.ToSingle(bytes, 0);
        }
#else
        public static float ReadSingle(this ReadOnlySpan<byte> data, int start, bool isBigEndian)
        {
            return isBigEndian
                ? BinaryPrimitives.ReadSingleBigEndian(data[start..])
                : BinaryPrimitives.ReadSingleLittleEndian(data[start..]);
        }
#endif

        public static string ReadStringToNull(this Span<byte> data, int maxLength = 32767)
        {
            return ReadStringToNull((ReadOnlySpan<byte>)data, maxLength);
        }

        public static string ReadStringToNull(this ReadOnlySpan<byte> data, int maxLength = 32767)
        {
            Span<byte> bytes = stackalloc byte[maxLength];
            var count = 0;
            while (count != data.Length && count < maxLength)
            {
                var b = data[count];
                if (b == 0)
                {
                    break;
                }
                bytes[count] = b;
                count++;
            }
            bytes = bytes.Slice(0, count);
#if NETFRAMEWORK
            return Encoding.UTF8.GetString(bytes.ToArray());
#else
            return Encoding.UTF8.GetString(bytes);
#endif
        }
    }
}
