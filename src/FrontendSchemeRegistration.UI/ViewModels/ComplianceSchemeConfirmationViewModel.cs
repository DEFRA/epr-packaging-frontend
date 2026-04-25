namespace FrontendSchemeRegistration.UI.ViewModels;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Application.DTOs.ComplianceScheme;

public class ComplianceSchemeConfirmationViewModel
{
    [Required]
    public ComplianceSchemeDto SelectedComplianceScheme { get; set; }

    public ProducerComplianceSchemeDto? CurrentComplianceScheme { get; set; }
}