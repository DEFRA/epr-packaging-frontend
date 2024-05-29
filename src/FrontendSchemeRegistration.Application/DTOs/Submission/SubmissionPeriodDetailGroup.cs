using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.Enums;

namespace FrontendSchemeRegistration.Application.DTOs.Submission;

public class SubmissionPeriodDetailGroup
{
    public string DataPeriod { get; set; }

    public List<SubmissionPeriodDetail> SubmissionPeriodDetails { get; set; }

    public int Quantity { get; set; }

    public string DatePeriodYear { get; set; }
}