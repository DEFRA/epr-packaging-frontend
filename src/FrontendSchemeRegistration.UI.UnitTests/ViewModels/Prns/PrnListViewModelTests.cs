using FluentAssertions;
using FrontendSchemeRegistration.UI.ViewModels.Prns;

namespace FrontendSchemeRegistration.UI.UnitTests.ViewModels.Prns
{
    public class PrnListViewModelTests
    {

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void GetCountBreakdown_WhenGivenPrnsOnly_Returns_Prn_Count(int prnCount)
        {
            var prns = new List<PrnViewModel>();
            for (int i = 0; i < prnCount; i++)
            {
                prns.Add(new PrnViewModel { NoteType = "PRN" });
            }

            var sut = new PrnListViewModel();

            var actual = sut.GetCountBreakdown(prns);
            actual.PrnCount.Should().Be(prnCount);
            actual.PernCount.Should().Be(0);
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void GetCountBreakdown_WhenGivenPernsOnly_Returns_Pern_Count(int pernCount)
        {
            var perns = new List<PrnViewModel>();
            for (int i = 0; i < pernCount; i++)
            {
                perns.Add(new PrnViewModel { NoteType = "PERN" });
            }

            var sut = new PrnListViewModel();

            var actual = sut.GetCountBreakdown(perns);
            actual.PernCount.Should().Be(pernCount);
            actual.PrnCount.Should().Be(0);
        }

        [Test]
        public void GetCountBreakdown_Returns_Prn_And_Pern_Counts()
        {
            var recyclingNotes = new List<PrnViewModel>();
            recyclingNotes.Add(new PrnViewModel { NoteType = "PRN" });
            recyclingNotes.Add(new PrnViewModel { NoteType = "PERN" });

            var sut = new PrnListViewModel();

            var actual = sut.GetCountBreakdown(recyclingNotes);
            actual.PernCount.Should().Be(1);
            actual.PrnCount.Should().Be(1);
        }

        [Test]
        [TestCase(0, "PRNs")]
        [TestCase(1, "PRN")]
        [TestCase(2, "PRNs")]
        public void GetPrnWord_WhenGivenPrnsOnly_Returns_Description(int prnCount, string word)
        {
            var sut = new PrnListViewModel();

            var actual = sut.GetPrnWord(prnCount);
            actual.Should().Be(word);
        }

        [Test]
        [TestCase(0, "PERNs")]
        [TestCase(1, "PERN")]
        [TestCase(2, "PERNs")]
        public void GetPernWord_WhenGivenPernsOnly_Returns_Description(int pernCount, string word)
        {
            var sut = new PrnListViewModel();

            var actual = sut.GetPernWord(pernCount);
            actual.Should().Be(word);
        }

        [Test]
        [TestCase(0, 0, "")]
        [TestCase(0, 1, "PERN")]
        [TestCase(0, 2, "PERNs")]
        [TestCase(1, 0, "PRN")]
        [TestCase(1, 1, "PRNs,PERNs")]
        [TestCase(1, 2, "PRNs,PERNs")]
        [TestCase(2, 0, "PRNs")]
        [TestCase(2, 1, "PRNs,PERNs")]
        [TestCase(2, 2, "PRNs,PERNs")]
        public void GetNoteType_Returns_Singular_Or_Plural_Prn_And_Pern_Description(int prnCount, int pernCount, string expected)
        {
            var recyclingNotes = new List<PrnViewModel>();
            for (int i = 0; i < prnCount; i++)
            {
                recyclingNotes.Add(new PrnViewModel { NoteType = "PRN" });
            }

            for (int j = 0; j < pernCount; j++)
            {
                recyclingNotes.Add(new PrnViewModel { NoteType = "PERN" });
            }

            var sut = new PrnListViewModel();

            var actual = sut.GetNoteType(recyclingNotes);
            actual.Should().Be(expected);
        }

        [Test]
        [TestCase(0, 0, "")]
        [TestCase(0, 1, "PERNs")]
        [TestCase(0, 2, "PERNs")]
        [TestCase(1, 0, "PRNs")]
        [TestCase(1, 1, "PRNs,PERNs")]
        [TestCase(1, 2, "PRNs,PERNs")]
        [TestCase(2, 0, "PRNs")]
        [TestCase(2, 1, "PRNs,PERNs")]
        [TestCase(2, 2, "PRNs,PERNs")]
        public void GetPluralNoteType_Returns_Plural_Prn_And_Pern_Description(int prnCount, int pernCount, string expected)
        {
            var recyclingNotes = new List<PrnViewModel>();
            for (int i = 0; i < prnCount; i++)
            {
                recyclingNotes.Add(new PrnViewModel { NoteType = "PRN" });
            }

            for (int j = 0; j < pernCount; j++)
            {
                recyclingNotes.Add(new PrnViewModel { NoteType = "PERN" });
            }

            var sut = new PrnListViewModel();

            var actual = sut.GetPluralNoteType(recyclingNotes);
            actual.Should().Be(expected);
        }
    }
}
