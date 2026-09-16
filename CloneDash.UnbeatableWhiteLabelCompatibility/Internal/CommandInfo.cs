namespace CloneDash.Unbeatable.Internal;

public class CommandInfo
{
	public CommandInfo(HitObjectInfo hitObject, Side side) {
		this.start = hitObject.time;
		if (hitObject.IsHoldType()) {
			this.end = int.Parse(hitObject.objectParams[0]);
			this.hold = true;
		}
		else {
			this.end = this.start;
			this.hold = false;
		}
		this.lane = hitObject.laneNumber;
		this.whistle = (hitObject.hitSound & HitObjectInfo.HitSound.Whistle) > HitObjectInfo.HitSound.Normal;
		this.finish = (hitObject.hitSound & HitObjectInfo.HitSound.Finish) > HitObjectInfo.HitSound.Normal;
		this.clap = (hitObject.hitSound & HitObjectInfo.HitSound.Clap) > HitObjectInfo.HitSound.Normal;
		this.side = side;
	}
	
	public int start;
	public bool hold;
	public int end;
	public int lane;
	public bool whistle;
	public bool finish;
	public bool clap;
	public Side side;
}