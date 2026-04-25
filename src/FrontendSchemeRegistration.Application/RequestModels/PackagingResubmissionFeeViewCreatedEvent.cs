
using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.RequestModels
{
        public class PackagingResubmissionFeeViewCreatedEvent 
    {
        public Guid? FileId { get; set; }

        public bool? IsPackagingResubmissionFeeViewed { get; set; }
    }
}
