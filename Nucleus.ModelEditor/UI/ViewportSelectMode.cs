namespace Nucleus.ModelEditor
{
	public enum ViewportSelectMode
	{
		NotApplicable = -1,
		None = 0,

		Bones = 1 << 0,
		Images = 1 << 1,
		Meshes = 1 << 2,
		Other = 1 << 31,

		All = Bones | Images | Other | Meshes
	}
}
