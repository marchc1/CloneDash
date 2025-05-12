namespace CloneDash.Characters;

public interface ICharacterProvider
{
	public ICharacterDescriptor? FindByName(string name);
	public int Priority { get; }
	public IEnumerable<string> GetAvailable();
}