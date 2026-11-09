using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace Nucleus.Models
{
	public class FCurve<T> : IFCurve
	{
		public List<Keyframe<T>> Keyframes { get; set; } = [];
		public int Count => Keyframes.Count;

		[JsonIgnore] private int nextId;
		[JsonIgnore] private Dictionary<int, KeyframeRef<T>>? handles;

		private int NewId() {
			if (nextId == 0)
				foreach (var keyframe in Keyframes)
					nextId = Math.Max(nextId, keyframe.Id);
			return ++nextId;
		}

		private static readonly HashSet<int> seenIds = [];

		private void EnsureIds() {
			var keys = CollectionsMarshal.AsSpan(Keyframes);
			lock (seenIds) {
				seenIds.Clear();
				for (int i = 0; i < keys.Length; i++)
					if (keys[i].Id == 0 || !seenIds.Add(keys[i].Id))
						keys[i].Id = NewId();
			}
		}

		private KeyframeRef<T> Handle(int index) {
			ref Keyframe<T> keyframe = ref CollectionsMarshal.AsSpan(Keyframes)[index];
			handles ??= [];
			if (!handles.TryGetValue(keyframe.Id, out var handle))
				handles[keyframe.Id] = handle = new KeyframeRef<T>(this, in keyframe, index);
			return handle;
		}

		private void Detach(in Keyframe<T> removed) {
			if (handles != null && handles.TryGetValue(removed.Id, out var handle))
				handle.Detached = removed;
		}

		internal ref Keyframe<T> FindRef(KeyframeRef<T> handle) {
			var keys = CollectionsMarshal.AsSpan(Keyframes);
			int index = handle.IndexHint;
			if ((uint)index >= (uint)keys.Length || keys[index].Id != handle.Id) {
				index = -1;
				for (int i = 0; i < keys.Length; i++)
					if (keys[i].Id == handle.Id) {
						index = i;
						break;
					}
			}
			if (index < 0)
				return ref handle.Detached;

			handle.IndexHint = index;
			handle.Detached = keys[index];
			return ref keys[index];
		}

		public int IndexOf(KeyframeRef<T> handle) {
			if (handle.Curve != this)
				return -1;
			var keys = CollectionsMarshal.AsSpan(Keyframes);
			for (int i = 0; i < keys.Length; i++)
				if (keys[i].Id == handle.Id)
					return i;
			return -1;
		}

		public IEnumerable<KeyframeRef<T>> GetKeyframes() {
			Recompute();
			EnsureIds();

			for (int i = 0; i < Keyframes.Count; i++)
				yield return Handle(i);
		}

		public void ScaleTime(double scale) {
			var keys = CollectionsMarshal.AsSpan(Keyframes);
			for (int i = 0; i < keys.Length; i++)
				keys[i].Time = keys[i].Time * scale;
			valid = false;
		}

		public FCurve<T> Copy(double scale = 1) {
			FCurve<T> copy = new FCurve<T>();
			Recompute();
			foreach (var kf in Keyframes) {
				copy.AddKeyframe(kf.Copy(scale));
			}
			copy.valid = false;
			return copy;
		}

		[JsonIgnore] private bool valid = false;

		[JsonIgnore]
		public Keyframe<T>? First {
			get {
				Recompute();
				return Keyframes.Count == 0 ? null : Keyframes[0];
			}
		}

		[JsonIgnore]
		public Keyframe<T>? Last {
			get {
				Recompute();
				return Keyframes.Count == 0 ? null : Keyframes[^1];
			}
		}

		[JsonIgnore]
		public T? LastValue {
			get {
				Recompute();
				return Keyframes[^1].Value;
			}
		}

		public T? LastValueOr(T? fallback) {
			Recompute();
			return Keyframes.Count == 0 ? fallback : Keyframes[^1].Value;
		}

		public void SetKeyframeTime(IKeyframe keyframe, double time) {
			keyframe.SetTime(time);
			valid = false;
		}

		public IKeyframe KeyAt(int index) {
			Recompute();
			return RefAt(index);
		}

		public IEnumerable<KeyframeRef<T>> Refs() {
			for (int i = 0; i < Keyframes.Count; i++)
				yield return RefAt(i);
		}

		public KeyframeRef<T> RefAt(int index) {
			if (Keyframes[index].Id == 0)
				EnsureIds();
			return Handle(index);
		}

		public void RemoveKeyframe(IKeyframe keyframe) {
			if (keyframe is KeyframeRef<T> typedKeyframe)
				RemoveKeyframe(typedKeyframe);
		}

		public IKeyframe DuplicateKeyframe(IKeyframe keyframe, double time) {
			var source = (KeyframeRef<T>)keyframe;
			var copy = source.Copy();
			double delta = time - source.Time;
			copy.Time = time;
			if (copy.LeftHandle is KeyframeHandle<T> left)
				copy.LeftHandle = left with { Time = left.Time + delta };
			if (copy.RightHandle is KeyframeHandle<T> right)
				copy.RightHandle = right with { Time = right.Time + delta };
			EnsureIds();
			copy.Id = NewId();
			Keyframes.Add(copy);
			valid = false;
			return Handle(Keyframes.Count - 1);
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

		public void AddKeyframe(Keyframe<T> keyframe) {
			for (int i = Keyframes.Count - 1; i >= 0; i--) {
				if (Math.Abs(keyframe.Time - Keyframes[i].Time) <= KEYFRAME_TIME_EPSILON) {
					Detach(Keyframes[i]);
					Keyframes.RemoveAt(i);
				}
			}
			keyframe.Id = NewId();
			Keyframes.Add(keyframe);
			valid = false;
		}

		public void RestoreKeyframe(KeyframeRef<T> keyframe) {
			var data = keyframe.Data;
			for (int i = Keyframes.Count - 1; i >= 0; i--) {
				if (Math.Abs(data.Time - Keyframes[i].Time) <= KEYFRAME_TIME_EPSILON) {
					Detach(Keyframes[i]);
					Keyframes.RemoveAt(i);
				}
			}
			EnsureIds();
			if (keyframe.Curve != this || IndexOf(keyframe) >= 0)
				data.Id = NewId();
			else
				nextId = Math.Max(NewId(), data.Id);
			Keyframes.Add(data);
			valid = false;
		}

		public bool Contains(KeyframeRef<T> keyframe) => IndexOf(keyframe) >= 0;

		public KeyframeRef<T> AddKeyframeRef(Keyframe<T> keyframe) {
			EnsureIds();
			AddKeyframe(keyframe);
			return Handle(Keyframes.Count - 1);
		}

		public void RemoveKeyframe(KeyframeRef<T> keyframe) {
			int index = IndexOf(keyframe);
			if (index >= 0) {
				Detach(Keyframes[index]);
				Keyframes.RemoveAt(index);
			}
			valid = false;
		}

		public bool TryFindKeyframe(Predicate<Keyframe<T>> predicate, [NotNullWhen(true)] out KeyframeRef<T>? keyframe) {
			Recompute();

			keyframe = null;
			for (int i = 0; i < Keyframes.Count; i++) {
				if (predicate(Keyframes[i])) {
					EnsureIds();
					keyframe = Handle(i);
					break;
				}
			}

			return keyframe != null;
		}

		public const double KEYFRAME_TIME_EPSILON = 0.00001;

		public bool TryFindKeyframe(double time, [NotNullWhen(true)] out KeyframeRef<T>? keyframe) {
			int index = FindKeyframeIndex(time);
			if (index < 0) {
				keyframe = null;
				return false;
			}

			EnsureIds();
			keyframe = Handle(index);
			return true;
		}

		public int FindKeyframeIndex(double time) {
			Recompute();

			int low = 0;
			int high = Keyframes.Count - 1;
			while (low <= high) {
				int mid = (low + high) / 2;
				double candidate = Keyframes[mid].Time;
				if (Math.Abs(candidate - time) <= KEYFRAME_TIME_EPSILON)
					return mid;
				if (candidate < time)
					low = mid + 1;
				else
					high = mid - 1;
			}

			return -1;
		}

		public int BinarySearchKeyframe(double time) {
			var keys = CollectionsMarshal.AsSpan(Keyframes);
			int len = keys.Length;
			if (len == 0) return -1;

			if (time <= keys[0].Time) return 0; // pre-first
			if (time >= keys[len - 1].Time) return len - 2; // post-last

			int start = 0;
			int end = len - 1;

			while (end - start > 1) {
				int middle = (start + end) / 2;
				double kfTime = keys[middle].Time;
				if (kfTime == time) // exact
					return middle;
				else if (kfTime > time) // collapse backwards
					end = middle;
				else // collapse forwards
					start = middle;
			}

			return start;
		}

		public void Recompute() {
			if (valid == false) {
				if (!StrictlyIncreasing())
					Keyframes.Sort(static (x, y) => x.Time.CompareTo(y.Time));
				valid = true;
			}
		}

		private bool StrictlyIncreasing() {
			var keys = CollectionsMarshal.AsSpan(Keyframes);
			for (int i = 1; i < keys.Length; i++)
				if (!(keys[i - 1].Time < keys[i].Time))
					return false;
			return true;
		}

		[JsonIgnore] public bool Invalid => !valid;

		[OnDeserialized]
		internal void OnDeserialized(StreamingContext context) => Recompute();

		public object SaveKeyframes() {
			EnsureIds();
			return Keyframes.ToArray();
		}

		public void LoadKeyframes(object saved) {
			var keyframes = (Keyframe<T>[])saved;
			foreach (var keyframe in Keyframes) {
				if (Array.FindIndex(keyframes, x => x.Id == keyframe.Id) < 0)
					Detach(keyframe);
			}
			Keyframes.Clear();
			Keyframes.AddRange(keyframes);
			valid = false;
		}

		public bool MatchesKeyframes(object saved) {
			EnsureIds();
			var items = (Keyframe<T>[])saved;
			if (items.Length != Keyframes.Count) return false;
			for (int i = 0; i < items.Length; i++) {
				var values = items[i];
				var now = Keyframes[i];
				if (now.Id != values.Id || now.Time != values.Time || !EqualityComparer<T>.Default.Equals(now.Value, values.Value)
					|| !Nullable.Equals(now.LeftHandle, values.LeftHandle) || !Nullable.Equals(now.RightHandle, values.RightHandle)
					|| now.Interpolation != values.Interpolation || now.Easing != values.Easing)
					return false;
			}
			return true;
		}

		public void UpdateAutoHandles(int index) {
			if (typeof(T) != typeof(float) || index < 0 || index >= Keyframes.Count)
				return;

			var keys = CollectionsMarshal.AsSpan(Keyframes);
			ref Keyframe<float> keyframe = ref Unsafe.As<Keyframe<T>, Keyframe<float>>(ref keys[index]);
			bool hasPrev = index > 0;
			bool hasNext = index < keys.Length - 1;
			ref Keyframe<float> prev = ref Unsafe.As<Keyframe<T>, Keyframe<float>>(ref keys[hasPrev ? index - 1 : index]);
			ref Keyframe<float> next = ref Unsafe.As<Keyframe<T>, Keyframe<float>>(ref keys[hasNext ? index + 1 : index]);

			keyframe.LeftHandle = AutoHandle(in keyframe, keyframe.LeftHandle, hasPrev, in prev, hasNext, in next, true);
			keyframe.RightHandle = AutoHandle(in keyframe, keyframe.RightHandle, hasPrev, in prev, hasNext, in next, false);
		}

		private static KeyframeHandle<float>? AutoHandle(in Keyframe<float> keyframe, KeyframeHandle<float>? handle, bool hasPrev, in Keyframe<float> prev, bool hasNext, in Keyframe<float> next, bool left) {
			if (handle == null) return null;
			var type = handle.Value.HandleType;
			if (type is not (KeyframeHandleType.Automatic or KeyframeHandleType.AutoClamped or KeyframeHandleType.Vector))
				return handle;

			bool hasSide = left ? hasPrev : hasNext;
			bool hasOther = left ? hasNext : hasPrev;
			ref readonly Keyframe<float> side = ref left ? ref prev : ref next;
			ref readonly Keyframe<float> other = ref left ? ref next : ref prev;
			double length;
			if (hasSide)
				length = Math.Abs(side.Time - keyframe.Time) / 3;
			else if (hasOther)
				length = Math.Abs(other.Time - keyframe.Time) / 3;
			else
				length = 0;
			double direction = left ? -1 : 1;

			if (type == KeyframeHandleType.Vector) {
				float vectorValue = !hasSide ? keyframe.Value : keyframe.Value + (side.Value - keyframe.Value) / 3;
				return new() { HandleType = type, Time = keyframe.Time + direction * length, Value = vectorValue };
			}

			double slope = 0;
			if (hasPrev && hasNext) {
				slope = (next.Value - prev.Value) / (next.Time - prev.Time);
				// flat on peaks and dips
				if (type == KeyframeHandleType.AutoClamped && (keyframe.Value - prev.Value) * (next.Value - keyframe.Value) <= 0)
					slope = 0;
			}

			float value = (float)(keyframe.Value + direction * slope * length);
			if (type == KeyframeHandleType.AutoClamped && hasSide)
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
					if (time <= Keyframes[0].Time)
						return 0;

					var start = BinarySearchKeyframe(time);
					return start;
			}
		}

		public T? DetermineValueAtTime(double time, KeyframeInterpolation? interpolationOverride = null) {
			Recompute();

			var keys = CollectionsMarshal.AsSpan(Keyframes);
			switch (keys.Length) {
				case 0: return default;
				case 1: return keys[0].Value;
				case 2:
					UpdateAutoHandles(0);
					UpdateAutoHandles(1);
					return Keyframe<T>.DetermineValue(time, in keys[0], in keys[1], interpolationOverride);
				default:
					if (time <= keys[0].Time)
						return keys[0].Value;

					var start = BinarySearchKeyframe(time);
					UpdateAutoHandles(start);
					UpdateAutoHandles(start + 1);
					return Keyframe<T>.DetermineValue(time, in keys[start], in keys[start + 1], interpolationOverride);
			}
		}

		public T? DetermineValueAtTime(double time, int index, KeyframeInterpolation? interpolationOverride = null) {
			Recompute();

			var keys = CollectionsMarshal.AsSpan(Keyframes);
			switch (keys.Length) {
				case 0: return default;
				case 1: return keys[index].Value;
				default:
					if (keys.Length > 2 && time <= keys[0].Time)
						return keys[0].Value;

					UpdateAutoHandles(index);
					UpdateAutoHandles(index + 1);
					return Keyframe<T>.DetermineValue(time, in keys[index], in keys[index + 1], interpolationOverride);
			}
		}
	}
}
