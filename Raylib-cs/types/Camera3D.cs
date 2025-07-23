using System.Numerics;
using System.Runtime.InteropServices;

namespace Raylib_cs;

/// <summary>
/// Camera system modes
/// </summary>
public enum CameraMode
{
    Custom = 0,
    Free,
    Orbital,
    FirstPerson,
    ThirdPerson
}

/// <summary>
/// Camera projection
/// </summary>
public enum CameraProjection
{
    Perspective = 0,
    Orthographic
}

/// <summary>
/// Camera3D, defines position/orientation in 3d space
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public partial struct Camera3D
{
    /// <summary>
    /// Camera position
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// Camera target it looks-at
    /// </summary>
    public Vector3 Target;

    /// <summary>
    /// Camera up vector (rotation over its axis)
    /// </summary>
    public Vector3 Up;

    /// <summary>
    /// Camera field-of-view apperture in Y (degrees) in perspective, used as near plane width in orthographic
    /// </summary>
    public float FovY;

    /// <summary>
    /// Camera type, defines projection type: CAMERA_PERSPECTIVE or CAMERA_ORTHOGRAPHIC
    /// </summary>
    public CameraProjection Projection;

    public Camera3D(Vector3 position, Vector3 target, Vector3 up, float fovY, CameraProjection projection)
    {
        this.Position = position;
        this.Target = target;
        this.Up = up;
        this.FovY = fovY;
        this.Projection = projection;
    }

	// rcamera reimplementations

	public Vector3 GetForward() => Vector3.Normalize(Target - Position);
	public Vector3 GetUp() => Vector3.Normalize(Up);
	public Vector3 GetRight() => Vector3.Cross(GetForward(), GetUp());

	public void MoveForward(float distance, bool moveInWorldPlane) {
		Vector3 forward = GetForward();

		if (moveInWorldPlane) {
			forward.Z = 0;
			forward = Vector3.Normalize(forward);
		}

		forward *= distance;
		Position += forward;
		Target += forward;
	}
	public void MoveUp(float distance) {
		Vector3 up = GetUp();

		up *= distance;
		Position += up;
		Target += up;
	}

	public void MoveRight(float distance, bool moveInWorldPlane) {
		Vector3 right = GetRight();

		if (moveInWorldPlane) {
			right.Z = 0;
			right = Vector3.Normalize(right);
		}

		right *= distance;
		Position += right;
		Target += right;
	}

	public void MoveToTarget(float delta) {
		float distance = Vector3.Distance(Position, Target);

		distance += delta;
		if (distance <= 0) 
			distance = 0.001f;

		// Set new distance by moving the position along the forward vector
		Vector3 forward = GetForward();
		Position = Target + (forward * -distance);
	}
}
