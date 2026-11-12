using CloneDash.Common.Game;
using Nucleus.Types;

namespace CloneDash.Game;

public class MuseDash1FailTextComponent(MuseDash1HudComponent hud) : IRhythmGameComponent, IRhythmGameRenderer
{
	public void Render(FrameState frameState) => hud.UI?.RenderFailText();

	public void Reset() { }
}
