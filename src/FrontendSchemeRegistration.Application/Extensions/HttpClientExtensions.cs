namespace FrontendSchemeRegistration.Application.Extensions;

using System.Net.Mime;
using Enums;
using FrontendSchemeRegistration.Application.Constants;
using Microsoft.Net.Http.Headers;

public static class HttpClientExtensions
{
    public static void AddHeaderAuthorization(this HttpClient httpClient, string token)
    {
        httpClient.AddDefaultRequestHeaderIfDoesNotContain(HeaderNames.Authorization, token);
    }

    public static void AddHeaderAcceptJson(this HttpClient httpClient)
    {
        httpClient.AddDefaultRequestHeaderIfDoesNotContain(HeaderNames.Accept, MediaTypeNames.Application.Json);
    }

    public static void AddHeaderUserAgent(this HttpClient httpClient, string userAgent)
    {
        httpClient.AddDefaultRequestHeaderIfDoesNotContain(HeaderNames.UserAgent, userAgent);
    }

    public static void AddHeaderSubmissionType(this HttpClient httpClient, SubmissionType submissionType)
    {
        httpClient.AddDefaultRequestHeaderIfDoesNotContain("SubmissionType", submissionType.ToString());
    }

    public static void AddHeaderSubmissionPeriod(this HttpClient httpClient, string submissionPeriod)
    {
        httpClient.AddDefaultRequestHeaderIfDoesNotContain("SubmissionPeriod", submissionPeriod);
    }

    public static void AddHeaderSubmissionSubTypeIfNotNull(this HttpClient httpClient, SubmissionSubType? submissionSubType)
    {
        if (submissionSubType.HasValue)
        {
            httpClient.AddDefaultRequestHeaderIfDoesNotContain("SubmissionSubType", submissionSubType.Value.ToString());
        }
    }

    public static void AddHeaderSubmissionIdIfNotNull(this HttpClient httpClient, Guid? submissionId)
    {
        if (submissionId.HasValue)
        {
            httpClient.AddDefaultRequestHeaderIfDoesNotContain("SubmissionId", submissionId.Value.ToString());
        }
    }

    public static void AddHeaderComplianceSchemeIdIfNotNull(this HttpClient httpClient, Guid? complianceSchemeId)
    {
        if (complianceSchemeId.HasValue)
        {
            httpClient.AddDefaultRequestHeaderIfDoesNotContain(ComplianceScheme.ComplianceSchemeId, complianceSchemeId.Value.ToString());
        }
    }

    public static void AddHeaderFileName(this HttpClient httpClient, string fileName)
    {
        httpClient.AddDefaultRequestHeaderIfDoesNotContain("FileName", fileName);
    }

    public static void AddHeaderRegistrationSetIdIfNotNull(this HttpClient httpClient, Guid? registrationSetId)
    {
        if (registrationSetId.HasValue)
        {
            httpClient.AddDefaultRequestHeaderIfDoesNotContain("RegistrationSetId", registrationSetId.ToString());
        }
    }

    private static void AddDefaultRequestHeaderIfDoesNotContain(this HttpClient httpClient, string name, string value)
    {
        if (!httpClient.DefaultRequestHeaders.Contains(name))
        {
            httpClient.DefaultRequestHeaders.Add(name, value);
        }
    }
}