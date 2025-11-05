using Nucleus.Files;
using Nucleus.Types;
using Nucleus.Util;

using Raylib_cs;
using System.Runtime.InteropServices;
using System.Text;

namespace Nucleus.Core
{
	public class FontManager
    {
        private readonly HashSet<int> RegisteredCodepointsHash = new HashSet<int>();

        public readonly Dictionary<UtlSymId_t, FontEntry> FontNameToFilepath = new();

		// A dictionary of live fonts.
        private readonly Dictionary<UtlSymId_t, Dictionary<int, Font>> FontTable = new();
		// A dictionary of fonts marked to be killed.
        private readonly Dictionary<UtlSymId_t, Dictionary<int, Font>> FontsMarkedForDeath = new();

        private bool AreFontsDirty = false;

        public void RegisterCodepoints(string charsIn) =>
            RegisteredCodepointsHash.UnionWith(charsIn.EnumerateRunes().Select((r) => r.Value));

        public FontManager(Dictionary<string, FontEntry> fonttable, string[]? codepoints = null) {
            codepoints = codepoints ?? [];
            FontNameToFilepath = [];
			foreach (var kvp in fonttable) 
				FontNameToFilepath[kvp.Key.AsSpan().Hash()] = kvp.Value;
            foreach (var codepointStr in codepoints)
                RegisterCodepoints(codepointStr);
        }
        public Font this[ReadOnlySpan<char> text, ReadOnlySpan<char> fontName, int fontSize] {
            get {
                // determine if fonts need to be cleaned due to new codepoints
                // is there a better way to do this?

                bool wasFirst = !AreFontsDirty;
                if (text != null) {
					for (int i = 0; i < text.Length;) {
						Rune unicodeRune = text.GetRuneAt(i);
						AreFontsDirty |= RegisteredCodepointsHash.Add(unicodeRune.Value);
						i += unicodeRune.Utf16SequenceLength;
					}

					if (AreFontsDirty && wasFirst) {
						// We have to unload all fonts and reload them with new codepoints.
						// We will do that before the next frame to ensure nothing is stuck with invalid font textures.

						foreach (var kvp1 in FontTable)
							FontsMarkedForDeath[kvp1.Key] = kvp1.Value;

						MainThread.RunASAP(() => {
							if (!AreFontsDirty)
								return;

							foreach(var kvp in FontsMarkedForDeath) {
								foreach (var fontPair in kvp.Value)
									Raylib.UnloadFont(fontPair.Value);
								FontTable.Remove(kvp.Key);
							}

							FontsMarkedForDeath.Clear();
							AreFontsDirty = false;
                        }, ThreadExecutionTime.BeforeFrame);
                    }
                }

				UtlSymId_t fontHash = fontName.Hash();
				if (!FontTable.TryGetValue(fontHash, out Dictionary<int, Font>? f1)) {
                    FontTable[fontHash] = new();
                    f1 = FontTable[fontHash];
                }

                if (!f1.TryGetValue(fontSize, out Font font)) {
					var entry = FontNameToFilepath[fontHash];

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
