using Nucleus.Common.Graphics;
using Nucleus.Common.Images;
using Nucleus.Common.Types;

namespace Nucleus.Models;

public class AtlasPackSettings
{
	// Regions
	public bool StripWhitespaceX = true, StripWhitespaceY = true;
	public bool Rotation = true;
	public bool Alias = true;
	public bool IgnoreBlankImages = true;
	public int AlphaThreshold = 3;

	// Region padding
	public int PaddingX = 2, PaddingY = 2;
	public bool EdgePadding = true;
	public bool DuplicatePadding;

	// Pages
	public int MinWidth = 16, MinHeight = 16;
	public int MaxWidth = 2048, MaxHeight = 2048;
	public bool PowerOfTwo;
	public bool DivisibleBy4;
	public bool Square;

	// Output
	public bool PremultiplyAlpha = true;
	public bool Bleed;

	// Runtime
	public TextureFilterFunc FilterMin = TextureFilterFunc.Linear, FilterMag = TextureFilterFunc.Linear;
	public TextureWrap Wrap = TextureWrap.Clamp;

	// Options
	public bool Fast;

	public AtlasPackSettings Clone() => (AtlasPackSettings)MemberwiseClone();
}

public class PackedAtlasPage
{
	public string Name = "";
	public int Width, Height;
	/// <summary>
	/// RGBA8, row major
	/// </summary>
	public byte[] Pixels = [];
}

public class PackedAtlasRegion
{
	public string Name = "";
	public int Page;
	/// <summary>
	/// Rectangle on the page, width and height swapped when rotated
	/// </summary>
	public int X, Y;
	public int Width, Height;
	/// <summary>
	/// 90 = stored turned counter-clockwise
	/// </summary>
	public int Rotation;
	/// <summary>
	/// Whitespace stripped from the left and top
	/// </summary>
	public int OffsetX, OffsetY;
	public int OriginalWidth, OriginalHeight;
}

public class PackedAtlas
{
	public AtlasPackSettings Settings = new();
	public List<PackedAtlasPage> Pages = [];
	public List<PackedAtlasRegion> Regions = [];
	public List<string> Warnings = [];
}

/// <summary>
/// Packs images onto as few pages as fit the settings (MaxRects).
/// </summary>
public static class AtlasPacker
{
	class Item
	{
		public string Name = "";
		public int OrigW, OrigH;
		public int OffX, OffY;
		public int W, H;
		public byte[] Pixels = [];
		public Item? AliasOf;
		public bool Blank;
		// placement
		public int Page = -1;
		public int X, Y;
		public bool Rotated;
	}

	readonly record struct Rect(int X, int Y, int W, int H)
	{
		public int Right => X + W;
		public int Bottom => Y + H;
		public bool Contains(Rect other) => other.X >= X && other.Y >= Y && other.Right <= Right && other.Bottom <= Bottom;
	}

	enum Heuristic { ShortSide, LongSide, Area, BottomLeft }

	/// <summary>
	/// One MaxRects bin
	/// </summary>
	class Bin(int width, int height)
	{
		readonly List<Rect> free = [new(0, 0, width, height)];
		public int UsedW, UsedH;

		public bool Find(int itemWidth, int itemHeight, bool rotate, Heuristic heuristic, out Rect best, out bool rotated, out long score1, out long score2) {
			best = default;
			rotated = false;
			score1 = score2 = long.MaxValue;
			bool found = false;
			foreach (var freeRect in free) {
				for (int pass = 0; pass < (rotate && itemWidth != itemHeight ? 2 : 1); pass++) {
					int placedWidth = pass == 0 ? itemWidth : itemHeight;
					int placedHeight = pass == 0 ? itemHeight : itemWidth;
					if (placedWidth > freeRect.W || placedHeight > freeRect.H) continue;
					Score(freeRect, placedWidth, placedHeight, heuristic, out long primary, out long secondary);
					if (primary < score1 || (primary == score1 && secondary < score2)) {
						best = new(freeRect.X, freeRect.Y, placedWidth, placedHeight);
						rotated = pass == 1;
						score1 = primary;
						score2 = secondary;
						found = true;
					}
				}
			}
			return found;
		}

		private static void Score(Rect freeRect, int itemWidth, int itemHeight, Heuristic heuristic, out long score1, out long score2) {
			int leftW = freeRect.W - itemWidth, leftH = freeRect.H - itemHeight;
			int shortSide = Math.Min(leftW, leftH), longSide = Math.Max(leftW, leftH);
			switch (heuristic) {
				case Heuristic.LongSide:
					score1 = longSide;
					score2 = shortSide;
					break;
				case Heuristic.Area:
					score1 = (long)freeRect.W * freeRect.H - (long)itemWidth * itemHeight;
					score2 = shortSide;
					break;
				case Heuristic.BottomLeft:
					score1 = freeRect.Y + itemHeight;
					score2 = freeRect.X;
					break;
				default:
					score1 = shortSide;
					score2 = longSide;
					break;
			}
		}

		public void Place(Rect used) {
			for (int i = free.Count - 1; i >= 0; i--) {
				var freeRect = free[i];
				if (used.X >= freeRect.Right || used.Right <= freeRect.X || used.Y >= freeRect.Bottom || used.Bottom <= freeRect.Y) continue;
				free.RemoveAt(i);
				if (used.X > freeRect.X)
					free.Add(new(freeRect.X, freeRect.Y, used.X - freeRect.X, freeRect.H));
				if (used.Right < freeRect.Right)
					free.Add(new(used.Right, freeRect.Y, freeRect.Right - used.Right, freeRect.H));
				if (used.Y > freeRect.Y)
					free.Add(new(freeRect.X, freeRect.Y, freeRect.W, used.Y - freeRect.Y));
				if (used.Bottom < freeRect.Bottom)
					free.Add(new(freeRect.X, used.Bottom, freeRect.W, freeRect.Bottom - used.Bottom));
			}
			// drop free rects inside others
			for (int i = free.Count - 1; i >= 0; i--) {
				for (int j = 0; j < free.Count; j++) {
					if (i != j && free[j].Contains(free[i])) {
						free.RemoveAt(i);
						break;
					}
				}
			}
			UsedW = Math.Max(UsedW, used.Right);
			UsedH = Math.Max(UsedH, used.Bottom);
		}
	}

	/// <summary>
	/// Packs <paramref name="images"/> into pages named <paramref name="pageName"/>, name_2, name_3 etc.
	/// </summary>
	public static PackedAtlas Pack(IEnumerable<(string Name, Image Image)> images, AtlasPackSettings settings, string pageName) {
		var options = settings.Clone();
		var atlas = new PackedAtlas { Settings = options };
		var items = new List<Item>();
		foreach (var entry in images) {
			var item = Prepare(entry.Name, entry.Image, options);
			if (item != null)
				items.Add(item);
		}

		if (options.Alias) {
			var byHash = new Dictionary<int, List<Item>>();
			foreach (var item in items) {
				if (item.Blank) continue;
				int hash = HashCode.Combine(item.W, item.H, HashPixels(item.Pixels));
				if (!byHash.TryGetValue(hash, out var same))
					byHash[hash] = same = [];
				item.AliasOf = same.FirstOrDefault(other => other.W == item.W && other.H == item.H && other.Pixels.AsSpan().SequenceEqual(item.Pixels));
				if (item.AliasOf == null)
					same.Add(item);
			}
		}

		// Bin space: each rect carries its padding on the right/bottom
		int padX = Math.Max(0, options.PaddingX), padY = Math.Max(0, options.PaddingY);
		int originX = options.EdgePadding ? padX : 0, originY = options.EdgePadding ? padY : 0;
		int binW = options.EdgePadding ? options.MaxWidth - padX : options.MaxWidth + padX;
		int binH = options.EdgePadding ? options.MaxHeight - padY : options.MaxHeight + padY;

		var toPack = new List<Item>();
		var oversized = new List<Item>();
		foreach (var item in items) {
			if (item.AliasOf != null || item.Blank) continue;
			bool fits = (item.W + padX <= binW && item.H + padY <= binH) || (options.Rotation && item.H + padX <= binW && item.W + padY <= binH);
			if (!fits) {
				atlas.Warnings.Add($"'{item.Name}' ({item.W}x{item.H}) is bigger than the max page size, so it got a page of its own.");
				oversized.Add(item);
				continue;
			}
			toPack.Add(item);
		}

		var pages = options.Fast ? PackPages(toPack, binW, binH, padX, padY, options.Rotation, Heuristic.ShortSide, true)
			: PackBest(toPack, binW, binH, padX, padY, options.Rotation);
		foreach (var item in oversized) {
			var bin = new Bin(item.W + padX, item.H + padY);
			Place(bin, item, new(0, 0, item.W + padX, item.H + padY), false, pages.Count);
			pages.Add(bin);
			toPack.Add(item);
		}
		for (int pageIndex = 0; pageIndex < pages.Count; pageIndex++) {
			var bin = pages[pageIndex];
			int width = Math.Max(options.MinWidth, originX + bin.UsedW - padX + (options.EdgePadding ? padX : 0));
			int height = Math.Max(options.MinHeight, originY + bin.UsedH - padY + (options.EdgePadding ? padY : 0));
			if (options.DivisibleBy4) {
				width = (width + 3) & ~3;
				height = (height + 3) & ~3;
			}
			if (options.PowerOfTwo) {
				width = (int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)width);
				height = (int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)height);
			}
			if (options.Square)
				width = height = Math.Max(width, height);
			atlas.Pages.Add(new PackedAtlasPage {
				Name = pageIndex == 0 ? pageName
					: $"{Path.GetFileNameWithoutExtension(pageName)}_{pageIndex + 1}{Path.GetExtension(pageName)}",
				Width = width,
				Height = height,
				Pixels = new byte[width * height * 4]
			});
		}

		foreach (var item in toPack) {
			item.X += originX;
			item.Y += originY;
			Draw(atlas.Pages[item.Page], item, options, padX, padY);
		}

		if (options.PremultiplyAlpha) {
			foreach (var page in atlas.Pages)
				Premultiply(page.Pixels);
		}

		foreach (var item in items) {
			var placed = item.AliasOf ?? item;
			if (!item.Blank && placed.Page < 0) continue;
			int placedWidth = placed.Rotated ? placed.H : placed.W;
			int placedHeight = placed.Rotated ? placed.W : placed.H;
			atlas.Regions.Add(new PackedAtlasRegion {
				Name = item.Name,
				Page = Math.Max(placed.Page, 0),
				X = item.Blank ? 0 : placed.X,
				Y = item.Blank ? 0 : placed.Y,
				Width = item.Blank ? 0 : placedWidth,
				Height = item.Blank ? 0 : placedHeight,
				Rotation = !item.Blank && placed.Rotated ? 90 : 0,
				OffsetX = item.OffX,
				OffsetY = item.OffY,
				OriginalWidth = item.OrigW,
				OriginalHeight = item.OrigH
			});
		}
		return atlas;
	}

	private static unsafe Item? Prepare(string name, Image image, AtlasPackSettings settings) {
		if (!image.IsValid()) return null;
		var copy = Image.Copy(image);
		if (copy.Format != ImageFormat.R8G8B8A8)
			Image.Reformat(ref copy, ImageFormat.R8G8B8A8);
		int w = copy.Width, h = copy.Height;
		var src = new byte[w * h * 4];
		new Span<byte>(copy.Data, src.Length).CopyTo(src);
		Image.UnloadImage(copy);

		int threshold = Math.Max(1, settings.AlphaThreshold);
		int minX = w, minY = h;
		int maxX = -1, maxY = -1;
		for (int y = 0; y < h; y++) {
			for (int x = 0; x < w; x++) {
				if (src[(y * w + x) * 4 + 3] >= threshold) {
					if (x < minX)
						minX = x;
					if (x > maxX)
						maxX = x;
					if (y < minY)
						minY = y;
					if (y > maxY)
						maxY = y;
				}
			}
		}

		var item = new Item { Name = name, OrigW = w, OrigH = h };
		if (maxX < 0) {
			if (settings.IgnoreBlankImages) {
				item.Blank = true;
				return item;
			}
			minX = minY = 0;
			maxX = settings.StripWhitespaceX ? 0 : w - 1;
			maxY = settings.StripWhitespaceY ? 0 : h - 1;
		}
		if (!settings.StripWhitespaceX) {
			minX = 0;
			maxX = w - 1;
		}
		if (!settings.StripWhitespaceY) {
			minY = 0;
			maxY = h - 1;
		}

		item.OffX = minX;
		item.OffY = minY;
		item.W = maxX - minX + 1;
		item.H = maxY - minY + 1;
		item.Pixels = new byte[item.W * item.H * 4];
		for (int y = 0; y < item.H; y++)
			src.AsSpan(((minY + y) * w + minX) * 4, item.W * 4).CopyTo(item.Pixels.AsSpan(y * item.W * 4));
		if (settings.Bleed)
			Bleed(item.Pixels, item.W, item.H);
		return item;
	}

	private static int HashPixels(byte[] pixels) {
		var hash = new HashCode();
		hash.AddBytes(pixels);
		return hash.ToHashCode();
	}

	// Try every heuristic and use the one with the fewest pages, then the least page area
	private static List<Bin> PackBest(List<Item> items, int binW, int binH, int padX, int padY, bool rotate) {
		List<Bin>? best = null;
		long bestArea = long.MaxValue;
		(int Page, int X, int Y, bool Rotated)[] bestPlacement = new (int, int, int, bool)[items.Count];
		foreach (var heuristic in Enum.GetValues<Heuristic>()) {
			var pages = PackPages(items, binW, binH, padX, padY, rotate, heuristic, false);
			long area = pages.Sum(b => (long)b.UsedW * b.UsedH);
			if (best == null || pages.Count < best.Count || (pages.Count == best.Count && area < bestArea)) {
				best = pages;
				bestArea = area;
				for (int i = 0; i < items.Count; i++)
					bestPlacement[i] = (items[i].Page, items[i].X, items[i].Y, items[i].Rotated);
			}
		}
		for (int i = 0; i < items.Count; i++) {
			var placement = bestPlacement[i];
			items[i].Page = placement.Page;
			items[i].X = placement.X;
			items[i].Y = placement.Y;
			items[i].Rotated = placement.Rotated;
		}
		return best ?? [];
	}

	private static List<Item> SortLargestFirst(List<Item> items) {
		var order = new List<int>(items.Count);
		for (int i = 0; i < items.Count; i++)
			order.Add(i);
		order.Sort((first, second) => {
			Item firstItem = items[first], secondItem = items[second];
			int compare = Math.Max(secondItem.W, secondItem.H).CompareTo(Math.Max(firstItem.W, firstItem.H));
			if (compare != 0) return compare;
			compare = (secondItem.W * secondItem.H).CompareTo(firstItem.W * firstItem.H);
			if (compare != 0) return compare;
			return first.CompareTo(second);
		});
		var sorted = new List<Item>(items.Count);
		foreach (int index in order)
			sorted.Add(items[index]);
		return sorted;
	}

	private static List<Bin> PackPages(List<Item> items, int binW, int binH, int padX, int padY, bool rotate, Heuristic heuristic, bool sequential) {
		var pages = new List<Bin>();
		var left = SortLargestFirst(items);
		foreach (var item in left)
			item.Page = -1;
		while (left.Count > 0) {
			var bin = new Bin(binW, binH);
			int page = pages.Count;
			if (sequential) {
				foreach (var item in left) {
					if (bin.Find(item.W + padX, item.H + padY, rotate, heuristic, out var rect, out bool rotated, out _, out _))
						Place(bin, item, rect, rotated, page);
				}
			}
			else {
				// Global best fit: place whichever item scores best next
				while (true) {
					Item? bestItem = null;
					Rect bestRect = default;
					bool bestRotated = false;
					long best1 = long.MaxValue, best2 = long.MaxValue;
					foreach (var item in left) {
						if (item.Page >= 0) continue;
						if (!bin.Find(item.W + padX, item.H + padY, rotate, heuristic, out var rect, out bool rotated, out long score1, out long score2)) continue;
						if (score1 < best1 || (score1 == best1 && score2 < best2)) {
							bestItem = item;
							bestRect = rect;
							bestRotated = rotated;
							best1 = score1;
							best2 = score2;
						}
					}
					if (bestItem == null) break;
					Place(bin, bestItem, bestRect, bestRotated, page);
				}
			}
			left.RemoveAll(x => x.Page >= 0);
			pages.Add(bin);
		}
		return pages;
	}

	private static void Place(Bin bin, Item item, Rect rect, bool rotated, int page) {
		bin.Place(rect);
		item.Page = page;
		item.X = rect.X;
		item.Y = rect.Y;
		item.Rotated = rotated;
	}

	private static void Draw(PackedAtlasPage page, Item item, AtlasPackSettings settings, int padX, int padY) {
		int pageWidth = page.Width;
		var dst = page.Pixels;
		int placedWidth = item.Rotated ? item.H : item.W;
		int placedHeight = item.Rotated ? item.W : item.H;
		for (int sourceY = 0; sourceY < item.H; sourceY++) {
			for (int sourceX = 0; sourceX < item.W; sourceX++) {
				int destX = item.Rotated ? item.X + sourceY : item.X + sourceX;
				int destY = item.Rotated ? item.Y + (item.W - 1 - sourceX) : item.Y + sourceY;
				item.Pixels.AsSpan((sourceY * item.W + sourceX) * 4, 4)
					.CopyTo(dst.AsSpan((destY * pageWidth + destX) * 4));
			}
		}

		if (!settings.DuplicatePadding) return;
		// half the padding on each side
		int halfPadX = padX / 2, halfPadY = padY / 2;
		for (int destY = item.Y - halfPadY; destY < item.Y + placedHeight + halfPadY; destY++) {
			for (int destX = item.X - halfPadX; destX < item.X + placedWidth + halfPadX; destX++) {
				if (destX < 0 || destY < 0 || destX >= pageWidth || destY >= page.Height) continue;
				if (destX >= item.X && destX < item.X + placedWidth && destY >= item.Y && destY < item.Y + placedHeight) continue;
				int sourceX = Math.Clamp(destX, item.X, item.X + placedWidth - 1);
				int sourceY = Math.Clamp(destY, item.Y, item.Y + placedHeight - 1);
				dst.AsSpan((sourceY * pageWidth + sourceX) * 4, 4).CopyTo(dst.AsSpan((destY * pageWidth + destX) * 4));
			}
		}
	}

	// Transparent pixels take the color of the nearest visible pixel
	private static void Bleed(byte[] pixels, int width, int height) {
		var done = new bool[width * height];
		var queue = new Queue<int>();
		for (int i = 0; i < width * height; i++) {
			if (pixels[i * 4 + 3] != 0) {
				done[i] = true;
				queue.Enqueue(i);
			}
		}
		while (queue.Count > 0) {
			int i = queue.Dequeue();
			int x = i % width, y = i / width;
			for (int offsetY = -1; offsetY <= 1; offsetY++) {
				for (int offsetX = -1; offsetX <= 1; offsetX++) {
					int neighborX = x + offsetX, neighborY = y + offsetY;
					if (neighborX < 0 || neighborY < 0 || neighborX >= width || neighborY >= height) continue;
					int neighbor = neighborY * width + neighborX;
					if (done[neighbor]) continue;
					done[neighbor] = true;
					pixels[neighbor * 4] = pixels[i * 4];
					pixels[neighbor * 4 + 1] = pixels[i * 4 + 1];
					pixels[neighbor * 4 + 2] = pixels[i * 4 + 2];
					queue.Enqueue(neighbor);
				}
			}
		}
	}

	private static void Premultiply(byte[] pixels) {
		for (int i = 0; i < pixels.Length; i += 4) {
			int alpha = pixels[i + 3];
			if (alpha == 255) continue;
			pixels[i] = (byte)((pixels[i] * alpha + 127) / 255);
			pixels[i + 1] = (byte)((pixels[i + 1] * alpha + 127) / 255);
			pixels[i + 2] = (byte)((pixels[i + 2] * alpha + 127) / 255);
		}
	}
}
