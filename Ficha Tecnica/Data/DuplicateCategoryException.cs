namespace Ficha_Tecnica.Data;

public class DuplicateCategoryException : Exception
{
    public DuplicateCategoryException(string categoryName, Exception? innerException = null)
        : base($"A categoria '{categoryName}' já está cadastrada.", innerException)
    {
        CategoryName = categoryName;
    }

    public string CategoryName { get; }
}
