using System.Numerics;

namespace Nucleus.Types;

public class ConstantLengthNumericalQueue<T>(int capacity) where T : IFloatingPoint<T>
{
	private readonly T[] backing = new T[capacity];
	private int length = 0;
	private int pointer = 0;
	private int startat = 0;
	public int Capacity => capacity;
	public int Length => length;

	public int Start => startat;

	public T this[int index] => backing[(index + startat) % capacity];

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

	public T Max() {
		T ret = T.CreateSaturating(-10000);
		for (int i = 0; i < length; i++)
			ret = T.Max(ret, this[i]);
		return ret;
	}

	public T Min() {
		T ret = T.CreateSaturating(10000);
		for (int i = 0; i < length; i++)
			ret = T.Min(ret, this[i]);
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