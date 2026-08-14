namespace FrontendSchemeRegistration.UI.UnitTests.ViewModels.Prns;

using FluentAssertions;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.ViewModels.Prns;

[TestFixture]
public class AcceptedPrnsModelTests
{
    private static readonly Func<string, string> Identity = value => value;
    private static readonly Func<string, string> ResourceLookup = key => key switch
    {
        "you_have_accepted_one_prn_or_pern" => "You've accepted one {0}",
        "you_have_accepted_one_prn_or_pern_multi_year" => "You’ve accepted one {0} towards your {1} recycling obligations",
        "you_have_accepted_multipe_prn_or_pern" => "You’ve accepted {0} {1}",
        "you_have_accepted_multipe_prn_or_pern_multi_year" => "You’ve accepted {0} {1} towards your {2} recycling obligations",
        "you_have_accepted_mix_prns_and_perns" => "You’ve accepted {0} {1} and {2}",
        "you_have_accepted_mix_prns_and_perns_multi_year" => "You’ve accepted {0} {1} and {2} towards your {3} recycling obligations",
        _ => key
    };

    [Test]
    public void BuildConfirmationHeading_OnePrn_MultiYear_IncludesObligationYear()
    {
        var model = new AcceptedPrnsModel
        {
            Count = 1,
            NoteTypes = PrnConstants.PrnText,
            ObligationYears = "2026",
            Details = []
        };

        var result = model.BuildConfirmationHeading(true, ResourceLookup, Identity);

        result.Should().Be("You’ve accepted one PRN towards your 2026 recycling obligations");
    }

    [Test]
    public void BuildConfirmationHeading_MultiplePrns_MultiYear_IncludesCountAndYear()
    {
        var model = new AcceptedPrnsModel
        {
            Count = 5,
            NoteTypes = PrnConstants.PrnsText,
            ObligationYears = "2026",
            Details = []
        };

        var result = model.BuildConfirmationHeading(true, ResourceLookup, Identity);

        result.Should().Be("You’ve accepted 5 PRNs towards your 2026 recycling obligations");
    }

    [Test]
    public void BuildConfirmationHeading_MixedPrnsAndPerns_MultiYear_IncludesCountNoteTypesAndYear()
    {
        var model = new AcceptedPrnsModel
        {
            Count = 5,
            NoteTypes = PrnConstants.PrnsAndPernsText,
            ObligationYears = "2026",
            Details = []
        };

        var result = model.BuildConfirmationHeading(true, ResourceLookup, Identity);

        result.Should().Be("You’ve accepted 5 PRNs and PERNs towards your 2026 recycling obligations");
    }

    [Test]
    public void BuildConfirmationHeading_OnePern_WhenMultiYearDisabled_OmitsObligationYear()
    {
        var model = new AcceptedPrnsModel
        {
            Count = 1,
            NoteTypes = PrnConstants.PernText,
            ObligationYears = "2026",
            Details = []
        };

        var result = model.BuildConfirmationHeading(false, ResourceLookup, Identity);

        result.Should().Be("You've accepted one PERN");
    }

    [Test]
    public void BuildConfirmationHeading_MultiplePerns_WhenMultiYearDisabled_OmitsObligationYear()
    {
        var model = new AcceptedPrnsModel
        {
            Count = 3,
            NoteTypes = PrnConstants.PernsText,
            ObligationYears = "2026",
            Details = []
        };

        var result = model.BuildConfirmationHeading(false, ResourceLookup, Identity);

        result.Should().Be("You’ve accepted 3 PERNs");
    }

    [Test]
    public void BuildConfirmationHeading_Mixed_WhenMultiYearDisabled_OmitsObligationYear()
    {
        var model = new AcceptedPrnsModel
        {
            Count = 4,
            NoteTypes = PrnConstants.PrnsAndPernsText,
            ObligationYears = "2026",
            Details = []
        };

        var result = model.BuildConfirmationHeading(false, ResourceLookup, Identity);

        result.Should().Be("You’ve accepted 4 PRNs and PERNs");
    }
}
