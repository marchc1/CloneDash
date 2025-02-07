namespace Nucleus.Models
{
	public class FCurve<T> : IFCurve
	{
		public List<Keyframe<T>> Keyframes { get; set; } = [];
		private bool valid = false;

		public void Recompute() {
			if (valid == false) {
				Keyframes.Sort((x, y) => x.Time.CompareTo(y.Time));
				valid = true;
			}
		}

		public bool Invalid => !valid;

		public T? DetermineValueAtTime(double time) {
			Recompute();
			int count = Keyframes.Count;
			switch (count) {
				case 0: return default;
				case 1: return Keyframes[0].Value;
				case 2: return Keyframe<T>.DetermineValue(time, Keyframes[0], Keyframes[1]);
				default:
					var firstKeyframe = Keyframes[0];
					if (time <= firstKeyframe.Time) return firstKeyframe.Value;

					for (int i = 1; i < Keyframes.Count; i++) {
						var keyframe = Keyframes[i];
						if (keyframe.Time <= time) {
							return Keyframe<T>.DetermineValue(time, Keyframes[i - 1], keyframe);
						}
					}

					return Keyframes[count - 1].Value;
			}
		}
	}
}
