namespace Nucleus.Models
{
	public interface IFCurve {
		public const string FCURVE_DATAPATH_LOCATION_X = "location[0]";
		public const string FCURVE_DATAPATH_LOCATION_Y = "location[1]";

		public const string FCURVE_DATAPATH_ROTATION = "rotation";

		public const string FCURVE_DATAPATH_SCALE_X = "scale[0]";
		public const string FCURVE_DATAPATH_SCALE_Y = "scale[1]";

		public const string FCURVE_DATAPATH_SHEAR_X = "shear[0]";
		public const string FCURVE_DATAPATH_SHEAR_Y = "shear[1]";

		public const string FCURVE_DATAPATH_COLOR_R = "color[0]";
		public const string FCURVE_DATAPATH_COLOR_G = "color[1]";
		public const string FCURVE_DATAPATH_COLOR_B = "color[2]";
		public const string FCURVE_DATAPATH_COLOR_A = "color[3]";

		public const string FCURVE_DATAPATH_DARK_COLOR_R = "dark_color[0]";
		public const string FCURVE_DATAPATH_DARK_COLOR_G = "dark_color[1]";
		public const string FCURVE_DATAPATH_DARK_COLOR_B = "dark_color[2]";

		public const string FCURVE_DATAPATH_BLENDING = "blending";
		public const string FCURVE_DATAPATH_ACTIVE_ATTACHMENT = "active_attachment";
		public const string FCURVE_DATAPATH_TRANSFORM_MODE = "transform_mode";
		public const string FCURVE_DATAPATH_DRAW_ORDER = "draw_order";

		/// <summary>
		/// Combines data paths together.
		/// <br></br>
		/// ex.
		/// <code>
		/// CombineDataPath("bones", "[\"Bone Name\"]", <see cref="FCURVE_DATAPATH_LOCATION_X"/>)
		///		=> "bones[\"BoneName\"].location[1]"
		/// </code>
		/// </summary>
		/// <param name="dataPaths"></param>
		/// <returns></returns>
		public static string CombineDataPath(params string[] dataPaths) {
			string[] newDataPaths = new string[dataPaths.Length];
			for (int i = 0; i < dataPaths.Length; i++) {
				var dataPath = dataPaths[i].Trim('.');
				if (dataPath.Length <= 0)
					continue;

				var firstChar = dataPath[0];
				if (firstChar != '[' && i > 0)
					dataPath = $".{dataPath}";
			}

			return string.Join("", newDataPaths);
		}
	}
}
