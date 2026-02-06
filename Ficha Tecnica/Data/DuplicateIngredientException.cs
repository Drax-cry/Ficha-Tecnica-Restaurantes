namespace Ficha_Tecnica.Data;

public class DuplicateIngredientException : Exception
{
    public DuplicateIngredientException(string ingredientName, Exception? innerException = null)
        : base($"O ingrediente '{ingredientName}' já está cadastrado.", innerException)
    {
        IngredientName = ingredientName;
    }

    public string IngredientName { get; }
}
