using CloneDash.Scenes;

namespace CloneDash.Modding.Descriptors;

/// <summary>
/// Provides a way to get <see cref="ISceneDescriptor"/>'s by name and a total list of available <see cref="ISceneDescriptor"/> names.
/// <br/>
/// By implementing this interface, it will auto-register with Clone Dash.
/// </summary>
public interface ISceneProvider
{
	public ISceneDescriptor? FindByName(string name);
	public int Priority { get; }
	public IEnumerable<string> GetAvailable();
}