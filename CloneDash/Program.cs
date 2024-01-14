/*
    A *LOT* of this is subject to change. This is a prototype, and just a testbed of basic game functionality.
*/

using CloneDash.Systems;
using Raylib_cs;

namespace CloneDash
{
    internal class Program
    {
        static void Main(string[] args) {
            Raylib.InitAudioDevice();
            Raylib.SetTraceLogLevel(TraceLogLevel.LOG_WARNING);
            Raylib.SetConfigFlags(ConfigFlags.FLAG_MSAA_4X_HINT | ConfigFlags.FLAG_WINDOW_RESIZABLE);
            Raylib.InitWindow((int)1600, (int)900, "Clone Dash");
            Raylib.SetExitKey(KeyboardKey.KEY_NULL);
            TextureSystem.LoadTextures();
            GameLogic.Startup();

            while (!Raylib.WindowShouldClose()) {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.BLACK);
                //game.Draw();
                Graphics.SetDrawColor(255, 255, 255);
                GameLogic.Tick();
                ConsoleSystem.Draw();
                Raylib.EndDrawing();
            }
        }
    }
}
