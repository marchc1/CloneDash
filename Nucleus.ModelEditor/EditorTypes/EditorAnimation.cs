using Newtonsoft.Json;
using Nucleus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.ModelEditor
{
	public class EditorAnimation : IEditorType
	{
		[JsonIgnore] public string SingleName => "animation";
		[JsonIgnore] public string PluralName => "animations";

		[JsonIgnore] public bool Hovered { get; set; }
		[JsonIgnore] public bool Selected { get; set; }
		[JsonIgnore] public bool Hidden { get; set; }

		public string Name { get; set; }
		public bool Export { get; set; } = true;
	}
}
