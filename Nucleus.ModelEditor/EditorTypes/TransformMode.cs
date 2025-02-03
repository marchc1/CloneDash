namespace Nucleus.ModelEditor
{
	/// <summary>
	/// <code>
	/// 0 0 0 0
	/// ^ ^ ^ ^
	/// | | | |---- rotation
	/// | | |------ scale
	/// | |-------- reflection
	/// |---------- not used
	/// </code>
	/// </summary>
	public enum TransformMode {
		Normal = 0b0000,
		OnlyTranslation = 0b0111,
		NoRotationOrReflection = 0b0001,
		NoScale = 0b0010,
		NoScaleOrReflection = 0b0110
	}
}
