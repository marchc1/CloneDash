namespace Nucleus.ModelEditor
{
	public static class TransformTools {
		public static (bool Rotation, bool Scale, bool Reflection) Unpack(this TransformMode transformMode) {
			return (
				((int)transformMode & 0b0001) != 0b0001,
				((int)transformMode & 0b0010) != 0b0001,
				((int)transformMode & 0b0100) != 0b0001
				);
		}
		public static TransformMode Pack(bool rotation, bool scale, bool reflection) {
			if (!scale && !rotation)
				reflection = false;

			if (scale)
				reflection = rotation;

			return (TransformMode)(
				(!reflection ? 0b0100 : 0b0000) |
				(!scale ? 0b0010 : 0b0000) |
				(!rotation ? 0b0001 : 0b0000)
			);
		}
	}
}
