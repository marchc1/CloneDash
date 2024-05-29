using glTFLoader.Schema;
using System.Numerics;
using static glTFLoader.Schema.Accessor;

namespace Nucleus.Core
{
    public static class GLTFHelpers
    {
        public static string MIMEToRaylib(glTFLoader.Schema.Image.MimeTypeEnum mime) {
            switch (mime) {
                case glTFLoader.Schema.Image.MimeTypeEnum.image_png:
                    return ".png";
                default:
                    throw new NotImplementedException(mime.ToString());
            }
        }

        public class AccessorReader
        {
            public glTFLoader.Schema.Accessor Accessor { get; private set; }
            public glTFLoader.Schema.BufferView BufferView { get; private set; }
            public byte[] BufferData { get; private set; }
            public int BufferIndex { get; private set; } = 0;

            public AccessorReader(glTFLoader.Schema.Gltf gltf, byte[] buffer, int accessor) {
                Accessor = gltf.Accessors[accessor];
                if (!Accessor.BufferView.HasValue)
                    throw new Exception($"No BufferView associated with Accessor {accessor}");

                BufferView = gltf.BufferViews[Accessor.BufferView.Value];

                BufferData = new byte[BufferView.ByteLength];
                Array.Copy(buffer, BufferView.ByteOffset, BufferData, 0, BufferView.ByteLength);
            }

            public T Confirm<T>(T ret, TypeEnum? type = null, ComponentTypeEnum? componentType = null) {
                if ((type ?? Accessor.Type) != Accessor.Type)
                    throw new Exception($"Accessor.Type mismatch (expected {Enum.GetName(typeof(TypeEnum), Accessor.Type)}, got {Enum.GetName(typeof(TypeEnum), type.Value)})");
                if ((componentType ?? Accessor.ComponentType) != Accessor.ComponentType)
                    throw new Exception($"Accessor.ComponentType mismatch (expected {Enum.GetName(typeof(ComponentTypeEnum), Accessor.ComponentType)}, got {Enum.GetName(typeof(ComponentTypeEnum), componentType.Value)})");
                return ret;
            }

            private byte TakeByte() => BufferData[BufferIndex++];

            private byte[] TakeBytes(uint len, bool bigEndian = false) {
                byte[] ar = new byte[len];

                if (!bigEndian)
                    for (int i = 0; i < len; i++)
                        ar[i] = TakeByte();
                else
                    for (int i = 0; i < len; i++)
                        ar[len - i - 1] = TakeByte();

                return ar;
            }

            public byte ReadUByte() => Confirm(TakeByte(), componentType: ComponentTypeEnum.UNSIGNED_BYTE);
            public sbyte ReadByte() => Confirm(Convert.ToSByte(ReadUByte()), componentType: ComponentTypeEnum.BYTE);
            public short ReadShort() => Confirm(BitConverter.ToInt16(TakeBytes(2)), componentType: ComponentTypeEnum.SHORT);
            public ushort ReadUShort() => Confirm(BitConverter.ToUInt16(TakeBytes(2)), componentType: ComponentTypeEnum.UNSIGNED_SHORT);
            public uint ReadUInt() => Confirm(BitConverter.ToUInt32(TakeBytes(4)), componentType: ComponentTypeEnum.UNSIGNED_INT);
            public float ReadFloat() => Confirm(BitConverter.ToSingle(TakeBytes(4)), componentType: ComponentTypeEnum.FLOAT);

            public Vector2 ReadVector2F() => Confirm(new Vector2(ReadFloat(), ReadFloat()), TypeEnum.VEC3, ComponentTypeEnum.FLOAT);
            public Vector3 ReadVector3F() => Confirm(new Vector3(ReadFloat(), ReadFloat(), ReadFloat()), TypeEnum.VEC3, ComponentTypeEnum.FLOAT);
            public Vector4 ReadVector4F() => Confirm(new Vector4(ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat()), TypeEnum.VEC3, ComponentTypeEnum.FLOAT);

            public Matrix4x4 ReadMatrix4F() {
                Matrix4x4 ret = new Matrix4x4(ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat());
                return Confirm(Matrix4x4.Transpose(ret), TypeEnum.MAT4, ComponentTypeEnum.FLOAT);
            }

            public bool EOF => BufferIndex >= BufferData.Length;

            public List<byte> ReadUByteArray() {
                List<byte> ret = [];
                while (!EOF)
                    ret.Add(ReadUByte());
                return ret;
            }
            public List<ushort> ReadUShortArray() {
                List<ushort> ret = [];
                while (!EOF)
                    ret.Add(ReadUShort());
                return ret;
            }
            public List<float> ReadFloatArray() {
                List<float> ret = [];
                while (!EOF)
                    ret.Add(ReadFloat());
                return ret;
            }
            public List<Vector2> ReadVector2FArray() {
                List<Vector2> ret = [];
                while (!EOF)
                    ret.Add(ReadVector2F());
                return ret;
            }
            public List<Vector3> ReadVector3FArray() {
                List<Vector3> ret = [];
                while (!EOF)
                    ret.Add(ReadVector3F());
                return ret;
            }
            public List<Vector4> ReadVector4FArray() {
                List<Vector4> ret = [];
                while (!EOF)
                    ret.Add(ReadVector4F());
                return ret;
            }

            public List<Matrix4x4> ReadMatrix4FArray() {
                List<Matrix4x4> ret = [];
                while (!EOF)
                    ret.Add(ReadMatrix4F());
                return ret;
            }
        }
    }
}
