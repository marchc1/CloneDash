using System.Diagnostics.CodeAnalysis;

namespace Nucleus.Models
{
	public struct ReturnResult<T> {
		public T? Result;
		public string? Reason;

		[MemberNotNullWhen(false, nameof(Result))]
		public bool Failed => Result == null && Reason != null;

		public ReturnResult(T? result, string? reason = null) {
			Result = result;
			Reason = reason;
		}
	}
}
