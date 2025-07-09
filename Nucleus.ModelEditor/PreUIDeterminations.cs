using System.Diagnostics.CodeAnalysis;

namespace Nucleus.ModelEditor
{
	public struct PreUIDeterminations : IValidatable
	{
		public bool OnlySelectedOne;

		public bool AllShareAType;
		public Type? SharedType;
		public IEditorType[] Selected;
		public IEditorType? First;
		public IEditorType? Last;
		public int Count;

		[MemberNotNullWhen(true, nameof(Last))] public bool IsValid() => Count > 0;
	}
}
