namespace FrontendSchemeRegistration.Application.Services.Interfaces;

using DTOs;
using Microsoft.AspNetCore.JsonPatch;

public interface IApplicationClient
{
    Task<T?> GetApplicationAsync<T>(Guid id);

    Task<bool> PatchApplicationAsync(Guid id, JsonPatchDocument patchDocument);

    Task<bool> PostApplicationAsync(ApplicationDto applicationDto);
}