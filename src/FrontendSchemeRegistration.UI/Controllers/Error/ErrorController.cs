namespace FrontendSchemeRegistration.UI.Controllers.Error;

using Microsoft.AspNetCore.Mvc;
using Resources.Views.Error;

public class ErrorController : Controller
{
    [Route("error")]
    public async Task<IActionResult> HandleThrownExceptions()
    {
        return View(nameof(ProblemWithServiceError));
    }

    [Route("submission-error")]
    public async Task<IActionResult> HandleThrownSubmissionException()
    {
        return View(nameof(ProblemWithSubmissionError));
    }
}