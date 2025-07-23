using Raylib_cs;

namespace Nucleus.Engine;
public static class Cam
{
	public static void Begin2D(in Camera2D cam) => EngineCore.Window.BeginMode2D(cam);
	public static void End2D(in Camera2D cam) => EngineCore.Window.EndMode2D();
	public static void Begin3D(in Camera3D cam) => EngineCore.Window.BeginMode3D(cam);
	public static void End3D(in Camera3D cam) => EngineCore.Window.EndMode3D();
}