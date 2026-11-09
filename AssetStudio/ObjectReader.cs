using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AssetStudio
{
    public class ObjectReader : EndianBinaryReader
    {
        private static readonly HashSet<int> definedClassIDs = [.. Enum.GetValues<ClassIDType>().Select(x => (int)x)];

        public SerializedFile assetsFile;
        public long m_PathID;
        public long byteStart;
        public uint byteSize;
        public int classID;
        public ClassIDType type;
        public SerializedType serializedType;
        public BuildTarget platform;
        public SerializedFileFormatVersion m_Version;

        public UnityVersion version => assetsFile.version;

        public long Remaining => byteStart + byteSize - Position;

        public ObjectReader(EndianBinaryReader reader, SerializedFile assetsFile, ObjectInfo objectInfo) : base(BlockStream.View(reader.BaseStream), reader.Endian)
        {
            this.assetsFile = assetsFile;
            m_PathID = objectInfo.m_PathID;
            byteStart = objectInfo.byteStart;
            byteSize = objectInfo.byteSize;
            classID = objectInfo.classID;
            if (definedClassIDs.Contains(objectInfo.classID))
            {
                type = (ClassIDType)objectInfo.classID;
            }
            else
            {
                type = ClassIDType.UnknownType;
            }
            serializedType = objectInfo.serializedType;
            platform = assetsFile.m_TargetPlatform;
            m_Version = assetsFile.header.m_Version;
            IsPrivateView = BaseStream != reader.BaseStream;
        }

        private ObjectReader(ObjectReader source, byte[] data) : base(new MemoryStream(data, false), source.Endian)
        {
            assetsFile = source.assetsFile;
            m_PathID = source.m_PathID;
            byteSize = (uint)data.Length;
            classID = source.classID;
            type = source.type;
            serializedType = source.serializedType;
            platform = source.platform;
            m_Version = source.m_Version;
            IsPrivateView = true;
        }

        public bool IsPrivateView { get; }

        internal ObjectReader Detach()
        {
            Reset();
            return new ObjectReader(this, ReadBytes((int)byteSize));
        }

        public void ThrowIfTooLarge(float val)
        {
            if (val < 0 || val > Remaining)
                throw new EndOfStreamException();
        }

        public void Reset()
        {
            Position = byteStart;
        }
    }
}
