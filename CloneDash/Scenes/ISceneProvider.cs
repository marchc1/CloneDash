using CloneDash.Scenes;

namespace CloneDash.Modding.Descriptors;
public interface ISceneProvider {
	public ISceneDescriptor? FindByName(string name);
	public int Priority { get; }
	public IEnumerable<string> GetAvailable();
}