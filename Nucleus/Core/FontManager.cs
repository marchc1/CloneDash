using Raylib_cs;

namespace Nucleus.Core
{
    public class FontManager
    {
        public Dictionary<string, string> FontNameToFilepath = new();
        private Dictionary<string, Dictionary<int, Font>> fonttable = new();

        public FontManager(Dictionary<string, string> fonttable) {
            FontNameToFilepath = fonttable;
        }

        public Font this[string name, int size] {
            get {
                Font font;
                Dictionary<int, Font> f1;
                if (!fonttable.TryGetValue(name, out f1)) {
                    fonttable[name] = new();
                    f1 = fonttable[name];
                }

                if (!f1.TryGetValue(size, out font)) {
                    f1[size] = Raylib.LoadFontEx(FontNameToFilepath[name], size, null, 0);
                    font = f1[size]; // how did I miss this
                }

                return font;
            }
        }
    }
}
