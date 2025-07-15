namespace Nucleus.Commands
{
	public struct CVValue
	{
		public static CVValue Null => new();

		public string String;
		public int Length => String.Length;
		public float? AsFloat => AsDouble.HasValue ? (float)AsDouble.Value : null;
		public double? AsDouble;
		public int? AsInt;
		public static bool Update(ref CVValue cv, string input) {
			var different = cv.String != input;
			cv.String = input;
			if (double.TryParse(input, out double d)) {
				cv.AsDouble = d;
				if (int.TryParse(input, out int i))
					cv.AsInt = i;
				else
					cv.AsInt = Convert.ToInt32(Math.Round(d));
			}
			else {
				cv.AsDouble = null;
				cv.AsInt = null;
			}
			return different;
		}
		public static bool Update(ref CVValue cv, string input, double? min, double? max) {
			var updated = Update(ref cv, input);
			var clampChanged = Clamp(ref cv, min, max);
			return updated || clampChanged;
		}
		public static bool Clamp(ref CVValue cv, double? min, double? max) {
			if (cv.AsDouble == null) return false;
			var oldD = cv.AsDouble;
			var oldI = cv.AsInt;
			if (min.HasValue) cv.AsDouble = Math.Max(cv.AsDouble.Value, min.Value);
			if (max.HasValue) cv.AsDouble = Math.Min(cv.AsDouble.Value, max.Value);
			cv.AsInt = Convert.ToInt32(Math.Round(cv.AsDouble.Value));
			return cv.AsDouble != oldD || cv.AsInt != oldI;
		}
	}
}
