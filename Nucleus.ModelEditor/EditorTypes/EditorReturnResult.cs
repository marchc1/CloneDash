using System.Diagnostics.CodeAnalysis;

namespace Nucleus.ModelEditor
{
	public struct EditorResult
	{
		public bool Succeeded;
		public string? Reason;

		public static EditorResult OK => new(true, null);

		/// <summary>
		/// No error occured and the operation completed successfully.
		/// </summary>
		public EditorResult() {
			Succeeded = true;
			Reason = null;
		}

		/// <summary>
		/// An error occurred; type the error reason.
		/// </summary>
		/// <param name="reason">An error occurred; type the error reason.</param>
		public EditorResult(string? reason = null) {
			Succeeded = false;
			Reason = reason;
		}

		/// <summary>
		/// Custom logic where succeeded can be true/false and reason can be provided/excluded separately.
		/// </summary>
		/// <param name="succeeded"></param>
		/// <param name="reason"></param>
		public EditorResult(bool succeeded, string? reason = null) {
			Succeeded = succeeded;
			Reason = reason;
		}
	}
	public struct EditorReturnResult<T> {
		public T? Result;
		public string? Reason;

		[MemberNotNullWhen(false, nameof(Result))]
		public bool Failed => Result == null && Reason != null;

		public EditorReturnResult(T? result, string? reason = null) {
			Result = result;
			Reason = reason;
		}
	}
}
