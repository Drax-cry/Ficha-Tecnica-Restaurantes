using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Ficha_Tecnica.Models;
using MySqlConnector;

namespace Ficha_Tecnica.Data;

public class SupplierRepository : ISupplierRepository
{
    private readonly string _connectionString;

    public SupplierRepository(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("A valid connection string is required.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<Supplier>> GetSuppliersAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string query = @"SELECT id, user_id, name, contact_name, email, phone, whatsapp, website, tax_id,
                                        payment_terms, address_line1, address_line2, city, state, postal_code, country,
                                        notes, is_preferred, is_active, created_at, updated_at
                                 FROM suppliers
                                 WHERE user_id = @user_id
                                 ORDER BY name";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(query, connection);
        command.Parameters.Add("@user_id", MySqlDbType.Int32).Value = userId;

        var suppliers = new List<Supplier>();

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var supplier = new Supplier
            {
                Id = reader.GetInt32("id"),
                UserId = reader.GetInt32("user_id"),
                Name = reader.GetString("name"),
                ContactName = reader["contact_name"] is DBNull ? null : reader.GetString("contact_name"),
                Email = reader["email"] is DBNull ? null : reader.GetString("email"),
                Phone = reader["phone"] is DBNull ? null : reader.GetString("phone"),
                Whatsapp = reader["whatsapp"] is DBNull ? null : reader.GetString("whatsapp"),
                Website = reader["website"] is DBNull ? null : reader.GetString("website"),
                TaxId = reader["tax_id"] is DBNull ? null : reader.GetString("tax_id"),
                PaymentTerms = reader["payment_terms"] is DBNull ? null : reader.GetString("payment_terms"),
                AddressLine1 = reader["address_line1"] is DBNull ? null : reader.GetString("address_line1"),
                AddressLine2 = reader["address_line2"] is DBNull ? null : reader.GetString("address_line2"),
                City = reader["city"] is DBNull ? null : reader.GetString("city"),
                State = reader["state"] is DBNull ? null : reader.GetString("state"),
                PostalCode = reader["postal_code"] is DBNull ? null : reader.GetString("postal_code"),
                Country = reader["country"] is DBNull ? null : reader.GetString("country"),
                Notes = reader["notes"] is DBNull ? null : reader.GetString("notes"),
                IsPreferred = reader.GetBoolean("is_preferred"),
                IsActive = reader.GetBoolean("is_active"),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader["updated_at"] is DBNull ? null : reader.GetDateTime("updated_at"),
            };

            suppliers.Add(supplier);
        }

        return suppliers;
    }

    public async Task<Supplier> CreateSupplierAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        if (supplier is null)
        {
            throw new ArgumentNullException(nameof(supplier));
        }

        const string commandText = @"INSERT INTO suppliers (user_id, name, notes, is_preferred, is_active)
                                     VALUES (@user_id, @name, @notes, @is_preferred, @is_active);";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(commandText, connection);
        command.Parameters.Add("@user_id", MySqlDbType.Int32).Value = supplier.UserId;
        command.Parameters.Add("@name", MySqlDbType.VarChar, 150).Value = supplier.Name;
        command.Parameters.Add("@notes", MySqlDbType.Text).Value = supplier.Notes ?? (object)DBNull.Value;
        command.Parameters.Add("@is_preferred", MySqlDbType.Bool).Value = supplier.IsPreferred;
        command.Parameters.Add("@is_active", MySqlDbType.Bool).Value = supplier.IsActive;

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (MySqlException ex) when ((MySqlErrorCode)ex.Number == MySqlErrorCode.DuplicateKeyEntry)
        {
            throw new DuplicateSupplierException(supplier.Name, ex);
        }

        supplier.Id = Convert.ToInt32(command.LastInsertedId);
        supplier.CreatedAt = DateTime.UtcNow;
        supplier.UpdatedAt = null;

        return supplier;
    }

    public async Task UpdateSupplierAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        if (supplier is null)
        {
            throw new ArgumentNullException(nameof(supplier));
        }

        const string commandText = @"UPDATE suppliers
                                     SET name = @name,
                                         notes = @notes,
                                         is_preferred = @is_preferred,
                                         is_active = @is_active,
                                         updated_at = UTC_TIMESTAMP(6)
                                     WHERE id = @id AND user_id = @user_id;";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(commandText, connection);
        command.Parameters.Add("@name", MySqlDbType.VarChar, 150).Value = supplier.Name;
        command.Parameters.Add("@notes", MySqlDbType.Text).Value = supplier.Notes ?? (object)DBNull.Value;
        command.Parameters.Add("@is_preferred", MySqlDbType.Bool).Value = supplier.IsPreferred;
        command.Parameters.Add("@is_active", MySqlDbType.Bool).Value = supplier.IsActive;
        command.Parameters.Add("@id", MySqlDbType.Int32).Value = supplier.Id;
        command.Parameters.Add("@user_id", MySqlDbType.Int32).Value = supplier.UserId;

        try
        {
            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0)
            {
                throw new InvalidOperationException($"No supplier found with id {supplier.Id} for user {supplier.UserId}.");
            }
        }
        catch (MySqlException ex) when ((MySqlErrorCode)ex.Number == MySqlErrorCode.DuplicateKeyEntry)
        {
            throw new DuplicateSupplierException(supplier.Name, ex);
        }
    }
}
