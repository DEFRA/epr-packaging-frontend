namespace FrontendSchemeRegistration.UI.ViewModels.GovUk;

using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;

public class ErrorsViewModel
{
    public ErrorsViewModel(
        Dictionary<string, List<ErrorViewModel>> errors,
        IStringLocalizer<SharedResources> localizer)
        : this(errors, (x) => localizer[x].Value)
    {
    }

    public ErrorsViewModel(Dictionary<string, List<ErrorViewModel>> errors, IViewLocalizer localizer,
        params string[] fieldOrder)
        : this(errors, (x) => localizer[x].Value, fieldOrder)
    {
    }

    private ErrorsViewModel(Dictionary<string, List<ErrorViewModel>> errors, Func<string, string> localiseFunc, params string[]? fieldOrder)
    {
        Errors = GetOrderedErrors(errors, localiseFunc, fieldOrder);
    }

    public Dictionary<string, List<ErrorViewModel>> TextInserts { get; set; }

    public Dictionary<string, List<ErrorViewModel>> Errors { get; }

    public List<ErrorViewModel>? this[string key] => Errors.FirstOrDefault(e => e.Key == key).Value;

    public bool HasErrorKey(string key) => Errors.Any(e => e.Key == key);

    private static Dictionary<string, List<ErrorViewModel>> GetOrderedErrors(
        Dictionary<string, List<ErrorViewModel>> errors, Func<string, string> localiseFunc, string[]? fieldOrder)
    {
        fieldOrder ??= Array.Empty<string>();
        var orderedErrorsKvp = errors.OrderBy(x => fieldOrder.Contains(x.Key) ? Array.IndexOf(fieldOrder, x.Key) : int.MaxValue);
        var orderedErrors = new Dictionary<string, List<ErrorViewModel>>();
        foreach (var kvp in orderedErrorsKvp)
        {
            kvp.Value.ForEach(x => x.Message = localiseFunc(x.Message));
            orderedErrors.Add(kvp.Key, kvp.Value);
        }

        return orderedErrors;
    }
}