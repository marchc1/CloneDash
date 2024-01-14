using CloneDash.Game.Sheets;

namespace CloneDash
{
    public static class GameLogic
    {
        static DashGame? game;

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
        
        public static void Startup() {

            game = DashGame.LoadSheet(DashSheet.LoadFromMuseDash("the_89s_momentum_map3"));
            game.AutoPlayer.Enabled = true;
            game.Music.Volume = 0.15f;
        }
        public static void Tick() {
            if (game != null) {
                game.Tick();
                game.Draw();
            }
        }
    }
}
