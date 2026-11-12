using CloneDash.Common.Compatibility;

namespace CloneDash.Common.Scenes;

/// <summary>
/// Provides <see cref="ISceneDescriptor"/>'s to <see cref="SceneMod"/>.
/// <br/>
/// By implementing this interface, it will auto-register with Clone Dash.
/// </summary>
public interface ISceneProvider : ICloneDashModProvider<ISceneDescriptor>;
