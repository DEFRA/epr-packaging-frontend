﻿using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.CompaniesHouse;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace FrontendSchemeRegistration.UI.Controllers;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.SubsidiaryCompaniesHouseNumberSearch)]
public class SubsidiaryCompaniesHouseNumberController : Controller
{
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly ICompaniesHouseService _companiesHouseService;
    private readonly ILogger<SubsidiaryCompaniesHouseNumberController> _logger;
    private readonly ExternalUrlOptions _urlOptions;

    public SubsidiaryCompaniesHouseNumberController(
        IOptions<ExternalUrlOptions> urlOptions,
        ICompaniesHouseService companiesHouseService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        ILogger<SubsidiaryCompaniesHouseNumberController> logger)
    {
        _sessionManager = sessionManager;
        _urlOptions = urlOptions.Value;
        _companiesHouseService = companiesHouseService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        SetBackLink(session, PagePaths.SubsidiaryCompaniesHouseNumberSearch, Url.Content($"~{PagePaths.FileUpload}"));

        if (_urlOptions.FindAndUpdateCompanyInformation is not null)
        {
            ViewBag.FindAndUpdateCompanyInformationLink = _urlOptions.FindAndUpdateCompanyInformation;
        }

        var viewModel = new SubsidiaryCompaniesHouseNumberViewModel
        {
            CompaniesHouseNumber = session.SubsidiarySession?.Company?.CompaniesHouseNumber,
        };

        if (TempData["ModelState"] is not null)
        {
            ModelState.Merge(DeserializeModelState(TempData["ModelState"].ToString()));
            viewModel.CompaniesHouseNumber = TempData["CompaniesHouseNumber"].ToString();
        }

        return View("SubsidiaryCompaniesHouseNumber", viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> Post(SubsidiaryCompaniesHouseNumberViewModel model)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (!ModelState.IsValid)
        {
            SetBackLink(session, PagePaths.SubsidiaryCompaniesHouseNumberSearch, Url.Content($"~{PagePaths.FileUpload}"));

            ViewBag.FindAndUpdateCompanyInformationLink = _urlOptions.FindAndUpdateCompanyInformation;

            return View("SubsidiaryCompaniesHouseNumber", model);
        }

        session.SubsidiarySession ??= new SubsidiarySession();

        Company? company;
        model.CompaniesHouseNumber = model.CompaniesHouseNumber?.Trim();
        try
        {
            company = await _companiesHouseService.GetCompanyByCompaniesHouseNumber(model.CompaniesHouseNumber);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Companies House Lookup failed for {CompaniesHouseNumber}", model.CompaniesHouseNumber);

            session.SubsidiarySession.IsUserChangingDetails = false;
            await SaveSession(session, PagePaths.SubsidiaryCompaniesHouseNumberSearch, PagePaths.CannotVerifyOrganisation);

            return RedirectToAction("Get", "CannotVerifyOrganisation");
        }

        if (company == null)
        {
            ModelState.AddModelError(nameof(SubsidiaryCompaniesHouseNumberViewModel.CompaniesHouseNumber), "CompaniesHouseNumber.NotFoundError");

            SetBackLink(session, PagePaths.SubsidiaryCompaniesHouseNumberSearch, Url.Content($"~{PagePaths.FileUpload}"));
            ViewBag.FindAndUpdateCompanyInformationLink = _urlOptions.FindAndUpdateCompanyInformation;
            TempData["ModelState"] = SerializeModelState(ModelState);
            TempData["CompaniesHouseNumber"] = model.CompaniesHouseNumber;

            return RedirectToAction(nameof(Get));
        }

        session.SubsidiarySession.Company = company;

        return await SaveSessionAndRedirect(session, PagePaths.SubsidiaryCompaniesHouseNumberSearch, PagePaths.SubsidiaryConfirmCompanyDetails, null);
    }

    private static void ClearRestOfJourney(FrontendSchemeRegistrationSession session, string currentPagePath)
    {
        var index = session.SubsidiarySession.Journey.IndexOf(currentPagePath);

        // this also cover if current page not found (index = -1) then it clears all pages
        session.SubsidiarySession.Journey = session.SubsidiarySession.Journey.Take(index + 1).ToList();
    }

    private async Task SaveSession(FrontendSchemeRegistrationSession session, string currentPagePath, string? nextPagePath)
    {
        ClearRestOfJourney(session, currentPagePath);

        session.SubsidiarySession.Journey.AddIfNotExists(nextPagePath);

        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
    }

    private void SetBackLink(FrontendSchemeRegistrationSession session, string currentPagePath, string defaultPath = "")
    {
        if (session.SubsidiarySession?.IsUserChangingDetails == true && currentPagePath != PagePaths.SubsidiaryCheckYourDetails)
        {
            ViewBag.BackLinkToDisplay = PagePaths.SubsidiaryCheckYourDetails;
        }
        else
        {
            ViewBag.BackLinkToDisplay = session.SubsidiarySession?.Journey?.PreviousOrDefault(currentPagePath) ?? defaultPath;
        }
    }

    private async Task<RedirectToActionResult> SaveSessionAndRedirect(FrontendSchemeRegistrationSession session, string currentPagePath, string? nextPagePath, SubsidiaryConfirmCompanyDetailsViewModel viewModel)
    {
        session.SubsidiarySession.IsUserChangingDetails = false;
        await SaveSession(session, currentPagePath, nextPagePath);

        return RedirectToAction("Get", "SubsidiaryConfirmCompanyDetails", viewModel);
    }

    private static string SerializeModelState(ModelStateDictionary modelState)
    {
        var errorList = modelState.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
        );

        return JsonConvert.SerializeObject(errorList);
    }

    private static ModelStateDictionary DeserializeModelState(string serializedModelState)
    {
        var errorList = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(serializedModelState);
        var modelState = new ModelStateDictionary();

        foreach (var kvp in errorList)
        {
            foreach (var error in kvp.Value)
            {
                modelState.AddModelError(kvp.Key, error);
            }
        }

        return modelState;
    }
}
