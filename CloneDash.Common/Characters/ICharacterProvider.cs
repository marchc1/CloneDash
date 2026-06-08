namespace CloneDash.Characters;

/// <summary>
/// Provides a way to get <see cref="ICharacterDescriptor"/>'s by name and a total list of available <see cref="ICharacterDescriptor"/> names.
/// <br/>
/// By implementing this interface, it will auto-register with Clone Dash.
/// </summary>
public interface ICharacterProvider
{
	public ICharacterDescriptor? FindByName(string name);
	public int Priority { get; }
	public IEnumerable<string> GetAvailable();
}