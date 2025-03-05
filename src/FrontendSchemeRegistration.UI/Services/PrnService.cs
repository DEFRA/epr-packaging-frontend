using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs;
using FrontendSchemeRegistration.Application.DTOs.Prns;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.ViewModels;
using FrontendSchemeRegistration.UI.ViewModels.Prns;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace FrontendSchemeRegistration.UI.Services;

public class PrnService : IPrnService
{
    private readonly IAccountServiceApiClient _accountServiceApiClient;
	private readonly IWebApiGatewayClient _webApiGatewayClient;
	private readonly IStringLocalizer<PrnCsvResources> _csvLocalizer;
    private readonly IStringLocalizer<PrnDataResources> _dataLocalizer;
    private IOptions<GlobalVariables> _globalVariables;
    private readonly ILogger<PrnService> _logger;
    private readonly string logPrefix;

    public PrnService(IAccountServiceApiClient accountServiceApiClient, IWebApiGatewayClient webApiGatewayClient, IStringLocalizer<PrnCsvResources> csvLocalizer, IStringLocalizer<PrnDataResources> dataLocalizer, IOptions<GlobalVariables> globalVariables, ILogger<PrnService> logger)
    {
        _accountServiceApiClient = accountServiceApiClient;
        _webApiGatewayClient = webApiGatewayClient;
        _csvLocalizer = csvLocalizer;
        _dataLocalizer = dataLocalizer;
        _globalVariables = globalVariables;
        _logger = logger;
        logPrefix = _globalVariables.Value.LogPrefix;
    }

    // Used by "View all PRNs and PERNs" page
    public async Task<PrnListViewModel> GetAllPrnsAsync()
    {
        _logger.LogInformation("{Logprefix}: PrnService - GetAllPrnsAsync: Get all Prns for logged in user", logPrefix);
        var model = new PrnListViewModel();
        List<PrnModel> serverResponse = await _webApiGatewayClient.GetPrnsForLoggedOnUserAsync();
        _logger.LogInformation("{Logprefix}: PrnService - GetAllPrnsAsync: Get all Prns for logged in user prns returned : {Prns}", logPrefix, JsonConvert.SerializeObject(serverResponse));

        List<PrnViewModel> prnViewModels = serverResponse.Select(item => (PrnViewModel)item).ToList();
        model.Prns = prnViewModels.OrderByDescending(x => x.DateIssued).Take(9).ToList();
        _logger.LogInformation("{Logprefix}: PrnService - GetAllPrnsAsync: Get all Prns for logged in user PrnViewModel returned : {PrnViewModels}", logPrefix, JsonConvert.SerializeObject(prnViewModels));
        return model;
    }

    // Used by "Accept or reject PRNs and PERNs" page
    public async Task<PrnListViewModel> GetPrnsAwaitingAcceptanceAsync()
    {
        _logger.LogInformation("{Logprefix}: PrnService - GetPrnsAwaitingAcceptanceAsync: Get Prns awaiting acceptance for logged in user", logPrefix);

        var model = new PrnListViewModel();
        List<PrnModel> serverResponse = await _webApiGatewayClient.GetPrnsForLoggedOnUserAsync();
        _logger.LogInformation("{Logprefix}: PrnService - GetPrnsAwaitingAcceptanceAsync: Get Prns awaiting acceptance for logged in user server response: {Prns}", logPrefix, JsonConvert.SerializeObject(serverResponse));

        List<PrnViewModel> prnViewModels = serverResponse.Select(item => (PrnViewModel)item).ToList();
        model.Prns = prnViewModels.Where(x => x.ApprovalStatus.EndsWith("ACCEPTANCE")).OrderBy(x => x.Material).ThenByDescending(x => x.DateIssued).ToList();

        _logger.LogInformation("{Logprefix}: PrnService - GetPrnsAwaitingAcceptanceAsync: Get Prns awaiting acceptance for logged in user response {Model}", logPrefix, JsonConvert.SerializeObject(model));
        return model;
    }

    public async Task<PrnViewModel> GetPrnByExternalIdAsync(Guid id)
    {
        _logger.LogInformation("{Logprefix}: PrnService - GetPrnByExternalIdAsync: Get Prn for external Id {ExternalId}", logPrefix, id);
        
        var serverResponse = await _webApiGatewayClient.GetPrnByExternalIdAsync(id);
        PrnViewModel model = serverResponse;

        _logger.LogInformation("{Logprefix}: PrnService - GetPrnByExternalIdAsync: Prn returned for external Id {ExternalId} - {Model}", logPrefix, id, JsonConvert.SerializeObject(model));
        return model;
    }

    [ExcludeFromCodeCoverage]
    public async Task<PrnViewModel> GetPrnForPdfByExternalIdAsync(Guid id)
    {
        // This method will add PDF specific exceptions 
        return await GetPrnByExternalIdAsync(id);
    }

    public async Task AcceptPrnAsync(Guid id)
    {
        _logger.LogInformation("{Logprefix}: PrnService - AcceptPrnAsync: accept Prn for given organisation Id {OrgId}", logPrefix, id);
        await _webApiGatewayClient.SetPrnApprovalStatusToAcceptedAsync(id);
    }

    public async Task AcceptPrnsAsync(Guid[] ids)
    {
        _logger.LogInformation("{Logprefix}: PrnService - AcceptPrnsAsync: accept Prn for given organisation Id(s) {OrgIds}", logPrefix, JsonConvert.SerializeObject(ids));
        await _webApiGatewayClient.SetPrnApprovalStatusToAcceptedAsync(ids);
    }

    public async Task<PrnListViewModel> GetAllAcceptedPrnsAsync()
    {
        _logger.LogInformation("{Logprefix}: PrnService - GetAllAcceptedPrnsAsync: Get all accepted Prns for logged in user", logPrefix);

        var model = new PrnListViewModel();
        // this need refactoring when getorg api support filtering
        List<PrnModel> serverResponse = await _webApiGatewayClient.GetPrnsForLoggedOnUserAsync();
        _logger.LogInformation("{Logprefix}: PrnService - GetAllAcceptedPrnsAsync: Get all accepted Prns for logged in user server response: {Prns}", logPrefix, JsonConvert.SerializeObject(serverResponse));

        List<PrnViewModel> prnViewModels = serverResponse.Select(item => (PrnViewModel)item).ToList();
        model.Prns = prnViewModels.Where(x => x.ApprovalStatus.EndsWith("ACCEPTED")).ToList();

        _logger.LogInformation("{Logprefix}: PrnService - GetAllAcceptedPrnsAsync: Return all accepted Prns for logged in user - {Model}", logPrefix, JsonConvert.SerializeObject(model));
        return model;
    }

    public async Task RejectPrnAsync(Guid id)
    {
        _logger.LogInformation("{Logprefix}: PrnService - RejectPrnAsync: reject Prn for given organisation Id {OrgId}", logPrefix, id);
        await _webApiGatewayClient.SetPrnApprovalStatusToRejectedAsync(id);
    }

    public async Task<PrnSearchResultListViewModel> GetPrnSearchResultsAsync(SearchPrnsViewModel request)
    {
        _logger.LogInformation("{Logprefix}: PrnService - GetPrnSearchResultsAsync: Get Prns search results for given search criteria {Request}", logPrefix, JsonConvert.SerializeObject(request));

        PaginatedRequest paginatedRequest = request;
        var prnSearchResults = await _webApiGatewayClient.GetSearchPrnsAsync(paginatedRequest);
        _logger.LogInformation("{Logprefix}: PrnService - GetPrnSearchResultsAsync: Get aPrns search results for given search criteria server response: {SearchResults}", logPrefix, JsonConvert.SerializeObject(prnSearchResults));

        var pagingDetail = new PagingDetail
        {
            CurrentPage = prnSearchResults.CurrentPage,
            PageSize = request.PageSize,
            TotalItems = prnSearchResults.TotalItems,
            TotalPages = prnSearchResults.PageCount
        };

        var prnSearchResultListViewModel = new PrnSearchResultListViewModel
        {
            SearchString = prnSearchResults.SearchTerm,
            ActivePageOfResults = prnSearchResults.Items.Select(item => (PrnSearchResultViewModel)item).ToList(),
            PagingDetail = pagingDetail,
            TypeAhead = prnSearchResults.TypeAhead,
            SelectedFilter = request.FilterBy,
            SelectedSort = request.SortBy
        };

        _logger.LogInformation("{Logprefix}: PrnService - GetPrnSearchResultsAsync: Return Prn Search Result List ViewModel - {PrnSearchResultListViewModel}", logPrefix, JsonConvert.SerializeObject(prnSearchResultListViewModel));
        return prnSearchResultListViewModel;
    }

    public async Task<AwaitingAcceptancePrnsViewModel> GetPrnAwaitingAcceptanceSearchResultsAsync(SearchPrnsViewModel request)
    {
        _logger.LogInformation("{Logprefix}: PrnService - GetPrnAwaitingAcceptanceSearchResultsAsync: Get Prn awaiting acceptance for given search criteria {Request}", logPrefix, JsonConvert.SerializeObject(request));

        PaginatedRequest paginatedRequest = request;
        var prnSearchResults = await _webApiGatewayClient.GetSearchPrnsAsync(paginatedRequest);
        _logger.LogInformation("{Logprefix}: PrnService - GetPrnAwaitingAcceptanceSearchResultsAsync: Get Prn awaiting acceptance for given search criteria server response: {SearchResults}", logPrefix, JsonConvert.SerializeObject(prnSearchResults));

        var pagingDetail = new PagingDetail
        {
            CurrentPage = prnSearchResults.CurrentPage,
            PageSize = request.PageSize,
            TotalItems = prnSearchResults.TotalItems,
            TotalPages = prnSearchResults.PageCount
        };

        var awaitingAcceptancePrnsViewModel = new AwaitingAcceptancePrnsViewModel
        {
            Prns = prnSearchResults.Items.Select(item => (AwaitingAcceptanceResultViewModel)item).ToList(),
            PagingDetail = pagingDetail
        };

        _logger.LogInformation("{Logprefix}: PrnService - GetPrnAwaitingAcceptanceSearchResultsAsync: Return Prn Prn awaiting acceptance Search Result List ViewModel - {AwaitingAcceptancePrnsViewModel}", logPrefix, JsonConvert.SerializeObject(awaitingAcceptancePrnsViewModel));
        return awaitingAcceptancePrnsViewModel;
    }

    public async Task<Stream> GetPrnsCsvStreamAsync()
    {
        _logger.LogInformation("{Logprefix}: PrnService - GetPrnsCsvStreamAsync: Get Prns Csv Stream", logPrefix);

        List<PrnModel> prnDtos = await _webApiGatewayClient.GetPrnsForLoggedOnUserAsync();
        _logger.LogInformation("{Logprefix}: PrnService - GetPrnsCsvStreamAsync: Get Prns for logged on user server response: {PrnDtos}", logPrefix, JsonConvert.SerializeObject(prnDtos));

        List<PrnViewModel> prns = new List<PrnViewModel>(prnDtos.Select(x => (PrnViewModel)x));
        var stream = new MemoryStream();

            if (prns.Count > 0)
            {
                await using (var writer = new StreamWriter(stream, leaveOpen: true))
                {
                    await writer.WriteCsvCellAsync(_csvLocalizer["column_header_prn_or_pern_number"]);
                    await writer.WriteCsvCellAsync(_csvLocalizer["column_header_prn_or_pern"]);
                    await writer.WriteCsvCellAsync(_csvLocalizer["column_header_status"]);
                    await writer.WriteCsvCellAsync(_csvLocalizer["column_header_issued_by"]);
                    await writer.WriteCsvCellAsync(_csvLocalizer["column_header_issued_to"]);
                    await writer.WriteCsvCellAsync(_csvLocalizer["column_header_accreditation_number"]);
                    await writer.WriteCsvCellAsync(_csvLocalizer["column_header_date_issued"]);
                    await writer.WriteCsvCellAsync(_csvLocalizer["column_header_december_waste"]);
                    await writer.WriteCsvCellAsync(_csvLocalizer["column_header_material"]);
                    await writer.WriteCsvCellAsync(_csvLocalizer["column_header_recycling_process"]);
                    await writer.WriteCsvCellAsync(_csvLocalizer["column_header_tonnes"]);
                    await writer.WriteCsvCellAsync(_csvLocalizer["column_header_date_accepted"]);
                    await writer.WriteCsvCellAsync(_csvLocalizer["column_header_date_cancelled"]);
                    writer.WriteLineAsync(_csvLocalizer["column_header_issuer_note"]);

                foreach (var prn in prns)
                {
                    await writer.WriteCsvCellAsync(prn.PrnOrPernNumber);
                    await writer.WriteCsvCellAsync(prn.NoteType);
                    await writer.WriteCsvCellAsync(_csvLocalizer[prn.ApprovalStatus]);
                    await writer.WriteCsvCellAsync(prn.IssuedBy);
                    await writer.WriteCsvCellAsync(prn.NameOfProducerOrComplianceScheme);
                    await writer.WriteCsvCellAsync(prn.AccreditationNumber);
                    await writer.WriteCsvCellAsync(prn.DateIssued.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
                    await writer.WriteCsvCellAsync(_csvLocalizer[prn.DecemberWasteDisplay]);
                    await writer.WriteCsvCellAsync(_dataLocalizer[prn.Material]);
                    await writer.WriteCsvCellAsync(prn.RecyclingProcess);
                    await writer.WriteCsvCellAsync(prn.Tonnage.ToString());
                    await writer.WriteCsvCellAsync(prn.ApprovalStatus == PrnStatus.Accepted ? prn.StatusUpdatedOn?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : _csvLocalizer["not_accepted"]);
                    await writer.WriteCsvCellAsync(prn.ApprovalStatus == PrnStatus.Cancelled ? prn.StatusUpdatedOn?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : _csvLocalizer["not_cancelled"]);
                    writer.WriteLineAsync(!string.IsNullOrWhiteSpace(prn.AdditionalNotes) ? StreamWriterExtensions.CleanCsv(prn.AdditionalNotes) : _csvLocalizer["not_provided"]);
                }

                await writer.FlushAsync();
            }
        }

        stream.Position = 0;
        return stream;
    }

    public async Task<PrnObligationViewModel> GetRecyclingObligationsCalculation(List<Guid> externalIds, int year)
    {
        _logger.LogInformation("{Logprefix}: PrnService - GetRecyclingObligationsCalculation: Get Recycling Obligations Calculation for given year {Year} and {ExternalIds}", logPrefix, year, externalIds);

        var prnObligationViewModel = new PrnObligationViewModel();
        var prnObligationModel = await _webApiGatewayClient.GetRecyclingObligationsCalculation(externalIds, year);
        _logger.LogInformation("{Logprefix}: PrnService - GetRecyclingObligationsCalculation: Get obligations for given year {Year} server response {PrnObligationModel}", logPrefix, year, JsonConvert.SerializeObject(prnObligationModel));

        if (prnObligationModel != null)
        {
            prnObligationViewModel.NumberOfPrnsAwaitingAcceptance = prnObligationModel.NumberOfPrnsAwaitingAcceptance;

            var prnMaterialObligationViewModels = prnObligationModel.ObligationData?.Select(item => (PrnMaterialObligationViewModel)item).ToList();
            if (prnMaterialObligationViewModels?.Count > 0)
            {
                // Find and update the material "Glass" to "RemainingGlass"
                var glassItem = prnMaterialObligationViewModels.Find(r => r.MaterialName == MaterialType.Glass);
                if (glassItem != null)
                {
                    glassItem.MaterialName = MaterialType.RemainingGlass;
                }

                // Add Glass summary row
                prnMaterialObligationViewModels.Add(AddGlassRow(prnMaterialObligationViewModels));

                // Split obligations into non-glass and glass collections
                var materialObligationViewModels = prnMaterialObligationViewModels
                    .Where(r => r.MaterialName != MaterialType.GlassRemelt && r.MaterialName != MaterialType.RemainingGlass)
                    .OrderBy(r => r.MaterialName.ToString());

                var glassMaterialObligationViewModels = prnMaterialObligationViewModels
                    .Where(r => r.MaterialName == MaterialType.GlassRemelt || r.MaterialName == MaterialType.RemainingGlass)
                    .OrderBy(r => r.MaterialName.ToString());

                // Add rows and calculate totals for material table
                if (materialObligationViewModels.Any())
                {
                    prnObligationViewModel.MaterialObligationViewModels = [.. materialObligationViewModels];
                    prnObligationViewModel.MaterialObligationViewModels.Add(GetTotalRow(materialObligationViewModels));
                    prnObligationViewModel.OverallStatus = GetFinalStatus(prnObligationViewModel.MaterialObligationViewModels);
                }

                // Add rows and calculate totals for glass table
                if (glassMaterialObligationViewModels.Any())
                {
                    prnObligationViewModel.GlassMaterialObligationViewModels = [.. glassMaterialObligationViewModels];
                    prnObligationViewModel.GlassMaterialObligationViewModels.Add(GetTotalRow(glassMaterialObligationViewModels));
                }
            }
        }

        _logger.LogInformation("{Logprefix}: PrnService - GetRecyclingObligationsCalculation: Return Recycling Obligations Calculation - {PrnObligationViewModel}", logPrefix, JsonConvert.SerializeObject(prnObligationViewModel));
        return prnObligationViewModel;
    }

    private static PrnMaterialObligationViewModel CalculateRow(MaterialType materialName, IEnumerable<PrnMaterialObligationViewModel> obligations)
    {
        return new PrnMaterialObligationViewModel
        {
            MaterialName = materialName,
			OrganisationId = obligations.First().OrganisationId,
			ObligationToMeet = obligations.Any(r => r.ObligationToMeet != null) ? obligations.Sum(r => r.ObligationToMeet) : null,
            TonnageAwaitingAcceptance = obligations.Sum(r => r.TonnageAwaitingAcceptance),
            TonnageAccepted = obligations.Sum(r => r.TonnageAccepted),
            TonnageOutstanding = obligations.Any(r => r.TonnageOutstanding != null) ? obligations.Sum(r => r.TonnageOutstanding) : null,
            Status = GetOverallStatus(obligations.Select(r => r.Status)),
            MaterialTarget = obligations.First().MaterialTarget,
            Tonnage = obligations.First().Tonnage,
        };
    }

    private static PrnMaterialObligationViewModel AddGlassRow(List<PrnMaterialObligationViewModel> obligationsResult)
    {
        var glassObligations = obligationsResult.Where(r => r.MaterialName == MaterialType.GlassRemelt || r.MaterialName == MaterialType.RemainingGlass);

        return CalculateRow(MaterialType.Glass, glassObligations);
    }

    private static PrnMaterialObligationViewModel GetTotalRow(IEnumerable<PrnMaterialObligationViewModel> recyclingObligations)
    {
        return CalculateRow(MaterialType.Totals, recyclingObligations);
    }

    static ObligationStatus GetOverallStatus(IEnumerable<ObligationStatus> statuses)
    {
        if (statuses.Contains(ObligationStatus.NoDataYet))
        {
            return ObligationStatus.NoDataYet;
        }
        if (statuses.Contains(ObligationStatus.NotMet))
        {
            return ObligationStatus.NotMet;
        }
        return ObligationStatus.Met;
    }

    private static ObligationStatus GetFinalStatus(IEnumerable<PrnMaterialObligationViewModel> materialObligationViewModels)
    {
        var totals = materialObligationViewModels.FirstOrDefault(m => m.MaterialName == MaterialType.Totals);
        return totals?.Status ?? ObligationStatus.NoDataYet;
    }

    public async Task<List<Guid>> GetChildOrganisationExternalIdsAsync(Guid organisationId, Guid? complianceSchemeId)
    {
        try
        {
            var accountApiGetChildOrgExternalIdsUrl = $"organisations/v1/child-organisation-external-ids?organisationId={organisationId}";
            if (complianceSchemeId is not null && complianceSchemeId != Guid.Empty)
            {
                accountApiGetChildOrgExternalIdsUrl += $"&complianceSchemeId={complianceSchemeId}";
            }

            _logger.LogInformation("{LogPrefix}: PrnService - GetChildOrganisationExternalIdsAsync: Get child organisation external ids via {Url}", logPrefix, accountApiGetChildOrgExternalIdsUrl);
            var response = await _accountServiceApiClient.SendGetRequest(accountApiGetChildOrgExternalIdsUrl);
            var content = await response.Content.ReadAsStringAsync();
            var externalIds = JsonConvert.DeserializeObject<List<Guid>>(content);
            _logger.LogInformation("{LogPrefix}: PrnService - GetChildOrganisationExternalIdsAsync: Get child organisation external ids are {ExternalIds}", logPrefix, externalIds);

            return externalIds;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "{LogPrefix}: PrnService - GetChildOrganisationExternalIdsAsync: Error while retrieving child organisation external ids for {OrganisationId}", logPrefix, organisationId);
            throw;
        }
	}
}