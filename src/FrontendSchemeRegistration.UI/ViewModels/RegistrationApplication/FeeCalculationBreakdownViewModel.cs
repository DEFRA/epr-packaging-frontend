using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.DTOs.Submission;

namespace FrontendSchemeRegistration.UI.ViewModels.RegistrationApplication;

[ExcludeFromCodeCoverage]
public class FeeCalculationBreakdownViewModel
{
    public string OrganisationSize { get; set; } = "large";

    public bool IsOnlineMarketplace { get; set; }

    public int NumberOfSubsidiaries { get; set; }

    public int NumberOfSubsidiariesBeingOnlineMarketplace { get; set; }

    public int BaseFee { get; set; }

    public int OnlineMarketplaceFee { get; set; }

    public int ProducerLateRegistrationFee { get; set; }

    public int TotalSubsidiaryFee { get; set; }
    public int TotalSubsidiaryOnlineMarketplaceFee { get; set; }

    public int TotalPreviousPayments { get; set; }

    public int TotalFeeAmount { get; set; }

    public int TotalAmountOutstanding { get; set; }

    public bool IsRegistrationFeePaid { get; set; }

    public bool IsLateFeeApplicable { get; set; }
    
    public ApplicationStatusType ApplicationStatus { get; set; }

    public bool RegistrationApplicationSubmitted { get; set; }

    public int RegistrationYear { get; set; }
}