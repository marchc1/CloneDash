using Newtonsoft.Json;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Nucleus.Models
{
	public class FCurve<T> : IFCurve
	{
		public List<Keyframe<T>> Keyframes { get; set; } = [];
		public int Count => Keyframes.Count;

		public IEnumerable<Keyframe<T>> GetKeyframes() {
			Recompute();

			foreach (var keyframe in Keyframes)
				yield return keyframe;
		}

		public void ScaleTime(double scale) {
			foreach (var kf in Keyframes) {
				kf.SetTime(kf.GetTime() * scale);
			}
			valid = false;
		}

		public FCurve<T> Copy(double scale = 1) {
			FCurve<T> copy = new FCurve<T>();
			foreach (var kf in GetKeyframes()) {
				copy.AddKeyframe(kf.Copy(scale));
			}
			copy.valid = false;
			return copy;
		}

		[JsonIgnore] private bool valid = false;

		public Keyframe<T>? First {
			get {
				Recompute();
				return __first;
			}
		}

		public Keyframe<T>? Last {
			get {
				Recompute();
				return __last;
			}
		}

		public void SetKeyframeTime(IKeyframe keyframe, double time) {
			Debug.Assert(Keyframes.Contains(keyframe));
			keyframe.SetTime(time);
			valid = false;
		}

		public IKeyframe KeyAt(int index) => this[index];

		public void RemoveKeyframe(IKeyframe keyframe) {
			if (keyframe is Keyframe<T> typedKeyframe)
				RemoveKeyframe(typedKeyframe);
		}

		public IKeyframe DuplicateKeyframe(IKeyframe keyframe, double time) {
			var source = (Keyframe<T>)keyframe;
			var copy = source.Copy();
			double delta = time - source.Time;
			copy.Time = time;
			if (copy.LeftHandle is KeyframeHandle<T> left)
				copy.LeftHandle = left with { Time = left.Time + delta };
			if (copy.RightHandle is KeyframeHandle<T> right)
				copy.RightHandle = right with { Time = right.Time + delta };
			Keyframes.Add(copy);
			valid = false;
			return copy;
		}

		public Keyframe<T> this[int index] {
			get {
				Recompute();
				return Keyframes[index];
			}
			set {
				valid = false;
				Keyframes[index] = value;
			}
		}

		public Keyframe<T> AddKeyframe(Keyframe<T> keyframe) {
			Keyframes.RemoveAll(x => Math.Abs(keyframe.Time - x.Time) <= KEYFRAME_TIME_EPSILON);
			Keyframes.Add(keyframe);
			valid = false;
			return keyframe;
		}

		public void RemoveKeyframe(Keyframe<T> keyframe) {
			Keyframes.Remove(keyframe);
			valid = false;
		}

		public bool TryFindKeyframe(Predicate<Keyframe<T>> predicate, [NotNullWhen(true)] out Keyframe<T>? keyframe) {
			Recompute();

			keyframe = null;
			foreach (var kf in Keyframes) {
				if (predicate(kf)) {
					keyframe = kf;
					break;
				}
			}

			return keyframe != null;
		}

		public const double KEYFRAME_TIME_EPSILON = 0.00001;

		public bool TryFindKeyframe(double time, [NotNullWhen(true)] out Keyframe<T>? keyframe) {
			Recompute();

			int low = 0;
			int high = Keyframes.Count - 1;
			while (low <= high) {
				int mid = (low + high) / 2;
				var candidate = Keyframes[mid];
				if (Math.Abs(candidate.Time - time) <= KEYFRAME_TIME_EPSILON) {
					keyframe = candidate;
					return true;
				}
				if (candidate.Time < time)
					low = mid + 1;
				else
					high = mid - 1;
			}

			keyframe = null;
			return false;
		}

		public int BinarySearchKeyframe(double time) {
			int len = Keyframes.Count;
			if (len == 0) return -1;

#nullable disable
			if (time <= Keyframes[0].Time) return 0; // pre-first
			if (time >= Keyframes[len - 1].Time) return len - 2; // post-last
#nullable enable

			int start = 0;
			int end = len - 1;

			while (end - start > 1) {
				int middle = (start + end) / 2;
				var kf = Keyframes[middle];
				if (kf.Time == time) // exact
					return middle;
				else if (kf.Time > time) // collapse backwards
					end = middle;
				else // collapse forwards
					start = middle;
			}

			return start;
		}

		private Keyframe<T>? __first;
		private Keyframe<T>? __last;

		public void Recompute() {
			if (valid == false) {
				Keyframes.Sort(static (x, y) => x.Time.CompareTo(y.Time));
				__first = Keyframes.Count == 0 ? null : Keyframes[0];
				__last = Keyframes.Count == 0 ? null : Keyframes[^1];
				valid = true;
			}
		}

		[JsonIgnore] public bool Invalid => !valid;

		private record SavedKeyframe(Keyframe<T> Keyframe, Keyframe<T> Values);

		public object SaveKeyframes() {
			var saved = new SavedKeyframe[Keyframes.Count];
			for (int i = 0; i < Keyframes.Count; i++)
				saved[i] = new SavedKeyframe(Keyframes[i], Keyframes[i].Copy());
			return saved;
		}

		public void LoadKeyframes(object saved) {
			Keyframes.Clear();
			foreach (var item in (SavedKeyframe[])saved) {
				var keyframe = item.Keyframe;
				keyframe.Time = item.Values.Time;
				keyframe.Value = item.Values.Value;
				keyframe.LeftHandle = item.Values.LeftHandle;
				keyframe.RightHandle = item.Values.RightHandle;
				keyframe.Interpolation = item.Values.Interpolation;
				keyframe.Easing = item.Values.Easing;
				Keyframes.Add(keyframe);
			}
			valid = false;
		}

		public bool MatchesKeyframes(object saved) {
			var items = (SavedKeyframe[])saved;
			if (items.Length != Keyframes.Count) return false;
			for (int i = 0; i < items.Length; i++) {
				var keyframe = items[i].Keyframe;
				var values = items[i].Values;
				var now = Keyframes[i];
				if (now != keyframe || now.Time != values.Time || !EqualityComparer<T>.Default.Equals(now.Value, values.Value)
					|| !Nullable.Equals(now.LeftHandle, values.LeftHandle) || !Nullable.Equals(now.RightHandle, values.RightHandle)
					|| now.Interpolation != values.Interpolation || now.Easing != values.Easing)
					return false;
			}
			return true;
		}

		public void UpdateAutoHandles(int index) {
			if (typeof(T) != typeof(float) || index < 0 || index >= Keyframes.Count)
				return;

			var keyframe = (Keyframe<float>)(object)Keyframes[index];
			var prev = index > 0 ? (Keyframe<float>)(object)Keyframes[index - 1] : null;
			var next = index < Keyframes.Count - 1 ? (Keyframe<float>)(object)Keyframes[index + 1] : null;

			keyframe.LeftHandle = AutoHandle(keyframe, keyframe.LeftHandle, prev, next, true);
			keyframe.RightHandle = AutoHandle(keyframe, keyframe.RightHandle, prev, next, false);
		}

		private static KeyframeHandle<float>? AutoHandle(Keyframe<float> keyframe, KeyframeHandle<float>? handle, Keyframe<float>? prev, Keyframe<float>? next, bool left) {
			if (handle == null) return null;
			var type = handle.Value.HandleType;
			if (type is not (KeyframeHandleType.Automatic or KeyframeHandleType.AutoClamped or KeyframeHandleType.Vector))
				return handle;

			var side = left ? prev : next;
			var other = left ? next : prev;
			double length;
			if (side != null)
				length = Math.Abs(side.Time - keyframe.Time) / 3;
			else if (other != null)
				length = Math.Abs(other.Time - keyframe.Time) / 3;
			else
				length = 0;
			double direction = left ? -1 : 1;

			if (type == KeyframeHandleType.Vector) {
				float vectorValue = side == null ? keyframe.Value : keyframe.Value + (side.Value - keyframe.Value) / 3;
				return new() { HandleType = type, Time = keyframe.Time + direction * length, Value = vectorValue };
			}

			double slope = 0;
			if (prev != null && next != null) {
				slope = (next.Value - prev.Value) / (next.Time - prev.Time);
				// flat on peaks and dips
				if (type == KeyframeHandleType.AutoClamped && (keyframe.Value - prev.Value) * (next.Value - keyframe.Value) <= 0)
					slope = 0;
			}

			float value = (float)(keyframe.Value + direction * slope * length);
			if (type == KeyframeHandleType.AutoClamped && side != null)
				value = Math.Clamp(value, Math.Min(keyframe.Value, side.Value), Math.Max(keyframe.Value, side.Value));

			return new() { HandleType = type, Time = keyframe.Time + direction * length, Value = value };

		}

		public int DetermineIndexAtTime(double time, KeyframeInterpolation? interpolationOverride = null) {
			Recompute();

			int count = Keyframes.Count;
			switch (count) {
				case 0: return -1;
				case 1: return 0;
				case 2: return 0;
				default:
					var firstKeyframe = Keyframes[0];
					if (time <= firstKeyframe.Time)
						return 0;

					var start = BinarySearchKeyframe(time);
					return start;
			}
		}

		public T? DetermineValueAtTime(double time, KeyframeInterpolation? interpolationOverride = null) {
			Recompute();

			int count = Keyframes.Count;
			switch (count) {
				case 0: return default;
				case 1: return Keyframes[0].Value;
				case 2:
					UpdateAutoHandles(0);
					UpdateAutoHandles(1);
					return Keyframe<T>.DetermineValue(time, Keyframes[0], Keyframes[1], interpolationOverride);
				default:
					var firstKeyframe = Keyframes[0];
					if (time <= firstKeyframe.Time)
						return firstKeyframe.Value;

					var start = BinarySearchKeyframe(time);
					UpdateAutoHandles(start);
					UpdateAutoHandles(start + 1);
					return Keyframe<T>.DetermineValue(time, Keyframes[start], Keyframes[start + 1], interpolationOverride);
			}
		}

		public T? DetermineValueAtTime(double time, int index, KeyframeInterpolation? interpolationOverride = null) {
			Recompute();

			int count = Keyframes.Count;
			switch (count) {
				case 0: return default;
				case 1: return Keyframes[index].Value;
				default:
					var firstKeyframe = Keyframes[0];
					if (count > 2 && time <= firstKeyframe.Time)
						return firstKeyframe.Value;

					UpdateAutoHandles(index);
					UpdateAutoHandles(index + 1);
					return Keyframe<T>.DetermineValue(time, Keyframes[index], Keyframes[index + 1], interpolationOverride);
			}
		}
	}
}