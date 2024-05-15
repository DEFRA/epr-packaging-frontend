using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrontendSchemeRegistration.Application.DTOs
{
    [ExcludeFromCodeCoverage]
    public class AcceptApprovedPersonRequest
    {
        public string Telephone { get; set; }

        public string DeclarationFullName { get; set; }

        public string JobTitle { get; set; }

        public DateTime? DeclarationTimeStamp { get; set; }

        public string OrganisationName { get; set; }

        public string PersonFirstName { get; set; }

        public string PersonLastName { get; set; }

        public string OrganisationNumber { get; set; }

        public string ContactEmail { get; set; }
    }
}
