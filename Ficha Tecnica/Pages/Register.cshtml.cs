using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Ficha_Tecnica.Data;
using Ficha_Tecnica.Extensions;
using Ficha_Tecnica.Models;
using Ficha_Tecnica.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using MySqlConnector;

public class RegisterModel : PageModel
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordHasher _passwordHasher;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(IUserRepository userRepository, PasswordHasher passwordHasher, ILogger<RegisterModel> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Input.Username = Input.Username?.Trim() ?? string.Empty;
        Input.Email = Input.Email?.Trim() ?? string.Empty;

        ModelState.ClearValidationState(nameof(Input));
        if (!TryValidateModel(Input, nameof(Input)))
        {
            _logger.LogWarning("Registration validation failed for initial input: {Errors}", ModelState.GetErrorMessages());
            return Page();
        }

        Input.Email = Input.Email.ToLowerInvariant();

        await ValidateUniquenessAsync(cancellationToken);
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Registration validation failed due to uniqueness constraints: {Errors}", ModelState.GetErrorMessages());
            return Page();
        }

        var (hash, salt) = _passwordHasher.HashPassword(Input.Password);
        var newUser = new UserAccount
        {
            Username = Input.Username,
            Email = Input.Email.ToLowerInvariant(),
            PasswordHash = hash,
            Salt = salt,
            IsActive = true
        };

        UserAccount createdUser;
        try
        {
            createdUser = await _userRepository.CreateUserAsync(newUser, cancellationToken);
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            AddDuplicateEntryErrors(ex);
            _logger.LogWarning(ex, "Duplicate entry detected while registering user {Username}.", Input.Username);
            return Page();
        }

        await SignInAsync(createdUser);
        _logger.LogInformation("User {UserId} registered and signed in successfully.", createdUser.Id);
        var destination = Url.Page("/Index") ?? "/Index";
        return LocalRedirect(destination);
    }

    private async Task ValidateUniquenessAsync(CancellationToken cancellationToken)
    {
        if (await _userRepository.UsernameExistsAsync(Input.Username, cancellationToken))
        {
            ModelState.AddModelError("Input.Username", "Este nome de usuário já está em uso.");
            _logger.LogWarning("Registration rejected because username {Username} already exists.", Input.Username);
        }

        if (await _userRepository.EmailExistsAsync(Input.Email, cancellationToken))
        {
            ModelState.AddModelError("Input.Email", "Este e-mail já está registado.");
            _logger.LogWarning("Registration rejected because email {Email} already exists.", Input.Email);
        }
    }

    private void AddDuplicateEntryErrors(MySqlException exception)
    {
        var message = exception.Message;
        if (message.Contains("ux_login_username", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("Input.Username", "Este nome de usuário já está em uso.");
        }
        else if (message.Contains("ux_login_email", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("Input.Email", "Este e-mail já está registado.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Não foi possível criar a conta. Tente novamente.");
            _logger.LogError(exception, "Unexpected duplicate entry constraint encountered while registering user {Username}.", Input.Username);
        }
    }

    private async Task SignInAsync(UserAccount user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Informe um nome de usuário.")]
        [Display(Name = "Nome de usuário")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome de usuário deve ter entre 3 e 100 caracteres.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe um e-mail.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe uma senha.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
        [Display(Name = "Senha")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme a senha.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar senha")]
        [Compare(nameof(Password), ErrorMessage = "As senhas não coincidem.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
