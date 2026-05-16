using System.IO;
using System.Text;

namespace WavHelper
{
    public sealed class WavHeader
    {
        private static byte[] ChunkId = Encoding.ASCII.GetBytes("RIFF");
        private uint ChunkSize;
        private static byte[] Format = Encoding.ASCII.GetBytes("WAVE");
        private static byte[] FmtChunkId = Encoding.ASCII.GetBytes("fmt ");
        private static uint FmtChunkSize = 16;
        private WavAudioFormat AudioFormat;
        private ushort NumChannels;
        private uint SampleRate;
        private uint ByteRate;
        private ushort BlockAlign;
        private ushort BitsPerSample;
        private static byte[] DataChunkId = Encoding.ASCII.GetBytes("data");
        private uint DataChunkSize;

        public WavHeader(uint dataSize, WavAudioFormat audioFormat, int channels, uint sampleRate, int bits)
        {
            ChunkSize = dataSize + 36;
            AudioFormat = audioFormat;
            NumChannels = (ushort)channels;
            SampleRate = sampleRate;
            BitsPerSample = (ushort)bits;
            ByteRate = SampleRate * NumChannels * BitsPerSample / 8;
            BlockAlign = (ushort)(NumChannels * BitsPerSample / 8);
            DataChunkSize = dataSize;
        }

        public void WriteToArray(byte[] buffer)
        {
            using (var stream = new MemoryStream(buffer))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(ChunkId);
                    writer.Write(ChunkSize);
                    writer.Write(Format);
                    writer.Write(FmtChunkId);
                    writer.Write(FmtChunkSize);
                    writer.Write((ushort)AudioFormat);
                    writer.Write(NumChannels);
                    writer.Write(SampleRate);
                    writer.Write(ByteRate);
                    writer.Write(BlockAlign);
                    writer.Write(BitsPerSample);
                    writer.Write(DataChunkId);
                    writer.Write(DataChunkSize);
                }
            }
        }
    }
}
