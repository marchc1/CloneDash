namespace CloneDash.Unbeatable.Internal;

internal sealed class PrivateImplementationDetails
{
	internal static uint ComputeStringHash(string s) {
		uint num = 0;
		if (s != null) {
			num = 2166136261U;
			for (int i = 0; i < s.Length; i++) {
				num = ((uint)s[i] ^ num) * 16777619U;
			}
		}
		return num;
	}
}
