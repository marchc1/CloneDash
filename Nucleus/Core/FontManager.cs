using Nucleus.Common.Extensions;
using Nucleus.Common.Graphics;
using Nucleus.Common.Images;
using Nucleus.Common.Types;
using Nucleus.Extensions;
using Nucleus.Files;
using Nucleus.Types;
using Nucleus.Util;

using Raylib_cs;
using System.Numerics;
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

		private byte[]? data;

		internal byte[] GetData() => data ??= filesystem.ReadAllBytes(PathID, Path) ?? throw new FileNotFoundException($"Cannot find '{Path}' in '{PathID}'!");
	}

	public struct FontGlyph
	{
		public int Page;
		public Rectangle Rec;
		public int OffsetX;
		public int OffsetY;
		public int AdvanceX;
	}

	public class FontState
	{
		public const int GlyphPadding = 4;
		public const int LineSpacing = 2;

		private class Page
		{
			public Texture2D Texture;
			public int ShelfX;
			public int ShelfY;
			public int ShelfHeight;
			public bool Dirty;
		}

		private static int nextId;

		public FontKey Key;
		public readonly int Id = Interlocked.Increment(ref nextId);
		public readonly int Size;
		private readonly FontEntry Entry;
		private readonly int PageSize;
		private readonly List<Page> Pages = [];
		private readonly List<FontGlyph> Glyphs = [];
		private readonly Dictionary<int, int> GlyphIndices = [];
		private int EmptyGlyph = -1;
		private bool Killed;
		private bool MarkedForDeath;
		private bool NeverKillThisFont;
		private DateTime LastUsed;

		private static readonly List<int> missingCodepoints = [];
		private static readonly HashSet<int> missingCodepointsHash = [];

		public FontState(FontEntry entry, int size) {
			Entry = entry;
			Size = size;
			PageSize = (int)Math.Clamp(BitOperations.RoundUpToPowerOf2((uint)((size + GlyphPadding * 2) * 8)), 256, 2048);
		}

		public void MarkForDeath() => MarkedForDeath = true;

		public void Rehydrate() => LastUsed = DateTime.UtcNow;

		public DateTime GetLastHydrate() => LastUsed;

		public bool IsMarkedForDeath() => MarkedForDeath;

		public int PageCount => Pages.Count;

		public Texture2D GetPageTexture(int page) => Pages[page].Texture;

		public void Destroy() {
			if (Killed) return;

			foreach (var page in Pages)
				Raylib.UnloadTexture(page.Texture);
			Pages.Clear();

			Killed = true;
		}

		public void MarkFontAsInvincible() => NeverKillThisFont = true;
		public bool IsFontInvincible() => NeverKillThisFont;

		internal ulong GetUsedGPUBits() {
			ulong bits = 0;
			foreach (var page in Pages)
				bits += (ulong)(page.Texture.Width * page.Texture.Height * 8 * page.Texture.Format.GetBitsPerPixel());
			return bits;
		}

		static int NextCodepoint(ReadOnlySpan<char> text, ref int i) {
			if (i >= text.Length)
				return 0;

			Rune.DecodeFromUtf16(text[i..], out Rune rune, out int used);
			i += Math.Max(used, 1);
			return rune.Value;
		}

		public FontGlyph GetGlyph(int codepoint) {
			if (!GlyphIndices.TryGetValue(codepoint, out int index)) {
				AddCodepoints([codepoint]);
				index = GlyphIndices[codepoint];
			}
			return Glyphs[index];
		}

		public void AddGlyphs(ReadOnlySpan<char> text) {
			missingCodepoints.Clear();
			missingCodepointsHash.Clear();
			for (int i = 0; i < text.Length;) {
				int codepoint = NextCodepoint(text, ref i);
				if (codepoint == 0)
					break;
				if (codepoint != '\n' && !GlyphIndices.ContainsKey(codepoint) && missingCodepointsHash.Add(codepoint))
					missingCodepoints.Add(codepoint);
			}

			if (missingCodepoints.Count > 0)
				AddCodepoints(missingCodepoints.ToArray());
		}

		private unsafe void AddCodepoints(ReadOnlySpan<int> codepoints) {
			byte[] data = Entry.GetData();
			int found = 0;
			GlyphInfo* infos;
			fixed (byte* fileData = data)
			fixed (int* codepointsPtr = codepoints)
				infos = Raylib.LoadFontData(fileData, data.Length, Size, codepointsPtr, codepoints.Length, FontType.FONT_DEFAULT, &found);

			bool fallbackNeeded = false;
			for (int i = 0, k = 0; i < codepoints.Length; i++) {
				int codepoint = codepoints[i];
				if (infos != null && k < found && infos[k].Value == codepoint) {
					GlyphIndices[codepoint] = Glyphs.Count;
					Glyphs.Add(Pack(in infos[k]));
					k++;
				}
				else
					fallbackNeeded = true;
			}

			if (infos != null)
				Raylib.UnloadFontData(infos, found);

			if (fallbackNeeded) {
				foreach (int codepoint in codepoints) {
					if (!GlyphIndices.ContainsKey(codepoint))
						GlyphIndices[codepoint] = codepoint == '?' ? GetEmptyGlyph() : FallbackGlyph();
				}
			}

			foreach (var page in Pages) {
				if (!page.Dirty)
					continue;
				Raylib.GenTextureMipmaps(ref page.Texture);
				page.Dirty = false;
			}
		}

		private int FallbackGlyph() {
			if (!GlyphIndices.TryGetValue('?', out int index)) {
				AddCodepoints([(int)'?']);
				index = GlyphIndices['?'];
			}
			return index;
		}

		private int GetEmptyGlyph() {
			if (EmptyGlyph < 0) {
				EmptyGlyph = Glyphs.Count;
				Glyphs.Add(new FontGlyph() { Page = -1 });
			}
			return EmptyGlyph;
		}

		private unsafe FontGlyph Pack(in GlyphInfo info) {
			int width = info.Image.Width;
			int height = info.Image.Height;
			FontGlyph glyph = new() {
				Page = -1,
				Rec = new Rectangle(0, 0, width, height),
				OffsetX = info.OffsetX,
				OffsetY = info.OffsetY,
				AdvanceX = info.AdvanceX
			};

			if (info.Image.Data == null || width <= 0 || height <= 0 || info.Value == 0x20 || info.Value == 0x3000)
				return glyph;

			int cellWidth = width + GlyphPadding * 2;
			int cellHeight = height + GlyphPadding * 2;

			Page page = Pages.Count > 0 ? Pages[^1] : AddPage();
			if (page.ShelfX + cellWidth > PageSize) {
				page.ShelfY += page.ShelfHeight;
				page.ShelfX = 0;
				page.ShelfHeight = 0;
			}
			if (page.ShelfY + cellHeight > PageSize)
				page = AddPage();

			int x = page.ShelfX + GlyphPadding;
			int y = page.ShelfY + GlyphPadding;
			page.ShelfX += cellWidth;
			page.ShelfHeight = Math.Max(page.ShelfHeight, cellHeight);

			byte* gray = (byte*)info.Image.Data;
			int pixels = width * height;
			byte[] grayAlpha = System.Buffers.ArrayPool<byte>.Shared.Rent(pixels * 2);
			for (int i = 0; i < pixels; i++) {
				grayAlpha[i * 2] = 255;
				grayAlpha[i * 2 + 1] = gray[i];
			}
			fixed (byte* upload = grayAlpha)
				Raylib.UpdateTextureRec(page.Texture, new Rectangle(x, y, width, height), upload);
			System.Buffers.ArrayPool<byte>.Shared.Return(grayAlpha);

			page.Dirty = true;
			glyph.Page = Pages.Count - 1;
			glyph.Rec = new Rectangle(x, y, width, height);
			return glyph;
		}

		private unsafe Page AddPage() {
			int pixels = PageSize * PageSize;
			ushort* data = (ushort*)NativeMemory.Alloc((nuint)(pixels * 2));
			new Span<ushort>(data, pixels).Fill(BitConverter.IsLittleEndian ? (ushort)0x00FF : (ushort)0xFF00);

			Image image = new() {
				Data = data,
				Width = PageSize,
				Height = PageSize,
				Mipmaps = 1,
				Format = ImageFormat.GrayAlpha
			};
			Texture2D texture = Raylib.LoadTextureFromImage(image);
			NativeMemory.Free(data);

			Raylib.GenTextureMipmaps(ref texture);
			Raylib.SetTextureFilter(texture, TextureFilter.Trilinear); // << CHANGE FOR 3D FONT DRAWING: REVIEW?

			Page page = new() { Texture = texture };
			Pages.Add(page);
			return page;
		}

		public Vector2F MeasureText(ReadOnlySpan<char> text, float fontSize, float spacing) {
			if (text.IsEmpty || text[0] == '\0')
				return default;

			int tempByteCounter = 0;
			int byteCounter = 0;

			float textWidth = 0.0f;
			float tempTextWidth = 0.0f;

			float textHeight = fontSize;
			float scaleFactor = fontSize / (float)Size;

			for (int i = 0; i < text.Length;) {
				int letter = NextCodepoint(text, ref i);
				if (letter == 0)
					break;

				byteCounter++;

				if (letter != '\n') {
					FontGlyph glyph = GetGlyph(letter);
					if (glyph.AdvanceX > 0) textWidth += glyph.AdvanceX;
					else textWidth += (glyph.Rec.Width + glyph.OffsetX);
				}
				else {
					if (tempTextWidth < textWidth) tempTextWidth = textWidth;
					byteCounter = 0;
					textWidth = 0;
					textHeight += (fontSize + LineSpacing);
				}

				if (tempByteCounter < byteCounter) tempByteCounter = byteCounter;
			}

			if (tempTextWidth < textWidth) tempTextWidth = textWidth;

			return new(tempTextWidth * scaleFactor + (float)((tempByteCounter - 1) * spacing), textHeight);
		}

		public void DrawText(ReadOnlySpan<char> text, Vector2 position, float fontSize, float spacing, Color tint) {
			float textOffsetY = 0;
			float textOffsetX = 0.0f;

			float scaleFactor = fontSize / Size;

			for (int i = 0; i < text.Length;) {
				int codepoint = NextCodepoint(text, ref i);
				if (codepoint == 0)
					break;

				if (codepoint == '\n') {
					textOffsetY += (fontSize + LineSpacing);
					textOffsetX = 0.0f;
					continue;
				}

				FontGlyph glyph = GetGlyph(codepoint);
				if ((codepoint != ' ') && (codepoint != '\t') && glyph.Page >= 0) {
					float x = position.X + textOffsetX;
					float y = position.Y + textOffsetY;
					Rectangle dstRec = new(x + glyph.OffsetX * scaleFactor - (float)GlyphPadding * scaleFactor,
										y + glyph.OffsetY * scaleFactor - (float)GlyphPadding * scaleFactor,
										(glyph.Rec.Width + 2.0f * GlyphPadding) * scaleFactor,
										(glyph.Rec.Height + 2.0f * GlyphPadding) * scaleFactor);
					Rectangle srcRec = new(glyph.Rec.X - (float)GlyphPadding, glyph.Rec.Y - (float)GlyphPadding,
										glyph.Rec.Width + 2.0f * GlyphPadding, glyph.Rec.Height + 2.0f * GlyphPadding);
					Raylib.DrawTexturePro(Pages[glyph.Page].Texture, srcRec, dstRec, Vector2.Zero, 0.0f, tint);
				}

				if (glyph.AdvanceX == 0) textOffsetX += ((float)glyph.Rec.Width * scaleFactor + spacing);
				else textOffsetX += ((float)glyph.AdvanceX * scaleFactor + spacing);
			}
		}
	}

	public class FontManager
	{
		private readonly UtlSymbolTableMT symbols = new();

		public readonly Dictionary<UtlSymId_t, FontEntry> FontNameToFilepath = new();

		// A dictionary of live fonts.
		private readonly Dictionary<FontKey, FontState> FontTable = new();

		private readonly List<FontKey> FontsMarkedForDeath = new();

		public ulong GetUsedGPUBits() {
			ulong bits = 0;

			foreach (var kvp in FontTable) {
				FontState state = kvp.Value;

				if (!state.IsMarkedForDeath())
					bits += state.GetUsedGPUBits();
			}

			return bits;
		}

		public void MarkFontForDeath(FontState font) {
			if (font.IsFontInvincible())
				return;

			if (font.IsMarkedForDeath())
				return;

			font.MarkForDeath();
			FontsMarkedForDeath.Add(font.Key);
		}

		private bool AreFontsMarkedForDeath => FontsMarkedForDeath.Count != 0;

		public FontManager(Dictionary<string, FontEntry> fonttable) {
			FontNameToFilepath = [];
			foreach (var kvp in fonttable)
				AddFont(kvp.Key, kvp.Value);
		}

		public void AddFont(ReadOnlySpan<char> key, FontEntry entry) {
			FontNameToFilepath[symbols.AddString(key)] = entry;
		}

		public bool HasFont(ReadOnlySpan<char> key) {
			var sym = symbols.Find(key);
			return FontNameToFilepath.ContainsKey(sym);
		}

		private DateTime lastCheckTimes;

		public void CleanUpFontsMarkedForDeath() {
			DateTime now = DateTime.UtcNow;
			if ((now - lastCheckTimes).TotalSeconds > 3) {
				lastCheckTimes = now;
				// Garbage collect fonts. A font not used for ~30 seconds will be thrown away.
				foreach (var font in FontTable) {
					if ((now - font.Value.GetLastHydrate()).TotalSeconds > 30)
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
				fontSize = Math.Clamp(fontSize, 7, 256);
				FontKey key = new FontKey(symbols.AddString(fontName), fontSize);

				if (!FontTable.TryGetValue(key, out FontState? state)) {
					if (!FontNameToFilepath.TryGetValue(key.FontSymbol, out var entry)) {
						FontState fallback = GetFallbackFont(fontSize);
						fallback.AddGlyphs(text);
						return fallback;
					}

					state = FontTable[key] = new FontState(entry, fontSize);
					state.Key = key;
				}

				state.AddGlyphs(text);
				state.Rehydrate();
				return state;
			}
		}

		private readonly FontEntry fallbackEntry = new("NotoSans-Regular.ttf", "fonts");
		private readonly Dictionary<int, FontState> fallbackFonts = [];

		private FontState GetFallbackFont(int fontSize) {
			if (!fallbackFonts.TryGetValue(fontSize, out FontState? state)) {
				state = fallbackFonts[fontSize] = new FontState(fallbackEntry, fontSize);
				state.Key = new(0, fontSize);
			}

			return state;
		}
	}
}
