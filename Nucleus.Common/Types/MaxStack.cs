using System.Collections;

namespace Nucleus.UI;

[Serializable]
public class MaxStack<T>
{
	#region Fields

	private readonly int _limit;
	private readonly LinkedList<T> _list;

	#endregion Fields

	#region Constructors

	public MaxStack(int maxSize) {
		_limit = maxSize;
		_list = new LinkedList<T>();
	}

	#endregion Constructors

	#region Public Stack Implementation

	public int Count {
		get { return _list.Count; }
	}

	public void Clear() {
		_list.Clear();
	}

	public bool Contains(T value) {
		bool result = false;
		if (this.Count > 0) {
			result = _list.Contains(value);
		}
		return result;
	}

	public IEnumerator GetEnumerator() {
		return _list.GetEnumerator();
	}

	/// ///
	public bool IsTop(T value) {
		bool result = false;
		if (this.Count > 0) {
			result = Peek().Equals(value);
		}
		return result;
	}

	public T Peek() {
		if (_list.Count > 0) {
			T value = _list.First.Value;
			return value;
		}
		else {
			throw new InvalidOperationException("The Stack is empty");
		}
	}

	public T Pop() {
		if (_list.Count > 0) {
			T value = _list.First.Value;
			_list.RemoveFirst();
			return value;
		}
		else {
			throw new InvalidOperationException("The Stack is empty");
		}
	}

	public void Push(T value) {
		if (_list.Count == _limit) {
			_list.RemoveLast();
		}
		_list.AddFirst(value);
	}

	///
	/// Checks if the top object on the stack matches the value passed in
	///

	#endregion Public Stack Implementation
}