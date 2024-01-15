/*
    A *LOT* of this is subject to change. This is a prototype, and just a testbed of basic game functionality.
*/

using CloneDash.Systems;
using Raylib_cs;

namespace CloneDash
{
    internal class Program
    {
        static string TurnMDAssetBundlePathToMapName(string filepath) {
            string ret = Path.GetFileNameWithoutExtension(filepath).Replace("noteasset_assets_", "");
            return ret.Substring(0, ret.LastIndexOf("_"));
        }

        static void Main(string[] args) {
            MuseDashCompatibility.InitializeCompatibilityLayer();
            
            string requestedMap = "";
            string realMapName = "";

            while (true) {
                Console.Write("Type a Muse Dash map name: ");
                requestedMap = Console.ReadLine();

                string[] mapMatches = Array.FindAll(MuseDashCompatibility.StreamingFiles.ToArray(), x => x.Contains("noteasset") && x.ToLower().Contains(requestedMap.Replace(" ", "_")));
                
                if (mapMatches.Length > 0) {
                    Console.WriteLine("The following maps were found:");
                    for (int i = 0; i < mapMatches.Length; i++) {
                        Console.WriteLine($"    [{i + 1}]: {TurnMDAssetBundlePathToMapName(mapMatches[i])}");
                    }

                    while (true) {
                        int index = 0;
                        Console.WriteLine($"\r\nType the number of the map you wish to load");
                        if (int.TryParse(Console.ReadLine(), out index)) {
                            if (!DashMath.InRange(index, 1, mapMatches.Length))
                                Console.WriteLine($"Index out of range (needs a value between 1 -> {mapMatches.Length}");
                            else {
                                realMapName = mapMatches[index - 1];
                                break;
                            }
                        }
                        else
                            Console.WriteLine("Input is not a number, try again");
                    }

                    break;
                }
                else {
                    Console.WriteLine("No maps found, try again");
                }
            }

            Raylib.InitAudioDevice();
            Raylib.SetTraceLogLevel(TraceLogLevel.LOG_WARNING);
            Raylib.SetConfigFlags(ConfigFlags.FLAG_MSAA_4X_HINT | ConfigFlags.FLAG_WINDOW_RESIZABLE);
            Raylib.InitWindow((int)1600, (int)900, "Clone Dash");
            Raylib.SetExitKey(KeyboardKey.KEY_NULL);
            TextureSystem.LoadTextures();
            GameLogic.Startup(realMapName);


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
