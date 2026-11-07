using Nucleus.Types;

namespace CloneDash.Common.Game;

/// <summary>
/// The phase in which an <see cref="IRhythmGameComponent"/> thinks. Only applicable to components
/// that implement <see cref="IRhythmGameThinker"/>. This does not control which method it runs in,
/// just the ordering of the component when thinking is executing
/// </summary>
public enum RhythmGameThinkPhase
{
	Input,
	Update,
	Judge,
	Present
}

public interface IRhythmGameThinker
{
	RhythmGameThinkPhase Phase => RhythmGameThinkPhase.Update;
	void Think(FrameState frameState);
}

public interface IRhythmGameBackgroundPreRenderer
{
	void PreRenderBackground(FrameState frameState);
}

public interface IRhythmGamePreRenderer
{
	void PreRender(FrameState frameState);
}

public interface IRhythmGameRenderer
{
	void Render(FrameState frameState);
}

public interface IRhythmGameEntityPostRenderer
{
	void PostRenderEntities(FrameState frameState);
}

public interface IRhythmGamePostRenderer
{
	void PostRender(FrameState frameState);
}
