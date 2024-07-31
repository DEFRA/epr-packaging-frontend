using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;

namespace FrontendSchemeRegistration.UI.ViewModels.Shared.GovUK;

[ExcludeFromCodeCoverage]
public class ErrorsViewModel
{
    public ErrorsViewModel(List<(string Key, List<ErrorViewModel> Errors)> errors, IStringLocalizer<SharedResources> localizer)
        : this(errors, (x) => localizer[x].Value)
    {
    }

    public ErrorsViewModel(List<(string Key, List<ErrorViewModel> Errors)> errors, IViewLocalizer localizer, params string[] fieldOrder)
        : this(errors, (x) => localizer[x].Value, fieldOrder)
    {
    }

    private ErrorsViewModel(List<(string Key, List<ErrorViewModel> Errors)> errors, Func<string, string> localiseFunc, params string[]? fieldOrder)
    {
        Errors = GetOrderedErrors(errors, localiseFunc, fieldOrder);
    }

    public List<(string Key, List<ErrorViewModel> Errors)> Errors { get; }

    public List<ErrorViewModel>? this[string key] => Errors.FirstOrDefault(e => e.Key == key).Errors;

    public bool HasErrorKey(string key) => Errors.Any(e => e.Item1 == key);

    private static List<(string Key, List<ErrorViewModel> Errors)> GetOrderedErrors(
       List<(string Key, List<ErrorViewModel> Errors)> errors, Func<string, string> localiseFunc, string[]? fieldOrder)
    {
        fieldOrder ??= Array.Empty<string>();
        var orderedErrorsKvp = errors.OrderBy(x => fieldOrder.Contains(x.Item1) ? Array.IndexOf(fieldOrder, x.Item1) : int.MaxValue);
        var orderedErrors = new List<(string Key, List<ErrorViewModel> Errors)>();
        foreach (var kvp in orderedErrorsKvp)
        {
            kvp.Item2.ForEach(x => x.Message = localiseFunc(x.Message));
            orderedErrors.Add((kvp.Key, kvp.Errors));
        }

        return orderedErrors;
    }
}