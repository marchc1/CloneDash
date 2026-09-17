using System;
using System.Collections.Generic;
using System.Text;

namespace CloneDash.Unbeatable.Internal;

public class FlipInfo
{
	public FlipInfo(int time, bool toggleCenter = false, CameraSwapMode swapMode = CameraSwapMode.ZoomOut) {
		this.time = time;
		this.toggleCenter = toggleCenter;
		this.swapMode = swapMode;
	}

	public FlipInfo(HitObjectInfo hitObject) {
		this.time = hitObject.time;
		this.toggleCenter = hitObject.IsToggleCenter();
		this.swapMode = (hitObject.IsCameraSwapImmediate() ? CameraSwapMode.Immediate : CameraSwapMode.ZoomOut);
	}

	public int time;
	public bool toggleCenter;
	public CameraSwapMode swapMode;
	public NoteInfo nextNote;
	public NoteInfo twinNote;
}
