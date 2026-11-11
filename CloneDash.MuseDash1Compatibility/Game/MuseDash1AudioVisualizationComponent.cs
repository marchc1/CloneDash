using CloneDash.Common.Game;
using Nucleus.Commands;
using Nucleus.Types;

namespace CloneDash.Game;

public class MuseDash1AudioVisualizationComponent : IRhythmGameComponent, IRhythmGameThinker
{
	public const int BANDS = 32;
	public const float BASE_N = 1.117f;
	public const float DOWN_SPEED = 1.35f;
	public const double FIXED_STEP = 0.01;
	public const int MAX_STEPS_PER_FRAME = 5;
	public const float POWER = 100;
	public const int SAMPLES = 1024;
	public const float UP_SPEED = 0.004f;

	public static readonly ConVar md1_audiovis = new(nameof(md1_audiovis), "1", FCvar.Saved, "Scene equalizer bars follow the music.");

	const int WINDOW = SAMPLES * 2;

	readonly float[] bandBuffer = new float[BANDS];
	readonly float[] bufferDecrease = new float[BANDS];
	readonly float[] freqBand = new float[BANDS];
	readonly float[] history = new float[WINDOW];
	readonly double[] imaginary = new double[WINDOW];
	readonly RhythmGameLevel level;
	readonly MusicComponent music;
	readonly double[] real = new double[WINDOW];
	readonly float[] spectrum = new float[SAMPLES];
	readonly double[] window = new double[WINDOW];

	double accumulator;
	bool attached;
	int historyWrite;

	public MuseDash1AudioVisualizationComponent(GameEventManager events, RhythmGameLevel level, MusicComponent music) {
		this.level = level;
		this.music = music;
		for (int i = 0; i < WINDOW; i++)
			window[i] = 0.42 - 0.5 * Math.Cos(2 * Math.PI * i / (WINDOW - 1)) + 0.08 * Math.Cos(4 * Math.PI * i / (WINDOW - 1));

		events.Listen<LevelLoadedGameEvent>(Attach);
	}

	public ReadOnlySpan<float> BandBuffer => bandBuffer;

	public RhythmGameThinkPhase Phase => RhythmGameThinkPhase.Present;

	public bool Visualizing => md1_audiovis.GetBool();

	public void Reset() {
		accumulator = 0;
		Array.Clear(bandBuffer);
		Array.Clear(bufferDecrease);
		Array.Clear(freqBand);
		lock (history)
			Array.Clear(history);
	}

	public void Think(FrameState frameState) {
		if (!Visualizing || level.Paused)
			return;

		accumulator = Math.Min(accumulator + globals.CurTimeDelta, FIXED_STEP * MAX_STEPS_PER_FRAME);
		while (accumulator >= FIXED_STEP) {
			accumulator -= FIXED_STEP;
			Step();
		}
	}

	void Attach(ref LevelLoadedGameEvent ev) {
		if (!attached)
			attached = audiosystem.AttachProcessor(in music.Handle, Capture);
	}

	void Capture(Span<float> buffer, object? userdata) {
		lock (history)
			for (int i = 0; i < buffer.Length; i += 2) {
				history[historyWrite] = buffer[i];
				historyWrite = (historyWrite + 1) % WINDOW;
			}
	}

	void Step() {
		lock (history)
			for (int i = 0; i < WINDOW; i++) {
				real[i] = history[(historyWrite + i) % WINDOW] * window[i];
				imaginary[i] = 0;
			}

		Transform(real, imaginary);
		for (int i = 0; i < SAMPLES; i++)
			spectrum[i] = (float)(Math.Sqrt(real[i] * real[i] + imaginary[i] * imaginary[i]) * 2 / WINDOW);

		MakeFrequencyBands();
		UpdateBandBuffer();
	}

	void MakeFrequencyBands() {
		int count = 0;
		for (int band = 0; band < BANDS; band++) {
			int width = (int)MathF.Pow(BASE_N, band) * 2;
			if (band == 7)
				width += 2;

			float sum = 0;
			for (int i = 0; i < width && count < SAMPLES; i++, count++)
				sum += spectrum[count] * (count + 1);
			freqBand[band] = count > 0 ? sum / count * POWER : 0;
		}
	}

	void UpdateBandBuffer() {
		for (int band = 0; band < BANDS; band++) {
			if (freqBand[band] > bandBuffer[band]) {
				bandBuffer[band] = freqBand[band];
				bufferDecrease[band] = UP_SPEED;
			}
			if (freqBand[band] < bandBuffer[band]) {
				bandBuffer[band] -= bufferDecrease[band];
				bufferDecrease[band] *= DOWN_SPEED;
			}
		}
	}

	static void Transform(double[] real, double[] imaginary) {
		int n = real.Length;
		for (int i = 1, j = 0; i < n; i++) {
			int bit = n >> 1;
			for (; (j & bit) != 0; bit >>= 1)
				j ^= bit;
			j ^= bit;
			if (i < j) {
				(real[i], real[j]) = (real[j], real[i]);
				(imaginary[i], imaginary[j]) = (imaginary[j], imaginary[i]);
			}
		}

		for (int length = 2; length <= n; length <<= 1) {
			double angle = -2 * Math.PI / length;
			double stepReal = Math.Cos(angle), stepImaginary = Math.Sin(angle);
			for (int start = 0; start < n; start += length) {
				double wReal = 1, wImaginary = 0;
				for (int k = 0; k < length / 2; k++) {
					int even = start + k, odd = even + length / 2;
					double oddReal = real[odd] * wReal - imaginary[odd] * wImaginary;
					double oddImaginary = real[odd] * wImaginary + imaginary[odd] * wReal;
					real[odd] = real[even] - oddReal;
					imaginary[odd] = imaginary[even] - oddImaginary;
					real[even] += oddReal;
					imaginary[even] += oddImaginary;
					double nextReal = wReal * stepReal - wImaginary * stepImaginary;
					wImaginary = wReal * stepImaginary + wImaginary * stepReal;
					wReal = nextReal;
				}
			}
		}
	}
}
