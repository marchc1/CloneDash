namespace Nucleus.Models
{
	public static class TransformTools
	{
		public static (bool Rotation, bool Scale, bool Reflection) Unpack(
			this TransformMode transformMode) => transformMode switch {
			TransformMode.OnlyTranslation => (false, false, false),
			TransformMode.NoRotationOrReflection => (false, true, false),
			TransformMode.NoScale => (true, false, true),
			TransformMode.NoScaleOrReflection => (true, false, false),
			_ => (true, true, true)
		};

		// Reflection only matters when scale is off
		public static TransformMode Pack(bool rotation, bool scale, bool reflection) {
			if (!rotation && !scale) return TransformMode.OnlyTranslation;
			if (!rotation) return TransformMode.NoRotationOrReflection;
			if (!scale) return reflection ? TransformMode.NoScale : TransformMode.NoScaleOrReflection;
			return TransformMode.Normal;
		}
	}
}