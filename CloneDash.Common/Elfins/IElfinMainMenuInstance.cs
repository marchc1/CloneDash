using Nucleus.Types;

namespace CloneDash.Elfins;

public interface IElfinMainMenuInstance : IDisposable
{
	IElfinDescriptor GetElfin();

	void Render(Vector2F offset = default);
	void Update();
}
