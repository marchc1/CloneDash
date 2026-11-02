using CloneDash.Common.Compatibility;

namespace CloneDash.Characters;

/// <summary>
/// Provides <see cref="ICharacterDescriptor"/>'s to <see cref="CharacterMod"/>.
/// <br/>
/// By implementing this interface, it will auto-register with Clone Dash.
/// </summary>
public interface ICharacterProvider : ICloneDashModProvider<ICharacterDescriptor>;
