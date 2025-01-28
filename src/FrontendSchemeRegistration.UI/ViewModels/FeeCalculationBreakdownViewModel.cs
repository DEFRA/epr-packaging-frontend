﻿using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels;

[ExcludeFromCodeCoverage]
public class FeeCalculationBreakdownViewModel
{
    public string ProducerSize { get; set; } = "large";

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

    public int TotalAmountOutstanding => TotalFeeAmount - TotalPreviousPayments;

    public bool RegistrationFeePaid { get; set; }

    public bool IsLateFeeApplicable { get; set; }

    public bool RegistrationApplicationSubmitted { get; set; }
}