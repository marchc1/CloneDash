using Newtonsoft.Json;

namespace CloneDash.Modding
{
	public class Descriptor_MultiAnimationClass
	{
		[JsonProperty("format")] public string Format;
		[JsonProperty("count")] public int Count;

		public static implicit operator Descriptor_MultiAnimationClass(string s) => new() {
			Format = s,
			Count = 1
		};

		public bool HasAnimations => Count > 0;
		/// <summary>
		/// Expects a start-at-1 index
		/// </summary>
		/// <param name="at"></param>
		/// <returns></returns>
		public string GetAnimation(int at) => string.Format(Format, (at - 1) % Count + 1);
		public string GetAnimation() => string.Format(Format, Random.Shared.Next(0, Count) + 1);
	}
}