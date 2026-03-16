namespace ModularApp.Sdk;

/// <summary>
/// Assembly-level attribute for fast module discovery.
/// The loader reads this attribute without instantiating any types.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class ModuleMetadataAttribute : Attribute
{
    /// <summary>Unique module identifier.</summary>
    public string ModuleId { get; }

    /// <summary>Assembly-qualified type name of the <see cref="IModule"/> implementation.</summary>
    public string ModuleType { get; }

    public ModuleMetadataAttribute(string moduleId, string moduleType)
    {
        ModuleId = moduleId;
        ModuleType = moduleType;
    }
}
