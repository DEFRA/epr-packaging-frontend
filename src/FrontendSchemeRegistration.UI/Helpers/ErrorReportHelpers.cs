namespace FrontendSchemeRegistration.UI.Helpers;

using Application.DTOs;
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

    public static IEnumerable<RegistrationErrorReportRow> ToRegistrationErrorReportRows(this IEnumerable<RegistrationValidationError> registrationValidationErrors)
    {
        return registrationValidationErrors.SelectMany(validationError => validationError.ColumnErrors.Select(columnValidationError => CreateRegistrationErrorReportRow(validationError, columnValidationError)));
    }

    public static string GetErrorMessage(string errorCode)
    {
        return ErrorCodes.ResourceManager.GetString(errorCode) ?? errorCode;
    }

    private static RegistrationErrorReportRow CreateRegistrationErrorReportRow(
        RegistrationValidationError validationError, ColumnValidationError columnValidationError)
    {
        return new RegistrationErrorReportRow()
        {
            Row = validationError.RowNumber.ToString(),
            OrganisationId = validationError.OrganisationId,
            SubsidiaryId = validationError.SubsidiaryId,
            Column = ToExcelColumn(columnValidationError.ColumnIndex),
            ColumnName = columnValidationError.ColumnName,
            Error = GetRegistrationErrorMessage(columnValidationError.ErrorCode)
        };
    }

    private static string GetRegistrationErrorMessage(string errorCode)
    {
        return CompanyDetailsSubmissionErrorCodes.ResourceManager.GetString(errorCode) ?? errorCode;
    }

    private static string ToExcelColumn(int columnIndex)
    {
        string columnName = string.Empty;

        do
        {
            int remainder = columnIndex % 26;
            columnName = (char)('A' + remainder) + columnName;
            columnIndex /= 26;
        }
        while (columnIndex-- > 0);

        return columnName;
    }
}