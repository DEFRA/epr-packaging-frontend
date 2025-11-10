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

    private static readonly int CurrentYear = DateTime.Now.Year;
    private static readonly int CurrentMonth = DateTime.Now.Month;
    private const int January = 1;

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
        var complianceYear = GetComplianceYear(CurrentMonth);
        var viewModel = await _prnService.GetRecyclingObligationsCalculation(complianceYear);
        _logger.LogInformation("{LogPrefix}: PrnsObligationController - ObligationsHome: Recycling Obligations returned for year {Year} : {Results}", logPrefix, CurrentYear, JsonConvert.SerializeObject(viewModel));
               
        await FillViewModelFromSessionAsync(viewModel, CurrentYear, CurrentMonth);

        ViewBag.HomeLinkToDisplay = _globalVariables.Value.BasePath;
        return View(viewModel);
    }

    [HttpGet]
    [Route(PagePaths.Prns.ObligationPerMaterial + "/{material}")]
    public async Task<IActionResult> ObligationPerMaterial(string material)
    {
        PrnObligationViewModel viewModel = new();        

        _logger.LogInformation("{LogPrefix}: PrnsObligationController - ObligationPerMaterial: Get Recycling Obligations Calculation request for year {Year}, material {Material}", logPrefix, CurrentYear, material);

        if (Enum.TryParse(material, true, out MaterialType materialType))
        {
            var complianceYear = GetComplianceYear(CurrentMonth);
            viewModel = await _prnService.GetRecyclingObligationsCalculation(complianceYear);
            _logger.LogInformation("{LogPrefix}: PrnsObligationController - ObligationsHome: Recycling Obligations returned for year {Year} : {Results}", logPrefix, CurrentYear, JsonConvert.SerializeObject(viewModel));

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
        
        await FillViewModelFromSessionAsync(viewModel, CurrentYear, CurrentMonth);

        if (_urlOptions.ProducerResponsibilityObligations is not null)
        {
            ViewBag.ProducerResponsibilityObligationsLink = _urlOptions.ProducerResponsibilityObligations;
        }
        _logger.LogInformation("{LogPrefix}: PrnsObligationController - ObligationsHome: populated view model : {Results}", logPrefix, JsonConvert.SerializeObject(viewModel));

        ViewBag.BackLinkToDisplay = Url.Content($"~/{PagePaths.Prns.ObligationsHome}");

        return View(viewModel);
    }  

    [NonAction]
    public async Task FillViewModelFromSessionAsync(PrnObligationViewModel viewModel, int currentYear, int currentMonth)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        var organisation = session.UserData.Organisations?.FirstOrDefault();

        if (organisation == null)
        {
            _logger.LogWarning("{LogPrefix}: PrnsObligationController - FillViewModelFromSessionAsync - No organisation found in session.", logPrefix);
            return;
        }

        var isDirectProducer = organisation.OrganisationRole == OrganisationRoles.Producer;

        viewModel.OrganisationRole = organisation.OrganisationRole;
        viewModel.OrganisationName = isDirectProducer ? organisation.Name : session.RegistrationSession.SelectedComplianceScheme?.Name;
        viewModel.NationId = isDirectProducer ? organisation.NationId : session.RegistrationSession.SelectedComplianceScheme?.NationId ?? 0;

        var isJanuary = IsCurrentMonthJanuary(currentMonth);
        viewModel.ComplianceYear = isJanuary ? currentYear - 1 : currentYear;
        viewModel.DeadlineYear = isJanuary ? currentYear : currentYear + 1;
    }

    // This is a temp fix for the compliance window change
    private static int GetComplianceYear(int currentMonth)
    {
        return IsCurrentMonthJanuary(currentMonth) ? CurrentYear - 1 : CurrentYear;
    }

    private static bool IsCurrentMonthJanuary(int currentMonth)
    {
        return currentMonth == January;
    }
}