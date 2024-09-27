using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace FrontendSchemeRegistration.UI.Services.FileUploadLimits
{
    public class SubsidiariesFileUploadLimit : IFileUploadSize
    {
        private readonly int _fileUploadLimitInBytes;
        public SubsidiariesFileUploadLimit(IOptions<GlobalVariables> globalVariables)
        {
            _fileUploadLimitInBytes = globalVariables.Value.SubsidiaryFileUploadLimitInBytes;
        }
        public int FileUploadLimitInBytes => _fileUploadLimitInBytes;
    }
}
