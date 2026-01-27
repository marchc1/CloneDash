using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Models;

public interface IModelInterface<BoneType, SlotType>
{
	public BoneType? FindBone(ReadOnlySpan<char> name);
	public SlotType? FindSlot(ReadOnlySpan<char> name);
}
