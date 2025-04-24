namespace Nucleus.Models;

public abstract class VertexClipper {
	public bool Active { get; protected set; }

	public abstract void Start();
	public abstract void End();
}
