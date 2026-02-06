using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ficha_Tecnica.Pages;

public class AboutModel : PageModel
{
    public string Version { get; private set; } = "2.3";

    public void OnGet()
    {
        // Versão definida conforme documentação institucional do produto.
    }
}
