using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.Subsidiary;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.SubsidiaryCheckDetails)]
public class SubsidiaryCheckDetailsController : Controller
{
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly ISubsidiaryService _subsidiaryService;

    public SubsidiaryCheckDetailsController(
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        ISubsidiaryService subsidiaryService)
    {
        _sessionManager = sessionManager;
        _subsidiaryService = subsidiaryService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (session?.SubsidiarySession?.Company == null)
        {
            return RedirectToAction("Get", "SubsidiaryCompaniesHouseNumber");
            session.SubsidiarySession = new SubsidiarySession();
        }

        var viewModel = new SubsidiaryCheckDetailsViewModel()
        {
            CompanyName = session.SubsidiarySession.Company.Name,
            CompaniesHouseNumber = session.SubsidiarySession.Company.CompaniesHouseNumber,
            BusinessAddress = new AddressViewModel
            {
                AddressSingleLine = session.SubsidiarySession.Company.BusinessAddress?.AddressSingleLine,
                Street = session.SubsidiarySession.Company.BusinessAddress?.Street,
                Town = session.SubsidiarySession.Company.BusinessAddress?.Town,
                County = session.SubsidiarySession.Company.BusinessAddress?.County,
                Country = session.SubsidiarySession.UkNation?.ToString(),
                Postcode = session.SubsidiarySession.Company.BusinessAddress?.Postcode
            }
        };

        ViewBag.BackLinkToDisplay = Url.Content($"~{PagePaths.SubsidiaryLocation}");

        return View("SubsidiaryCheckDetails", viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> Post(SubsidiaryCheckDetailsViewModel model)
    {
        ViewBag.BackLinkToDisplay = Url.Content($"~{PagePaths.SubsidiaryLocation}");

        if (!ModelState.IsValid)
        {
            return View("SubsidiaryCheckDetails", model);
        }

        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (session?.SubsidiarySession?.Company == null)
        {
            return RedirectToAction("Get", "SubsidiaryCompaniesHouseNumber");
        }

        var userOrganisation = User.GetUserData().Organisations.FirstOrDefault();

        var subsidiary = session.SubsidiarySession.Company;

        var newReferenceNumber = await _subsidiaryService.SaveSubsidiary(new SubsidiaryDto
        {
            Subsidiary = new OrganisationModel
            {
                OrganisationType = OrganisationType.NotSet,
                ProducerType = ProducerType.NotSet,
                CompaniesHouseNumber = subsidiary.CompaniesHouseNumber,
                Name = subsidiary.Name,
                Address = new AddressModel
                {
                    SubBuildingName = subsidiary.BusinessAddress?.SubBuildingName,
                    BuildingName = subsidiary.BusinessAddress?.BuildingName,
                    BuildingNumber = subsidiary.BusinessAddress?.BuildingNumber,
                    Street = subsidiary.BusinessAddress?.Street,
                    Locality = subsidiary.BusinessAddress?.Locality,
                    DependentLocality = subsidiary.BusinessAddress?.DependentLocality,
                    Town = subsidiary.BusinessAddress?.Town,
                    County = subsidiary.BusinessAddress?.County,
                    Postcode = subsidiary.BusinessAddress?.Postcode,
                    Country = subsidiary.BusinessAddress?.Country
                },
                ValidatedWithCompaniesHouse = true,
                IsComplianceScheme = false,
                Nation = session.SubsidiarySession.UkNation
            },
            ParentOrganisationId = userOrganisation.Id
        });

        _sessionManager.UpdateSessionAsync(HttpContext.Session, x => x.SubsidiarySession.Company.OrganisationId = newReferenceNumber);

        return RedirectToAction("Get", "SubsidiaryAdded");
    }
}