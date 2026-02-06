using System.Data;
using Ficha_Tecnica.Models;
using MySqlConnector;

namespace Ficha_Tecnica.Data;

public class PriceMovementRepository : IPriceMovementRepository
{
    private readonly string _connectionString;

    public PriceMovementRepository(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("A valid connection string is required.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<PriceMovement>> GetMovementsAsync(
        int userId,
        DateTime? startDate,
        DateTime? endDate,
        int? ingredientId,
        CancellationToken cancellationToken = default)
    {
        const string query = @"SELECT pm.id,
                                       pm.user_id,
                                       pm.ingredient_id,
                                       pm.previous_price,
                                       pm.new_price,
                                       pm.change_amount,
                                       pm.change_percentage,
                                       pm.effective_date,
                                       pm.recorded_at,
                                       pm.notes,
                                       i.name AS ingredient_name,
                                       i.unit,
                                       i.currency
                                FROM ingredient_price_movements AS pm
                                INNER JOIN ingredients AS i ON i.id = pm.ingredient_id
                                WHERE pm.user_id = @user_id
                                  AND (@ingredient_id IS NULL OR pm.ingredient_id = @ingredient_id)
                                  AND (@start_date IS NULL OR pm.effective_date >= @start_date)
                                  AND (@end_date IS NULL OR pm.effective_date <= @end_date)
                                ORDER BY pm.effective_date DESC, pm.recorded_at DESC;";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(query, connection);
        command.Parameters.Add("@user_id", MySqlDbType.Int32).Value = userId;
        command.Parameters.Add("@ingredient_id", MySqlDbType.Int32).Value = ingredientId ?? (object)DBNull.Value;
        command.Parameters.Add("@start_date", MySqlDbType.DateTime).Value = startDate ?? (object)DBNull.Value;
        command.Parameters.Add("@end_date", MySqlDbType.DateTime).Value = endDate ?? (object)DBNull.Value;

        var results = new List<PriceMovement>();

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var movement = new PriceMovement
            {
                Id = reader.GetInt32("id"),
                UserId = reader.GetInt32("user_id"),
                IngredientId = reader.GetInt32("ingredient_id"),
                PreviousPrice = reader.GetDecimal("previous_price"),
                NewPrice = reader.GetDecimal("new_price"),
                ChangeAmount = reader.GetDecimal("change_amount"),
                ChangePercentage = reader["change_percentage"] is DBNull ? null : reader.GetDecimal("change_percentage"),
                EffectiveDate = reader.GetDateTime("effective_date"),
                RecordedAt = reader.GetDateTime("recorded_at"),
                Notes = reader["notes"] is DBNull ? null : reader.GetString("notes"),
                IngredientName = reader.GetString("ingredient_name"),
                Unit = reader.GetString("unit"),
                Currency = reader.GetString("currency"),
            };

            results.Add(movement);
        }

        return results;
    }

    public async Task<PriceMovement> CreateMovementAsync(PriceMovement movement, CancellationToken cancellationToken = default)
    {
        if (movement is null)
        {
            throw new ArgumentNullException(nameof(movement));
        }

        const string commandText = @"INSERT INTO ingredient_price_movements
                                        (user_id, ingredient_id, previous_price, new_price, change_amount, change_percentage, effective_date, notes, recorded_at)
                                      VALUES
                                        (@user_id, @ingredient_id, @previous_price, @new_price, @change_amount, @change_percentage, @effective_date, @notes, UTC_TIMESTAMP(6));";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(commandText, connection);
        command.Parameters.Add("@user_id", MySqlDbType.Int32).Value = movement.UserId;
        command.Parameters.Add("@ingredient_id", MySqlDbType.Int32).Value = movement.IngredientId;
        command.Parameters.Add("@previous_price", MySqlDbType.Decimal).Value = movement.PreviousPrice;
        command.Parameters.Add("@new_price", MySqlDbType.Decimal).Value = movement.NewPrice;
        command.Parameters.Add("@change_amount", MySqlDbType.Decimal).Value = movement.ChangeAmount;
        command.Parameters.Add("@change_percentage", MySqlDbType.Decimal).Value = movement.ChangePercentage ?? (object)DBNull.Value;
        command.Parameters.Add("@effective_date", MySqlDbType.DateTime).Value = movement.EffectiveDate;
        command.Parameters.Add("@notes", MySqlDbType.Text).Value = movement.Notes ?? (object)DBNull.Value;

        await command.ExecuteNonQueryAsync(cancellationToken);

        movement.Id = Convert.ToInt32(command.LastInsertedId);
        movement.RecordedAt = DateTime.UtcNow;

        return movement;
    }
}
