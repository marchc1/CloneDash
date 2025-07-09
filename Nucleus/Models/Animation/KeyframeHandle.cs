namespace Nucleus.Models
{
	public struct KeyframeHandle<T>
	{
		public KeyframeHandleType HandleType;
		public double Time;
		public T Value;
	}
}
