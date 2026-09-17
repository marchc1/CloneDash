using System.Globalization;

namespace CloneDash.Unbeatable.Internal;

public class NoteInfo
{
	public Lane lane {
		get {
			return new Lane {
				height = this.height,
				side = this.side
			};
		}
	}

	public bool hasEndTime {
		get {
			return this.type == NoteType.Double || this.type == NoteType.Hold || this.type == NoteType.Spam;
		}
	}

	public NoteInfo(HitObjectInfo hitObject, Side side) {
		this.time = (float)hitObject.time;
		this.side = side;
		this.height = hitObject.GetNoteHeight();
		this.type = hitObject.GetNoteType();
		this.hiding = hitObject.IsHiding();
		this.speed = hitObject.GetNoteSpeed();
		this.spawnMid = hitObject.IsSpawnMid();
		this.extra = hitObject.objectParams;
	}

	public float GetEndTime() {
		if (!this.hasEndTime)
			throw new Exception(string.Format("Cannot get an end time for note type {0}", this.type));

		return float.Parse(this.extra[0], CultureInfo.InvariantCulture);
	}

	public float time;
	public Side side;
	public Height height;
	public NoteType type;
	public NoteSpeed speed;
	public bool hiding;
	public bool spawnMid;
	public string[] extra;

	public enum NoteSpeed
	{
		Standard,
		Stepped,
		Fast
	}
}
