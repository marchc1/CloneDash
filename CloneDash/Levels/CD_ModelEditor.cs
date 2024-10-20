using CloneDash.Game;

using Nucleus;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;

namespace CloneDash.Levels
{
    public class CD_ModelEditor : Level
    {
        public override void Initialize(params object[] args) {
            var goBack = UI.Add<Button>();
            goBack.Text = "<";
            goBack.Size = new(64, 64);
            goBack.Position = new(8, 8);
            goBack.TextSize = 42;
            goBack.TooltipText = "Back to Main Menu";

            goBack.MouseReleaseEvent += GoBack_MouseReleaseEvent;
        }

        private void GoBack_MouseReleaseEvent(Element self, FrameState state, Nucleus.Types.MouseButton button) {
            EngineCore.LoadLevel(new CD_MainMenu());
        }

        public override void CalcView(FrameState frameState, ref Camera3D cam) {
            base.CalcView(frameState, ref cam);
        }
        public override void PreRenderBackground(FrameState frameState) {
            base.PreRenderBackground(frameState);
        }
        public override void PreRender(FrameState frameState) {
            Rlgl.DrawRenderBatchActive();
            Rlgl.SetLineWidth(2);
            var lines = 12;
            var distance = 64;

            for (int y = -lines / 2; y < (lines / 2) + 1; y++) {
                Raylib.DrawLine3D(new(-lines / 2 * distance, y * distance, 0), new(lines / 2 * distance, y * distance, 0), new Color(45, 55, 58, 120));
            }
            for (int x = -lines / 2; x < (lines / 2) + 1; x++) {
                Raylib.DrawLine3D(new(x * distance, -lines / 2 * distance, 0), new(x * distance, lines / 2 * distance, 0), new Color(45, 55, 58, 120));
            }
            Raylib.DrawLine3D(new(-lines / 2 * distance, -lines / 2 * distance, 0), new(-lines / 2 * distance, lines / 2 * distance, 0), new Color(200, 207, 220, 127));
            Raylib.DrawLine3D(new(lines / 2 * distance, -lines / 2 * distance, 0), new(lines / 2 * distance, lines / 2 * distance, 0), new Color(200, 207, 220, 127));
            Raylib.DrawLine3D(new(lines / 2 * distance, lines / 2 * distance, 0), new(-lines / 2 * distance, lines / 2 * distance, 0), new Color(200, 207, 220, 127));
            Raylib.DrawLine3D(new(lines / 2 * distance, -lines / 2 * distance, 0), new(-lines / 2 * distance, -lines / 2 * distance, 0), new Color(200, 207, 220, 127));

            Rlgl.DrawRenderBatchActive();
            Rlgl.SetLineWidth(3);
            Raylib.DrawLine3D(new(1.5f, 0, 0), new(lines / 2 * distance - 7, 0, 0), new Color(255, 140, 130, 255));
            Raylib.DrawLine3D(new(0, 1.5f, 0), new(0, lines / 2 * distance - 7, 0), new Color(130, 255, 140, 255));
            Rlgl.DrawRenderBatchActive();
            Rlgl.SetLineWidth(1);
        }
    }
}
