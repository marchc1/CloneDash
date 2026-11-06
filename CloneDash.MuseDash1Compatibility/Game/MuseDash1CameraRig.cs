using CloneDash.Common.Game;
using Nucleus;
using Nucleus.Types;
using Raylib_cs;

namespace CloneDash.Game;

public class MuseDash1CameraRig(MashComponent mash) : ICameraRig
{
	readonly SecondOrderSystem MashZoomSOS = new(1.1f, 0.9f, 2f, 0);

	public void CalcView2D(FrameState frameState, ref Camera2D cam) {
		var zoomValue = MashZoomSOS.Update(mash.InMashState ? 1 : 0) * .5f;
		cam.Zoom = ((frameState.WindowHeight / 900) * 120) + (zoomValue * 45) * 0.5f;
		cam.Rotation = 0.0f;
		cam.Offset = new(frameState.WindowWidth / 2, frameState.WindowHeight / 2);
		cam.Target = new(zoomValue * -5, 0);
		cam.Offset += cam.Target;

		//cam.Offset = new(frameState.WindowWidth * Game.Pathway.PATHWAY_LEFT_PERCENTAGE * .5f, frameState.WindowHeight * 0.5f);
		//cam.Target = cam.Offset;
	}
}
