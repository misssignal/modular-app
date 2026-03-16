using Semver;

namespace ModularApp.Sdk;

/// <summary>
/// Utility for checking engine version compatibility against a module's declared range.
/// </summary>
public static class VersionCompatibility
{
    /// <summary>
    /// Returns true if <paramref name="engineVersion"/> satisfies
    /// the module's declared <paramref name="compatibleRange"/>.
    /// </summary>
    public static bool IsCompatible(string engineVersion, string compatibleRange)
    {
        var engine = SemVersion.Parse(engineVersion, SemVersionStyles.Strict);
        var range = SemVersionRange.Parse(compatibleRange);
        return range.Contains(engine);
    }
}
