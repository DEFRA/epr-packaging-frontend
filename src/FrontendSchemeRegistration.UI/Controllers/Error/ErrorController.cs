namespace FrontendSchemeRegistration.UI.Controllers.Error;

using global::FrontendSchemeRegistration.UI.ViewModels.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Resources.Views.Error;

public class ErrorController : Controller
{
    private const string PageNotFoundView = "PageNotFound";

    [Route("error")]
    [AllowAnonymous]
    public IActionResult HandleThrownExceptions()
    {
        // Both UseStatusCodePagesWithReExecute and UseExceptionHandler re-execute this action, but only the
        // former rewrites the query string, so ?statusCode= cannot be trusted (a user could supply their own).
        // The response status code is already correct on both paths, and a ViewResult does not overwrite it.
        var reExecute = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
        var statusCode = reExecute?.OriginalStatusCode ?? HttpContext.Response.StatusCode;

        if (statusCode == StatusCodes.Status404NotFound)
        {
            return View(PageNotFoundView);
        }

        // With neither feature set nothing re-executed us, so this is a plain GET of /error: either a user
        // typing the URL, or one of the app's own redirects here to report a failure it has already logged
        // (a failed fee lookup, an empty payment link, a B2C remote failure, a missing submissionId). Both
        // are a service failure, so report one instead of serving "Something has gone wrong" as a 200 OK.
        if (reExecute is null && HttpContext.Features.Get<IExceptionHandlerFeature>() is null)
        {
            Response.StatusCode = StatusCodes.Status500InternalServerError;
        }

        return View(nameof(ProblemWithServiceError), new ErrorViewModel());
    }

    [Route("submission-error")]
    public IActionResult HandleThrownSubmissionException()
    {
        return View(nameof(ProblemWithSubmissionError));
    }

    [HttpGet]
    [Route("javascript-required")]
    [AllowAnonymous]
    public IActionResult JavaScriptRequired()
    {
        return View(nameof(JavaScriptRequired));
    }
}