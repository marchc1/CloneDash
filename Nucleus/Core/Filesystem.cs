using System.Text;

namespace Nucleus.Core
{
    public class FileIO
    {
        public enum Method
        {
            Read,
            Write,
            ReadWrite
        }

        public string Filepath { get; private set; }
        private FileStream _stream;
        public FileIO(string filepath, FileMode mode = FileMode.Open, FileAccess access = FileAccess.ReadWrite) {
            this.Filepath = filepath;
            _stream = File.Open(Filepath, mode, access);
        }
    }
    public static class Filesystem
    {
        public static Dictionary<string, List<string>> Path { get; } = new() {
            { "game", [$"{AppContext.BaseDirectory}"] },
            { "assets", [$"{AppContext.BaseDirectory}assets"] },

            { "audio", [$"{AppContext.BaseDirectory}assets/audio"] },
            { "fonts", [$"{AppContext.BaseDirectory}assets/fonts"] },
            { "images", [$"{AppContext.BaseDirectory}assets/images"] },
            { "models", [$"{AppContext.BaseDirectory}assets/models"] },
            { "shaders", [$"{AppContext.BaseDirectory}assets/shaders"] },
        };

        public static void AddPath(string pathIdentifier, string absolutePath) {
            Directory.CreateDirectory(absolutePath);

            if (!Path.ContainsKey(pathIdentifier))
                Path[pathIdentifier] = [];
            if (Path[pathIdentifier].Contains(absolutePath))
                return;

            Path[pathIdentifier].Insert(0, absolutePath);

        }

        private static bool __firstResolve = true;
        public static string Resolve(string localFilepath, string path = null, bool catchMissing = true) {
            localFilepath = localFilepath.Replace("\\", "/");
            if (__firstResolve) {
                foreach (var kvp in Path)
                    foreach (var dir in kvp.Value)
                        Directory.CreateDirectory(dir);

                __firstResolve = false;
            }
            if (path == null) {
                foreach (var KVP in Path)
                    foreach (var dir in KVP.Value)
                        if (File.Exists(System.IO.Path.Combine(dir, localFilepath)))
                            return System.IO.Path.Combine(dir, localFilepath);

                return Path[localFilepath][0];
                //throw new FileNotFoundException($"File '{localFilepath}' not found anywhere in the Filesystem's path.");
            }
            else {
                path = path.ToLower();
                if (!Path.ContainsKey(path))
                    throw new NotImplementedException($"Mode '{path}' not found in Filesystem.Path");

                foreach (var folderPath in Path[path]) {
                    string realFilepath = System.IO.Path.Combine(folderPath, localFilepath);

                    if (!File.Exists(realFilepath) && catchMissing)
                        continue;

                    return realFilepath;
                }
                throw new FileNotFoundException($"File '{localFilepath}' not found in path '{path}'");
                //Logs.Debug($"filesystem resolve: [{path}]/{localFilepath} = {realFilepath}");
            }
        }

        public static FileIO Open(string localFilepath, string path = null, FileMode mode = FileMode.OpenOrCreate, FileAccess access = FileAccess.ReadWrite) => new FileIO(Resolve(localFilepath, path), mode, access);

        public static string ReadAllText(string localFilepath, string path = null, Encoding encoding = null) => File.ReadAllText(Resolve(localFilepath, path), encoding == null ? Encoding.UTF8 : encoding);
        public static string[] ReadAllLines(string localFilepath, string path = null, Encoding encoding = null) => File.ReadAllLines(Resolve(localFilepath, path), encoding == null ? Encoding.UTF8 : encoding);
        public static byte[] ReadAllBytes(string localFilepath, string path = null) => File.ReadAllBytes(Resolve(localFilepath, path));

        public static void WriteAllText(string localFilepath, string path, string data, Encoding encoding = null) => File.WriteAllText(Resolve(localFilepath, path), data, encoding == null ? Encoding.UTF8 : encoding);
        public static void WriteAllLines(string localFilepath, string path, string[] data, Encoding encoding = null) => File.WriteAllLines(Resolve(localFilepath, path), data, encoding == null ? Encoding.UTF8 : encoding);
        public static void WriteAllBytes(string localFilepath, string path, byte[] data) => File.WriteAllBytes(Resolve(localFilepath, path), data);
    }
}
