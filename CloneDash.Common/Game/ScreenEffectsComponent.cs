using Nucleus;
using Nucleus.Engine;
using Nucleus.ManagedMemory;
using Nucleus.Types;

using Color = Nucleus.Common.Types.Color;

namespace CloneDash.Common.Game;

public class ScreenEffectsComponent(Level level, RhythmGameLevel game) : IRhythmGameComponent
{
	static int nextTypeId;

	readonly List<ScreenEffect> effects = [];

	char[]? ActiveEffectsBuffer;
	ScreenEffect?[] byType = [];

	public RenderTexture? RenderTexture { get; private set; }

	public RenderTexture? RenderTexture2 { get; private set; }

	public T Add<T>(T effect) where T : ScreenEffect {
		effect.Attach(level, game, this);
		effects.Add(effect);

		int id = TypeId<T>.Value;
		if (id >= byType.Length)
			Array.Resize(ref byType, id + 1);
		byType[id] = effect;

		return effect;
	}

	public void Begin(float width, float height) {
		// Evaluate if complex render texture needs to be remade
		// This is only the case if null or bounds changed
		if (RenderTexture == null || (RenderTexture.GetWidth() != width || RenderTexture.GetHeight() != height)) {
			RenderTexture?.Dispose();
			RenderTexture2?.Dispose();
			// TODO: If complex render textures are too slow for this (and they might be), then
			// comment out this line to remove it from the rendering pipeline here - you just won't get screenspace effects, 
			// when i have that working
			RenderTexture = (RenderTexture)textures.CreateRenderTexture((int)width, (int)height, samples: 4);
			RenderTexture2 = (RenderTexture)textures.CreateRenderTexture((int)width, (int)height, samples: 4);
		}

		RenderTexture?.Begin();
	}

	public void Draw(FrameState frameState) {
		if (RenderTexture == null || RenderTexture2 == null)
			return;

		RenderTexture read = RenderTexture;
		RenderTexture write = RenderTexture2;
		foreach (var effect in effects)
			effect.Render(frameState, ref read, ref write);

		bool presented = false;
		foreach (var effect in effects) {
			if (effect.Present(frameState, read)) {
				presented = true;
				break;
			}
		}

		if (!presented)
			read.Draw(new(0, 0, frameState.WindowWidth, -frameState.WindowHeight), new(0, 0), Color.White);

		foreach (var effect in effects)
			effect.RenderOverlay(frameState);
	}

	public void End() {
		RenderTexture?.End();
	}

	public T Get<T>() where T : ScreenEffect {
		int id = TypeId<T>.Value;
		if (id >= byType.Length || byType[id] is not T effect)
			throw new InvalidOperationException($"{typeof(T).Name} was never added to the screen effects.");

		return effect;
	}

	public ReadOnlySpan<char> GetActiveEffects() {
		if (!EngineCore.developer.GetBool())
			return "";

		ActiveEffectsBuffer ??= new char[1024];
		Span<char> writeBuf = ActiveEffectsBuffer.AsSpan();
		int ptr = 0;
		foreach (var effect in effects) {
			if (effect.Target != 0) {
				string name = effect.GetType().Name;

				if (ptr != 0) {
					", ".CopyTo(writeBuf[ptr..]);
					ptr += 2;
				}

				name.CopyTo(writeBuf[ptr..]);
				ptr += name.Length;
			}
		}

		return writeBuf[..ptr];
	}

	public void Initialize() {
		foreach (var effect in effects)
			effect.Initialize();
	}

	public void Reset() {
		foreach (var effect in effects)
			effect.Reset();
	}

	static class TypeId<T> where T : ScreenEffect
	{
		public static readonly int Value = Interlocked.Increment(ref nextTypeId) - 1;
	}
}
