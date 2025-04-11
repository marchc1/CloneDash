using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace Nucleus.Models
{
	public class FCurve<T> : IFCurve<T>
	{
		private List<Keyframe<T>> keyframes { get; set; } = [];

		public IEnumerable<Keyframe<T>> Keyframes {
			get {
				Recompute();

				foreach (var keyframe in keyframes)
					yield return keyframe;
			}
		}

		[JsonIgnore] private bool valid = false;

		public Keyframe<T> First {
			get {
				Recompute();
				return keyframes.FirstOrDefault();
			}
		}
		public Keyframe<T> Last {
			get {
				Recompute();
				return keyframes.LastOrDefault();
			}
		}

		public void AddKeyframe(Keyframe<T> keyframe) {
			keyframes.RemoveAll(x => keyframe.Time == x.Time);
			keyframes.Add(keyframe);
			valid = false;
		}

		public void RemoveKeyframe(Keyframe<T> keyframe) {
			keyframes.Remove(keyframe);
			valid = false;
		}

		public bool TryFindKeyframe(Predicate<Keyframe<T>> predicate, [NotNullWhen(true)] out Keyframe<T>? keyframe) {
			Recompute();

			keyframe = null;
			foreach (var kf in keyframes) {
				if (predicate(kf)) {
					keyframe = kf;
					break;
				}
			}

			return keyframe.HasValue;
		}

		public bool TryFindKeyframe(double time, [NotNullWhen(true)] out Keyframe<T>? keyframe)
			=> TryFindKeyframe(x => x.Time == time, out keyframe);

		public void Recompute() {
			if (valid == false) {
				keyframes.Sort((x, y) => x.Time.CompareTo(y.Time));
				valid = true;
			}
		}

		[JsonIgnore] public bool Invalid => !valid;

		public T? DetermineValueAtTime(double time, KeyframeInterpolation? interpolationOverride = null) {
			Recompute();

			int count = keyframes.Count;
			switch (count) {
				case 0: return default;
				case 1: return keyframes[0].Value;
				case 2: return Keyframe<T>.DetermineValue(time, keyframes[0], keyframes[1], interpolationOverride);
				default:
					var firstKeyframe = keyframes[0];
					if (time <= firstKeyframe.Time) 
						return firstKeyframe.Value;

					for (int i = 1; i < keyframes.Count; i++) {
						var keyframe = keyframes[i];
						if (time <= keyframe.Time) {
							return Keyframe<T>.DetermineValue(time, keyframes[i - 1], keyframe, interpolationOverride);
						}
					}

					return keyframes[count - 1].Value;
			}
		}
	}
}
