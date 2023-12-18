namespace FrontendSchemeRegistration.Application.Services.Interfaces;

using DTOs.UserAccount;

public interface IUserAccountService
{
    public Task<UserAccountDto?> GetUserAccount();

    public Task<PersonDto?> GetPersonByUserId(Guid userId);
}