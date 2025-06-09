
using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.RequestModels
{
    [ExcludeFromCodeCoverage]
    public class PackagingResubmissionFeeViewCreatedEvent 
    {
        public Guid? FileId { get; set; }

        public bool? IsPackagingResubmissionFeeViewed { get; set; }
    }
}
