using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;

public class PaymentInitiationRequest
{
    public Guid UserId { get; set; }

    public Guid OrganisationId { get; set; }

    public string Reference { get; set; }

    public string Description { get; set; }

    public string Regulator { get; set; }

    public int Amount { get; set; }

    public Guid? FileId { get; set; }
}
