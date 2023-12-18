namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Microsoft.AspNetCore.Mvc;
using UI.Attributes.ActionFilters;
using ViewModels;

[Route(PagePaths.FileUploadSubmissionError)]
public class FileUploadSubmissionErrorController : Controller
{
    [HttpGet]
    [SubmissionIdActionFilter(PagePaths.FileUploadSubLanding)]
    public IActionResult Get()
    {
        var model = new FileUploadSubmissionErrorViewModel
        {
            SubmissionId = Guid.Parse(Request.Query["submissionId"])
        };

        return View("FileUploadSubmissionError", model);
    }
}