namespace Nucleus.UI
{
	public class SafeArray<T> : List<T>
	{
		public SafeArray() : base() { }
		public SafeArray(int count) : base(count) { }
		public SafeArray(IEnumerable<T> source) : base(source) { }
		public new T? this[int index] {
			get {
				if (index < 0)
					return default;
				else if (index >= base.Count)
					return default;
				return base[index];
			}
			set {
				if (index < 0) throw new IndexOutOfRangeException($"index < 0");
				if (index >= base.Count) throw new IndexOutOfRangeException($"index > count[{base.Count}]");
				if (value == null) throw new ArgumentNullException("value");
				base[index] = value;
			}
		}
	}
}
