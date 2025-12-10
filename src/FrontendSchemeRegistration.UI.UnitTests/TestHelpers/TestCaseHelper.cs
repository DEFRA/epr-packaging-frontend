namespace FrontendSchemeRegistration.UI.UnitTests.TestHelpers;

using Application.Enums;

public class TestCaseHelper
{
    public static IEnumerable<object[]> GenerateRegistrationInformation()
    {
        yield return new object[]
        {
            SubmissionType.Registration,
            SubmissionSubType.CompanyDetails,
            Guid.NewGuid(),
            Guid.NewGuid()
        };
        yield return new object[]
        {
            SubmissionType.Registration,
            SubmissionSubType.Brands,
            Guid.NewGuid(),
            Guid.NewGuid()
        };
        yield return new object[]
        {
            SubmissionType.Registration,
            SubmissionSubType.Partnerships,
            Guid.NewGuid(),
            Guid.NewGuid()
        };
    }

    public static IEnumerable<object[]> GenerateRegistrationInformationWithSubId(bool withSubmissionId)
    {
        yield return new object[]
        {
            SubmissionType.Registration,
            SubmissionSubType.CompanyDetails,
            Guid.NewGuid(),
            Guid.NewGuid(),
            withSubmissionId,
            null
        };
    }

    public static IEnumerable<object[]> NewGuid()
    {
        yield return new object[]
        {
            Guid.NewGuid()
        };
    }
}