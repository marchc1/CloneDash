using Raylib_cs;
using System.Runtime.InteropServices;

namespace Nucleus.Core
{
    public class FontManager
    {
        private int[] RegisteredCodepoints = [];
        private HashSet<int> RegisteredCodepointsHash = new HashSet<int>();

        public Dictionary<string, string> FontNameToFilepath = new();
        private Dictionary<string, Dictionary<int, Font>> fonttable = new();

        public void RegisterCodepoint(int c) {
            if (RegisteredCodepointsHash.Add(c)) {
                RegisteredCodepoints = RegisteredCodepointsHash.ToArray();
            }
        }

        public void RegisterCodepoints(string charsIn) {
            for (int i = 0; i < charsIn.Length; i += char.IsSurrogatePair(charsIn, i) ? 2 : 1) {
                RegisterCodepoint(char.ConvertToUtf32(charsIn, i));
            }
        }

        public FontManager(Dictionary<string, string> fonttable, string[]? codepoints = null) {
            codepoints = codepoints ?? [];
            FontNameToFilepath = fonttable;
            foreach (var codepointStr in codepoints)
                RegisterCodepoints(codepointStr);
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

                    f1[size] = Raylib.LoadFontEx(FontNameToFilepath[name], size, RegisteredCodepoints, RegisteredCodepoints.Length);
                    font = f1[size]; // how did I miss this
                }

                return font;
            }
        }
    }
}
