using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Core
{
	public interface IDataBinder
	{
		public object? GetBackingObject();
		public void SetBackingObject(object? obj);

		public static IDataBinder Create(object data) {
			Type constructType = typeof(DataBinder<>).MakeGenericType(data.GetType());
			IDataBinder x = (IDataBinder)(Activator.CreateInstance(constructType, data) ?? throw new Exception());
			return x;
		}
	}

	public class DataBinder<T> : IDataBinder
	{
		private T? _backing = default;
		public T? Backing {
			get {
				if (WhenAccessed != null) {
					return WhenAccessed.Invoke(_backing);
				}
				return _backing;
			}
			set {
				var block = WhenChanged?.Invoke(_backing, value);
				if (block.HasValue && block.Value == true)
					return;
				_backing = value;
			}
		}

		public delegate T? WhenAccessedDelegate<T>(T? current);
		public delegate bool WhenChangedDelegate<T>(T? oldV, T? newV);
		public event WhenAccessedDelegate<T>? WhenAccessed;
		public event WhenChangedDelegate<T>? WhenChanged;

		public static implicit operator T?(DataBinder<T> binder) => binder.Backing;

		public object? GetBackingObject() => Backing;
		public void SetBackingObject(object? o) => Backing = (T?)o;

		public static DataBinder<T> New(Func<T?, T?> access, Func<T?, T?, bool> change) {
			DataBinder<T?> binder = new();
			binder.WhenAccessed += (c) => access(c);
			binder.WhenChanged += (o, n) => change(o, n);
			return binder;
		}
		public static DataBinder<T?> From<TypeFrom>(DataBinder<TypeFrom?> dataBinder) {
			DataBinder<T?> typeToBinder = new();
			typeToBinder.WhenAccessed += (c) => {
				if (dataBinder.Backing == null)
					return default;
				return (T?)(object?)dataBinder.Backing;
			};
			typeToBinder.WhenChanged += (o, n) => {
				dataBinder.Backing = (TypeFrom?)(object?)n;
				return false;
			};
			return typeToBinder;
		}

		public DataBinder(T? val) {
			_backing = val;
		}
		public void SetNoUpdate(T? val) => _backing = val;
		public DataBinder() {

		}
	}
}