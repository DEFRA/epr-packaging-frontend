namespace FrontendSchemeRegistration.UI.Helpers;

using Microsoft.AspNetCore.Mvc.ModelBinding;

public static class ModelStateHelpers
{
    public static void AddFileUploadExceptionsToModelState(List<string> exceptionCodes, ModelStateDictionary modelState)
    {
        foreach (var exceptionCode in exceptionCodes)
        {
            modelState.AddModelError("file", ErrorReportHelpers.GetErrorMessage(exceptionCode));
        }
    }
}