using FrontendSchemeRegistration.Application.DTOs;
using FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;

namespace FrontendSchemeRegistration.UI.Extensions
{
    internal static class ComplianceSchemeMemberExtension
    {
        internal static (
            IList<ComplianceSchemePaymentCalculationResponseMember> largeProducers, 
            IList<ComplianceSchemePaymentCalculationResponseMember> smallProducers) 
            GetIndividualProducers(
            this List<ComplianceSchemePaymentCalculationResponseMember> complianceSchemeMembers, 
            RegistrationFeeCalculationDetails[] registrationFeeCalculationDetails)
        {
            IList<ComplianceSchemePaymentCalculationResponseMember> largeProducers = [];
            IList<ComplianceSchemePaymentCalculationResponseMember> smallProducers = [];

            foreach (var csoMembershipDetail in registrationFeeCalculationDetails)
            {
                //Filter the member based on the member id match between the req and res object
                var complianceSchemeMember = complianceSchemeMembers
                    .Find(r => r.MemberId == csoMembershipDetail.OrganisationId && r.MemberRegistrationFee > 0);

                //Check the member type from the request object to filter the large producers
                if (csoMembershipDetail.OrganisationSize.Equals("Large", StringComparison.OrdinalIgnoreCase))
                {
                    largeProducers.Add(complianceSchemeMember);
                }

                //Check the member type from the request object to filter the small producers
                if (csoMembershipDetail.OrganisationSize.Equals("Small", StringComparison.OrdinalIgnoreCase))
                {
                    smallProducers.Add(complianceSchemeMember);
                }
            }

            return (largeProducers, smallProducers);
        }

        internal static int GetFees(this IList<ComplianceSchemePaymentCalculationResponseMember> complianceSchemeMembers) =>
            complianceSchemeMembers.Sum(r => r.MemberRegistrationFee);

        internal static IList<int> GetLateProducers(this List<ComplianceSchemePaymentCalculationResponseMember> complianceSchemeMembers) =>
            complianceSchemeMembers.Where(r => r.MemberLateRegistrationFee > 0).Select(r => r.MemberLateRegistrationFee).ToList();

        internal static IList<int> GetOnlineMarketPlaces(this List<ComplianceSchemePaymentCalculationResponseMember> complianceSchemeMembers) =>
            complianceSchemeMembers.Where(r => r.MemberOnlineMarketPlaceFee > 0).Select(r => r.MemberOnlineMarketPlaceFee).ToList();

        internal static IList<int> GetSubsidiariesCompanies(this List<ComplianceSchemePaymentCalculationResponseMember> complianceSchemeMembers) =>
            complianceSchemeMembers.Where(r => r.SubsidiariesFee > 0).Select(r => r.SubsidiariesFee).ToList();
    }
}
