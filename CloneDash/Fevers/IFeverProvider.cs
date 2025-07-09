namespace CloneDash.Fevers;

/// <summary>
/// Provides a way to get <see cref="IFeverDescriptor"/>'s by name and a total list of available <see cref="IFeverDescriptor"/> names.
/// <br/>
/// By implementing this interface, it will auto-register with Clone Dash.
/// </summary>
public interface IFeverProvider
{
	public IFeverDescriptor? FindByName(string name);
	public int Priority { get; }
	public IEnumerable<string> GetAvailable();
}