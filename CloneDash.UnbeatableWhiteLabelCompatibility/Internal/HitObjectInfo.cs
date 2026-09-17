using System;
using System;
using System.Collections.Generic;
using System.Text;

namespace CloneDash.Unbeatable.Internal
{
	[Serializable]
	public class HitObjectInfo
	{
		public int laneNumber {
			get {
				return this.x * 6 / 512 + 1;
			}
		}

		public bool IsCommand() {
			return this.laneNumber == 1 || this.laneNumber == 2;
		}

		public bool IsNormalNote() {
			return this.laneNumber == 3 || this.laneNumber == 4;
		}

		public bool IsFlip() {
			return this.laneNumber == 5;
		}

		public bool IsExtraNote() {
			return this.laneNumber == 6;
		}

		public bool IsAnyNote() {
			return this.IsNormalNote() || this.IsExtraNote();
		}

		public bool IsInstantType() {
			return (this.type & 129) == 1;
		}

		public bool IsHoldType() {
			return (this.type & 129) == 128;
		}

		public bool IsSpawnMid() {
			return this.hitSample[0] == "1";
		}

		public bool IsHiding() {
			return this.IsNormalNote() && this.hitSound == HitObjectInfo.HitSound.Clap && (this.IsInstantType() || this.IsHoldType());
		}

		public bool IsToggleCenter() {
			return this.IsFlip() && this.hitSound == HitObjectInfo.HitSound.Whistle;
		}

		public bool IsCameraSwapImmediate() {
			return this.IsFlip() && this.hitSound == HitObjectInfo.HitSound.Clap;
		}

		public Height GetNoteHeight() {
			switch (this.laneNumber) {
				case 1:
				case 2:
					throw new Exception(string.Format("Trying to get the height of an effect (lane {0}, time {1})", this.laneNumber, this.time));
				case 3:
					return Height.Top;
				case 4:
					return Height.Low;
				case 5:
					throw new Exception(string.Format("Trying to get the height of a side flip (lane {0}, time {1})", this.laneNumber, this.time));
				case 6:
					return Height.Mid;
				default:
					throw new Exception(string.Format("Trying to get the height of a hit object on unsupported lane {0} (time {1})", this.laneNumber, this.time));
			}
		}

		public NoteType GetNoteType() {
			if (this.IsNormalNote()) {
				if (this.IsInstantType()) {
					if (this.hitSound == HitObjectInfo.HitSound.Normal) {
						return NoteType.Default;
					}
					if (this.hitSound == HitObjectInfo.HitSound.Whistle) {
						return NoteType.Dodge;
					}
					if (this.hitSound == HitObjectInfo.HitSound.Clap) {
						return NoteType.Default;
					}
					if (this.hitSound == HitObjectInfo.HitSound.Finish) {
						return NoteType.Setpiece;
					}
					throw new Exception(string.Format("Normal hit sound {0} does not correspond to any note type (lane {1}, time {2})", this.hitSound, this.laneNumber, this.time));
				}
				else {
					if (!this.IsHoldType()) {
						throw new Exception(string.Format("Unsupported hit object note of type {0} (lane {1}, time {2})", this.type, this.laneNumber, this.time));
					}
					if (this.hitSound == HitObjectInfo.HitSound.Normal) {
						return NoteType.Hold;
					}
					if (this.hitSound == HitObjectInfo.HitSound.Whistle) {
						return NoteType.Double;
					}
					if (this.hitSound == HitObjectInfo.HitSound.Clap) {
						return NoteType.Hold;
					}
					throw new Exception(string.Format("Hold hit sound {0} does not correspond to any note type (lane {1}, time {2})", this.hitSound, this.laneNumber, this.time));
				}
			}
			else {
				if (!this.IsExtraNote()) {
					throw new Exception(string.Format("Unsupported hit object on lane {0} (time {1})", this.laneNumber, this.time));
				}
				if (this.IsInstantType()) {
					if (this.hitSound == HitObjectInfo.HitSound.Normal) {
						return NoteType.Freestyle;
					}
					throw new Exception(string.Format("Normal hit sound {0} does not correspond to any extra note type (lane {1}, time {2})", this.hitSound, this.laneNumber, this.time));
				}
				else {
					if (!this.IsHoldType()) {
						throw new Exception(string.Format("Unsupported hit object extra note of type {0} (lane {1}, time {2})", this.type, this.laneNumber, this.time));
					}
					if (this.hitSound == HitObjectInfo.HitSound.Finish) {
						return NoteType.Spam;
					}
					throw new Exception(string.Format("Hold hit sound {0} does not correspond to any extra note type (lane {1}, time {2})", this.hitSound, this.laneNumber, this.time));
				}
			}
		}

		public NoteInfo.NoteSpeed GetNoteSpeed() {
			string text = this.hitSample[1];
			if (text == "1") {
				return NoteInfo.NoteSpeed.Stepped;
			}
			if (!(text == "2")) {
				return NoteInfo.NoteSpeed.Standard;
			}
			return NoteInfo.NoteSpeed.Fast;
		}
		
		public int x;

		public int y;

		public int time;

		public int type;

		public HitObjectInfo.HitSound hitSound;

		public string[] objectParams;

		public string[] hitSample;

		[Flags]
		public enum HitSound
		{
			Normal = 0,
			Whistle = 2,
			Finish = 4,
			Clap = 8,
			WhistleFinish = 6,
			WhistleClap = 10,
			FinishClap = 12,
			All = 14
		}
	}
}
