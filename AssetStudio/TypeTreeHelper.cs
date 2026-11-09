using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssetStudio
{
    public static class TypeTreeHelper
    {
        private static readonly JsonSerializerOptions JsonOptions;
        static TypeTreeHelper()
        {
            JsonOptions = new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                IncludeFields = true,
            };
        }

        public static string ReadTypeString(TypeTree m_Type, ObjectReader reader)
        {
            reader.Reset();
            var readed = 0L;
            var sb = new StringBuilder();
            var m_Nodes = new NodeRange(m_Type);
            try
            {
                for (var i = 0; i < m_Nodes.Count; i++)
                {
                    ReadStringValue(sb, m_Nodes, reader, ref i);
                    readed = reader.Position - reader.byteStart;
                }
            }
            catch (Exception)
            {
                //Ignore
            }
            if (readed != reader.byteSize)
            {
                Logger.Info($"Failed to read type, read {readed} bytes but expected {reader.byteSize} bytes");
            }

            return sb.ToString();
        }

        private static void ReadStringValue(StringBuilder sb, NodeRange m_Nodes, BinaryReader reader, ref int i)
        {
            var m_Node = m_Nodes[i];
            var level = m_Node.m_Level;
            var varTypeStr = m_Node.m_Type;
            var varNameStr = m_Node.m_Name;
            object value = null;
            var append = true;
            var align = (m_Node.m_MetaFlag & 0x4000) != 0;
            switch (varTypeStr)
            {
                case "SInt8":
                    value = reader.ReadSByte();
                    break;
                case "UInt8":
                    value = reader.ReadByte();
                    break;
                case "char":
                    value = BitConverter.ToChar(reader.ReadBytes(2), 0);
                    break;
                case "short":
                case "SInt16":
                    value = reader.ReadInt16();
                    break;
                case "UInt16":
                case "unsigned short":
                    value = reader.ReadUInt16();
                    break;
                case "int":
                case "SInt32":
                    value = reader.ReadInt32();
                    break;
                case "UInt32":
                case "unsigned int":
                case "Type*":
                    value = reader.ReadUInt32();
                    break;
                case "long long":
                case "SInt64":
                    value = reader.ReadInt64();
                    break;
                case "UInt64":
                case "unsigned long long":
                case "FileSize":
                    value = reader.ReadUInt64();
                    break;
                case "float":
                    value = reader.ReadSingle();
                    break;
                case "double":
                    value = reader.ReadDouble();
                    break;
                case "bool":
                    value = reader.ReadBoolean();
                    break;
                case "string" when m_Nodes[i + 1].m_Type == "Array":
                    append = false;
                    var str = reader.ReadAlignedString();
                    sb.AppendFormat("{0}{1} {2} = \"{3}\"\r\n", (new string('\t', level)), varTypeStr, varNameStr, str);
                    var toSkip = m_Nodes.Subtree(i);
                    i += toSkip.Count - 1;
                    break;
                case "map":
                    {
                        if ((m_Nodes[i + 1].m_MetaFlag & 0x4000) != 0)
                            align = true;
                        append = false;
                        sb.AppendFormat("{0}{1} {2}\r\n", (new string('\t', level)), varTypeStr, varNameStr);
                        sb.AppendFormat("{0}{1} {2}\r\n", (new string('\t', level + 1)), "Array", "Array");
                        var size = reader.ReadInt32();
                        sb.AppendFormat("{0}{1} {2} = {3}\r\n", (new string('\t', level + 1)), "int", "size", size);
                        var map = m_Nodes.Subtree(i);
                        i += map.Count - 1;
                        var first = map.Subtree(4);
                        var next = 4 + first.Count;
                        var second = map.Subtree(next);
                        for (int j = 0; j < size; j++)
                        {
                            sb.AppendFormat("{0}[{1}]\r\n", (new string('\t', level + 2)), j);
                            sb.AppendFormat("{0}{1} {2}\r\n", (new string('\t', level + 2)), "pair", "data");
                            int tmp1 = 0;
                            int tmp2 = 0;
                            ReadStringValue(sb, first, reader, ref tmp1);
                            ReadStringValue(sb, second, reader, ref tmp2);
                        }
                        break;
                    }
                case "TypelessData":
                    {
                        append = false;
                        var size = reader.ReadInt32();
                        reader.BaseStream.Position += size;
                        i += 2;
                        sb.AppendFormat("{0}{1} {2}\r\n", (new string('\t', level)), varTypeStr, varNameStr);
                        sb.AppendFormat("{0}{1} {2} = {3}\r\n", (new string('\t', level)), "int", "size", size);
                        break;
                    }
                default:
                    {
                        if (i < m_Nodes.Count - 1 && m_Nodes[i + 1].m_Type == "Array") //Array
                        {
                            if ((m_Nodes[i + 1].m_MetaFlag & 0x4000) != 0)
                                align = true;
                            append = false;
                            sb.AppendFormat("{0}{1} {2}\r\n", (new string('\t', level)), varTypeStr, varNameStr);
                            sb.AppendFormat("{0}{1} {2}\r\n", (new string('\t', level + 1)), "Array", "Array");
                            var size = reader.ReadInt32();
                            sb.AppendFormat("{0}{1} {2} = {3}\r\n", (new string('\t', level + 1)), "int", "size", size);
                            var vector = m_Nodes.Subtree(i);
                            i += vector.Count - 1;
                            for (int j = 0; j < size; j++)
                            {
                                sb.AppendFormat("{0}[{1}]\r\n", (new string('\t', level + 2)), j);
                                int tmp = 3;
                                ReadStringValue(sb, vector, reader, ref tmp);
                            }
                            break;
                        }
                        else //Class
                        {
                            if (m_Node.m_Type == "string")
                                m_Node.m_Type = "CustomType";
                            append = false;
                            sb.AppendFormat("{0}{1} {2}\r\n", (new string('\t', level)), varTypeStr, varNameStr);
                            var @class = m_Nodes.Subtree(i);
                            i += @class.Count - 1;
                            for (int j = 1; j < @class.Count; j++)
                            {
                                ReadStringValue(sb, @class, reader, ref j);
                            }
                            break;
                        }
                    }
            }
            if (append)
                sb.AppendFormat("{0}{1} {2} = {3}\r\n", (new string('\t', level)), varTypeStr, varNameStr, value);
            if (align)
                reader.AlignStream();
        }

        public static byte[] ReadTypeByteArray(TypeTree m_Types, ObjectReader reader)
        {
            var type = ReadType(m_Types, reader);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(type, JsonOptions);
            type.Clear();
            return bytes;
        }

        public static OrderedDictionary ReadType(TypeTree m_Types, ObjectReader reader) => ReadType(m_Types, reader, null);

        public static OrderedDictionary ReadType(TypeTree m_Types, ObjectReader reader, IReadOnlySet<string> fields)
        {
            reader.Reset();
            var obj = new OrderedDictionary();
            var m_Nodes = new NodeRange(m_Types);
            var readed = 0L;
            try
            {
                for (int i = 1; i < m_Nodes.Count; i++)
                {
                    var m_Node = m_Nodes[i];
                    var varNameStr = m_Node.m_Name;
                    if (fields == null || fields.Contains(varNameStr))
                        obj[varNameStr] = ReadValue(m_Nodes, reader, ref i);
                    else
                        SkipValue(m_Nodes, reader, ref i);
                    readed = reader.Position - reader.byteStart;
                }
            }
            catch (Exception)
            {
                //Ignore
            }
            if (readed != reader.byteSize)
            {
                Logger.Info($"Failed to read type, read {readed} bytes but expected {reader.byteSize} bytes");
            }
            return obj;
        }

        private static object ReadValue(NodeRange m_Nodes, BinaryReader reader, ref int i)
        {
            var m_Node = m_Nodes[i];
            var varTypeStr = m_Node.m_Type;
            object value;
            var align = (m_Node.m_MetaFlag & 0x4000) != 0;
            switch (varTypeStr)
            {
                case "SInt8":
                    value = reader.ReadSByte();
                    break;
                case "UInt8":
                    value = reader.ReadByte();
                    break;
                case "char":
                    value = BitConverter.ToChar(reader.ReadBytes(2), 0);
                    break;
                case "short":
                case "SInt16":
                    value = reader.ReadInt16();
                    break;
                case "UInt16":
                case "unsigned short":
                    value = reader.ReadUInt16();
                    break;
                case "int":
                case "SInt32":
                    value = reader.ReadInt32();
                    break;
                case "UInt32":
                case "unsigned int":
                case "Type*":
                    value = reader.ReadUInt32();
                    break;
                case "long long":
                case "SInt64":
                    value = reader.ReadInt64();
                    break;
                case "UInt64":
                case "unsigned long long":
                case "FileSize":
                    value = reader.ReadUInt64();
                    break;
                case "float":
                    value = reader.ReadSingle();
                    break;
                case "double":
                    value = reader.ReadDouble();
                    break;
                case "bool":
                    value = reader.ReadBoolean();
                    break;
                case "string" when m_Nodes[i + 1].m_Type == "Array":
                    value = reader.ReadAlignedString();
                    var toSkip = m_Nodes.Subtree(i);
                    i += toSkip.Count - 1;
                    break;
                case "map":
                    {
                        if ((m_Nodes[i + 1].m_MetaFlag & 0x4000) != 0)
                            align = true;
                        var map = m_Nodes.Subtree(i);
                        i += map.Count - 1;
                        var first = map.Subtree(4);
                        var next = 4 + first.Count;
                        var second = map.Subtree(next);
                        var size = reader.ReadInt32();
                        var dic = new List<KeyValuePair<object, object>>(size);
                        for (int j = 0; j < size; j++)
                        {
                            int tmp1 = 0;
                            int tmp2 = 0;
                            dic.Add(new KeyValuePair<object, object>(ReadValue(first, reader, ref tmp1), ReadValue(second, reader, ref tmp2)));
                        }
                        value = dic;
                        break;
                    }
                case "TypelessData":
                    {
                        var size = reader.ReadInt32();
                        var offset = size > 0 ? reader.BaseStream.Position : 0;
                        var dic = new OrderedDictionary
                        {
                            {"Offset", offset},
                            {"Size", size}
                        };
                        value = dic;
                        reader.BaseStream.Position += size;
                        i += 2;
                        break;
                    }
                default:
                    {
                        if (m_Node.m_Type == "string")
                            m_Node.m_Type = "CustomType";
                        if (i < m_Nodes.Count - 1 && m_Nodes[i + 1].m_Type == "Array") //Array
                        {
                            if ((m_Nodes[i + 1].m_MetaFlag & 0x4000) != 0)
                                align = true;
                            var vector = m_Nodes.Subtree(i);
                            i += vector.Count - 1;
                            var size = reader.ReadInt32();
                            var array = new object[size];
                            for (int j = 0; j < size; j++)
                            {
                                int tmp = 3;
                                array[j] = ReadValue(vector, reader, ref tmp);
                            }
                            value = array;
                            break;
                        }
                        else //Class
                        {
                            var @class = m_Nodes.Subtree(i);
                            i += @class.Count - 1;
                            var obj = new OrderedDictionary();
                            for (int j = 1; j < @class.Count; j++)
                            {
                                var classmember = @class[j];
                                var name = classmember.m_Name;
                                obj[name] = ReadValue(@class, reader, ref j);
                            }
                            value = obj;
                            break;
                        }
                    }
            }
            if (align)
                reader.AlignStream();
            return value;
        }

        private static void SkipValue(NodeRange m_Nodes, BinaryReader reader, ref int i)
        {
            var m_Node = m_Nodes[i];
            var varTypeStr = m_Node.m_Type;
            var align = (m_Node.m_MetaFlag & 0x4000) != 0;
            var size = PrimitiveSize(varTypeStr);
            if (size > 0)
            {
                Skip(reader, size);
            }
            else if (varTypeStr == "string" && m_Nodes[i + 1].m_Type == "Array")
            {
                var length = reader.ReadInt32();
                if (length > reader.BaseStream.Length - reader.BaseStream.Position)
                    throw new EndOfStreamException();
                if (length > 0)
                {
                    reader.BaseStream.Position += length;
                    reader.AlignStream();
                }
                var toSkip = m_Nodes.Subtree(i);
                i += toSkip.Count - 1;
            }
            else if (varTypeStr == "map")
            {
                if ((m_Nodes[i + 1].m_MetaFlag & 0x4000) != 0)
                    align = true;
                var map = m_Nodes.Subtree(i);
                i += map.Count - 1;
                var first = map.Subtree(4);
                var next = 4 + first.Count;
                var second = map.Subtree(next);
                var count = reader.ReadInt32();
                for (int j = 0; j < count; j++)
                {
                    int tmp1 = 0;
                    int tmp2 = 0;
                    SkipValue(first, reader, ref tmp1);
                    SkipValue(second, reader, ref tmp2);
                }
            }
            else if (varTypeStr == "TypelessData")
            {
                var count = reader.ReadInt32();
                reader.BaseStream.Position += count;
                i += 2;
            }
            else
            {
                if (m_Node.m_Type == "string")
                    m_Node.m_Type = "CustomType";
                if (i < m_Nodes.Count - 1 && m_Nodes[i + 1].m_Type == "Array")
                {
                    if ((m_Nodes[i + 1].m_MetaFlag & 0x4000) != 0)
                        align = true;
                    var vector = m_Nodes.Subtree(i);
                    i += vector.Count - 1;
                    var count = reader.ReadInt32();
                    if (count > 0)
                    {
                        var element = vector[3];
                        var elementSize = PrimitiveSize(element.m_Type);
                        if (elementSize > 0 && (element.m_MetaFlag & 0x4000) == 0 && vector.Count == 4)
                        {
                            Skip(reader, (long)count * elementSize);
                        }
                        else
                        {
                            for (int j = 0; j < count; j++)
                            {
                                int tmp = 3;
                                SkipValue(vector, reader, ref tmp);
                            }
                        }
                    }
                }
                else
                {
                    var @class = m_Nodes.Subtree(i);
                    i += @class.Count - 1;
                    for (int j = 1; j < @class.Count; j++)
                    {
                        SkipValue(@class, reader, ref j);
                    }
                }
            }
            if (align)
                reader.AlignStream();
        }

        private static int PrimitiveSize(string type) => type switch
        {
            "SInt8" or "UInt8" or "bool" => 1,
            "char" or "short" or "SInt16" or "UInt16" or "unsigned short" => 2,
            "int" or "SInt32" or "UInt32" or "unsigned int" or "Type*" or "float" => 4,
            "long long" or "SInt64" or "UInt64" or "unsigned long long" or "FileSize" or "double" => 8,
            _ => 0
        };

        private static void Skip(BinaryReader reader, long count)
        {
            if (count > reader.BaseStream.Length - reader.BaseStream.Position)
                throw new EndOfStreamException();
            reader.BaseStream.Position += count;
        }

        private readonly struct NodeRange
        {
            private readonly List<TypeTreeNode> nodes;
            private readonly int[] ends;
            private readonly int start;
            public readonly int Count;

            public NodeRange(TypeTree type) : this(type.m_Nodes, type.GetSubtreeEnds(), 0, type.m_Nodes.Count) { }

            private NodeRange(List<TypeTreeNode> nodes, int[] ends, int start, int count)
            {
                this.nodes = nodes;
                this.ends = ends;
                this.start = start;
                Count = count;
            }

            public TypeTreeNode this[int index] => (uint)index < (uint)Count ? nodes[start + index] : throw new ArgumentOutOfRangeException(nameof(index));

            public NodeRange Subtree(int index)
            {
                if ((uint)index >= (uint)Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                var first = start + index;
                var end = Math.Min(ends[first], start + Count);
                return new NodeRange(nodes, ends, first, end - first);
            }
        }
    }
}
