namespace FrontendSchemeRegistration.Application.Services.Interfaces;

using Microsoft.AspNetCore.JsonPatch;

public interface IPatchService
{
    JsonPatchDocument CreatePatchDocument<T>(T originalObject, T modifiedObject)
        where T : class;
}