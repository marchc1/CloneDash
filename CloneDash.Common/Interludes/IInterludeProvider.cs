using CloneDash.Common.Compatibility;

namespace CloneDash.Common.Interludes;

/// <summary>
/// Provides <see cref="IInterludeDescriptor"/>'s to <see cref="InterludeMod"/>.
/// <br/>
/// By implementing this interface, it will auto-register with Clone Dash.
/// </summary>
public interface IInterludeProvider : ICloneDashModProvider<IInterludeDescriptor>;
