using Nucleus.Common.Types;
using Nucleus.Types;

namespace CloneDash.Scenes;

public struct PathwayInformation
{
	public Color Color;
	public Vector2F Position;
	public object? UserData;

	public PathwayInformation(float x, float y, object? userdata) {
		Position = new(x, y);
		UserData = userdata;
	}
}
