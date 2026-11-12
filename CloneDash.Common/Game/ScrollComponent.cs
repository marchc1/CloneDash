namespace CloneDash.Common.Game;

public class ScrollComponent
{
	readonly List<ScrollModifier> modifiers = [];

	public T Add<T>(T modifier) where T : ScrollModifier {
		modifiers.Add(modifier);
		return modifier;
	}

	public TimeRemap? GetRemap(IScrollable note) {
		foreach (var modifier in modifiers) {
			var remap = modifier.GetRemap(note);
			if (remap != null)
				return remap;
		}

		return null;
	}

	public double GetScrollSpeed(IScrollable note) {
		double speed = note.ScrollSpeed;
		foreach (var modifier in modifiers)
			speed *= modifier.GetSpeedFactor(note);

		return speed;
	}
}
