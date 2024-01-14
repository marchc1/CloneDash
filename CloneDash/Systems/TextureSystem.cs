using Raylib_cs;

namespace CloneDash.Systems
{
    public static class TextureSystem
    {
        public static Texture2D fightable_arrow;
        public static Texture2D fightable_bag;
        public static Texture2D fightable_ball;
        public static Texture2D fightable_beam;
        public static Texture2D fightable_double;
        public static Texture2D fightable_gear;
        public static Texture2D fightable_ghost;
        public static Texture2D fightable_hammer;

        public static Texture2D boss_main;
        public static Texture2D boss_shooter;
        public static Texture2D boss_projectile;

        public static Texture2D pickup_heart;
        public static Texture2D pickup_note;

        public static Texture2D texture_warning;
        public static Texture2D texture_paused;

        public static void LoadTextures()
        {
            fightable_arrow = Raylib.LoadTexture($"{Filesystem.Images}fightable_arrow.png");
            fightable_bag = Raylib.LoadTexture($"{Filesystem.Images}fightable_bag.png");
            fightable_ball = Raylib.LoadTexture($"{Filesystem.Images}fightable_ball.png");
            fightable_beam = Raylib.LoadTexture($"{Filesystem.Images}fightable_beam.png");
            fightable_double = Raylib.LoadTexture($"{Filesystem.Images}fightable_double.png");
            fightable_gear = Raylib.LoadTexture($"{Filesystem.Images}fightable_gear.png");
            fightable_ghost = Raylib.LoadTexture($"{Filesystem.Images}fightable_ghost.png");
            fightable_hammer = Raylib.LoadTexture($"{Filesystem.Images}fightable_hammer.png");

            boss_main = Raylib.LoadTexture($"{Filesystem.Images}boss_main.png");
            boss_shooter = Raylib.LoadTexture($"{Filesystem.Images}boss_shooter.png");
            boss_projectile = Raylib.LoadTexture($"{Filesystem.Images}boss_projectile.png");

            pickup_heart = Raylib.LoadTexture($"{Filesystem.Images}pickup_heart.png");
            pickup_note = Raylib.LoadTexture($"{Filesystem.Images}pickup_note.png");

            texture_warning = Raylib.LoadTexture($"{Filesystem.Images}warning.png");
            texture_paused = Raylib.LoadTexture($"{Filesystem.Images}pause.png");
        }
    }
}
