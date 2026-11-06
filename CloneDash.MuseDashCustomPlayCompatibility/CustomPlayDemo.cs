using Nucleus;
using Nucleus.Common.Audio;
using NVorbis;

namespace CloneDash.MuseDashCustomPlayCompatibility;

public sealed class CustomPlayDemo
{
	public const double DEFAULT_LENGTH = 15;
	public const double FADE_IN = 1.0;
	public const double FADE_OUT = 2.0;
	public const double LOOP_GAP = 1.0;

	readonly float[] samples;
	readonly long demoFrames;
	readonly long cycleFrames;
	readonly long fadeInFrames;
	readonly long fadeOutFrames;

	public int SampleRate { get; }
	public int Channels { get; }

	CustomPlayDemo(float[] samples, int sampleRate, int channels) {
		this.samples = samples;
		SampleRate = sampleRate;
		Channels = channels;

		demoFrames = samples.Length / channels;
		fadeInFrames = Math.Min((long)(FADE_IN * sampleRate), demoFrames / 2);
		fadeOutFrames = Math.Min((long)(FADE_OUT * sampleRate), demoFrames / 2);
		cycleFrames = demoFrames + (long)(LOOP_GAP * sampleRate);
	}

	public static CustomPlayDemo? Load(string oggPath, double start, double end) {
		try {
			using var reader = new VorbisReader(oggPath);
			double total = reader.TotalTime.TotalSeconds;

			start = Math.Clamp(start, 0, total);
			if (end <= start)
				end = start + DEFAULT_LENGTH;
			end = Math.Min(end, total);
			if (end <= start)
				return null;

			int channels = reader.Channels;
			int sampleRate = reader.SampleRate;
			long frames = (long)((end - start) * sampleRate);

			reader.SeekTo(TimeSpan.FromSeconds(start));
			var samples = new float[frames * channels];
			int read = 0;
			while (read < samples.Length) {
				int got = reader.ReadSamples(samples.AsSpan(read));
				if (got <= 0) break;
				read += got;
			}

			read -= read % channels;
			if (read == 0)
				return null;
			if (read < samples.Length)
				Array.Resize(ref samples, read);

			return new CustomPlayDemo(samples, sampleRate, channels);
		}
		catch (Exception ex) {
			Logs.Warn($"CustomPlay: couldn't build a demo from '{oggPath}': {ex.Message}");
			return null;
		}
	}

	static float Smoothstep(double t) => (float)(t * t * (3 - 2 * t));

	float Gain(long frame) {
		float gain = 1;
		if (frame < fadeInFrames)
			gain *= Smoothstep((double)frame / fadeInFrames);
		long fromEnd = demoFrames - frame;
		if (fromEnd < fadeOutFrames)
			gain *= Smoothstep((double)fromEnd / fadeOutFrames);
		return gain;
	}

	public void Render(Span<float> buffer, object? userdata) {
		if (userdata is not DynamicAudioCursor cursor) {
			buffer.Clear();
			return;
		}

		long start = cursor.Frame;
		int frames = buffer.Length / Channels;
		for (int i = 0; i < frames; i++) {
			long frame = (start + i) % cycleFrames;
			var dst = buffer.Slice(i * Channels, Channels);
			if (frame >= demoFrames) {
				dst.Clear();
				continue;
			}

			float gain = Gain(frame);
			var src = samples.AsSpan((int)(frame * Channels), Channels);
			for (int c = 0; c < Channels; c++)
				dst[c] = src[c] * gain;
		}
	}
}
