using Nucleus.Types;

namespace CloneDash.Common.Game;

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
