using FrontendSchemeRegistration.UI.ViewModels.Prns;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers.Prns
{
    [FeatureGate("ShowPrn")]
    public class AcceptedPernsOrPrnsController : Controller
    {
        [HttpGet]
        [Route("accepted-prn")]
        [Route("accepted-pern")]
        public IActionResult AcceptedPernsOrPrns(string prnOrPernNumber)
        {
            var model = new AcceptedPernsOrPrnsViewModel()
            {
                PrnOrPernNumber = "EX4545452026",
                Tonnage = 170,
                Material = "paper and board",
                Year = 2024,
                DateIssued = DateTime.Now,
                AuthorisedBy = "PRNAuthoriser",
                IsDecemberWaste = false,
                IsPern = true,
                IssuedBy = "PRNIssuer",
                Note = "Note123",
                ProducerOrComplianceSchemeName = "Tesco",
                ProducerOrComplianceSchemeNumber = "TEC12345"
            };

            return View("Views/Prns/AcceptedPernsOrPrns.cshtml", model);
        }

        [HttpGet]
        [Route("download-prn")]
        public IActionResult DownloadPrn(string prnId)
        {
            throw new NotImplementedException("Downlaod functionality is yet to develop");
        }
    }
}
