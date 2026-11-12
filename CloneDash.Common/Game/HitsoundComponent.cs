using Nucleus.Types;
using System.Runtime.CompilerServices;

namespace CloneDash.Common.Game;

public class HitsoundComponent(Func<bool> isSeeking) : IRhythmGameThinker
{
	readonly Dictionary<string, HitsoundCue> byName = [];
	readonly List<HitsoundCue> cues = [];
	readonly List<IHitsoundOutput> outputs = [];

	int[] enumOffsets = [];
	// This helps with doubles/sustains played in the same frame...
	// probably a better way to handle it, but this works
	bool[] playedThisFrame = [];

	public RhythmGameThinkPhase Phase => RhythmGameThinkPhase.Input;

	public void AddOutput(IHitsoundOutput output) {
		outputs.Add(output);
	}

	public HitsoundCue Find(string name) => byName[name];

	public void Play<TEnum>(TEnum sound, int hits = 0, HitsoundOptions options = HitsoundOptions.None) where TEnum : struct, Enum {
		Play(cues[enumOffsets[HitsoundEnumType<TEnum>.Value] + ToInt(sound)], hits, options);
	}

	public void Play(HitsoundCue cue, int hits = 0, HitsoundOptions options = HitsoundOptions.None) {
		if ((options & HitsoundOptions.PlayWhileSeeking) == 0 && isSeeking())
			return;

		if ((options & HitsoundOptions.AllowRepeat) == 0) {
			if (playedThisFrame[cue.Index])
				return;
			playedThisFrame[cue.Index] = true;
		}

		foreach (var output in outputs)
			output.Play(cue, hits);
	}

	public void RegisterEnum<TEnum>(string prefix) where TEnum : struct, Enum {
		if (Unsafe.SizeOf<TEnum>() > sizeof(int))
			throw new NotSupportedException($"{typeof(TEnum).Name} is larger than an int.");

		int type = HitsoundEnumType<TEnum>.Value;
		if (type >= enumOffsets.Length)
			Array.Resize(ref enumOffsets, type + 1);
		enumOffsets[type] = cues.Count;

		int count = 0;
		foreach (var value in Enum.GetValues<TEnum>())
			count = Math.Max(count, ToInt(value) + 1);

		for (int i = 0; i < count; i++) {
			int raw = i;
			TEnum value = Unsafe.As<int, TEnum>(ref raw);
			var cue = new HitsoundCue(cues.Count, $"{prefix}/{Enum.GetName(value) ?? i.ToString()}", type, i);
			cues.Add(cue);
			byName[cue.Name] = cue;
		}

		Array.Resize(ref playedThisFrame, cues.Count);
	}

	public void ResetFrame() {
		for (int i = 0; i < playedThisFrame.Length; i++)
			playedThisFrame[i] = false;
	}

	public void Think(FrameState frameState) => ResetFrame();

	static int ToInt<TEnum>(TEnum value) where TEnum : struct, Enum => Unsafe.SizeOf<TEnum>() switch {
		1 => Unsafe.As<TEnum, byte>(ref value),
		2 => Unsafe.As<TEnum, ushort>(ref value),
		_ => Unsafe.As<TEnum, int>(ref value)
	};
}

static class HitsoundEnumType
{
	public static int Next;
}

static class HitsoundEnumType<TEnum> where TEnum : struct, Enum
{
	public static readonly int Value = Interlocked.Increment(ref HitsoundEnumType.Next);
}
