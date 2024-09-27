using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace FrontendSchemeRegistration.UI.Services.FileUploadLimits
{
    public class DefaultFileUploadLimit : IFileUploadSize
    {
        private readonly int _fileUploadLimitInBytes;
        public DefaultFileUploadLimit(IOptions<GlobalVariables> globalVariables)
        {
            _fileUploadLimitInBytes = globalVariables.Value.FileUploadLimitInBytes;
        }
        public int FileUploadLimitInBytes => _fileUploadLimitInBytes;
    }
}
