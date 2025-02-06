using Newtonsoft.Json;
using Nucleus.Models;
using Nucleus.UI;
using Nucleus.UI.Elements;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Nucleus.Util.Util;

namespace Nucleus.ModelEditor
{
	public class ModelImage : IEditorType
	{
		public string SingleName => "Image File";
		public string PluralName => "Image Files";
		public bool Hovered { get; set; }
		public bool Selected { get; set; }
		public bool Visible { get; set; }

		public string Name { get; set; }
		public string Filepath { get; set; }

		// We don't need to wrap this in a mainthread runASAP call because
		// textures already do that under the hood in Dispose
	}
	public class ModelImages : IEditorType
	{
		public string SingleName => "Images";
		public string PluralName => "Images";
		public bool Hovered { get; set; }
		public bool Selected { get; set; }
		public bool Visible { get; set; }

		public EditorModel Model { get; set; }
		private string? __filepath;
		public string? Filepath {
			get => __filepath;
			set => __filepath = value;
		}
		[JsonIgnore] public ModelImage[] Images { get; private set; } = [];
		[JsonIgnore] public string[] ImageNames { get; private set; } = [];

		[JsonIgnore] public TextureAtlasSystem TextureAtlas { get; } = new();

		public EditorResult Scan() {
			Images = [];
			ImageNames = [];

			TextureAtlas.ClearTextures();

			if (Filepath == null)
				return new("Filepath was null.");

			if (!Directory.Exists(Filepath))
				return new("Directory does not exist.");

			List<ModelImage> images = [];
			List<string> imageNames = [];
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
							images.Add(new() {
								Name = nameExt,
								Filepath = file
							});
							imageNames.Add(name);
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

		}

		public string DetermineHeaderText(PreUIDeterminations determinations) => "Image files";
	}
}
