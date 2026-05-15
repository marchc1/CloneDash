namespace Nucleus;

/// <summary>
/// Marks the class as needing to be statically constructed during the engine initialization. Will fix most convar/concommand issues.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
public class MarkForStaticConstructionAttribute : Attribute;
