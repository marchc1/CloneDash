namespace Nucleus.Models;

public interface IModelInterface<BoneType, SlotType>
{
	public BoneType? FindBone(ReadOnlySpan<char> name);

	public SlotType? FindSlot(ReadOnlySpan<char> name);
}