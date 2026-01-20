namespace FrontendSchemeRegistration.UI.UnitTests.Mappers;

using Application.Constants;
using Application.DTOs.Prns;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Constants;
using Moq;
using UI.Mappers;
using UI.ViewModels.Prns;

public class PrnModelMapperTests
{
    private IMapper _mapper;

    [SetUp]
    public void Setup()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new PrnModelMapper());
            cfg.ConstructServicesUsing(type =>
            {
                if (type == typeof(PrnAvailableAcceptanceYearsResolver))
                {
                    var mock = new Mock<IValueResolver<PrnModel, object, HashSet<int>>>();
                    mock.Setup(x => x.Resolve(
                            It.IsAny<PrnModel>(),
                            It.IsAny<BasePrnViewModel>(),
                            It.IsAny<HashSet<int>>(),
                            It.IsAny<ResolutionContext>()))
                        .Returns([2025]);

                    return mock.Object;
                }
                return null!;
            });
        });

        _mapper = configuration.CreateMapper();
    }

    [Test]
    public void Map_PrnModel_To_BasePrnViewModel_Maps_All_Properties()
    {
        // Arrange
        var issueDate = new DateTime(2025, 5, 15, 0, 0, 0, DateTimeKind.Utc);
        var prnModel = new PrnModel
        {
            ExternalId = Guid.NewGuid(),
            PrnNumber = "PRN12345",
            MaterialName = "Paper and board",
            IssueDate = issueDate,
            DecemberWaste = true,
            IssuedByOrg = "Test Org",
            TonnageValue = 100,
            PrnStatus = "AWAITINGACCEPTANCE",
            ObligationYear = "2026",
            IssuerNotes = "Test notes",
            IsExport = false
        };

        // Act
        var result = _mapper.Map<BasePrnViewModel>(prnModel);

        // Assert
        result.ExternalId.Should().Be(prnModel.ExternalId);
        result.PrnOrPernNumber.Should().Be("PRN12345");
        result.Material.Should().Be("Paper and board");
        result.DateIssued.Should().Be(issueDate);
        result.IsDecemberWaste.Should().BeTrue();
        result.IssuedBy.Should().Be("Test Org");
        result.Tonnage.Should().Be(100);
        result.ApprovalStatus.Should().Be(PrnStatus.AwaitingAcceptance);
        result.ObligationYear.Should().Be(2026);
        result.AdditionalNotes.Should().Be("Test notes");
        result.NoteType.Should().Be(PrnConstants.PrnText);
        result.IsStatusEditable.Should().BeTrue();
    }

    [Test]
    public void Map_PrnModel_To_BasePrnViewModel_Maps_IsExport_To_Pern()
    {
        // Arrange
        var prnModel = new PrnModel
        {
            IsExport = true
        };

        // Act
        var result = _mapper.Map<BasePrnViewModel>(prnModel);

        // Assert
        result.NoteType.Should().Be(PrnConstants.PernText);
    }

    [TestCase("2026", 2026)]
    [TestCase("2025", 2025)]
    [TestCase("invalid", 0)]
    [TestCase("", 0)]
    [TestCase(null, 0)]
    public void Map_PrnModel_To_BasePrnViewModel_Parses_ObligationYear(string obligationYear, int expected)
    {
        // Arrange
        var prnModel = new PrnModel
        {
            ExternalId = Guid.NewGuid(),
            PrnNumber = "PRN001",
            MaterialName = "Glass",
            IssueDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DecemberWaste = false,
            IssuedByOrg = "Test Org",
            TonnageValue = 25,
            PrnStatus = "ACCEPTED",
            ObligationYear = obligationYear,
            IsExport = false
        };

        // Act
        var result = _mapper.Map<BasePrnViewModel>(prnModel);

        // Assert
        result.ObligationYear.Should().Be(expected);
    }

    [TestCase("AWAITINGACCEPTANCE", PrnStatus.AwaitingAcceptance)]
    [TestCase("CANCELED", PrnStatus.Cancelled)]
    [TestCase("ACCEPTED", "ACCEPTED")]
    [TestCase("REJECTED", "REJECTED")]
    public void Map_PrnModel_To_BasePrnViewModel_Maps_Status_Correctly(string prnStatus, string expectedStatus)
    {
        // Arrange
        var prnModel = new PrnModel
        {
            ExternalId = Guid.NewGuid(),
            PrnNumber = "PRN002",
            MaterialName = "Plastic",
            IssueDate = new DateTime(2025, 2, 20, 0, 0, 0, DateTimeKind.Utc),
            DecemberWaste = false,
            IssuedByOrg = "Test Org",
            TonnageValue = 75,
            PrnStatus = prnStatus,
            ObligationYear = "2025",
            IsExport = false
        };

        // Act
        var result = _mapper.Map<BasePrnViewModel>(prnModel);

        // Assert
        result.ApprovalStatus.Should().Be(expectedStatus);
    }

    [Test]
    public void Map_PrnModel_To_PrnViewModel_Maps_All_Properties()
    {
        // Arrange
        var prnModel = new PrnModel
        {
            ExternalId = Guid.NewGuid(),
            PrnNumber = "PRN99999",
            MaterialName = "Steel",
            IssueDate = new DateTime(2025, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            DecemberWaste = false,
            IssuedByOrg = "Test Issuer",
            TonnageValue = 150,
            PrnStatus = "ACCEPTED",
            ObligationYear = "2025",
            IsExport = false,
            ReprocessingSite = "123 Test Street, Test City",
            PrnSignatory = "John Doe",
            PrnSignatoryPosition = "Manager",
            OrganisationName = "Test Company Ltd",
            ProcessToBeUsed = "Mechanical recycling",
            IssuerNotes = "Additional information"
        };

        // Act
        var result = _mapper.Map<PrnViewModel>(prnModel);

        // Assert
        result.ExternalId.Should().Be(prnModel.ExternalId);
        result.PrnOrPernNumber.Should().Be("PRN99999");
        result.Material.Should().Be("Steel");
        result.DateIssued.Should().Be(new DateTime(2025, 8, 1, 0, 0, 0, DateTimeKind.Utc));
        result.IsDecemberWaste.Should().BeFalse();
        result.IssuedBy.Should().Be("Test Issuer");
        result.Tonnage.Should().Be(150);
        result.ApprovalStatus.Should().Be("ACCEPTED");
        result.ObligationYear.Should().Be(2025);
        result.AdditionalNotes.Should().Be("Additional information");
        result.NoteType.Should().Be(PrnConstants.PrnText);
        result.ReproccessingSiteAddress.Should().Be("123 Test Street, Test City");
        result.AuthorisedBy.Should().Be("John Doe");
        result.NameOfProducerOrComplianceScheme.Should().Be("Test Company Ltd");
        result.Position.Should().Be("Manager");
        result.RecyclingProcess.Should().Be("Mechanical recycling");
    }

    [Test]
    public void Map_PrnModel_To_PrnViewModel_Handles_Null_Optional_Fields()
    {
        // Arrange
        var prnModel = new PrnModel
        {
            ExternalId = Guid.NewGuid(),
            PrnNumber = "PRN11111",
            MaterialName = "Wood",
            IssueDate = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            DecemberWaste = true,
            IssuedByOrg = "Test Org",
            TonnageValue = 200,
            PrnStatus = "AWAITINGACCEPTANCE",
            ObligationYear = "2026",
            IsExport = false,
            PrnSignatoryPosition = null,
            ProcessToBeUsed = null
        };

        // Act
        var result = _mapper.Map<PrnViewModel>(prnModel);

        // Assert
        result.Position.Should().Be(string.Empty);
        result.RecyclingProcess.Should().Be(string.Empty);
    }

    [Test]
    public void Map_PrnModel_To_PrnViewModel_IsSelected_Is_Ignored()
    {
        // Arrange
        var prnModel = new PrnModel
        {
            ExternalId = Guid.NewGuid(),
            PrnNumber = "PRN22222",
            MaterialName = "Glass",
            IssueDate = new DateTime(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc),
            DecemberWaste = false,
            IssuedByOrg = "Test Org",
            TonnageValue = 80,
            PrnStatus = "ACCEPTED",
            ObligationYear = "2025",
            IsExport = false
        };

        // Act
        var result = _mapper.Map<PrnViewModel>(prnModel);

        // Assert
        result.IsSelected.Should().BeFalse();
    }

    [Test]
    public void Map_PrnModel_To_PrnSearchResultViewModel_Maps_Base_Properties()
    {
        // Arrange
        var prnModel = new PrnModel
        {
            ExternalId = Guid.NewGuid(),
            PrnNumber = "PRN33333",
            MaterialName = "Aluminium",
            IssueDate = new DateTime(2025, 4, 20, 0, 0, 0, DateTimeKind.Utc),
            DecemberWaste = true,
            IssuedByOrg = "Search Test Org",
            TonnageValue = 120,
            PrnStatus = "REJECTED",
            ObligationYear = "2025",
            IsExport = false,
            IssuerNotes = "Search test notes"
        };

        // Act
        var result = _mapper.Map<PrnSearchResultViewModel>(prnModel);

        // Assert
        result.ExternalId.Should().Be(prnModel.ExternalId);
        result.PrnOrPernNumber.Should().Be("PRN33333");
        result.Material.Should().Be("Aluminium");
        result.DateIssued.Should().Be(new DateTime(2025, 4, 20, 0, 0, 0, DateTimeKind.Utc));
        result.IsDecemberWaste.Should().BeTrue();
        result.IssuedBy.Should().Be("Search Test Org");
        result.Tonnage.Should().Be(120);
        result.ApprovalStatus.Should().Be("REJECTED");
        result.ObligationYear.Should().Be(2025);
        result.AdditionalNotes.Should().Be("Search test notes");
    }

    [Test]
    public void Map_PrnModel_To_AwaitingAcceptanceResultViewModel_Maps_Base_Properties()
    {
        // Arrange
        var prnModel = new PrnModel
        {
            ExternalId = Guid.NewGuid(),
            PrnNumber = "PRN44444",
            MaterialName = "Paper and board",
            IssueDate = new DateTime(2025, 12, 15, 0, 0, 0, DateTimeKind.Utc),
            DecemberWaste = true,
            IssuedByOrg = "Awaiting Test Org",
            TonnageValue = 90,
            PrnStatus = "AWAITINGACCEPTANCE",
            ObligationYear = "2026",
            IsExport = false,
            IssuerNotes = "Awaiting notes"
        };

        // Act
        var result = _mapper.Map<AwaitingAcceptanceResultViewModel>(prnModel);

        // Assert
        result.ExternalId.Should().Be(prnModel.ExternalId);
        result.PrnOrPernNumber.Should().Be("PRN44444");
        result.Material.Should().Be("Paper and board");
        result.DateIssued.Should().Be(new DateTime(2025, 12, 15, 0, 0, 0, DateTimeKind.Utc));
        result.IsDecemberWaste.Should().BeTrue();
        result.IssuedBy.Should().Be("Awaiting Test Org");
        result.Tonnage.Should().Be(90);
        result.ApprovalStatus.Should().Be(PrnStatus.AwaitingAcceptance);
        result.ObligationYear.Should().Be(2026);
        result.AdditionalNotes.Should().Be("Awaiting notes");
        result.IsStatusEditable.Should().BeTrue();
    }

    [Test]
    public void Map_PrnModel_To_AwaitingAcceptanceResultViewModel_IsSelected_Is_Ignored()
    {
        // Arrange
        var prnModel = new PrnModel
        {
            ExternalId = Guid.NewGuid(),
            PrnNumber = "PRN55555",
            MaterialName = "Plastic",
            IssueDate = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            DecemberWaste = false,
            IssuedByOrg = "Test Org",
            TonnageValue = 60,
            PrnStatus = "AWAITINGACCEPTANCE",
            ObligationYear = "2025",
            IsExport = false
        };

        // Act
        var result = _mapper.Map<AwaitingAcceptanceResultViewModel>(prnModel);

        // Assert
        result.IsSelected.Should().BeFalse();
    }
}