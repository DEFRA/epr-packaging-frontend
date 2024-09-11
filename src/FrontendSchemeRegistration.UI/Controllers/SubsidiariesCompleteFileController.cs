using EPR.Common.Authorization.Constants;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.SubsidiariesCompleteFile)]
public class SubsidiariesCompleteFileController : Controller
{
    private readonly ISubsidiaryService _subsidiaryService;

    public SubsidiariesCompleteFileController(ISubsidiaryService subsidiaryService)
    {
        _subsidiaryService = subsidiaryService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return View("SubsidiariesCompleteFile");
    }

    [HttpGet("template")]
    public async Task<IActionResult> GetFileUploadTemplate()
    {
        var file = await _subsidiaryService.GetFileUploadTemplateAsync();

        if (file == null)
        {
            return Redirect("/errors");
        }

        return File(file.Content, file.ContentType, file.Name);
    }
}
