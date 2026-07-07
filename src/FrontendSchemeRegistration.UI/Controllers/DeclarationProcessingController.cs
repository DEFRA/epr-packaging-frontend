namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.Enums;
using Application.Options;
using EPR.Common.Authorization.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Services;
using ViewModels;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
public class DeclarationProcessingController(
    IRegistrationApplicationService registrationApplicationService,
    IOptions<RegistrationFeeSnapshotPollingOptions> pollingOptions) : Controller
{
    private const string ViewName = "DeclarationProcessing";
    private const string ConfirmationController = "CompanyDetailsConfirmation";

    [HttpGet]
    [Route(PagePaths.DeclarationProcessing)]
    public IActionResult Get(
        [FromQuery] Guid submissionId,
        [FromQuery] int? registrationYear = null,
        [FromQuery] RegistrationJourney? registrationJourney = null)
    {
        var options = pollingOptions.Value;
        var routeValues = new { submissionId, registrationyear = registrationYear, registrationjourney = registrationJourney };

        var model = new DeclarationProcessingViewModel
        {
            SubmissionId = submissionId,
            StatusUrl = Url.Action(nameof(Status), routeValues) ?? string.Empty,
            FallbackUrl = Url.Action("Get", ConfirmationController, routeValues) ?? string.Empty,
            PollingIntervalMs = options.IntervalSeconds * 1000,
            PollingTimeoutMs = options.TimeoutSeconds * 1000
        };

        return View(ViewName, model);
    }

    [HttpGet]
    [Route(PagePaths.DeclarationProcessingStatus)]
    public async Task<JsonResult> Status(
        [FromQuery] Guid submissionId,
        [FromQuery] int? registrationYear = null,
        [FromQuery] RegistrationJourney? registrationJourney = null)
    {
        var populated = await registrationApplicationService.TryPopulateRegistrationFeeSnapshotAsync(
            HttpContext.Session,
            submissionId,
            HttpContext.RequestAborted);

        if (populated)
        {
            var routeValues = new { submissionId, registrationyear = registrationYear, registrationjourney = registrationJourney };
            return Json(new { redirectUrl = Url.Action("Get", ConfirmationController, routeValues) });
        }

        return Json(new { isFeeCalculationInProgress = true });
    }
}
