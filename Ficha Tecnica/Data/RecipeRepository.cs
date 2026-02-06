using System.Data;
using System.Globalization;
using System.Linq;
using Ficha_Tecnica.Models;
using MySqlConnector;

namespace Ficha_Tecnica.Data;

public class RecipeRepository : IRecipeRepository
{
    private readonly string _connectionString;

    public RecipeRepository(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("A valid connection string is required.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<Recipe>> GetRecipesAsync(int userId, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var supportsChefNotes = await ChefNotesColumnExistsAsync(connection, cancellationToken);
        var recipeQuery = supportsChefNotes
            ? @"SELECT r.id, r.user_id, r.name, r.category_id, rc.name AS category_name, rc.icon_key AS category_icon_key,
                                             rc.color AS category_color, r.description, r.chef_notes,
                                             r.preparation_time, r.yield, r.target_margin, r.ingredient_cost, r.suggested_price, r.image_path,
                                             r.created_at, r.updated_at
                                      FROM recipes AS r
                                      LEFT JOIN recipe_categories AS rc ON rc.id = r.category_id
                                      WHERE r.user_id = @user_id
                                      ORDER BY r.created_at DESC;"
            : @"SELECT r.id, r.user_id, r.name, r.category_id, rc.name AS category_name, rc.icon_key AS category_icon_key,
                                             rc.color AS category_color, r.description, NULL AS chef_notes,
                                             r.preparation_time, r.yield, r.target_margin, r.ingredient_cost, r.suggested_price, r.image_path,
                                             r.created_at, r.updated_at
                                      FROM recipes AS r
                                      LEFT JOIN recipe_categories AS rc ON rc.id = r.category_id
                                      WHERE r.user_id = @user_id
                                      ORDER BY r.created_at DESC;";

        await using var recipeCommand = new MySqlCommand(recipeQuery, connection);
        recipeCommand.Parameters.Add("@user_id", MySqlDbType.Int32).Value = userId;

        var recipes = new List<Recipe>();

        await using var reader = await recipeCommand.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            recipes.Add(new Recipe
            {
                Id = reader.GetInt32("id"),
                UserId = reader.GetInt32("user_id"),
                Name = reader.GetString("name"),
                CategoryId = reader.GetInt32("category_id"),
                CategoryName = reader["category_name"] is DBNull ? null : reader.GetString("category_name"),
                CategoryIconKey = reader["category_icon_key"] is DBNull ? null : reader.GetString("category_icon_key"),
                CategoryColor = reader["category_color"] is DBNull ? null : reader.GetString("category_color"),
                Description = reader["description"] is DBNull ? null : reader.GetString("description"),
                ChefNotes = reader["chef_notes"] is DBNull ? null : reader.GetString("chef_notes"),
                PreparationTime = reader["preparation_time"] is DBNull ? null : reader.GetString("preparation_time"),
                Yield = reader["yield"] is DBNull ? null : reader.GetString("yield"),
                TargetMargin = reader.GetDecimal("target_margin"),
                IngredientCost = reader.GetDecimal("ingredient_cost"),
                SuggestedPrice = reader.GetDecimal("suggested_price"),
                ImagePath = reader["image_path"] is DBNull ? null : reader.GetString("image_path"),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader["updated_at"] is DBNull ? null : reader.GetDateTime("updated_at"),
            });
        }

        if (recipes.Count == 0)
        {
            return recipes;
        }

        await reader.DisposeAsync();

        var recipeIds = recipes.Select(r => r.Id).ToArray();
        var parameterNames = recipeIds.Select((_, index) => "@id" + index).ToArray();
        var ingredientQuery = $@"SELECT recipe_id, ingredient_id, ingredient_name, quantity, unit, cost_per_unit, total_cost
                                FROM recipe_ingredients
                                WHERE recipe_id IN ({string.Join(",", parameterNames)})
                                ORDER BY recipe_id, total_cost DESC;";

        await using var ingredientCommand = new MySqlCommand(ingredientQuery, connection);
        for (var i = 0; i < recipeIds.Length; i++)
        {
            ingredientCommand.Parameters.Add("@id" + i, MySqlDbType.Int32).Value = recipeIds[i];
        }

        var ingredientLookup = recipes.ToDictionary(r => r.Id, _ => new List<RecipeIngredient>());

        await using var ingredientReader = await ingredientCommand.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        while (await ingredientReader.ReadAsync(cancellationToken))
        {
            var ingredient = new RecipeIngredient
            {
                RecipeId = ingredientReader.GetInt32("recipe_id"),
                IngredientId = ingredientReader.GetInt32("ingredient_id"),
                IngredientName = ingredientReader.GetString("ingredient_name"),
                Quantity = ingredientReader.GetDecimal("quantity"),
                Unit = ingredientReader.GetString("unit"),
                CostPerUnit = ingredientReader.GetDecimal("cost_per_unit"),
                TotalCost = ingredientReader.GetDecimal("total_cost"),
            };

            ingredientLookup[ingredient.RecipeId].Add(ingredient);
        }

        foreach (var recipe in recipes)
        {
            recipe.Ingredients = ingredientLookup.TryGetValue(recipe.Id, out var list)
                ? list
                : Array.Empty<RecipeIngredient>();
        }

        return recipes;
    }

    public async Task<Recipe?> GetRecipeAsync(int userId, int recipeId, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var supportsChefNotes = await ChefNotesColumnExistsAsync(connection, cancellationToken);
        var recipeQuery = supportsChefNotes
            ? @"SELECT r.id, r.user_id, r.name, r.category_id, rc.name AS category_name, rc.icon_key AS category_icon_key,
                                             rc.color AS category_color, r.description, r.chef_notes,
                                             r.preparation_time, r.yield, r.target_margin, r.ingredient_cost, r.suggested_price, r.image_path,
                                             r.created_at, r.updated_at
                                      FROM recipes AS r
                                      LEFT JOIN recipe_categories AS rc ON rc.id = r.category_id
                                      WHERE r.user_id = @user_id AND r.id = @recipe_id
                                      LIMIT 1;"
            : @"SELECT r.id, r.user_id, r.name, r.category_id, rc.name AS category_name, rc.icon_key AS category_icon_key,
                                             rc.color AS category_color, r.description, NULL AS chef_notes,
                                             r.preparation_time, r.yield, r.target_margin, r.ingredient_cost, r.suggested_price, r.image_path,
                                             r.created_at, r.updated_at
                                      FROM recipes AS r
                                      LEFT JOIN recipe_categories AS rc ON rc.id = r.category_id
                                      WHERE r.user_id = @user_id AND r.id = @recipe_id
                                      LIMIT 1;";

        await using var recipeCommand = new MySqlCommand(recipeQuery, connection);
        recipeCommand.Parameters.Add("@user_id", MySqlDbType.Int32).Value = userId;
        recipeCommand.Parameters.Add("@recipe_id", MySqlDbType.Int32).Value = recipeId;

        await using var reader = await recipeCommand.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var recipe = new Recipe
        {
            Id = reader.GetInt32("id"),
            UserId = reader.GetInt32("user_id"),
            Name = reader.GetString("name"),
            CategoryId = reader.GetInt32("category_id"),
            CategoryName = reader["category_name"] is DBNull ? null : reader.GetString("category_name"),
            CategoryIconKey = reader["category_icon_key"] is DBNull ? null : reader.GetString("category_icon_key"),
            CategoryColor = reader["category_color"] is DBNull ? null : reader.GetString("category_color"),
            Description = reader["description"] is DBNull ? null : reader.GetString("description"),
            ChefNotes = reader["chef_notes"] is DBNull ? null : reader.GetString("chef_notes"),
            PreparationTime = reader["preparation_time"] is DBNull ? null : reader.GetString("preparation_time"),
            Yield = reader["yield"] is DBNull ? null : reader.GetString("yield"),
            TargetMargin = reader.GetDecimal("target_margin"),
            IngredientCost = reader.GetDecimal("ingredient_cost"),
            SuggestedPrice = reader.GetDecimal("suggested_price"),
            ImagePath = reader["image_path"] is DBNull ? null : reader.GetString("image_path"),
            CreatedAt = reader.GetDateTime("created_at"),
            UpdatedAt = reader["updated_at"] is DBNull ? null : reader.GetDateTime("updated_at"),
        };

        await reader.DisposeAsync();

        const string ingredientQuery = @"SELECT recipe_id, ingredient_id, ingredient_name, quantity, unit, cost_per_unit, total_cost
                                         FROM recipe_ingredients
                                         WHERE recipe_id = @recipe_id
                                         ORDER BY total_cost DESC;";

        await using var ingredientCommand = new MySqlCommand(ingredientQuery, connection);
        ingredientCommand.Parameters.Add("@recipe_id", MySqlDbType.Int32).Value = recipe.Id;

        var ingredients = new List<RecipeIngredient>();

        await using var ingredientReader = await ingredientCommand.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        while (await ingredientReader.ReadAsync(cancellationToken))
        {
            ingredients.Add(new RecipeIngredient
            {
                RecipeId = ingredientReader.GetInt32("recipe_id"),
                IngredientId = ingredientReader.GetInt32("ingredient_id"),
                IngredientName = ingredientReader.GetString("ingredient_name"),
                Quantity = ingredientReader.GetDecimal("quantity"),
                Unit = ingredientReader.GetString("unit"),
                CostPerUnit = ingredientReader.GetDecimal("cost_per_unit"),
                TotalCost = ingredientReader.GetDecimal("total_cost"),
            });
        }

        recipe.Ingredients = ingredients;

        return recipe;
    }

    public async Task<Recipe> CreateRecipeAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        if (recipe is null)
        {
            throw new ArgumentNullException(nameof(recipe));
        }

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var supportsChefNotes = await ChefNotesColumnExistsAsync(connection, cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var recipeCommandText = supportsChefNotes
                ? @"INSERT INTO recipes (user_id, name, category_id, description, chef_notes, preparation_time, yield,
                                                                   target_margin, ingredient_cost, suggested_price, image_path)
                                             VALUES (@user_id, @name, @category_id, @description, @chef_notes, @preparation_time, @yield,
                                                     @target_margin, @ingredient_cost, @suggested_price, @image_path);"
                : @"INSERT INTO recipes (user_id, name, category_id, description, preparation_time, yield,
                                                                   target_margin, ingredient_cost, suggested_price, image_path)
                                             VALUES (@user_id, @name, @category_id, @description, @preparation_time, @yield,
                                                     @target_margin, @ingredient_cost, @suggested_price, @image_path);";

            await using var recipeCommand = new MySqlCommand(recipeCommandText, connection, (MySqlTransaction)transaction);
            recipeCommand.Parameters.Add("@user_id", MySqlDbType.Int32).Value = recipe.UserId;
            recipeCommand.Parameters.Add("@name", MySqlDbType.VarChar, 200).Value = recipe.Name;
            recipeCommand.Parameters.Add("@category_id", MySqlDbType.Int32).Value = recipe.CategoryId;
            recipeCommand.Parameters.Add("@description", MySqlDbType.Text).Value = recipe.Description ?? (object)DBNull.Value;
            recipeCommand.Parameters.Add("@preparation_time", MySqlDbType.VarChar, 80).Value = recipe.PreparationTime ?? (object)DBNull.Value;
            recipeCommand.Parameters.Add("@yield", MySqlDbType.VarChar, 80).Value = recipe.Yield ?? (object)DBNull.Value;
            recipeCommand.Parameters.Add("@target_margin", MySqlDbType.Decimal).Value = recipe.TargetMargin;
            recipeCommand.Parameters.Add("@ingredient_cost", MySqlDbType.Decimal).Value = recipe.IngredientCost;
            recipeCommand.Parameters.Add("@suggested_price", MySqlDbType.Decimal).Value = recipe.SuggestedPrice;
            recipeCommand.Parameters.Add("@image_path", MySqlDbType.VarChar, 255).Value = recipe.ImagePath ?? (object)DBNull.Value;

            if (supportsChefNotes)
            {
                recipeCommand.Parameters.Add("@chef_notes", MySqlDbType.Text).Value = recipe.ChefNotes ?? (object)DBNull.Value;
            }

            await recipeCommand.ExecuteNonQueryAsync(cancellationToken);

            recipe.Id = Convert.ToInt32(recipeCommand.LastInsertedId);

            const string ingredientCommandText = @"INSERT INTO recipe_ingredients (recipe_id, ingredient_id, ingredient_name, quantity, unit, cost_per_unit, total_cost)
                                                   VALUES (@recipe_id, @ingredient_id, @ingredient_name, @quantity, @unit, @cost_per_unit, @total_cost);";

            foreach (var ingredient in recipe.Ingredients)
            {
                await using var ingredientCommand = new MySqlCommand(ingredientCommandText, connection, (MySqlTransaction)transaction);
                ingredientCommand.Parameters.Add("@recipe_id", MySqlDbType.Int32).Value = recipe.Id;
                ingredientCommand.Parameters.Add("@ingredient_id", MySqlDbType.Int32).Value = ingredient.IngredientId;
                ingredientCommand.Parameters.Add("@ingredient_name", MySqlDbType.VarChar, 200).Value = ingredient.IngredientName;
                ingredientCommand.Parameters.Add("@quantity", MySqlDbType.Decimal).Value = ingredient.Quantity;
                ingredientCommand.Parameters.Add("@unit", MySqlDbType.VarChar, 50).Value = ingredient.Unit;
                ingredientCommand.Parameters.Add("@cost_per_unit", MySqlDbType.Decimal).Value = ingredient.CostPerUnit;
                ingredientCommand.Parameters.Add("@total_cost", MySqlDbType.Decimal).Value = ingredient.TotalCost;

                await ingredientCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        recipe.CreatedAt = DateTime.UtcNow;
        recipe.UpdatedAt = null;

        return recipe;
    }

    public async Task<Recipe> UpdateRecipeAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        if (recipe is null)
        {
            throw new ArgumentNullException(nameof(recipe));
        }

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var supportsChefNotes = await ChefNotesColumnExistsAsync(connection, cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var updateCommandText = supportsChefNotes
                ? @"UPDATE recipes
                                              SET name = @name,
                                                  category_id = @category_id,
                                                  description = @description,
                                                  chef_notes = @chef_notes,
                                                  preparation_time = @preparation_time,
                                                  yield = @yield,
                                                  target_margin = @target_margin,
                                                  ingredient_cost = @ingredient_cost,
                                                  suggested_price = @suggested_price,
                                                  image_path = @image_path,
                                                  updated_at = UTC_TIMESTAMP()
                                              WHERE id = @id AND user_id = @user_id;"
                : @"UPDATE recipes
                                              SET name = @name,
                                                  category_id = @category_id,
                                                  description = @description,
                                                  preparation_time = @preparation_time,
                                                  yield = @yield,
                                                  target_margin = @target_margin,
                                                  ingredient_cost = @ingredient_cost,
                                                  suggested_price = @suggested_price,
                                                  image_path = @image_path,
                                                  updated_at = UTC_TIMESTAMP()
                                              WHERE id = @id AND user_id = @user_id;";

            await using var updateCommand = new MySqlCommand(updateCommandText, connection, (MySqlTransaction)transaction);
            updateCommand.Parameters.Add("@name", MySqlDbType.VarChar, 200).Value = recipe.Name;
            updateCommand.Parameters.Add("@category_id", MySqlDbType.Int32).Value = recipe.CategoryId;
            updateCommand.Parameters.Add("@description", MySqlDbType.Text).Value = recipe.Description ?? (object)DBNull.Value;
            updateCommand.Parameters.Add("@preparation_time", MySqlDbType.VarChar, 80).Value = recipe.PreparationTime ?? (object)DBNull.Value;
            updateCommand.Parameters.Add("@yield", MySqlDbType.VarChar, 80).Value = recipe.Yield ?? (object)DBNull.Value;
            updateCommand.Parameters.Add("@target_margin", MySqlDbType.Decimal).Value = recipe.TargetMargin;
            updateCommand.Parameters.Add("@ingredient_cost", MySqlDbType.Decimal).Value = recipe.IngredientCost;
            updateCommand.Parameters.Add("@suggested_price", MySqlDbType.Decimal).Value = recipe.SuggestedPrice;
            updateCommand.Parameters.Add("@image_path", MySqlDbType.VarChar, 255).Value = recipe.ImagePath ?? (object)DBNull.Value;
            updateCommand.Parameters.Add("@id", MySqlDbType.Int32).Value = recipe.Id;
            updateCommand.Parameters.Add("@user_id", MySqlDbType.Int32).Value = recipe.UserId;

            if (supportsChefNotes)
            {
                updateCommand.Parameters.Add("@chef_notes", MySqlDbType.Text).Value = recipe.ChefNotes ?? (object)DBNull.Value;
            }

            var affected = await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new InvalidOperationException("A receita não foi encontrada ou não pertence a este utilizador.");
            }

            const string deleteIngredientsCommandText = @"DELETE FROM recipe_ingredients WHERE recipe_id = @recipe_id;";
            await using var deleteCommand = new MySqlCommand(deleteIngredientsCommandText, connection, (MySqlTransaction)transaction);
            deleteCommand.Parameters.Add("@recipe_id", MySqlDbType.Int32).Value = recipe.Id;
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);

            const string insertIngredientCommandText = @"INSERT INTO recipe_ingredients (recipe_id, ingredient_id, ingredient_name, quantity, unit, cost_per_unit, total_cost)
                                                       VALUES (@recipe_id, @ingredient_id, @ingredient_name, @quantity, @unit, @cost_per_unit, @total_cost);";

            foreach (var ingredient in recipe.Ingredients)
            {
                await using var ingredientCommand = new MySqlCommand(insertIngredientCommandText, connection, (MySqlTransaction)transaction);
                ingredientCommand.Parameters.Add("@recipe_id", MySqlDbType.Int32).Value = recipe.Id;
                ingredientCommand.Parameters.Add("@ingredient_id", MySqlDbType.Int32).Value = ingredient.IngredientId;
                ingredientCommand.Parameters.Add("@ingredient_name", MySqlDbType.VarChar, 200).Value = ingredient.IngredientName;
                ingredientCommand.Parameters.Add("@quantity", MySqlDbType.Decimal).Value = ingredient.Quantity;
                ingredientCommand.Parameters.Add("@unit", MySqlDbType.VarChar, 50).Value = ingredient.Unit;
                ingredientCommand.Parameters.Add("@cost_per_unit", MySqlDbType.Decimal).Value = ingredient.CostPerUnit;
                ingredientCommand.Parameters.Add("@total_cost", MySqlDbType.Decimal).Value = ingredient.TotalCost;

                await ingredientCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        recipe.UpdatedAt = DateTime.UtcNow;
        return recipe;
    }

    private static async Task<bool> ChefNotesColumnExistsAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        const string columnExistsQuery = @"SELECT COUNT(*)
                                           FROM information_schema.columns
                                           WHERE table_schema = DATABASE()
                                             AND table_name = 'recipes'
                                             AND column_name = 'chef_notes';";

        await using var command = new MySqlCommand(columnExistsQuery, connection);
        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result is null || result is DBNull)
        {
            return false;
        }

        return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture) > 0;
    }
}
