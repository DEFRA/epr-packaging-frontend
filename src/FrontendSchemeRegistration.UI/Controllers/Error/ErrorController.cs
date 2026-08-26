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
        var statusCode = HttpContext.Features.Get<IStatusCodeReExecuteFeature>()?.OriginalStatusCode
                         ?? HttpContext.Response.StatusCode;

        return statusCode == StatusCodes.Status404NotFound
            ? View(PageNotFoundView)
            : View(nameof(ProblemWithServiceError), new ErrorViewModel());
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