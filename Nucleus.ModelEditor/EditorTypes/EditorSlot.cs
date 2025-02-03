using Nucleus.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Models
{
	public class EditorSlot : IValidatable
	{
		public EditorBone Bone { get; set; }
		public string Name { get; set; }

		private bool __isvalid = true;
		public void Invaldiate() => __isvalid = false;
		public bool IsValid() => __isvalid && IValidatable.IsValid(Bone);
	}
}
