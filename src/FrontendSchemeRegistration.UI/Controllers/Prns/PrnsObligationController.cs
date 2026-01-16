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

using Application.Extensions;

[FeatureGate(FeatureFlags.ShowPrn)]
[ServiceFilter(typeof(ComplianceSchemeIdHttpContextFilterAttribute))]
[PrnsObligationActionFilterAttribute]
public class PrnsObligationController : Controller
{
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly IPrnService _prnService;
    private readonly IOptions<GlobalVariables> _globalVariables;
    private readonly ExternalUrlOptions _urlOptions;
    private readonly ILogger<PrnsObligationController> _logger;
    private readonly string _logPrefix;
    private readonly int _complianceYear;

    public PrnsObligationController(ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        IPrnService prnService, IOptions<GlobalVariables> globalVariables, IOptions<ExternalUrlOptions> urlOptions,
        ILogger<PrnsObligationController> logger)
    {
        _sessionManager = sessionManager;
        _prnService = prnService;
        _globalVariables = globalVariables;
        _urlOptions = urlOptions.Value;
        _logger = logger;
        _logPrefix = _globalVariables.Value.LogPrefix;

        var now = DateTime.UtcNow;
        var date = new DateTime(
            _globalVariables.Value.OverrideCurrentYear ?? now.Year,
            _globalVariables.Value.OverrideCurrentMonth ?? now.Month,
            now.Day,
            0,
            0,
            0,
            DateTimeKind.Utc);

        _complianceYear = date.GetComplianceYear();
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

        await FillViewModelFromSessionAsync(viewModel);

        ViewBag.HomeLinkToDisplay = _globalVariables.Value.BasePath;
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
    public async Task FillViewModelFromSessionAsync(PrnObligationViewModel viewModel)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        var organisation = session.UserData.Organisations?.FirstOrDefault();

        if (organisation == null)
        {
            _logger.LogWarning(
                "{LogPrefix}: PrnsObligationController - FillViewModelFromSessionAsync - No organisation found in session.",
                _logPrefix);
            return;
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
    }
}