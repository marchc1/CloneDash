using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Models;

public interface IModelInterface<BoneType, SlotType>
{
	public BoneType? FindBone(string name);
	public SlotType? FindSlot(string name);
}
