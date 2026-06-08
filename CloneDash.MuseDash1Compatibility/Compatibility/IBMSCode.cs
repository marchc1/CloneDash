using Nucleus.Util;
using System.Collections.Frozen;

namespace CloneDash.Compatibility.MuseDash
{
	public static partial class MuseDash1Compatibility
	{
		static readonly string?[] ibmsCodeValueToName = new Func<string?[]>(() => {
			var valuesRaw = (int[])Enum.GetValuesAsUnderlyingType<IBMSCode>();
			int count = valuesRaw.Max() + 1;
			string?[] ret = new string?[count];

			Span<char> tempBuffer = stackalloc char[13];
			for (int i = 0; i < valuesRaw.Length; i++) {
				int idx = valuesRaw[i];
				string? name = Enum.GetName<IBMSCode>((IBMSCode)idx);
				if (name == null)
					ret[idx] = $"Unknown (b36 {NumberToBase36String(idx, tempBuffer)}, b10 {idx})";
				else
					ret[idx] = name;
			}

			return ret;
		})();

		static readonly FrozenDictionary<ulong, IBMSCode> ibmsCodeNameToValue = new Func<FrozenDictionary<ulong, IBMSCode>>(() => {
			var valuesRaw = (int[])Enum.GetValuesAsUnderlyingType<IBMSCode>();
			int count = valuesRaw.Max() + 1;
			Dictionary<ulong, IBMSCode> ret = [];

			for (int i = 0; i < valuesRaw.Length; i++) {
				int idx = valuesRaw[i];
				string? name = Enum.GetName<IBMSCode>((IBMSCode)idx);
				if (name == null) continue; // shouldn't happen but just to be safe
				ret[name.Hash()] = (IBMSCode)idx;
			}

			return ret.ToFrozenDictionary();
		})();

		public static ReadOnlySpan<char> IBMSCodeToName(IBMSCode code) {
			int idx = (int)code;
			if (idx < 0) return null;
			if (idx >= ibmsCodeValueToName.Length) return null;

			return ibmsCodeValueToName[(int)code];
		}

		public static IBMSCode? IBMSNameToCode(ReadOnlySpan<char> name) {
			if (!ibmsCodeNameToValue.TryGetValue(name.Hash(), out IBMSCode code))
				return null;
			return code;
		}

		/// <summary>
		/// Muse Dash's IBMS codes, which defines behavior of certain entities
		/// </summary>
		public enum IBMSCode
		{
			None,

			SmallNormal,
			SmallUp,
			SmallDown,

			Medium1Normal,
			Medium1Up,
			Medium1Down,
			Medium2Normal,
			Medium2Up,
			Medium2Down,

			Large1,
			Large2,

			Raider,
			Hammer,
			Gemini,
			LongPress,
			Mul,
			Block,
			RaiderFlip,
			HammerFlip,

			DoubleSpeed1 = 24,
			DoubleSpeed2,
			DoubleSpeed3,
			RoadSpeed1,
			RoadSpeed2,
			RoadSpeed3,
			AirSpeed1,
			AirSpeed2,
			AirSpeed3,

			BossNear1 = 37,
			BossNear2,
			BossAttack1,
			BossAttack2_1,
			BossAttack2_2,
			BossMul1,
			BossMul2,
			BossBlock,
			BossIn = 46,
			BossOut,
			BossFar1Start,
			BossFar1End,
			BossFar2Start,
			BossFar2End,
			BossFar1To2,
			BossFar2To1,

			NoteHide = 55,
			NoteShow,

			BossHide,
			BossShow,

			ToggleScene1 = 60,
			ToggleScene2,
			ToggleScene3,
			ToggleScene4,
			ToggleScene5,
			ToggleScene6,
			ToggleScene7,
			ToggleScene8,
			ToggleScene9,
			ToggleScene10,

			TouhouRedPoint = 72,
			Ghost,
			Hp,
			Music,
			// "Hide/Show Background"
			SceneHide = 77,
			SceneShow,
			// "Screen Scroll"
			CanvasUpScroll,
			CanvasDownScroll,
			CanvasScrollOver,
			// "Scanlines"
			RandomWave,
			RandomWaveOver,
			// "Chromatic Aberration"
			RgbSplit,
			RgbSplitOver,
			// "Vignette"
			ShadowEdgeIn,
			ShadowEdgeOut,
			// "TV static"
			OldTv,
			OldTvOver,
			// "Flashbang"
			FlashStart,
			FlashHigh,
			FlashEnd,
			// Note freeze
			NoteFreeze,
			NoteUnfreeze,
			// Background freeze
			BgFreeze,
			BgUnfreeze,
			// "Mosaic"
			PixelStart,
			PixelEnd,
			// "Sepia"
			GrayScaleStart,
			GrayScaleEnd,
			// Focus lines
			FocusLinesBlack,
			FocusLinesWhite,
			FocusLinesOff,
			// Film grain
			FilmGrainOn,
			FilmGrainOff,
			// Auto play
			AutoPlayOn,
			AutoPlayOff,

			// Touhou mode (todo)
			Touhou_MediumBullet,
			Touhou_MediumBulletUp,
			Touhou_MediumBulletDown,
			Touhou_MediumBulletLaneshift,
			Touhou_SmallBullet,
			Touhou_SmallBulletUp,
			Touhou_SmallBulletDown,
			Touhou_SmallBulletLaneshift,
			Touhou_LargeBullet,
			Touhou_LargeBulletUp,
			Touhou_LargeBulletDown,
			Touhou_LargeBulletLaneshift,
			Touhou_BossBullet1,
			Touhou_BossBullet1Laneshift,
			Touhou_BossBullet2,
			Touhou_BossBullet2Laneshift,

			// Flashbang colors
			FlashbangColorWhite = 454,
			FlashbangColorBlack,
			FlashbangColorRed,
			FlashbangColorGreen,
			FlashbangColorBlue,
			FlashbangColorCyan,
			FlashbangColorMagenta,
			FlashbangColorYellow,
		}
	}
}
