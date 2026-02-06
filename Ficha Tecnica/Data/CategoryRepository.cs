using Ficha_Tecnica.Models;
using MySqlConnector;
using System.Data;

namespace Ficha_Tecnica.Data;

public class CategoryRepository : ICategoryRepository
{
    private readonly string _connectionString;

    public CategoryRepository(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("A valid connection string is required.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<IngredientCategory>> GetCategoriesAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string query = @"SELECT id, user_id, name, icon_key, description, color, display_order, is_active, created_at, updated_at
                               FROM categories
                               WHERE user_id = @user_id
                               ORDER BY COALESCE(display_order, 2147483647), name;";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(query, connection);
        command.Parameters.Add("@user_id", MySqlDbType.Int32).Value = userId;

        var categories = new List<IngredientCategory>();

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            categories.Add(new IngredientCategory
            {
                Id = reader.GetInt32("id"),
                UserId = reader.GetInt32("user_id"),
                Name = reader.GetString("name"),
                IconKey = reader.GetString("icon_key"),
                Description = reader["description"] is DBNull ? null : reader.GetString("description"),
                Color = reader["color"] is DBNull ? null : reader.GetString("color"),
                DisplayOrder = reader["display_order"] is DBNull ? null : reader.GetInt32("display_order"),
                IsActive = reader.GetBoolean("is_active"),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader["updated_at"] is DBNull ? null : reader.GetDateTime("updated_at"),
            });
        }

        return categories;
    }

    public async Task<IngredientCategory> CreateCategoryAsync(IngredientCategory category, CancellationToken cancellationToken = default)
    {
        if (category is null)
        {
            throw new ArgumentNullException(nameof(category));
        }

        const string commandText = @"INSERT INTO categories (user_id, name, icon_key, description, color, display_order, is_active)
                                      VALUES (@user_id, @name, @icon_key, @description, @color, @display_order, @is_active);";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(commandText, connection);
        command.Parameters.Add("@user_id", MySqlDbType.Int32).Value = category.UserId;
        command.Parameters.Add("@name", MySqlDbType.VarChar, 150).Value = category.Name;
        command.Parameters.Add("@icon_key", MySqlDbType.VarChar, 50).Value = category.IconKey;
        command.Parameters.Add("@description", MySqlDbType.Text).Value = category.Description ?? (object)DBNull.Value;
        command.Parameters.Add("@color", MySqlDbType.VarChar, 20).Value = category.Color ?? (object)DBNull.Value;
        command.Parameters.Add("@display_order", MySqlDbType.Int32).Value = category.DisplayOrder ?? (object)DBNull.Value;
        command.Parameters.Add("@is_active", MySqlDbType.Bool).Value = category.IsActive;

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (MySqlException ex) when ((MySqlErrorCode)ex.Number == MySqlErrorCode.DuplicateKeyEntry)
        {
            throw new DuplicateCategoryException(category.Name, ex);
        }

        category.Id = Convert.ToInt32(command.LastInsertedId);
        category.CreatedAt = DateTime.UtcNow;
        category.UpdatedAt = null;

        return category;
    }
}
