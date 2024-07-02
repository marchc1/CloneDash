/*
    A *LOT* of this is subject to change. This is a prototype, and just a testbed of basic game functionality.
*/

using CloneDash.Game;
using CloneDash.Game.Sheets;
using CloneDash.Systems;
using Nucleus;
using Nucleus.Core;
using Raylib_cs;

namespace CloneDash
{

    // I've been testing with these levels:
    /*
        8bit_adventurer_map3
        bass_telekinesis_map3
        can_i_friend_you_on_bassbook_lol_map3
        night_of_knights_map3
        hg_makaizou_polyvinyl_shounen_map3

        kyouki_ranbu_map3
        ourovoros_map3
        mujinku_vacuum_track_add8e6_map3
        the_89s_momentum_map3
    */

    internal class Program
    {
        static void Main(string[] args) {
            MuseDashCompatibility.InitializeCompatibilityLayer();

            EngineCore.Initialize(1600, 900, "Clone Dash");
            EngineCore.GameInfo = new() {
                GameName = "Clone Dash"
            };

            EngineCore.LoadLevel(new CD_MainMenu());
            // need a better way to implement custom scenes
            Filesystem.AddPath("audio", Filesystem.Resolve("game") + "assets\\scenes\\default\\audio\\");
            Filesystem.AddPath("models", Filesystem.Resolve("game") + "assets\\scenes\\default\\models\\");
            Filesystem.AddPath("scripts", Filesystem.Resolve("game") + "assets\\scenes\\default\\scripts\\");

            EngineCore.Start();
        }
    }
}
