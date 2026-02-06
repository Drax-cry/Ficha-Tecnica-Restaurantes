using Ficha_Tecnica.Models;

namespace Ficha_Tecnica.Services;

public interface IRecipePdfExporter
{
    byte[] Export(Recipe recipe, byte[]? dishImageBytes = null);
}
