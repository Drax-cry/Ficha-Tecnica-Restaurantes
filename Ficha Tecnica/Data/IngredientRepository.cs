using System.Data;
using Ficha_Tecnica.Models;
using MySqlConnector;

namespace Ficha_Tecnica.Data;

public class IngredientRepository : IIngredientRepository
{
    private readonly string _connectionString;

    public IngredientRepository(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("A valid connection string is required.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<Ingredient>> GetIngredientsAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string query = @"SELECT i.id, i.user_id, i.name, i.description, i.category_id, i.unit, i.cost_per_unit, i.currency,
                                      i.reorder_level, i.notes, i.is_active, i.created_at, i.updated_at,
                                      i.supplier, i.package_quantity, i.package_size, i.total_cost, i.icon_key, i.last_price_update,
                                      c.name AS category_name, c.icon_key AS category_icon_key
                               FROM ingredients AS i
                               LEFT JOIN categories AS c ON c.id = i.category_id
                               WHERE i.user_id = @user_id
                               ORDER BY i.name;";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(query, connection);
        command.Parameters.Add("@user_id", MySqlDbType.Int32).Value = userId;

        var ingredients = new List<Ingredient>();

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var ingredient = new Ingredient
            {
                Id = reader.GetInt32("id"),
                UserId = reader.GetInt32("user_id"),
                Name = reader.GetString("name"),
                Description = reader["description"] is DBNull ? null : reader.GetString("description"),
                CategoryId = reader["category_id"] is DBNull ? null : reader.GetInt32("category_id"),
                CategoryName = reader["category_name"] is DBNull ? null : reader.GetString("category_name"),
                CategoryIconKey = reader["category_icon_key"] is DBNull ? null : reader.GetString("category_icon_key"),
                Unit = reader.GetString("unit"),
                CostPerUnit = reader.GetDecimal("cost_per_unit"),
                Currency = reader.GetString("currency"),
                ReorderLevel = reader["reorder_level"] is DBNull ? null : reader.GetDecimal("reorder_level"),
                Notes = reader["notes"] is DBNull ? null : reader.GetString("notes"),
                IsActive = reader.GetBoolean("is_active"),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader["updated_at"] is DBNull ? null : reader.GetDateTime("updated_at"),
                Supplier = reader["supplier"] is DBNull ? null : reader.GetString("supplier"),
                PackageQuantity = reader["package_quantity"] is DBNull ? null : reader.GetDecimal("package_quantity"),
                PackageSize = reader["package_size"] is DBNull ? null : reader.GetString("package_size"),
                TotalCost = reader["total_cost"] is DBNull ? null : reader.GetDecimal("total_cost"),
                IconKey = reader["icon_key"] is DBNull ? null : reader.GetString("icon_key"),
                LastPriceUpdate = reader["last_price_update"] is DBNull ? null : reader.GetDateTime("last_price_update"),
            };

            ingredients.Add(ingredient);
        }

        return ingredients;
    }

    public async Task<Ingredient> CreateIngredientAsync(Ingredient ingredient, CancellationToken cancellationToken = default)
    {
        if (ingredient is null)
        {
            throw new ArgumentNullException(nameof(ingredient));
        }

        const string commandText = @"INSERT INTO ingredients (user_id, name, description, category_id, unit, cost_per_unit, currency,
                                                              reorder_level, supplier, package_quantity, package_size, total_cost,
                                                              icon_key, last_price_update, notes, is_active)
                                     VALUES (@user_id, @name, @description, @category_id, @unit, @cost_per_unit, @currency,
                                             @reorder_level, @supplier, @package_quantity, @package_size, @total_cost,
                                             @icon_key, @last_price_update, @notes, @is_active);";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(commandText, connection);
        command.Parameters.Add("@user_id", MySqlDbType.Int32).Value = ingredient.UserId;
        command.Parameters.Add("@name", MySqlDbType.VarChar, 150).Value = ingredient.Name;
        command.Parameters.Add("@description", MySqlDbType.Text).Value = ingredient.Description ?? (object)DBNull.Value;
        command.Parameters.Add("@category_id", MySqlDbType.Int32).Value = ingredient.CategoryId ?? (object)DBNull.Value;
        command.Parameters.Add("@unit", MySqlDbType.VarChar, 50).Value = ingredient.Unit;
        command.Parameters.Add("@cost_per_unit", MySqlDbType.Decimal).Value = ingredient.CostPerUnit;
        command.Parameters.Add("@currency", MySqlDbType.VarChar, 3).Value = ingredient.Currency;
        command.Parameters.Add("@reorder_level", MySqlDbType.Decimal).Value = ingredient.ReorderLevel ?? (object)DBNull.Value;
        command.Parameters.Add("@supplier", MySqlDbType.VarChar, 150).Value = ingredient.Supplier ?? (object)DBNull.Value;
        command.Parameters.Add("@package_quantity", MySqlDbType.Decimal).Value = ingredient.PackageQuantity ?? (object)DBNull.Value;
        command.Parameters.Add("@package_size", MySqlDbType.VarChar, 100).Value = ingredient.PackageSize ?? (object)DBNull.Value;
        command.Parameters.Add("@total_cost", MySqlDbType.Decimal).Value = ingredient.TotalCost ?? (object)DBNull.Value;
        command.Parameters.Add("@icon_key", MySqlDbType.VarChar, 50).Value = ingredient.IconKey ?? (object)DBNull.Value;
        command.Parameters.Add("@last_price_update", MySqlDbType.DateTime).Value = ingredient.LastPriceUpdate ?? (object)DBNull.Value;
        command.Parameters.Add("@notes", MySqlDbType.Text).Value = ingredient.Notes ?? (object)DBNull.Value;
        command.Parameters.Add("@is_active", MySqlDbType.Bool).Value = ingredient.IsActive;

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (MySqlException ex) when ((MySqlErrorCode)ex.Number == MySqlErrorCode.DuplicateKeyEntry)
        {
            throw new DuplicateIngredientException(ingredient.Name, ex);
        }

        ingredient.Id = Convert.ToInt32(command.LastInsertedId);
        ingredient.CreatedAt = DateTime.UtcNow;
        ingredient.UpdatedAt = null;

        return ingredient;
    }

    public async Task UpdateIngredientAsync(Ingredient ingredient, CancellationToken cancellationToken = default)
    {
        if (ingredient is null)
        {
            throw new ArgumentNullException(nameof(ingredient));
        }

        const string commandText = @"UPDATE ingredients
                                     SET name = @name,
                                         category_id = @category_id,
                                         supplier = @supplier,
                                         unit = @unit,
                                         package_quantity = @package_quantity,
                                         total_cost = @total_cost,
                                         cost_per_unit = @cost_per_unit,
                                         last_price_update = @last_price_update,
                                         notes = @notes,
                                         updated_at = UTC_TIMESTAMP(6)
                                     WHERE id = @id AND user_id = @user_id;";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(commandText, connection);
        command.Parameters.Add("@name", MySqlDbType.VarChar, 150).Value = ingredient.Name;
        command.Parameters.Add("@category_id", MySqlDbType.Int32).Value = ingredient.CategoryId ?? (object)DBNull.Value;
        command.Parameters.Add("@supplier", MySqlDbType.VarChar, 150).Value = ingredient.Supplier ?? (object)DBNull.Value;
        command.Parameters.Add("@unit", MySqlDbType.VarChar, 50).Value = ingredient.Unit;
        command.Parameters.Add("@package_quantity", MySqlDbType.Decimal).Value = ingredient.PackageQuantity;
        command.Parameters.Add("@total_cost", MySqlDbType.Decimal).Value = ingredient.TotalCost;
        command.Parameters.Add("@cost_per_unit", MySqlDbType.Decimal).Value = ingredient.CostPerUnit;
        command.Parameters.Add("@last_price_update", MySqlDbType.DateTime).Value = ingredient.LastPriceUpdate ?? (object)DBNull.Value;
        command.Parameters.Add("@notes", MySqlDbType.Text).Value = ingredient.Notes ?? (object)DBNull.Value;
        command.Parameters.Add("@id", MySqlDbType.Int32).Value = ingredient.Id;
        command.Parameters.Add("@user_id", MySqlDbType.Int32).Value = ingredient.UserId;

        try
        {
            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0)
            {
                throw new InvalidOperationException($"No ingredient found with id {ingredient.Id} for user {ingredient.UserId}.");
            }
        }
        catch (MySqlException ex) when ((MySqlErrorCode)ex.Number == MySqlErrorCode.DuplicateKeyEntry)
        {
            throw new DuplicateIngredientException(ingredient.Name, ex);
        }
    }

    public async Task<Ingredient?> GetIngredientByIdAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        const string query = @"SELECT i.id, i.user_id, i.name, i.description, i.category_id, i.unit, i.cost_per_unit, i.currency,
                                      i.reorder_level, i.notes, i.is_active, i.created_at, i.updated_at,
                                      i.supplier, i.package_quantity, i.package_size, i.total_cost, i.icon_key, i.last_price_update
                               FROM ingredients AS i
                               WHERE i.id = @id AND i.user_id = @user_id
                               LIMIT 1;";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(query, connection);
        command.Parameters.Add("@id", MySqlDbType.Int32).Value = id;
        command.Parameters.Add("@user_id", MySqlDbType.Int32).Value = userId;

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var ingredient = new Ingredient
            {
                Id = reader.GetInt32("id"),
                UserId = reader.GetInt32("user_id"),
                Name = reader.GetString("name"),
                Description = reader["description"] is DBNull ? null : reader.GetString("description"),
                CategoryId = reader["category_id"] is DBNull ? null : reader.GetInt32("category_id"),
                Unit = reader.GetString("unit"),
                CostPerUnit = reader.GetDecimal("cost_per_unit"),
                Currency = reader.GetString("currency"),
                ReorderLevel = reader["reorder_level"] is DBNull ? null : reader.GetDecimal("reorder_level"),
                Notes = reader["notes"] is DBNull ? null : reader.GetString("notes"),
                IsActive = reader.GetBoolean("is_active"),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader["updated_at"] is DBNull ? null : reader.GetDateTime("updated_at"),
                Supplier = reader["supplier"] is DBNull ? null : reader.GetString("supplier"),
                PackageQuantity = reader["package_quantity"] is DBNull ? null : reader.GetDecimal("package_quantity"),
                PackageSize = reader["package_size"] is DBNull ? null : reader.GetString("package_size"),
                TotalCost = reader["total_cost"] is DBNull ? null : reader.GetDecimal("total_cost"),
                IconKey = reader["icon_key"] is DBNull ? null : reader.GetString("icon_key"),
                LastPriceUpdate = reader["last_price_update"] is DBNull ? null : reader.GetDateTime("last_price_update"),
            };

            return ingredient;
        }

        return null;
    }
}
