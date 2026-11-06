using Newtonsoft.Json;
using Nucleus.Common.Graphics;
using Nucleus.Core;
using Nucleus.ManagedMemory;
using Nucleus.ModelEditor.UI.Operators;
using Nucleus.Models;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Nucleus.Util.Util;
using Image = Nucleus.UI.Elements.Image;

namespace Nucleus.ModelEditor
{
	public class ModelImage : IEditorType
	{
		public EditorModel GetModel() => null;
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
			PropertiesPanel.ButtonIcon(buttons, "Preview", "models/search.png", (e, mb) => {
				Window imageWindow = new Window(EngineCore.Level.RootPanel);
				imageWindow.HideNonCloseButtons();
				imageWindow.Title = $"Image '{Name}'";

				ITexture tex = textures.CreateTexture(File.ReadAllBytes(Filepath));

				var imagePanel = new Nucleus.UI.Elements.Image(imageWindow);
				imagePanel.				Texture = tex;
				imagePanel.				ImageOrientation = Types.ImageOrientation.Centered;
				imagePanel.				Dock = Dock.Fill;
				imageWindow.				Size = new(MathF.Max(300, tex.GetWidth() + 32), MathF.Max(300, tex.GetHeight() + 32));
				imageWindow.Center();

				imageWindow.Removed += (_) => tex.Dispose();
			});
		}

		public bool CanHide() => false;
	}
	public class ModelImageFolder : IEditorType
	{
		public EditorModel Model { get; set; }
		public EditorModel GetModel() => Model;
		public string SingleName => "folder";
		public string PluralName => "folders";
		public bool Hovered { get; set; }
		public bool Selected { get; set; }
		public bool Hidden { get; set; }

		public string Path { get; set; } = "";
		public string GetName() => Path;
		public bool CanRename() => false;
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
		/// <summary>
		/// Keeps subfolders alive for the tree, its nodes only hold weak references
		/// </summary>
		[JsonIgnore] public List<ModelImageFolder> Folders { get; } = [];

		[JsonIgnore] public EditorTextureAtlas TextureAtlas { get; } = new();

		FileSystemWatcher? watcher;
		volatile bool changedOnDisk;
		double lastChange;

		/// <summary>
		/// True once, a moment after the images folder stops changing on disk
		/// </summary>
		public bool TakeDiskChange(double now) {
			if (changedOnDisk) {
				changedOnDisk = false;
				lastChange = now;
			}
			if (lastChange <= 0 || now - lastChange < 0.5) return false;
			lastChange = 0;
			return true;
		}

		public string? FullPath(string? projectFolder) => Filepath == null || projectFolder == null || Path.IsPathRooted(Filepath) ? Filepath
				: Path.GetFullPath(Path.Combine(projectFolder, Filepath));

		private void Watch(string? root) {
			watcher?.Dispose();
			watcher = null;
			if (root == null || !Directory.Exists(root)) return;

			watcher = new FileSystemWatcher(root) {
				IncludeSubdirectories = true,
				NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName
					| NotifyFilters.LastWrite | NotifyFilters.Size
			};
			FileSystemEventHandler changed = (_, _) => changedOnDisk = true;
			watcher.Created += changed;
			watcher.Changed += changed;
			watcher.Deleted += changed;
			watcher.Renamed += (_, _) => changedOnDisk = true;
			watcher.EnableRaisingEvents = true;
		}

		public EditorResult Scan(string? projectFolder = null) {
			var root = FullPath(projectFolder);
			if (watcher?.Path != root)
				Watch(root);
			Images = [];
			ImageNames = [];
			ImageLookup = [];

			TextureAtlas.ClearTextures();

			if (root == null)
				return new("Filepath was null.");

			if (!Directory.Exists(root))
				return new("Directory does not exist.");

			List<ModelImage> images = [];
			List<string> imageNames = [];
			Dictionary<string, ModelImage> imageLookup = [];
			var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
			foreach (var file in files) {
				var nameExt = file.Replace(root, "").TrimStart('\\').Replace("\\", "/");
				var name = Path.ChangeExtension(nameExt, null);
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
				txtbox.				Text = txt;
			});
		}

		public void BuildOperators(Panel buttons, PreUIDeterminations determinations) {
			PropertiesPanel.ButtonIcon(buttons, "Rescan", "models/search.png", (_, _) => ModelEditor.Active.File.RescanModelImages(Model));
			PropertiesPanel.ButtonIcon(buttons, "Preview", "models/search.png", (e, mb) => {
				Window imageWindow = new Window(EngineCore.Level.RootPanel);
				imageWindow.HideNonCloseButtons();
				imageWindow.Title = $"Texture Atlas";

				ITexture tex = TextureAtlas.PackedTexture;

				var imagePanel = new Image(imageWindow);
				imagePanel.				Texture = tex;
				imagePanel.				ImageOrientation = Types.ImageOrientation.Centered;
				imagePanel.				Dock = Dock.Fill;

				imageWindow.
				Size = new(MathF.Max(300, tex.GetWidth() + 32), MathF.Max(300, tex.GetHeight() + 32));

				imageWindow.Center();

			});
		}

		public string DetermineHeaderText(PreUIDeterminations determinations) => "Image files";

		public bool CanHide() => false;
	}
}
