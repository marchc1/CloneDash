using Nucleus.Common.Types;
using Nucleus.Extensions;
using Nucleus.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Core
{
	public class NProfilable
	{
		private static Dictionary<UtlSymbol, NProfilable> Name2ProfilableRecord = [];

		public readonly UtlSymbol Name;
		public readonly Color Color;
		public NProfilable(ReadOnlySpan<char> name, Color? color = null) {
			Name = name;
			Color = color ?? Color.White;
			Name2ProfilableRecord[Name] = this;
		}

		public static NProfilable UI_LAYOUT { get; } = Make("UserInterfaceLayout", new(235, 110, 50, 255));
		public static NProfilable UI_RENDER { get; } = Make("UserInterfaceRendering", new(50, 110, 235, 255));

		public static NProfilable? FromString(ReadOnlySpan<char> str) => Name2ProfilableRecord.TryGetValue(str, out var ret) ? ret : null;
		public static NProfilable Make(ReadOnlySpan<char> str, Color? color = null) {
			if (Name2ProfilableRecord.TryGetValue(str, out var ret))
				return ret;

			return new(str, color);
		}

		public void Start() => NProfiler.Start(this);
		public void End() => NProfiler.End(this);
	}
	public struct NProfileResult
	{
		public string Name;
		public Color Color;
		public TimeSpan Elapsed;
	}
	public static class NProfiler
	{
		private static Dictionary<NProfilable, Stopwatch> timers = [];

		[MemberNotNull(nameof(results))]
		public static void PotentiallyRebuild() {
			if (timers.Count != (results?.Length ?? -10)) {
				results = new NProfileResult[timers.Count];
			}
		}

		public static void Reset() {
			if (true) return; // Stubbing for now
			PotentiallyRebuild();

			int i = 0;

			foreach (var timer in timers) {
				results[i] = new NProfileResult() {
					Name = timer.Key?.Name.String(),
					Color = timer.Key?.Color ?? Color.White,
					Elapsed = timer.Value.Elapsed
				};

				i += 1;
			}

			foreach (var kvp in timers)
				kvp.Value.Reset();
		}
		public static Stopwatch Get(NProfilable profilable) {
			if (timers.TryGetValue(profilable, out var stopwatch))
				return stopwatch;
			stopwatch = new Stopwatch();
			stopwatch.Reset();
			timers[profilable] = stopwatch;
			return stopwatch;
		}
		public static void Start(NProfilable profilable) {
			if (true) return; // Stubbing for now
			Stopwatch s = Get(profilable);
			s.Start();
		}
		private static float Str2Flt(ReadOnlySpan<char> s) {
			return MathF.Abs((s.Hash() / 329.248f) % 360);
		}
		public static void Start(ReadOnlySpan<char> name) {
			if (true) return; // Stubbing for now
			NProfilable? profilable = NProfilable.FromString(name);
			if (profilable == null) {
				Vector3 retC = new(Str2Flt(name) % 360, 0.86f, 1);
				profilable = NProfilable.Make(name, retC.HSVfToRGBub());
			}
			Start(profilable);
		}
		public static void End(NProfilable profilable) {
			if (true) return; // Stubbing for now

			Stopwatch s = Get(profilable);
			s.Stop();
		}
		public static void End(string name) {
			if (true) return; // Stubbing for now

			NProfilable? profilable = NProfilable.FromString(name);
			if (profilable == null) {
				Vector3 retC = new(Str2Flt(name) % 360, 0.86f, 1);
				profilable = NProfilable.Make(name, retC.HSVfToRGBub());
			}
			End(profilable);
		}
		public static TimeSpan Elapsed(NProfilable profilable) {
			Stopwatch s = Get(profilable);
			return s.Elapsed;
		}
		private static NProfileResult[]? results;
		public static NProfileResult[] Results() {
			return results ?? [];
		}
	}
}