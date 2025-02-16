namespace Nucleus.ModelEditor
{
	public enum ViewportSelectMode
	{
		NotApplicable = -1,
		None = 0,

		Bones = 1,
		Images = 2,
		Other = 4,

		All = Bones | Images | Other
	}
}
