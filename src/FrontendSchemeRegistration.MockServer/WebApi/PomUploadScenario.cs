namespace FrontendSchemeRegistration.MockServer.WebApi;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Holds the expected outcome for POM (packaging data) file upload scenarios.
/// Set by BeforeScenario hooks; read by GET /api/v1/submissions/{id} callback.
/// </summary>
[ExcludeFromCodeCoverage]
public static class PomUploadScenario
{
    public const string Success = "Success";
    public const string Warnings = "Warnings";
    public const string Errors = "Errors";

    public static string? Current { get; set; }

    /// <summary>
    /// The submission ID returned in the Location header of the last POM file upload.
    /// Used by GetSubmissionByIdResponse to route the correct response type by ID.
    /// </summary>
    public static string? LastSubmissionId { get; set; }

    /// <summary>
    /// When true, the POST /api/v1/file-upload callback will not override Current with a
    /// filename-derived value. Set to true by BeforeScenario hooks and false by AfterScenario hooks.
    /// </summary>
    public static bool LockedByHook { get; set; }
}
