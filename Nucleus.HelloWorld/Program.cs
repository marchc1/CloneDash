using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Rendering;
using Nucleus.Types;

using Raylib_cs;

using System.Numerics;

// This is a debugging project.

namespace Nucleus.HelloWorld
{
	internal class Program
	{
		public class HelloWorldLevel : Level
		{
			public override void Initialize(params object[] args) {
				base.Initialize(args);
			}

			Camera3D cam;
			public override void PostRender(FrameState frameState) {
				base.PostRender(frameState);
				Graphics2D.SetDrawColor(255, 255, 255);
				Graphics2D.DrawText(0, 0, "Hello SDL!", "Noto Sans", 32);
				Graphics2D.DrawRectangle(500, 500, 120, 120);

				Surface.SetViewport(256, 256, 256, 256);
				var s = 4;
				cam.Position = new(s * MathF.Sin(CurtimeF), s * MathF.Cos(CurtimeF), s * 0.75f);
				cam.Up = new(0, 0, 1);
				cam.Target = new(0, 0, 0);
				cam.Projection = CameraProjection.Perspective;
				cam.FovY = 90;
				EngineCore.Window.BeginMode3D(cam);
				{
					Raylib.DrawCube(new(0, 0, 0), 2, 2, 2, Color.Yellow);
				}
				EngineCore.Window.EndMode3D();
				Surface.ResetViewport();
			}
		}
		static void Main(string[] args) {
			EngineCore.GameInfo = new() {
				GameName = "Hello World"
			};
			EngineCore.Initialize(1600, 900, "Nucleus Testing Project", args);
			EngineCore.ShowDebuggingInfo = true;
			EngineCore.LoadLevel(new HelloWorldLevel());
			EngineCore.StartMainThread();
		}
	}
}
