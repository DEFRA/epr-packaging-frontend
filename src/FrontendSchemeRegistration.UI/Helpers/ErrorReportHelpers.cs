namespace FrontendSchemeRegistration.UI.Helpers;

using Application.DTOs;
using Constants;
using Resources;

public static class ErrorReportHelpers
{
    public static IEnumerable<ErrorReportRow> ToErrorReportRows(this IEnumerable<ProducerValidationError> validationErrors)
    {
        return validationErrors.SelectMany(x =>
        {
            var issueText = ErrorCodes.ResourceManager.GetString($"{x.Issue}Issue");
            return x.ErrorCodes.Select(code => new ErrorReportRow(x, issueText, GetErrorMessage(code)));
        });
    }

    public static string GetErrorMessage(string errorCode)
    {
        return ErrorCodes.ResourceManager.GetString(errorCode) ?? errorCode;
    }
}