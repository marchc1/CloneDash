using Nucleus.Common.Extensions;
using Nucleus.Common.Graphics;
using Nucleus.Files;
using Nucleus.ManagedMemory;
using Nucleus.Types;
using Nucleus.Util;

using Raylib_cs;
using System.Runtime.InteropServices;
using System.Text;

namespace Nucleus.Core
{
	public readonly record struct FontKey
	{
		readonly UtlSymId_t fontName;
		readonly int size;
		public FontKey(UtlSymId_t fontName, int size) {
			this.fontName = fontName;
			this.size = size;
		}
		public readonly UtlSymId_t FontSymbol => fontName;
		public readonly int Size => size;
	}

	public class FontState
	{
		public FontKey Key;
		bool Killed;
		Font Font;
		bool MarkedForDeath;
		DateTime LastUsed;

		public void OwnFont(in Font font) => Font = font;
		public void MarkForDeath() => MarkedForDeath = true;
		public void Rehydrate() => LastUsed = DateTime.UtcNow;
		public DateTime GetLastHydrate() => LastUsed;

		public bool IsMarkedForDeath() => MarkedForDeath;
		public ref Font GetFont() => ref Font;

		public void Destroy() {
			if (Killed) return;

			Raylib.UnloadFont(Font);

			Killed = true;
		}
	}

	public class FontManager
	{
		readonly UtlSymbolTableMT symbols = new();
		private readonly HashSet<int> RegisteredCodepointsHash = new HashSet<int>();

		public readonly Dictionary<UtlSymId_t, FontEntry> FontNameToFilepath = new();

		// A dictionary of live fonts.
		private readonly Dictionary<FontKey, FontState> FontTable = new();
		private readonly List<FontKey> FontsMarkedForDeath = new();

		public ulong GetUsedGPUBits() {
			ulong bits = 0;

			foreach (var kvp in FontTable) {
				FontState state = kvp.Value;

				if (!state.IsMarkedForDeath()) {
					ref Font f = ref state.GetFont();
					bits += (ulong)(f.Texture.Width * f.Texture.Height * 8 * f.Texture.Format.GetBitsPerPixel());
				}
			}

			return bits;
		}

		public void MarkFontForDeath(FontState font) {
			if (font.IsMarkedForDeath())
				return;

			font.MarkForDeath();
			FontsMarkedForDeath.Add(font.Key);
		}

		private bool FullFontRefreshRequired = false;
		private bool AreFontsMarkedForDeath => FontsMarkedForDeath.Count != 0;

		public void RegisterCodepoints(ReadOnlySpan<char> chars) {
			char ch;
			bool dirty = false;
			for (int i = 0; i < chars.Length;) {
				Rune unicodeRune = chars.GetRuneAt(i);
				FullFontRefreshRequired |= RegisteredCodepointsHash.Add(unicodeRune.Value);
				i += unicodeRune.Utf16SequenceLength;
			}
			FullFontRefreshRequired |= dirty;
		}

		public FontManager(Dictionary<string, FontEntry> fonttable, string[]? codepoints = null) {
			codepoints = codepoints ?? [];
			FontNameToFilepath = [];
			foreach (var kvp in fonttable)
				FontNameToFilepath[symbols.AddString(kvp.Key.AsSpan())] = kvp.Value;
			foreach (var codepointStr in codepoints)
				RegisterCodepoints(codepointStr);
		}

		DateTime lastCheckTimes;
		public void CleanUpFontsMarkedForDeath() {
			DateTime now = DateTime.UtcNow;
			if((now - lastCheckTimes).TotalSeconds > 3){
				// Garbage collect fonts. A font not used for ~3 seconds will be thrown away.
				foreach(var font in FontTable){
					if ((now - font.Value.GetLastHydrate()).TotalSeconds > 3)
						MarkFontForDeath(font.Value);
				}
			}

			if (!AreFontsMarkedForDeath)
				return;

			foreach (var kvp in FontsMarkedForDeath) {
				FontState state = FontTable[kvp];
				state.Destroy();
				FontTable.Remove(kvp);
			}

			FontsMarkedForDeath.Clear();
		}

		public FontState this[ReadOnlySpan<char> text, ReadOnlySpan<char> fontName, int fontSize] {
			get {
				// determine if fonts need to be cleaned due to new codepoints
				// is there a better way to do this?

				bool wasFirst = !FullFontRefreshRequired;
				if (!text.IsEmpty) {
					for (int i = 0; i < text.Length;) {
						Rune unicodeRune = text.GetRuneAt(i);
						FullFontRefreshRequired |= RegisteredCodepointsHash.Add(unicodeRune.Value);
						i += unicodeRune.Utf16SequenceLength;
					}

					if (FullFontRefreshRequired && wasFirst) {
						// We have to unload all fonts and reload them with new codepoints.
						// We will do that before the next frame to ensure nothing is stuck with invalid font textures.

						foreach (var kvp1 in FontTable)
							MarkFontForDeath(kvp1.Value);
					}
				}

				FontKey key = new FontKey(symbols.AddString(fontName), fontSize);

				if (!FontTable.TryGetValue(key, out FontState? state)) {
					var entry = FontNameToFilepath[key.FontSymbol];

					var newFont = Filesystem.ReadFont(entry.PathID, entry.Path, fontSize, RegisteredCodepointsHash.ToArray(), RegisteredCodepointsHash.Count);
					Raylib.GenTextureMipmaps(ref newFont.Texture);
					Raylib.SetTextureFilter(newFont.Texture, TextureFilter.TEXTURE_FILTER_TRILINEAR); // << CHANGE FOR 3D FONT DRAWING: REVIEW?
					state = FontTable[key] = new FontState();
					state.OwnFont(newFont);
					state.Key = key;
				}
				state.Rehydrate();
				return state;
			}
		}
	}
}
