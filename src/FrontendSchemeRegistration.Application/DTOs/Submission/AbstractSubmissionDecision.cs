﻿using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Submission;

[ExcludeFromCodeCoverage]
public abstract class AbstractSubmissionDecision : AbstractSubmission
{
    public string Decision { get; set; } = string.Empty;

    public string Comment { get; set; } = string.Empty;

    public bool IsResubmissionRequired { get; set; }
}