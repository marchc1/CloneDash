using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Models
{
	public class ModelImages(EditorModel Model)
	{
		private string? __filepath;
		public string? Filepath {
			get => __filepath;
			set => __filepath = value;
		}
		public string[] Images { get; private set; } = [];
		public string[] ImageNames { get; private set; } = [];

		public EditorResult Scan() {
			Images = [];
			ImageNames = [];

			if (Filepath == null)
				return new("Filepath was null.");

			if (!Directory.Exists(Filepath))
				return new("Directory does not exist.");

			List<string> images = [];
			List<string> imageNames = [];
			var files = Directory.GetFiles(Filepath);
			foreach (var file in files) {
				var nameExt = Path.GetFileName(file);
				var name = Path.GetFileNameWithoutExtension(file);
				var ext = Path.GetExtension(file);
				if (ext != null && nameExt != null && name != null) {
					switch (ext) {
						case "jpg":
						case "jpeg":
						case "png":
							images.Add(nameExt);
							imageNames.Add(name);
							break;
					}
				}
			}

			images.Sort();
			imageNames.Sort();

			Images = images.ToArray();
			ImageNames = imageNames.ToArray();

			return new();
		}
	}
}
