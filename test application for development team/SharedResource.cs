using Microsoft.Extensions.Localization;

// The assembly name contains spaces, so tell the localizer the real root namespace;
// otherwise Resources/SharedResource.*.resx is never found.
[assembly: RootNamespace("test_application_for_development_team")]

namespace test_application_for_development_team;

/// <summary>Marker type for the shared UI texts in Resources/SharedResource.*.resx.</summary>
public class SharedResource;
