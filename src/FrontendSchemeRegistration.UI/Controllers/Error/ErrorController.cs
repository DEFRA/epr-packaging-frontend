namespace FrontendSchemeRegistration.UI.Controllers.Error;

using global::FrontendSchemeRegistration.UI.ViewModels.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Resources.Views.Error;

public class ErrorController : Controller
{
    [Route("error")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleThrownExceptions()
    {
        return View(nameof(ProblemWithServiceError), new ErrorViewModel());
    }

    [Route("submission-error")]
    public async Task<IActionResult> HandleThrownSubmissionException()
    {
        return View(nameof(ProblemWithSubmissionError));
    }
}