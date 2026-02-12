using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Raylib_cs;

/// <summary>
/// Mouse cursor
/// </summary>
public enum MouseCursor
{
    /// <summary>
    /// Default pointer shape
    /// </summary>
    MOUSE_CURSOR_DEFAULT = 0,

    /// <summary>
    /// Arrow shape
    /// </summary>
    MOUSE_CURSOR_ARROW = 1,

    /// <summary>
    /// Text writing cursor shape
    /// </summary>
    MOUSE_CURSOR_IBEAM = 2,

    /// <summary>
    /// Cross shape
    /// </summary>
    MOUSE_CURSOR_CROSSHAIR = 3,

    /// <summary>
    /// Pointing hand cursor
    /// </summary>
    MOUSE_CURSOR_POINTING_HAND = 4,

    /// <summary>
    /// Horizontal resize/move arrow shape
    /// </summary>
    MOUSE_CURSOR_RESIZE_EW = 5,

    /// <summary>
    /// Vertical resize/move arrow shape
    /// </summary>
    MOUSE_CURSOR_RESIZE_NS = 6,

    /// <summary>
    /// Top-left to bottom-right diagonal resize/move arrow shape
    /// </summary>
    MOUSE_CURSOR_RESIZE_NWSE = 7,

    /// <summary>
    /// The top-right to bottom-left diagonal resize/move arrow shape
    /// </summary>
    MOUSE_CURSOR_RESIZE_NESW = 8,

    /// <summary>
    /// The omnidirectional resize/move cursor shape
    /// </summary>
    MOUSE_CURSOR_RESIZE_ALL = 9,

    /// <summary>
    /// The operation-not-allowed shape
    /// </summary>
    MOUSE_CURSOR_NOT_ALLOWED = 10
}

/// <summary>Gamepad axis</summary>
public enum GamepadAxis
{
    /// <summary>
    /// Gamepad left stick X axis
    /// </summary>
    GAMEPAD_AXIS_LEFT_X = 0,

    /// <summary>
    /// Gamepad left stick Y axis
    /// </summary>
    GAMEPAD_AXIS_LEFT_Y = 1,

    /// <summary>
    /// Gamepad right stick X axis
    /// </summary>
    GAMEPAD_AXIS_RIGHT_X = 2,

    /// <summary>
    /// Gamepad right stick Y axis
    /// </summary>
    GAMEPAD_AXIS_RIGHT_Y = 3,

    /// <summary>
    /// Gamepad back trigger left, pressure level: [1..-1]
    /// </summary>
    GAMEPAD_AXIS_LEFT_TRIGGER = 4,

    /// <summary>
    /// Gamepad back trigger right, pressure level: [1..-1]
    /// </summary>
    GAMEPAD_AXIS_RIGHT_TRIGGER = 5
}

/// <summary>
/// Gamepad buttons
/// </summary>
public enum GamepadButton
{
    /// <summary>
    /// Unknown button, just for error checking
    /// </summary>
    GAMEPAD_BUTTON_UNKNOWN = 0,

    /// <summary>
    /// Gamepad left DPAD up button
    /// </summary>
    GAMEPAD_BUTTON_LEFT_FACE_UP,

    /// <summary>
    /// Gamepad left DPAD right button
    /// </summary>
    GAMEPAD_BUTTON_LEFT_FACE_RIGHT,

    /// <summary>
    /// Gamepad left DPAD down button
    /// </summary>
    GAMEPAD_BUTTON_LEFT_FACE_DOWN,

    /// <summary>
    /// Gamepad left DPAD left button
    /// </summary>
    GAMEPAD_BUTTON_LEFT_FACE_LEFT,

    /// <summary>
    /// Gamepad right button up (i.e. PS3: Triangle, Xbox: Y)
    /// </summary>
    GAMEPAD_BUTTON_RIGHT_FACE_UP,

    /// <summary>
    /// Gamepad right button right (i.e. PS3: Square, Xbox: X)
    /// </summary>
    GAMEPAD_BUTTON_RIGHT_FACE_RIGHT,

    /// <summary>
    /// Gamepad right button down (i.e. PS3: Cross, Xbox: A)
    /// </summary>
    GAMEPAD_BUTTON_RIGHT_FACE_DOWN,

    /// <summary>
    /// Gamepad right button left (i.e. PS3: Circle, Xbox: B)
    /// </summary>
    GAMEPAD_BUTTON_RIGHT_FACE_LEFT,

    /// <summary>
    /// Gamepad top/back trigger left (first), it could be a trailing button
    /// </summary>
    GAMEPAD_BUTTON_LEFT_TRIGGER_1,

    /// <summary>
    /// Gamepad top/back trigger left (second), it could be a trailing button
    /// </summary>
    GAMEPAD_BUTTON_LEFT_TRIGGER_2,

    /// <summary>
    /// Gamepad top/back trigger right (first), it could be a trailing button
    /// </summary>
    GAMEPAD_BUTTON_RIGHT_TRIGGER_1,

    /// <summary>
    /// Gamepad top/back trigger right (second), it could be a trailing button
    /// </summary>
    GAMEPAD_BUTTON_RIGHT_TRIGGER_2,

    /// <summary>
    /// Gamepad center buttons, left one (i.e. PS3: Select)
    /// </summary>
    GAMEPAD_BUTTON_MIDDLE_LEFT,

    /// <summary>
    /// Gamepad center buttons, middle one (i.e. PS3: PS, Xbox: XBOX)
    /// </summary>
    GAMEPAD_BUTTON_MIDDLE,

    /// <summary>
    /// Gamepad center buttons, right one (i.e. PS3: Start)
    /// </summary>
    GAMEPAD_BUTTON_MIDDLE_RIGHT,

    /// <summary>
    /// Gamepad joystick pressed button left
    /// </summary>
    GAMEPAD_BUTTON_LEFT_THUMB,

    /// <summary>
    /// Gamepad joystick pressed button right
    /// </summary>
    GAMEPAD_BUTTON_RIGHT_THUMB
}

/// <summary>
/// Gesture
/// NOTE: It could be used as flags to enable only some gestures
/// </summary>
[Flags]
public enum Gesture : uint
{
    GESTURE_NONE = 0,
    GESTURE_TAP = 1,
    GESTURE_DOUBLETAP = 2,
    GESTURE_HOLD = 4,
    GESTURE_DRAG = 8,
    GESTURE_SWIPE_RIGHT = 16,
    GESTURE_SWIPE_LEFT = 32,
    GESTURE_SWIPE_UP = 64,
    GESTURE_SWIPE_DOWN = 128,
    GESTURE_PINCH_IN = 256,
    GESTURE_PINCH_OUT = 512
}

/// <summary>
/// Head-Mounted-Display device parameters
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe partial struct VrDeviceInfo
{
    /// <summary>
    /// HMD horizontal resolution in pixels
    /// </summary>
    public int HResolution;

    /// <summary>
    /// HMD vertical resolution in pixels
    /// </summary>
    public int VResolution;

    /// <summary>
    /// HMD horizontal size in meters
    /// </summary>
    public float HScreenSize;

    /// <summary>
    /// HMD vertical size in meters
    /// </summary>
    public float VScreenSize;

    /// <summary>
    /// HMD screen center in meters
    /// </summary>
    public float VScreenCenter;

    /// <summary>
    /// HMD distance between eye and display in meters
    /// </summary>
    public float EyeToScreenDistance;

    /// <summary>
    /// HMD lens separation distance in meters
    /// </summary>
    public float LensSeparationDistance;

    /// <summary>
    /// HMD IPD (distance between pupils) in meters
    /// </summary>
    public float InterpupillaryDistance;

    /// <summary>
    /// HMD lens distortion constant parameters
    /// </summary>
    public fixed float LensDistortionValues[4];

    /// <summary>
    /// HMD chromatic aberration correction parameters
    /// </summary>
    public fixed float ChromaAbCorrection[4];
}

/// <summary>
/// VR Stereo rendering configuration for simulator
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public partial struct VrStereoConfig
{
    /// <summary>
    /// VR projection matrices (per eye)
    /// </summary>
    public Matrix4x4 Projection1;

    /// <summary>
    /// VR projection matrices (per eye)
    /// </summary>
    public Matrix4x4 Projection2;

    /// <summary>
    /// VR view offset matrices (per eye)
    /// </summary>
    public Matrix4x4 ViewOffset1;

    /// <summary>
    /// VR view offset matrices (per eye)
    /// </summary>
    public Matrix4x4 ViewOffset2;

    /// <summary>
    /// VR left lens center
    /// </summary>
    public Vector2 LeftLensCenter;

    /// <summary>
    /// VR right lens center
    /// </summary>
    public Vector2 RightLensCenter;

    /// <summary>
    /// VR left screen center
    /// </summary>
    public Vector2 LeftScreenCenter;

    /// <summary>
    /// VR right screen center
    /// </summary>
    public Vector2 RightScreenCenter;

    /// <summary>
    /// VR distortion scale
    /// </summary>
    public Vector2 Scale;

    /// <summary>
    /// VR distortion scale in
    /// </summary>
    public Vector2 ScaleIn;
}
