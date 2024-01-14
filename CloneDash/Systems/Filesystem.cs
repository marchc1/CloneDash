namespace CloneDash
{
    public static class Filesystem
    {
        private static string MakeIfDirectoryDoesntExist(string dir) {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        public static string AppDirectory { get; private set; } = AppContext.BaseDirectory;
        public static string Assets { get; private set; } = MakeIfDirectoryDoesntExist(AppDirectory + "assets\\");
        public static string Audio { get; private set; } = MakeIfDirectoryDoesntExist(Assets + "audio\\");
        public static string Images { get; private set; } = MakeIfDirectoryDoesntExist(Assets + "images\\");
        public static string Shaders { get; private set; } = MakeIfDirectoryDoesntExist(Assets + "shaders\\");
        public static string Sheets { get; private set; } = MakeIfDirectoryDoesntExist(AppDirectory + "sheets\\");
    }
}
