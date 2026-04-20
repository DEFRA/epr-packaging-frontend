using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.UI.Attributes.ActionFilters;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels.Prns;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.Mvc;
using Newtonsoft.Json;

namespace FrontendSchemeRegistration.UI.Controllers.Prns;

using System.Diagnostics.CodeAnalysis;
using Application.Extensions;
using EPR.Common.Authorization.Models;
using Extensions;
using Helpers;
using Microsoft.FeatureManagement;

[SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Required for dependency injection")]
[FeatureGate(FeatureFlags.ShowPrn)]
[ServiceFilter(typeof(ComplianceSchemeIdHttpContextFilterAttribute))]
[PrnsObligationActionFilterAttribute]
public class PrnsObligationController : Controller
{
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly IPrnService _prnService;
    private readonly TimeProvider _timeProvider;
    private readonly IOptions<GlobalVariables> _globalVariables;
    private readonly ExternalUrlOptions _urlOptions;
    private readonly ILogger<PrnsObligationController> _logger;
    private readonly IFeatureManager _featureManager;
    private readonly IOptions<CsocOptions> _csocOptions;
    private readonly string _logPrefix;
    private readonly int _complianceYear;

    public PrnsObligationController(ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        IPrnService prnService,
        TimeProvider timeProvider,
        IOptions<GlobalVariables> globalVariables,
        IOptions<ExternalUrlOptions> urlOptions,
        ILogger<PrnsObligationController> logger,
        IFeatureManager featureManager,
        IOptions<CsocOptions> csocOptions)
    {
        _sessionManager = sessionManager;
        _prnService = prnService;
        _timeProvider = timeProvider;
        _globalVariables = globalVariables;
        _urlOptions = urlOptions.Value;
        _logger = logger;
        _featureManager = featureManager;
        _csocOptions = csocOptions;
        _logPrefix = _globalVariables.Value.LogPrefix;
        _complianceYear = timeProvider.GetUtcNow().GetComplianceYear();
    }

    private const string GlassOrNonGlassResource = "GlassOrNonGlassResource";

    [HttpGet]
    [Route(PagePaths.Prns.ObligationsHome)]
    public async Task<IActionResult> ObligationsHome()
    {
        var viewModel = await _prnService.GetRecyclingObligationsCalculation(_complianceYear);
        
        _logger.LogInformation(
            "{LogPrefix}: PrnsObligationController - ObligationsHome: Recycling Obligations returned for year {Year} : {Results}",
            _logPrefix, _complianceYear, JsonConvert.SerializeObject(viewModel));

        var userData = await FillViewModelFromSessionAsync(viewModel);

        ViewBag.HomeLinkToDisplay = _globalVariables.Value.BasePath;

        var isApprovedUser = userData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Delegated, ServiceRole.Approved);
        var organisation = userData.Organisations[0];
        
        viewModel.CsocViewModel = await CsocHelper.CreateViewModel(
            _featureManager, 
            isApprovedUser, 
            organisation,
            _timeProvider.GetLocalNow().DateTime,
            _csocOptions.Value,
            viewModel);
        
        return View(viewModel);
    }

    [HttpGet]
    [Route(PagePaths.Prns.ObligationPerMaterial + "/{material}")]
    public async Task<IActionResult> ObligationPerMaterial(string material)
    {
        PrnObligationViewModel viewModel = new();

        _logger.LogInformation(
            "{LogPrefix}: PrnsObligationController - ObligationPerMaterial: Get Recycling Obligations Calculation request for year {Year}, material {Material}",
            _logPrefix, _complianceYear, material);

        if (Enum.TryParse(material, true, out MaterialType materialType))
        {
            viewModel = await _prnService.GetRecyclingObligationsCalculation(_complianceYear);
            _logger.LogInformation(
                "{LogPrefix}: PrnsObligationController - ObligationsHome: Recycling Obligations returned for year {Year} : {Results}",
                _logPrefix, _complianceYear, JsonConvert.SerializeObject(viewModel));

            if (materialType == MaterialType.Glass || materialType == MaterialType.GlassRemelt ||
                materialType == MaterialType.RemainingGlass)
            {
                viewModel.MaterialObligationViewModels.Clear();
                ViewData[GlassOrNonGlassResource] =
                    PrnMaterialObligationViewModel.MaterialCategoryResource(materialType);
            }
            else if (materialType != MaterialType.Totals)
            {
                viewModel.MaterialObligationViewModels = viewModel.MaterialObligationViewModels
                    .Where(prn => prn.MaterialName == materialType).ToList();
                viewModel.GlassMaterialObligationViewModels.Clear();
                ViewData[GlassOrNonGlassResource] =
                    PrnMaterialObligationViewModel.MaterialCategoryResource(materialType);
            }
        }

        await FillViewModelFromSessionAsync(viewModel);

        if (_urlOptions.ProducerResponsibilityObligations is not null)
        {
            ViewBag.ProducerResponsibilityObligationsLink = _urlOptions.ProducerResponsibilityObligations;
        }

        _logger.LogInformation(
            "{LogPrefix}: PrnsObligationController - ObligationsHome: populated view model : {Results}", _logPrefix,
            JsonConvert.SerializeObject(viewModel));

        ViewBag.BackLinkToDisplay = Url.Content($"~/{PagePaths.Prns.ObligationsHome}");

        return View(viewModel);
    }

    [NonAction]
    public async Task<UserData> FillViewModelFromSessionAsync(PrnObligationViewModel viewModel)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        var organisation = session.UserData.Organisations?.FirstOrDefault();

        if (organisation == null)
        {
            _logger.LogWarning(
                "{LogPrefix}: PrnsObligationController - FillViewModelFromSessionAsync - No organisation found in session.",
                _logPrefix);
            return session.UserData;
        }

        var isDirectProducer = organisation.OrganisationRole == OrganisationRoles.Producer;

        viewModel.OrganisationRole = organisation.OrganisationRole;
        viewModel.OrganisationName = isDirectProducer
            ? organisation.Name
            : session.RegistrationSession.SelectedComplianceScheme?.Name;
        viewModel.NationId = isDirectProducer
            ? organisation.NationId
            : session.RegistrationSession.SelectedComplianceScheme?.NationId ?? 0;
        viewModel.ComplianceYear = _complianceYear;
        
        return session.UserData;
    }
}