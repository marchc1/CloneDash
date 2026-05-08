namespace CloneDash.Compatibility.MuseDash
{
	public static partial class MuseDash1Compatibility
	{
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
