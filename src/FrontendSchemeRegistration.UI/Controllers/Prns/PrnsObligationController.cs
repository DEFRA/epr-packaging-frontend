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
    private readonly string logPrefix;

    public PrnsObligationController(ISessionManager<FrontendSchemeRegistrationSession> sessionManager, IPrnService prnService, IOptions<GlobalVariables> globalVariables, IOptions<ExternalUrlOptions> urlOptions, ILogger<PrnsObligationController> logger)
    {
        _sessionManager = sessionManager;
        _prnService = prnService;
        _globalVariables = globalVariables;
        _urlOptions = urlOptions.Value;
        _logger = logger;
        logPrefix = _globalVariables.Value.LogPrefix;
    }

    private const string GlassOrNonGlassResource = "GlassOrNonGlassResource";

    [HttpGet]
    [Route(PagePaths.Prns.ObligationsHome)]
    public async Task<IActionResult> ObligationsHome()
    {
        var year = DateTime.Now.Year;

        _logger.LogInformation("{Logprefix}: PrnsObligationController - ObligationsHome: Get Recycling Obligations Calculation request for year {Year}", logPrefix, year);

        var viewModel = await _prnService.GetRecyclingObligationsCalculation(year);

        _logger.LogInformation("{Logprefix}: PrnsObligationController - ObligationsHome: Recycling Obligations returned for year {Year} : {Results}", logPrefix, year, JsonConvert.SerializeObject(viewModel));

        await FillViewModelFromSessionAsync(viewModel, year);

        ViewBag.HomeLinkToDisplay = _globalVariables.Value.BasePath;
        return View(viewModel);
    }

    [HttpGet]
    [Route(PagePaths.Prns.ObligationPerMaterial + "/{material}")]
    public async Task<IActionResult> ObligationPerMaterial(string material)
    {
        int year = DateTime.Now.Year;
        PrnObligationViewModel viewModel = new();

        _logger.LogInformation("{Logprefix}: PrnsObligationController - ObligationPerMaterial: Get Recycling Obligations Calculation request for year {Year}, material {Material}", logPrefix, year, material);

        if (Enum.TryParse(material, true, out MaterialType materialType))
        {
            viewModel = await _prnService.GetRecyclingObligationsCalculation(year);
            _logger.LogInformation("{Logprefix}: PrnsObligationController - ObligationsHome: Recycling Obligations returned for year {Year} : {Results}", logPrefix, year, JsonConvert.SerializeObject(viewModel));

            if (materialType == MaterialType.Glass || materialType == MaterialType.GlassRemelt || materialType == MaterialType.RemainingGlass)
            {
                viewModel.MaterialObligationViewModels.Clear();
                ViewData[GlassOrNonGlassResource] = PrnMaterialObligationViewModel.MaterialCategoryResource(materialType);
            }
            else if (materialType != MaterialType.Totals)
            {
                viewModel.MaterialObligationViewModels = viewModel.MaterialObligationViewModels.Where(prn => prn.MaterialName == materialType).ToList();
                viewModel.GlassMaterialObligationViewModels.Clear();
                ViewData[GlassOrNonGlassResource] = PrnMaterialObligationViewModel.MaterialCategoryResource(materialType);
            }
        }

        await FillViewModelFromSessionAsync(viewModel, year);

        if (_urlOptions.ProducerResponsibilityObligations is not null)
        {
            ViewBag.ProducerResponsibilityObligationsLink = _urlOptions.ProducerResponsibilityObligations;
        }

        _logger.LogInformation("{Logprefix}: PrnsObligationController - ObligationsHome: populated view model : {Results}", logPrefix, JsonConvert.SerializeObject(viewModel));

        ViewBag.BackLinkToDisplay = Url.Content($"~/{PagePaths.Prns.ObligationsHome}");

        return View(viewModel);
    }

    [NonAction]
    public async Task FillViewModelFromSessionAsync(PrnObligationViewModel viewModel, int year)
    {
        _logger.LogInformation("{Logprefix}: PrnsObligationController - FillViewModelFromSessionAsync: Get oblication calculation from Session year: {Year}, obligation model: {ViewModel}", logPrefix, year, JsonConvert.SerializeObject(viewModel));
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        _logger.LogInformation("{Logprefix}: PrnsObligationController - FillViewModelFromSessionAsync: Session results : {Results}", logPrefix, JsonConvert.SerializeObject(session));

        var organisation = session.UserData.Organisations?.FirstOrDefault();

        if (organisation != null)
        {
            var isDirectProducer = organisation.OrganisationRole == OrganisationRoles.Producer;
            viewModel.OrganisationRole = organisation.OrganisationRole;
            viewModel.OrganisationName = isDirectProducer ? organisation.Name : session.RegistrationSession.SelectedComplianceScheme?.Name;
            viewModel.NationId = isDirectProducer ? organisation.NationId : (session.RegistrationSession.SelectedComplianceScheme?.NationId ?? 0);
            viewModel.CurrentYear = year;
            viewModel.DeadlineYear = year + 1;
        }

        _logger.LogInformation("{Logprefix}: PrnsObligationController - FillViewModelFromSessionAsync: updated Organisation details : {Organisation}", logPrefix, JsonConvert.SerializeObject(organisation));
    }
}