using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Types
{

	public class ConstantLengthNumericalQueue<T>(int capacity) where T : IFloatingPoint<T>
	{
		public int Capacity => capacity;
		int pointer = 0;
		int length = 0;
		T[] backing = new T[capacity];
		public void Add(T item) {
			if (length < capacity)
				length++;
			else {
				startat++;
				if (startat >= capacity)
					startat = 0;
			}

			if (pointer >= capacity) pointer = 0;
			backing[pointer] = item;
			pointer++;
		}

		public int Length => length;

		public T this[int index] => backing[(index + startat) % capacity];

		int startat = 0;
		public int Start => startat;

		public T Min() {
			T ret = T.CreateSaturating(10000);
			for (int i = 0; i < length; i++)
				ret = T.Min(ret, this[i]);
			return ret;
		}

		public T Max() {
			T ret = T.CreateSaturating(-10000);
			for (int i = 0; i < length; i++)
				ret = T.Max(ret, this[i]);
			return ret;
		}

		public T[] ToArray() {
			T[] ret = new T[capacity];
			for (int i = 0; i < length; i++) {
				ret[i] = this[i];
			}

			return ret;
		}
	}
}
