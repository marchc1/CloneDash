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
        private bool AreFontsDirty = false;

        public bool RegisterCodepoint(int c) {
            if (RegisteredCodepointsHash.Add(c)) {
                RegisteredCodepoints = RegisteredCodepointsHash.ToArray();
                return true;
            }

            return false;
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
        public Font this[string text, string fontName, int fontSize] {
            get {
                // determine if fonts need to be cleaned due to new codepoints
                // is there a better way to do this?

                bool wasFirst = !AreFontsDirty;
                if (text != null) {
                    for (int i = 0; i < text.Length; i += char.IsSurrogatePair(text, i) ? 2 : 1) 
                        AreFontsDirty |= RegisterCodepoint(char.ConvertToUtf32(text, i));
                    
                    if (AreFontsDirty && wasFirst) {
                        // run font unloading here
                        MainThread.RunASAP(() => {
                            foreach(var fsDict in fonttable) 
                                foreach(var fs in fsDict.Value) 
                                    Raylib.UnloadFont(fs.Value);

                            fonttable.Clear();
                            AreFontsDirty = false;
                        }, ThreadExecutionTime.BeforeFrame);
                    }
                }

                Font font;
                Dictionary<int, Font> f1;
                if (!fonttable.TryGetValue(fontName, out f1)) {
                    fonttable[fontName] = new();
                    f1 = fonttable[fontName];
                }

                if (!f1.TryGetValue(fontSize, out font)) {

                    f1[fontSize] = Raylib.LoadFontEx(FontNameToFilepath[fontName], fontSize, RegisteredCodepoints, RegisteredCodepoints.Length);
                    font = f1[fontSize]; // how did I miss this
                }

                return font;
            }
        }
    }
}
