using CloneDash.Common.Game;
using CloneDash.Unbeatable.Internal;
using Nucleus;
using Nucleus.Types;
using Raylib_cs;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public class UnbeatableWhiteLabelCameraRig(UnbeatableWhiteLabelPlayerComponent player) : ICameraRig
{
	const float CENTER_DISTANCE = 8f;
	const float SIDE_DISTANCE = 6f;
	const float SIDE_X = 5.5f;
	const float TARGET_Y = 2f;

	static readonly float ViewHeightPerDistance = 2 * MathF.Tan(MathF.PI / 6);

	readonly SecondOrderSystem cameraDistance = new(1.5f, 1, 0, SIDE_DISTANCE);
	readonly SecondOrderSystem cameraX = new(1.5f, 1, 0, SIDE_X);

	public float Distance { get; private set; } = SIDE_DISTANCE;
	public float X { get; private set; } = SIDE_X;
	public float Y => TARGET_Y;

	public void CalcView2D(FrameState frameState, ref Camera2D cam) {
		X = cameraX.Update(player.CameraCentered ? 0 : player.Side.Pick(-SIDE_X, SIDE_X));
		Distance = cameraDistance.Update(player.CameraCentered ? CENTER_DISTANCE : SIDE_DISTANCE);

		cam.Zoom = frameState.WindowHeight / (Distance * ViewHeightPerDistance);
		cam.Rotation = 0;
		cam.Offset = new(frameState.WindowWidth / 2, frameState.WindowHeight / 2);
		cam.Target = new(X, -TARGET_Y);
	}
}
