using Nucleus.Core;
using Raylib_cs;

namespace CloneDash.Systems
{
    public class Texture {
        public Texture2D Texture2D { get; private set; }

        public Texture(string texturePath, string filesystemPath = "images") {
            Texture2D = Raylib.LoadTexture(Filesystem.Resolve(texturePath, filesystemPath));
        }
        ~Texture() {
            Raylib.UnloadTexture(Texture2D);
        }
    }
    public static class TextureSystem
    {
        public static Dictionary<string, Texture2D> Cache = new();

        private static Texture2D __loadTexture(string path) {
            if(Cache.ContainsKey(path)) 
                return Cache[path];

            Cache[path] = Raylib.LoadTexture(path);
            return Cache[path];
        }
        public static Texture2D LoadTexture(string localizedPath, string path = "images") => __loadTexture(Filesystem.Resolve(localizedPath, path));

        public static void Unload() {
            foreach(var kvp in Cache) {
                Raylib.UnloadTexture(kvp.Value);
            }

            Cache.Clear();
        }
    }
}
