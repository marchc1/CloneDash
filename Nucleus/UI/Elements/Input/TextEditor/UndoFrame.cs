namespace Nucleus.UI
{
	public struct UndoFrame
	{
		public TextEditorCaret CaretBefore;
		public TextEditorCaret CaretAfter;
		public string[] Rows;
	}
}