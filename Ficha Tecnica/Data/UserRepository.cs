using Ficha_Tecnica.Models;
using MySqlConnector;
using System.Data;

namespace Ficha_Tecnica.Data;

public class UserRepository : IUserRepository
{
    private readonly string _connectionString;

    public UserRepository(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("A valid connection string is required.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    public async Task<UserAccount?> GetByUsernameOrEmailAsync(string identifier, CancellationToken cancellationToken = default)
    {
        const string query = @"SELECT id, username, email, password_hash, salt, is_active, created_at, updated_at
                                FROM login
                                WHERE (username = @identifier OR email = @identifier)
                                LIMIT 1;";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(query, connection);
        command.Parameters.Add("@identifier", MySqlDbType.VarChar, 255).Value = identifier;

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new UserAccount
            {
                Id = reader.GetInt32("id"),
                Username = reader.GetString("username"),
                Email = reader.GetString("email"),
                PasswordHash = reader.GetString("password_hash"),
                Salt = reader["salt"] is DBNull ? null : (byte[])reader["salt"],
                IsActive = reader.GetBoolean("is_active"),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader["updated_at"] is DBNull ? null : reader.GetDateTime("updated_at")
            };
        }

        return null;
    }

    public async Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
    {
        const string query = "SELECT EXISTS(SELECT 1 FROM ficha_tecnica_restaurantes.login WHERE username = @username);";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(query, connection);
        command.Parameters.Add("@username", MySqlDbType.VarChar, 100).Value = username;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result) > 0;
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        const string query = "SELECT EXISTS(SELECT 1 FROM login WHERE email = @email);";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(query, connection);
        command.Parameters.Add("@email", MySqlDbType.VarChar, 255).Value = email;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result) > 0;
    }

    public async Task<UserAccount> CreateUserAsync(UserAccount user, CancellationToken cancellationToken = default)
    {
        const string commandText = @"INSERT INTO login (username, email, password_hash, salt, is_active)
                                      VALUES (@username, @email, @password_hash, @salt, @is_active);";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(commandText, connection);
        command.Parameters.Add("@username", MySqlDbType.VarChar, 100).Value = user.Username;
        command.Parameters.Add("@email", MySqlDbType.VarChar, 255).Value = user.Email;
        command.Parameters.Add("@password_hash", MySqlDbType.VarChar, 255).Value = user.PasswordHash;
        command.Parameters.Add("@salt", MySqlDbType.VarBinary).Value = user.Salt ?? Array.Empty<byte>();
        command.Parameters.Add("@is_active", MySqlDbType.Bool).Value = user.IsActive;

        await command.ExecuteNonQueryAsync(cancellationToken);
        user.Id = Convert.ToInt32(command.LastInsertedId);
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = null;

        return user;
    }
}
