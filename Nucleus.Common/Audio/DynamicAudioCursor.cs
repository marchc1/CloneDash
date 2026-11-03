namespace Nucleus.Common.Audio;

public sealed class DynamicAudioCursor(int sampleRate, int channels)
{
	long frame;

	public int SampleRate { get; } = sampleRate;
	public int Channels { get; } = channels;

	public long Frame {
		get => Interlocked.Read(ref frame);
		set => Interlocked.Exchange(ref frame, value);
	}

	public double Time {
		get => (double)Frame / SampleRate;
		set => Frame = (long)Math.Max(0, value * SampleRate);
	}

	public void Advance(long frames) => Interlocked.Add(ref frame, frames);
}
