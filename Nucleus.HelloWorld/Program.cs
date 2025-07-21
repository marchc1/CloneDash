using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Types;

// This is a debugging project.

namespace Nucleus.HelloWorld
{
	internal class Program
	{
		public class HelloWorldLevel : Level {
			public override void Initialize(params object[] args) {
				base.Initialize(args);
			}

			public override void PostRender(FrameState frameState) {
				base.PostRender(frameState);
				Graphics2D.SetDrawColor(255, 255, 255);
				Graphics2D.DrawText(0, 0, "Hello SDL!", "Noto Sans", 32);
				Graphics2D.DrawRectangle(500, 500, 120, 120);
			}
		}
		static void Main(string[] args) {
			EngineCore.Initialize(1600, 900, "Clone Dash", args);
			EngineCore.GameInfo = new() {
				GameName = "Hello World"
			};
			EngineCore.ShowDebuggingInfo = true;
			EngineCore.LoadLevel(new HelloWorldLevel());
			EngineCore.StartMainThread();
		}
	}
}
