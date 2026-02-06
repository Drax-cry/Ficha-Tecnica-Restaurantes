using Ficha_Tecnica.Models;

namespace Ficha_Tecnica.Data;

public interface IUserRepository
{
    Task<UserAccount?> GetByUsernameOrEmailAsync(string identifier, CancellationToken cancellationToken = default);
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<UserAccount> CreateUserAsync(UserAccount user, CancellationToken cancellationToken = default);
}
