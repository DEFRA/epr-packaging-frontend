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

    [HttpGet]
    [Route(PagePaths.SubsidiaryTemplateDownload)]
    public IActionResult TemplateFileDownload()
    {
        TempData["DownloadCompleted"] = false;
        return RedirectToAction(nameof(TemplateFileUploadView), "SubsidiariesCompleteFile");
    }

    [HttpGet]
    [Route(PagePaths.SubsidiaryTemplateDownloadView)]
    public IActionResult TemplateFileUploadView() 
    {
        return View(nameof(TemplateFileDownload));
    }

    [HttpGet]
    [Route(PagePaths.SubsidiaryTemplateDownloadFailed)]
    public IActionResult TemplateFileDownloadFailed()
    {
        return View(nameof(TemplateFileDownloadFailed));
    }

    [HttpGet("template")]
    public async Task<IActionResult> GetFileUploadTemplate()
    {
        try
        {
            var file = await _subsidiaryService.GetFileUploadTemplateAsync();

            if (file == null)
            {
                return RedirectToAction(nameof(TemplateFileDownloadFailed));
            }
            TempData["DownloadCompleted"] = true;
            return File(file.Content, file.ContentType, file.Name);
        }
        catch (Exception ex)
        {
            return RedirectToAction(nameof(TemplateFileDownloadFailed));
        }
    }
}
