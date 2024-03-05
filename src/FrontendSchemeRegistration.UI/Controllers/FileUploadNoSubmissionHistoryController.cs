using EPR.Common.Authorization.Constants;
using FrontendSchemeRegistration.Application.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers
{
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.FileUploadNoSubmissionHistory)]
    public class FileUploadNoSubmissionHistoryController : Controller
    {
        public async Task<IActionResult> Get()
        {
            return View("FileUploadNoSubmissionHistory");
        }
    }
}
