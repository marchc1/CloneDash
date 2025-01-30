using Nucleus.Core;
using Nucleus.Platform;
using Nucleus.Engine;
using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;

namespace Nucleus.ModelViewer
{
	public class Model3Viewer : Level
	{
		Checkbox loop;
		Checkbox visBones;
		ModelEntity? Entity;
		static string? LastFile;
		static DateTime LastFileWriteTime;

		public static Vector2F CamOffset = Vector2F.Zero;
		public static float CamZoom = 1;

		Textbox fallbackAnimation;

		public override void Initialize(params object[] args) {
			base.Initialize(args);
			CamOffset = Vector2F.Zero;
			CamZoom = 1;
			EngineCore.ShowDebuggingInfo = true;
			Keybinds.AddKeybind([KeyboardLayout.USA.LeftControl, KeyboardLayout.USA.R], () => {
				EngineCore.LoadLevel(new Model3Viewer());
			});

			Button openFile = UI.Add<Button>();
			openFile.Origin = Anchor.TopRight;
			openFile.Anchor = Anchor.TopRight;
			openFile.Position = new(-8, 8);
			openFile.TextPadding = new(8);
			openFile.AutoSize = true;
			openFile.Text = "Open File";

			openFile.MouseReleaseEvent += OpenFile_MouseReleaseEvent;

			Button hardReload = UI.Add<Button>();
			hardReload.Origin = Anchor.TopRight;
			hardReload.Anchor = Anchor.TopRight;
			hardReload.Position = new(-8, 40);
			hardReload.TextPadding = new(8);
			hardReload.AutoSize = true;
			hardReload.Text = "Reload Model3 Data";
			hardReload.MouseReleaseEvent += HardReload_MouseReleaseEvent;

			Button changeAnimation = UI.Add<Button>();
			changeAnimation.Position = new(8, 384 + (0 * 32));
			changeAnimation.TextPadding = new(8);
			changeAnimation.AutoSize = true;
			changeAnimation.Text = "Play Animation";
			changeAnimation.MouseReleaseEvent += ChangeAnimation_MouseReleaseEvent;

			fallbackAnimation = UI.Add<Textbox>();
			fallbackAnimation.Position = new(8, 384 + (1 * 32));
			fallbackAnimation.TextPadding = new(8);
			fallbackAnimation.Size = new(256, 28);
			fallbackAnimation.HelperText = "Fallback animation...";

			loop = UI.Add<Checkbox>();
			loop.Position = new(8, 384 + (2 * 32));
			loop.Size = new Vector2F(24);
			loop.TextPadding = new(8);
			loop.TooltipText = "Loop the animation?";

			visBones = UI.Add<Checkbox>();
			visBones.Position = new(8, 384 + (3 * 32));
			visBones.Size = new Vector2F(24);
			visBones.TextPadding = new(8);
			visBones.TooltipText = "Visualize bones?";

			var speed = UI.Add<NumSlider>();
			speed.Position = new(8, 384 + (4 * 32));
			speed.Size = new Vector2F(256, 24);
			speed.TextPadding = new(8);
			speed.TooltipText = "Animation speed";
			speed.HelperText = "Animation speed...";
			speed.Digits = 3;
			speed.MinimumValue = 0;
			speed.Value = 1;
			speed.OnValueChanged += Speed_OnValueChanged;

			if (LastFile != null) {
				Entity = ModelEntity.Create(LastFile, true);
			}

			UI.MouseDragEvent += UI_MouseDragEvent;
			UI.MouseScrollEvent += UI_MouseScrollEvent;
		}

		private void UI_MouseScrollEvent(Element self, FrameState state, Vector2F delta) {
			CamZoom += (delta.Y / -10f);
		}

		private void UI_MouseDragEvent(Element self, FrameState state, Vector2F delta) {
			CamOffset += (delta * -0.5f);
		}

		double animationSpeed = 1;
		private void Speed_OnValueChanged(NumSlider self, double oldValue, double newValue) {
			if (!IValidatable.IsValid(Entity)) return;

			animationSpeed = newValue;
			foreach (var animation in Entity.Model.Animations) {
				if (animation.Value.Playing) {
					animation.Value.Speed = newValue;
				}
			}
		}

		private void ChangeAnimation_MouseReleaseEvent(Element self, FrameState state, Types.MouseButton button) {
			if (!IValidatable.IsValid(Entity)) return;
			var menu = UI.Menu();

			foreach (var animation in Entity.Model.Animations) {
				menu.AddButton(animation.Key, null, () => {
					Entity.PlayAnimation(animation.Key, loop?.Checked ?? false, fallbackAnimation.Text.Length > 0 ? fallbackAnimation.Text : null);
					foreach (var animation in Entity.Model.Animations) {
						if (animation.Value.Playing) {
							animation.Value.Speed = animationSpeed;
						}
					}
				});
			}

			menu.Open(state.MouseState.MousePos);
		}

		private void HardReload_MouseReleaseEvent(Element self, FrameState state, Types.MouseButton button) {
			if (LastFile == null) return;
			Entity?.Remove();
			Entity = ModelEntity.Create(LastFile, true);
			LastFileWriteTime = File.GetLastWriteTime(LastFile);
		}

		private void OpenFile_MouseReleaseEvent(Element self, FrameState state, Types.MouseButton button) {
			var result = TinyFileDialogs.OpenFileDialog(
				"Open a .glb Model3...",
				Path.Combine(Filesystem.Resolve("game") + "assets", "scenes", "default", "models/"),
				["*.glb"],
				"GL Transmission Format Binary (Model 3 extended)",
				false);

			if (!result.Cancelled) {
				var file = result.Files[0];
				LastFile = file;
				Entity = ModelEntity.Create(file, true);
				LastFileWriteTime = File.GetLastWriteTime(LastFile);
			}
		}
		List<StringData> strings = [];
		public override void Render(FrameState frameState) {
			Raylib.ClearBackground(new(30));
			strings.Clear();
			base.Render(frameState);

			if (IValidatable.IsValid(Entity) && LastFile != null) {
				Entity.Render(frameState);
				strings.Add(new("Model", Path.GetFileName(LastFile)));
				strings.Add(new("Write Time", LastFileWriteTime));
				strings.Add(new("Model3Cache outdated?", (LastFileWriteTime != File.GetLastWriteTime(LastFile)) ? "Yes" : "No"));
				strings.Add(new("", null));
				strings.Add(new("Vertices", Entity.Model.Model.Vertices));
				strings.Add(new("Triangles", Entity.Model.Model.Triangles));
				strings.Add(new("Animating", $"{Entity.Model.PlayingAnimation}"));
				strings.Add(new("Animations:", null, true));
				foreach (var animation in Entity.Model.Animations) {
					strings.Add(new("", $"{animation.Key}"));
				}
			}
		}

		public record StringData(string Label, object? Value, bool Valueless = false)
		{

		}

		public override void Render2D(FrameState frameState) {
			base.Render2D(frameState);

			int maxStrLen = 0;

			string[] labels = new string[strings.Count];
			string?[] values = new string?[strings.Count];
			bool[] valuelessness = new bool[strings.Count];

			for (int i = 0; i < strings.Count; i++) {
				var data = strings[i];
				if (data.Label.Length == 0 && data.Value == null) {
					continue;
				}
				var ts = data.Value?.ToString() ?? "<null>";
				if (ts.Length > maxStrLen)
					maxStrLen = ts.Length;

				labels[i] = data.Label;
				values[i] = ts;
				valuelessness[i] = data.Valueless;
			}

			for (int i = 0; i < strings.Count; i++) {
				string label = labels[i];
				string? value = values[i];
				bool labelless = label == null || label.Length == 0;
				bool valueless = valuelessness[i];
				if (labelless && value == null && !valueless) continue;

				Graphics2D.DrawText(new(frameState.WindowWidth - 8, 120 + (i * 16)), $"{label}" + (valueless ? new string(' ', maxStrLen + 1) : $"{(labelless || label.EndsWith("?") ? "" : ":")}{new string(' ', maxStrLen - value.Length)} {value ?? " <null>"}"), "Consolas", 14, Anchor.TopRight);
			}

			if (IValidatable.IsValid(Entity)) {
				if (IValidatable.IsValid(visBones) && visBones.Checked) {
					foreach (var bone in Entity.Model.Bones) {
						var worldPos = bone.LocalToWorld(new(0, 0, 0));
						var screenPos = Raylib.GetWorldToScreen(worldPos, frameState.Camera);

						Graphics2D.SetDrawColor(255, 50, 50);
						Graphics2D.DrawCircle(screenPos.ToNucleus(), 4);
						if(bone.Parent != null && !bone.Parent.IsRoot) {
							var parentWorldPos = bone.Parent.LocalToWorld(new(0, 0, 0));
							var parentScreenPos = Raylib.GetWorldToScreen(parentWorldPos, frameState.Camera);
							Graphics2D.DrawLine(screenPos.ToNucleus(), parentScreenPos.ToNucleus(), 2);
						}

						if (!bone.HasChildren) { 
							var outwardWorldPos = bone.LocalToWorld(new(0, 50, 0));
							var outwardScreenPos = Raylib.GetWorldToScreen(outwardWorldPos, frameState.Camera);
							Graphics2D.DrawLine(screenPos.ToNucleus(), outwardScreenPos.ToNucleus(), 2);
						}

						var text = $"name: {bone.Name}, ActiveSlotAlpha: {bone.ActiveSlotAlpha}";
						var ts = Graphics2D.GetTextSize(text, "Consolas", 10) + new Vector2F(4);
						Graphics2D.SetDrawColor(30, 30, 30, 200);
						Graphics2D.DrawRectangle(screenPos.X - (ts.X / 2), screenPos.Y - (ts.Y / 2), ts.X, ts.Y);
						Graphics2D.SetDrawColor(255, 255, 255);
						Graphics2D.DrawText(screenPos.ToNucleus(), text, "Consolas", 10, Anchor.Center);
					}
				}
			}
		}

		public override void CalcView(FrameState frameState, ref Camera3D cam) {
			cam.FovY = CamZoom * 340;
			cam.Position = new(CamOffset.X, CamOffset.Y, -500);
			cam.Target = new(CamOffset.X, CamOffset.Y, 0);
			base.CalcView(frameState, ref cam);
		}
	}
	internal class Program
	{
		static void Main(string[] args) {
			EngineCore.Initialize(1152, 864, "Model v3 Viewer", args);
			EngineCore.GameInfo = new() {
				GameName = "Model v3 Viewer"
			};

			EngineCore.LoadLevel(new Model3Viewer());

			Filesystem.AddPath("audio", Filesystem.Resolve("game") + "assets/scenes/default/audio/");
			Filesystem.AddPath("models", Filesystem.Resolve("game") + "assets/scenes/default/models/");
			Filesystem.AddPath("scripts", Filesystem.Resolve("game") + "assets/scenes/default/scripts/");

			EngineCore.Start();
		}
	}
}
