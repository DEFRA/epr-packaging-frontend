﻿using System.Diagnostics.CodeAnalysis;
using EPR.Common.Authorization.Interfaces;
using EPR.Common.Authorization.Models;

namespace FrontendSchemeRegistration.UI.Sessions;

using Application.DTOs.Submission;

[ExcludeFromCodeCoverage]
public class FrontendSchemeRegistrationSession : IHasUserData
{
    public UserData UserData { get; set; } = new();

    public RegistrationSession RegistrationSession { get; set; } = new();

    public NominatedDelegatedPersonSession NominatedDelegatedPersonSession { get; set; } = new();

    public SchemeMembershipSession SchemeMembershipSession { get; set; } = new();

    public NominatedApprovedPersonSession NominatedApprovedPersonSession { get; set; } = new();

    public SubsidiarySession SubsidiarySession { get; set; } = new();

    public PrnSession PrnSession { get; set; } = new();

    public RegistrationApplicationSession RegistrationApplicationSession { get; set; } = new ();
}