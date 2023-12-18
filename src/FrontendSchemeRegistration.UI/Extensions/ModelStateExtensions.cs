namespace FrontendSchemeRegistration.UI.Extensions;

using Microsoft.AspNetCore.Mvc.ModelBinding;

public static class ModelStateExtensions
{
    public static KeyValuePair<string, ModelStateEntry> GetModelStateEntry(this ModelStateDictionary modelState, string key)
    {
        return modelState.FirstOrDefault(x => x.Key == key);
    }
}