using Newtonsoft.Json;
using Nucleus.Core;
using Nucleus.ModelEditor.UI.Operators;
using Nucleus.Models;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Nucleus.Util.Util;

namespace Nucleus.ModelEditor
{
	public class ModelImage : IEditorType
	{
		public EditorModel GetModel() => throw new Exception();
		public string SingleName => "Image File";
		public string PluralName => "Image Files";
		public bool Hovered { get; set; }
		public bool Selected { get; set; }
		public bool Hidden { get; set; }

		public string Name { get; set; }
		public string Filepath { get; set; }

		public string GetName() => Name;

		public void BuildTopOperators(Panel props, PreUIDeterminations determinations) {

		}

		public void BuildProperties(Panel props, PreUIDeterminations determinations) {

		}

		public void BuildOperators(Panel buttons, PreUIDeterminations determinations) {
			PropertiesPanel.OperatorButton<ImageSetParentOperator>(buttons, "Set Parent", "models/setparent.png");
			PropertiesPanel.ButtonIcon(buttons, "Preview", "models/search.png", (e, fs, mb) => {
				Window imageWindow = EngineCore.Level.UI.Add<Window>();
				imageWindow.HideNonCloseButtons();
				imageWindow.Title = $"Image '{Name}'";

				ManagedMemory.Texture tex = new ManagedMemory.Texture(imageWindow.Level.Textures, Raylib.LoadTexture(Filepath), true);

				var imagePanel = imageWindow.Add<Panel>();
				imagePanel.Image = tex;
				imagePanel.ImageOrientation = Types.ImageOrientation.Centered;
				imagePanel.Dock = Dock.Fill;

				imagePanel.PaintOverride += (e, w, h) => {
					Graphics2D.SetDrawColor(255, 255, 255);
					e.ImageDrawing(new(0, 0), new(w, h));
					Graphics2D.SetDrawColor(150, 150, 150);
					var c = new Vector2F(w / 2, h / 2) - new Vector2F(tex.Width / 2, tex.Height / 2);
					Graphics2D.DrawRectangleOutline(c - 2, new Vector2F(tex.Width, tex.Height) + 4, 2);
				};

				imageWindow.Size = new(MathF.Max(300, tex.Width + 32), MathF.Max(300, tex.Height + 32));

				imageWindow.Center();

				imageWindow.Removed += (_) => tex.Dispose();
			});
		}

		public bool CanHide() => false;
	}
	public class ModelImages : IEditorType
	{
		public EditorModel GetModel() => Model;
		public string SingleName => "Images";
		public string PluralName => "Images";
		public bool Hovered { get; set; }
		public bool Selected { get; set; }
		public bool Hidden { get; set; }

		public EditorModel Model { get; set; }
		private string? __filepath;
		public string? Filepath {
			get => __filepath;
			set => __filepath = value;
		}
		[JsonIgnore] public ModelImage[] Images { get; private set; } = [];
		[JsonIgnore] public string[] ImageNames { get; private set; } = [];
		[JsonIgnore] public Dictionary<string, ModelImage> ImageLookup { get; private set; } = [];

		[JsonIgnore] public TextureAtlasSystem TextureAtlas { get; } = new();

		public EditorResult Scan() {
			Images = [];
			ImageNames = [];
			ImageLookup = [];

			TextureAtlas.ClearTextures();

			if (Filepath == null)
				return new("Filepath was null.");

			if (!Directory.Exists(Filepath))
				return new("Directory does not exist.");

			List<ModelImage> images = [];
			List<string> imageNames = [];
			Dictionary<string, ModelImage> imageLookup = [];
			var files = Directory.GetFiles(Filepath);
			foreach (var file in files) {
				var nameExt = Path.GetFileName(file);
				var name = Path.GetFileNameWithoutExtension(file);
				var ext = Path.GetExtension(file);
				if (ext != null && nameExt != null && name != null) {
					switch (ext) {
						case ".jpg":
						case ".jpeg":
						case ".png":
							ModelImage img = new() {
								Name = name,
								Filepath = file
							};

							images.Add(img);
							imageNames.Add(name);
							imageLookup[name] = img;

							TextureAtlas.AddTexture(name, file);

							break;
					}
				}
			}

			AlphanumComparatorFast alphanum = new AlphanumComparatorFast();
			images.Sort((x, y) => alphanum.Compare(x.Name, y.Name));
			imageNames.Sort((x, y) => alphanum.Compare(x, y));

			Images = images.ToArray();
			ImageNames = imageNames.ToArray();
			ImageLookup = imageLookup;

			return new();
		}

		public void BuildTopOperators(Panel props, PreUIDeterminations determinations) {

		}
		public void BuildProperties(Panel props, PreUIDeterminations determinations) {
			var pathRow = PropertiesPanel.NewRow(props, "Path", "models/images.png");

			var boneRotation = PropertiesPanel.AddFilepath(pathRow, Filepath, (txtbox, txt) => {
				ModelEditor.Active.File.SetModelImages(Model, txt);
				txtbox.Text = txt;
			});
		}

		public void BuildOperators(Panel buttons, PreUIDeterminations determinations) {
			PropertiesPanel.ButtonIcon(buttons, "Rescan", "models/search.png", (_, _, _) => ModelEditor.Active.File.RescanModelImages(Model));
			PropertiesPanel.ButtonIcon(buttons, "Preview", "models/search.png", (e, fs, mb) => {
				Window imageWindow = EngineCore.Level.UI.Add<Window>();
				imageWindow.HideNonCloseButtons();
				imageWindow.Title = $"Texture Atlas";

				ManagedMemory.Texture tex = TextureAtlas.Texture;

				var imagePanel = imageWindow.Add<Panel>();
				imagePanel.Image = tex;
				imagePanel.ImageOrientation = Types.ImageOrientation.Centered;
				imagePanel.Dock = Dock.Fill;

				imagePanel.PaintOverride += (e, w, h) => {
					Graphics2D.SetDrawColor(255, 255, 255);
					e.ImageDrawing(new(0, 0), new(w, h));
					Graphics2D.SetDrawColor(150, 150, 150);
					var c = new Vector2F(w / 2, h / 2) - new Vector2F(tex.Width / 2, tex.Height / 2);
					Graphics2D.DrawRectangleOutline(c - 2, new Vector2F(tex.Width, tex.Height) + 4, 2);
				};

				imageWindow.Size = new(MathF.Max(300, tex.Width + 32), MathF.Max(300, tex.Height + 32));

				imageWindow.Center();

			});
		}

		public string DetermineHeaderText(PreUIDeterminations determinations) => "Image files";

		public bool CanHide() => false;
	}
}
