using Nucleus.Common.Extensions;
using Nucleus.Common.Graphics;
using Nucleus.Engine;
using Nucleus.Extensions;
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

	public record class FontEntry(string Path, string PathID)
	{
		public string GetPath() => Path;
		public string GetPathID() => PathID;
		public HashSet<int> RegisteredCodepointsHash = [];
		public List<int> RegisteredCodepoints = [];
		public List<int> BadCodepointsList = [];
		public int BadCodepoints;

		// Validate incoming codepoints (did the font fail to make codepoints) and register accordingly
		internal void ValidateCodepoints(in Font newFont) {
			for (int i = RegisteredCodepoints.Count - 1; i >= 0; i--) {
				int codepoint = RegisteredCodepoints[i];
				int glyphIndex = Raylib.GetGlyphIndex(newFont, codepoint);

				if (glyphIndex == 0 && codepoint != 0) {
					RegisteredCodepoints.RemoveAt(i);
					RegisteredCodepointsHash.Remove(codepoint);
					if (!BadCodepointsList.Contains(codepoint))
						BadCodepointsList.Add(codepoint);
					BadCodepoints++;
				}
			}
		}

		// Only return codepoints that are known to be OK
		internal Span<int> GetGoodOrUnknownCodepoints() {
			return CollectionsMarshal.AsSpan(RegisteredCodepoints);
		}

		// Only push codepoints that haven't been registered
		internal void PushCodepoints(Span<int> registeredCodepoints) {
			for (int i = 0; i < registeredCodepoints.Length; i++) {
				int c = registeredCodepoints[i];
				if (RegisteredCodepointsHash.Add(c))
					RegisteredCodepoints.Add(c);
			}
		}
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
		private readonly List<int> RegisteredCodepoints = new List<int>();

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
			bool dirty = false;
			for (int i = 0; i < chars.Length;) {
				Rune unicodeRune = chars.GetRuneAt(i);
				bool added = RegisteredCodepointsHash.Add(unicodeRune.Value);
				FullFontRefreshRequired |= added;
				if (added)
					RegisteredCodepoints.Add(unicodeRune.Value);
				i += unicodeRune.Utf16SequenceLength;
			}
			FullFontRefreshRequired |= dirty;
		}

		public FontManager(Dictionary<string, FontEntry> fonttable, string[]? codepoints = null) {
			codepoints = codepoints ?? [];
			FontNameToFilepath = [];
			foreach (var kvp in fonttable)
				AddFont(kvp.Key, kvp.Value);
			foreach (var codepointStr in codepoints)
				RegisterCodepoints(codepointStr);
		}
		
		public void AddFont(ReadOnlySpan<char> key, FontEntry entry) {
			FontNameToFilepath[symbols.AddString(key)] = entry;
		}

		public bool HasFont(ReadOnlySpan<char> key) {
			var sym = symbols.Find(key);
			return FontNameToFilepath.ContainsKey(sym);
		}

		DateTime lastCheckTimes;
		public void CleanUpFontsMarkedForDeath() {
			DateTime now = DateTime.UtcNow;
			if ((now - lastCheckTimes).TotalSeconds > 3) {
				// Garbage collect fonts. A font not used for ~3 seconds will be thrown away.
				foreach (var font in FontTable) {
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
				fontSize = Math.Clamp(fontSize, 7, 256);
				bool wasFirst = !FullFontRefreshRequired;
				if (!text.IsEmpty) {
					for (int i = 0; i < text.Length;) {
						Rune unicodeRune = text.GetRuneAt(i);
						bool added = RegisteredCodepointsHash.Add(unicodeRune.Value);
						FullFontRefreshRequired |= added;
						if (added)
							RegisteredCodepoints.Add(unicodeRune.Value);
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
					if (FontNameToFilepath.TryGetValue(key.FontSymbol, out var entry)) {
						var registeredCodepoints = RegisteredCodepoints.AsSpan();
						entry.PushCodepoints(registeredCodepoints);
						Font newFont = Filesystem.ReadFont(entry.PathID, entry.Path, fontSize, entry.GetGoodOrUnknownCodepoints());
						entry.ValidateCodepoints(in newFont);
						Raylib.GenTextureMipmaps(ref newFont.Texture);
						Raylib.SetTextureFilter(newFont.Texture, TextureFilter.Trilinear); // << CHANGE FOR 3D FONT DRAWING: REVIEW?
						state = FontTable[key] = new FontState();
						state.OwnFont(newFont);
						state.Key = key;
					}
					else {
						return GetFallbackFont(fontSize);
					}
				}

				state.Rehydrate();
				return state;
			}
		}

		readonly FontEntry fallbackEntry = new("NotoSans-Regular.ttf", "fonts");
		readonly Dictionary<int, FontState> fallbackFonts = [];
		private FontState GetFallbackFont(int fontSize) {
			if (!fallbackFonts.TryGetValue(fontSize, out FontState? state)) {
				var registeredCodepoints = RegisteredCodepoints.AsSpan();
				fallbackEntry.PushCodepoints(registeredCodepoints);
				Font newFont = Filesystem.ReadFont(fallbackEntry.PathID, fallbackEntry.Path, fontSize, fallbackEntry.GetGoodOrUnknownCodepoints());
				fallbackEntry.ValidateCodepoints(in newFont);
				Raylib.GenTextureMipmaps(ref newFont.Texture);
				Raylib.SetTextureFilter(newFont.Texture,
					TextureFilter.Trilinear); // << CHANGE FOR 3D FONT DRAWING: REVIEW?
				state = fallbackFonts[fontSize] = new FontState();
				state.OwnFont(newFont);
				state.Key = new(0, fontSize);
			}

			return state;
		}
	}
}