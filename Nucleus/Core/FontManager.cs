using Nucleus.Files;
using Raylib_cs;
using System.Runtime.InteropServices;

namespace Nucleus.Core
{
	public class FontManager
    {
        private readonly HashSet<int> RegisteredCodepointsHash = new HashSet<int>();

        public readonly Dictionary<string, FontEntry> FontNameToFilepath = new();
        private readonly Dictionary<string, Dictionary<int, Font>> fonttable = new();
        private bool AreFontsDirty = false;

        public void RegisterCodepoints(string charsIn) =>
            RegisteredCodepointsHash.UnionWith(charsIn.EnumerateRunes().Select((r) => r.Value));

        public FontManager(Dictionary<string, FontEntry> fonttable, string[]? codepoints = null) {
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
                    foreach (bool registerResult in text.EnumerateRunes().Select((r) => RegisteredCodepointsHash.Add(r.Value)))
                        AreFontsDirty |= registerResult;

                    if (AreFontsDirty && wasFirst) {
                        // run font unloading here
                        MainThread.RunASAP(() => {
                            foreach (Font f in fonttable.Values.SelectMany(kv => kv.Values, (_, v) => v))
                                Raylib.UnloadFont(f);
                                
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
					var entry = FontNameToFilepath[fontName];

					var newFont = Filesystem.ReadFont(entry.PathID, entry.Path, fontSize, RegisteredCodepointsHash.ToArray(), RegisteredCodepointsHash.Count);
					Raylib.GenTextureMipmaps(ref newFont.Texture);
					Raylib.SetTextureFilter(newFont.Texture, TextureFilter.TEXTURE_FILTER_TRILINEAR); // << CHANGE FOR 3D FONT DRAWING: REVIEW?
					f1[fontSize] = newFont;
                    font = f1[fontSize]; // how did I miss this
                }

                return font;
            }
        }
    }
}
