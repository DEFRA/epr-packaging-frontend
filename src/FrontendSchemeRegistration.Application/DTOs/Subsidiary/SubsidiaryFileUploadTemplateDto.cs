using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Subsidiary
{
    [ExcludeFromCodeCoverage]
    public class SubsidiaryFileUploadTemplateDto
    {
        public string Name { get; set; }

        public string ContentType { get; set; }

        public Stream Content { get; set; }
    }
}
