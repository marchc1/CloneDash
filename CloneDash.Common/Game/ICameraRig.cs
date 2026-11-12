using Nucleus.Types;
using Raylib_cs;

namespace CloneDash.Common.Game;

public interface ICameraRig
{
	void CalcView2D(FrameState frameState, ref Camera2D cam);
}
