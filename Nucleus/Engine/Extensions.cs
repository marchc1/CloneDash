using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus
{
	public static class Extensions
	{
		public static unsafe Image ToImage(this byte[] data, int width, int height, PixelFormat format, int mipmaps) {
			var ptr = Raylib.New<byte>(data.Length);
			for (int i = 0; i < data.Length; i++) {
				ptr[i] = data[i];
			}
			var img = new Image() {
				Data = ptr,
				Format = format,
				Width = width,
				Height = height,
				Mipmaps = mipmaps
			};
			return img;
		}

		/// <summary>
		/// A convenience method to iterate over an <see cref="IList{T}"/> and return the first item that matches <see cref="Predicate{T}"/>'s index. Returns -1 when not found.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="cond"></param>
		/// <returns>The index of the found item, or -1 if not found in the <see cref="IList{T}"/></returns>
		public static int FirstOrDefaultIndex<T>(this IList<T> list, Predicate<T> cond) {
			for (int i = 0, len = list.Count; i < len; i++)
				if (cond(list[i])) 
					return i;

			return -1;
		}

		public static bool InRange(this int n, int min, int max) => n >= min && n <= max;
		public static bool InRange(this float n, float min, float max) => n >= min && n <= max;
		public static bool InRange(this double n, double min, double max) => n >= min && n <= max;
		public static bool InRange(this uint n, uint min, uint max) => n >= min && n <= max;
		public static bool InRange(this long n, long min, long max) => n >= min && n <= max;

		public static T Random<T>(this IList<T> list) {
			return list[System.Random.Shared.Next(0, list.Count)];
		}

		public static List<T> ReadList<T>(this BinaryReader reader, Func<BinaryReader, T> deserializer) {
			int size = reader.Read7BitEncodedInt();
			List<T> array = new(size);
			for (int i = 0; i < size; i++) {
				array[i] = deserializer(reader);
			}
			return array;
		}

		public static void WriteList<T>(this BinaryWriter writer, IList<T> items, Action<BinaryWriter, T> serializer) {
			int c = items.Count;
			writer.Write7BitEncodedInt(c);
			for (int i = 0; i < c; i++) {
				serializer(writer, items[i]);
			}
		}

		public static Dictionary<K, V> ReadDictionary<K, V>(this BinaryReader reader, Func<BinaryReader, K> keyDeserializer, Func<BinaryReader, V> valueDeserializer) {
			int size = reader.Read7BitEncodedInt();
			Dictionary<K, V> dict = new(size);
			for (int i = 0; i < size; i++) 
				dict[keyDeserializer(reader)] = valueDeserializer(reader);
			return dict;
		}

		public static void WriteDictionary<K, V>(this BinaryWriter writer, IDictionary<K, V> items, Action<BinaryWriter, K> keySerializer, Action<BinaryWriter, V> valueSerializer) {
			int c = items.Count;
			writer.Write7BitEncodedInt(c);
			foreach (KeyValuePair<K, V> kvp in items) {
				keySerializer(writer, kvp.Key);
				valueSerializer(writer, kvp.Value);
			}
		}

		public static string? ReadNullableString(this BinaryReader reader) {
			if (!reader.ReadBoolean()) return null;
			return reader.ReadString();
		}

		public static void WriteNullableString(this BinaryWriter writer, string? str) {
			if (str == null) {
				writer.Write(false);
				return;
			}

			writer.Write(true);
			writer.Write(str);
		}

		public static T ReadIndexThenFetch<T>(this BinaryReader reader, IList<T> array) {
			return array[reader.ReadInt32()];
		}

		public static void WriteIndexOf<T>(this BinaryWriter writer, IList<T> array, T item) {
			writer.Write(array.IndexOf(item));
		}
	}
}