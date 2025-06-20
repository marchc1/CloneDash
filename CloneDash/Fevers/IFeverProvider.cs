namespace CloneDash.Fevers;

public interface IFeverProvider
{
	public IFeverDescriptor? FindByName(string name);
	public int Priority { get; }
	public IEnumerable<string> GetAvailable();
}