namespace FrontendSchemeRegistration.UI.Helpers;

using Microsoft.AspNetCore.Mvc.ModelBinding;

public static class ModelStateHelpers
{
    public static void AddFileUploadExceptionsToModelState(List<string> exceptionCodes, ModelStateDictionary modelState, int closedLoopRegistrationFromYear = 0)
    {
        foreach (var exceptionCode in exceptionCodes)
        {
            var message = ErrorReportHelpers.GetErrorMessage(exceptionCode);
            if (exceptionCode == "935" && closedLoopRegistrationFromYear > 0)
            {
                message = string.Format(message, closedLoopRegistrationFromYear);
            }

            modelState.AddModelError("file", message);
        }
    }
}