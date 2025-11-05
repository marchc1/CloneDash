using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Rendering;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;

using Raylib_cs;

using System.Numerics;

// This is a debugging project.

namespace Nucleus.HelloWorld;

struct TestBtn(string text, Action<Level> click)
{
	public string Text = text;
	public Action<Level> Click = click;
}

internal class Program
{
	public class HelloWorldLevel : Level
	{
		readonly TestBtn[] tests = [
			new("Label Content Alignment", (level) => {
				var window = level.UI.Add<Window>();

				Panel row(Dock vertical){
					var row = window.Add<Panel>();
					row.Dock = vertical;

					Label column(Anchor alignment){
						var column = row.Add<Label>();
						var h = alignment.ToTextAlignment().horizontal;
						column.Dock = h == TextAlignment.Left ? Dock.Left : h == TextAlignment.Right ? Dock.Right : Dock.Fill;
						column.Text = "The quick brown fox jumps over the lazy dog, lorem ipsum, etc, etc, oh yeah, the text alignment for this label is " + alignment.ToString();
						column.Size = new(window.Size.W / 3, 0);
						column.TextAlignment = alignment;
						return column;
					}

					var v = vertical == Dock.Top ? TextAlignment.Top : vertical == Dock.Bottom ? TextAlignment.Bottom : TextAlignment.Center;

					var left = column(TextAlignment.FromTextAlignment(TextAlignment.Left, v));
					var right = column(TextAlignment.FromTextAlignment(TextAlignment.Right, v));
					var center = column(TextAlignment.FromTextAlignment(TextAlignment.Center, v));
					row.Size = new(0, window.Size.H / 3);
					row.DrawPanelBackground = false;

					return row;
				}

				var top = row(Dock.Top);
				var bottom = row(Dock.Bottom);
				var mid = row(Dock.Fill);
				window.Center();
			}),
			new("Subwindow Test", (_) => EngineCore.LoadLevelSubWindow(new HelloWorldLevel(), 640, 480, "test")),
		];

		public override void Initialize(params object[] args) {
			base.Initialize(args);

			var tools = UI.Add<Panel>();
			tools.Dock = Dock.Right;
			tools.Size = new(640, 0);
			var testLabel = tools.Add<Label>();
			testLabel.AutoSize = true;
			testLabel.Dock = Dock.Top;
			testLabel.Text = "Test Functions";
			foreach (var test in tests) {
				var b = tools.Add<Button>();
				b.Text = test.Text;
				b.Dock = Dock.Top;
				b.MouseClickEvent += (_, _, _) => test.Click(this);
			}
		}

		private void B_MouseClickEvent(Element self, FrameState state, Input.MouseButton button) {

		}

		Camera3D cam;
		public override void PostRender(FrameState frameState) {
			base.PostRender(frameState);
			Graphics2D.SetDrawColor(255, 255, 255);
			Graphics2D.DrawText(0, 0, "Hello SDL!", Graphics2D.UI_FONT_NAME, 32);
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
			AppName = "Hello World",
			AppIdentifier = "com.github.marchc1.NucleusHelloWorld"
		};
		EngineCore.Initialize(1600, 900, "Nucleus Testing Project", args);
		EngineCore.ShowDebuggingInfo = true;
		EngineCore.LoadLevel(new HelloWorldLevel());
		EngineCore.StartMainThread();
	}
}
