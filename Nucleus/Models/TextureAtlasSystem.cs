using Nucleus.Core;
using Nucleus.Types;
using Nucleus.Util;
using Raylib_cs;
using RectpackSharp;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Nucleus.Models
{
	public struct AtlasRegion : IValidatable
	{
		public bool IsValid() => Valid;
		public bool Valid;
		public int X;
		public int Y;
		public int W;
		public int H;
		public AtlasRegion(int x, int y, int w, int h) {
			X = x;
			Y = y;
			W = w;
			H = h;
			Valid = true;
		}
		public AtlasRegion(uint x, uint y, uint w, uint h) {
			X = (int)x;
			Y = (int)y;
			W = (int)w;
			H = (int)h;
			Valid = true;
		}
		public static readonly AtlasRegion MISSING = new AtlasRegion() {
			X = 0,
			Y = 0,
			W = 512,
			H = 512,
			Valid = false
		};

		public override string ToString() {
			return $"x {X} y {Y} w {W} h {H}";
		}
	}

	/// <summary>
	/// The texture atlasing system.
	/// <br/>
	/// <br/>
	/// Packs a Texture[] list into a single Texture and AtlasRegion[] regions, which then can be accessed by a realtime renderer
	/// </summary>
	public class TextureAtlasSystem
	{
		public bool Debugging { get; set; }
		public const string VERSION = "TextureAtlasVersion1";

		public static string SerializeAtlas(Dictionary<string, AtlasRegion> regions) {
			List<string> lines = [];

			lines.Add(VERSION);
			foreach (var region in regions) {
				lines.Add(region.Key);
				lines.Add($"\tX: {region.Value.X}");
				lines.Add($"\tY: {region.Value.Y}");
				lines.Add($"\tW: {region.Value.W}");
				lines.Add($"\tH: {region.Value.H}");
			}

			return string.Join("\n", lines);
		}

		public static void PopulateAtlas(string data, Dictionary<string, AtlasRegion> regions) {
			data = data.ReplaceLineEndings();
			string[] lines = data.Split(Environment.NewLine);
			if (lines.Length <= 0) return;

			var version = lines[0];
			switch (version) {
				case "TextureAtlasVersion1":
					AtlasRegion workingRegion = new();
					string workingRegionName = "";
					for (int i = 1; i < lines.Length; i++) {
						var line = lines[i];

						if (line.Length <= 0) continue;
						if (char.IsWhiteSpace(line[0])) {
							var pLine = line.Trim();
							var pLineParts = pLine.Split(':');
							if (pLineParts.Length != 2) continue;

							var key = pLineParts[0].Trim();
							var val = pLineParts[1].Trim();
							switch (key) {
								case "X": workingRegion.X = int.TryParse(val, out int xR) ? xR : 0; break;
								case "Y": workingRegion.Y = int.TryParse(val, out int yR) ? yR : 0; break;
								case "W": workingRegion.W = int.TryParse(val, out int wR) ? wR : 0; break;
								case "H": workingRegion.H = int.TryParse(val, out int hR) ? hR : 0; break;
							}
						}
						else {
							if (!string.IsNullOrWhiteSpace(workingRegionName)) {
								regions[workingRegionName] = workingRegion;
							}
							workingRegion = new AtlasRegion();
							workingRegionName = line;
						}
					}
					if (!string.IsNullOrWhiteSpace(workingRegionName))
						regions[workingRegionName] = workingRegion;
					break;
			}
		}

		public static Dictionary<string, AtlasRegion> DeserializeAtlas(string data) {
			Dictionary<string, AtlasRegion> regions = new();
			PopulateAtlas(data, regions);
			return regions;
		}

		private ManagedMemory.Texture? packedTex;
		private Image? packedImg;
		private Dictionary<string, Image> unpacked = [];
		private Dictionary<string, AtlasRegion> regions = [];

		private bool valid = false;
		private bool locked = false;

		public void Invalidate() => valid = false;

		public float AcceptableDensity => .5f;
		public uint StepSize => 1;

		[MemberNotNull(nameof(packedTex))]
		public void Validate() {
			if (valid && packedTex != null) return;

			regions.Clear();
			if (packedTex != null)
				packedTex.Dispose();

			Span<PackingRectangle> rects = stackalloc PackingRectangle[unpacked.Count];
			int i = 0;
			string[] keys = new string[unpacked.Count];
			int additionalPadding = 4; // added onto drawing position; added * 2 onto pack-size

			foreach (var strTexPair in unpacked) {
				rects[i] = new PackingRectangle(
					0,
					0,
					(uint)(strTexPair.Value.Width + (additionalPadding * 2)),
					(uint)(strTexPair.Value.Height + (additionalPadding * 2)),
					i
				);

				keys[i] = strTexPair.Key;

				i++;
			}

			RectanglePacker.Pack(
				rectangles: rects,
				bounds: out PackingRectangle bounds,
				packingHint: PackingHints.FindBest,
				acceptableDensity: AcceptableDensity,
				stepSize: StepSize,
				maxBoundsWidth: null,
				maxBoundsHeight: null
			);

			int rw = (int)bounds.Width.RoundUpToPowerOf2();
			int rh = (int)bounds.Height.RoundUpToPowerOf2();
			bool testing = Debugging;

			Image workingImage = Raylib.GenImageColor(rw, rh, testing ? Color.LightGray : Color.Blank);

			for (int j = 0; j < rects.Length; j++) {
				PackingRectangle rect = rects[j];
				string key = keys[rect.Id];
				Image src = unpacked[key];
				Raylib.ImageDraw(ref workingImage, src, 
					new(0, 0, src.Width, src.Height), 
					new(rect.X + additionalPadding, rect.Y + additionalPadding, rect.Width - (additionalPadding * 2), rect.Height - (additionalPadding * 2)),
					Color.White);

				if (testing) {
					renderTestData(rect, key, src, ref workingImage);
				}

				regions[key] = new(rect.X, rect.Y, rect.Width, rect.Height);
			}

			packedImg = workingImage;
			var tex = Raylib.LoadTextureFromImage(workingImage);
			Raylib.SetTextureFilter(tex, TextureFilter.TEXTURE_FILTER_BILINEAR);
			packedTex = new ManagedMemory.Texture(EngineCore.Level.Textures, tex, true, packedImg);

			valid = true;
		}

		public void renderTestData(PackingRectangle rect, string key, Image src, ref Image workingImage) {
			Raylib.ImageDrawRectangleLines(ref workingImage, new(rect.X, rect.Y, rect.Width, rect.Height), 2, (new Vector3(rect.Id * 30, 0.85f, 1f)).ToRGB());
			var text = $"{key}";
			Raylib.ImageDrawTextEx(ref workingImage, Graphics2D.FontManager[text, "Consolas", 11], text, new((int)rect.X + 4, (int)rect.Y + 4), 11, 1f, (new Vector3(rect.Id * 30, 0.85f, .3f)).ToRGB());
			text = $"{rect.X}, {rect.Y} [{rect.Width}x{rect.Height}]";
			Raylib.ImageDrawTextEx(ref workingImage, Graphics2D.FontManager[text, "Consolas", 11], text, new((int)rect.X + 16, (int)rect.Y + 4 + (14 * 1)), 11, 1f, (new Vector3(rect.Id * 30, 0.85f, .3f)).ToRGB());
		}

		public void SaveTo(string filepath) {
			Validate();
			if (packedImg != null) {
				var filePNG = Path.ChangeExtension(filepath, "png");
				var fileAtlas = Path.ChangeExtension(filepath, "texatlas");
				Raylib.ExportImage(packedImg.Value, filePNG);
				File.WriteAllText(fileAtlas, SerializeAtlas(regions));
			}
			else throw new Exception("Validation failed, no packed image to save!");
		}

		public void LoadFrom(string filepath) {
			var filePNG = Path.ChangeExtension(filepath, "png");
			var fileAtlas = Path.ChangeExtension(filepath, "texatlas");

			if (packedTex != null) packedTex.Dispose();
			if (packedImg != null) Raylib.UnloadImage(packedImg.Value);

			packedImg = Raylib.LoadImage(filePNG);
			var tex = Raylib.LoadTextureFromImage(packedImg.Value);
			Raylib.SetTextureFilter(tex, TextureFilter.TEXTURE_FILTER_BILINEAR);
			packedTex = new(EngineCore.Level.Textures, tex, true);
			regions = DeserializeAtlas(File.ReadAllText(fileAtlas));
			unpacked.Clear();
			valid = true;
			Lock();
		}

		public void Lock() => locked = true;

		public void ClearTextures() {
			if (locked)
				Logs.Warn("The TextureAtlasSystem has been set to read-only (probably because it's in a runtime context). If this isn't due to a level change, it should be investigated.");

			foreach (var kvp in unpacked)
				Raylib.UnloadImage(kvp.Value);

			unpacked.Clear();
			Invalidate();
		}

		public void AddTexture(string name, string filepath) {
			if (locked)
				throw new InvalidOperationException("The TextureAtlasSystem has been set to read-only (probably because it's in a runtime context).");

			if (unpacked.TryGetValue(name, out var packed))
				Raylib.UnloadImage(packed);

			unpacked[name] = Raylib.LoadImage(filepath);
			Invalidate();
		}

		public void AddTexture(string name, Image cpuImage) {
			if (locked)
				throw new InvalidOperationException("The TextureAtlasSystem has been set to read-only (probably because it's in a runtime context).");

			if (unpacked.TryGetValue(name, out var packed))
				Raylib.UnloadImage(packed);

			unpacked[name] = cpuImage;
			Invalidate();
		}

		public bool RemoveTexture(string name) {
			bool removed = unpacked.Remove(name);
			Invalidate();
			return removed;
		}

		public bool TryGetTextureRegion(string name, out AtlasRegion region) {
			Validate();

			if (regions.TryGetValue(name, out region))
				return true;

			region = default;
			return false;
		}

		public AtlasRegion? GetTextureRegion(string name) => TryGetTextureRegion(name, out var reg) ? reg : null;
		/// <summary>
		/// not implemented yet
		/// </summary>
		/// <param name="name"></param>
		/// <param name="uS"></param>
		/// <param name="vS"></param>
		/// <param name="uE"></param>
		/// <param name="vE"></param>
		/// <returns></returns>
		public bool TryGetTextureUV(string name, out Vector2F uS, out Vector2F vS, out Vector2F uE, out Vector2F vE) {
			Validate();

			if (regions.TryGetValue(name, out var region)) {
				uS = Vector2F.Zero;
				vS = Vector2F.Zero;
				uE = Vector2F.Zero;
				vE = Vector2F.Zero;
				return true;
			}

			uS = Vector2F.Zero;
			vS = Vector2F.Zero;
			uE = Vector2F.Zero;
			vE = Vector2F.Zero;
			return false;
		}

		public void AddRegion(string name, AtlasRegion newRegion) {
			regions.Add(name, newRegion);
		}

		public ManagedMemory.Texture Texture {
			get {
				Validate();
				return packedTex;
			}
			set {
				packedTex = value;
				valid = true;
			}
		}

		~TextureAtlasSystem() {
			MainThread.RunASAP(() => {
				foreach (var img in unpacked) {
					Raylib.UnloadImage(img.Value);
				}
			});
		}
	}
}
