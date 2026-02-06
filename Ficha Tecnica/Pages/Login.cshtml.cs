using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Ficha_Tecnica.Data;
using Ficha_Tecnica.Extensions;
using Ficha_Tecnica.Models;
using Ficha_Tecnica.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using MySqlConnector;

public class LoginModel : PageModel
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordHasher _passwordHasher;
    private readonly ILoginRateLimiter _loginRateLimiter;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        IUserRepository userRepository,
        PasswordHasher passwordHasher,
        ILoginRateLimiter loginRateLimiter,
        ILogger<LoginModel> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _loginRateLimiter = loginRateLimiter ?? throw new ArgumentNullException(nameof(loginRateLimiter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [BindProperty]
    [Display(Name = "Usuário ou e-mail")]
    [Required(ErrorMessage = "Informe o usuário ou e-mail.")]
    public string? Username { get; set; }

    [BindProperty]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    [Required(ErrorMessage = "Informe a senha.")]
    public string? Password { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            ReturnUrl = returnUrl;
        }
        else
        {
            ReturnUrl = Url.Page("/Index") ?? "/Index";
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Username = Username?.Trim();
        if (string.IsNullOrWhiteSpace(Username))
        {
            ModelState.AddModelError(nameof(Username), "Informe o usuário ou e-mail.");
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ModelState.AddModelError(nameof(Password), "Informe a senha.");
        }

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Login form validation failed: {Errors}", ModelState.GetErrorMessages());
            return Page();
        }

        var identifier = Username!;
        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var throttleKey = $"{remoteIp}:{identifier.ToLowerInvariant()}";

        var enforcedDelay = await _loginRateLimiter.GetDelayAsync(throttleKey, cancellationToken);
        if (enforcedDelay > TimeSpan.Zero)
        {
            _logger.LogWarning(
                "Applying throttling delay of {Delay} for login identifier {Identifier} from IP {IP}.",
                enforcedDelay,
                identifier,
                remoteIp);
            await Task.Delay(enforcedDelay, cancellationToken);
        }

        UserAccount? user;
        try
        {
            user = await _userRepository.GetByUsernameOrEmailAsync(identifier, cancellationToken);
        }
        catch (Exception ex) when (ex is MySqlException or DbException or TimeoutException or InvalidOperationException)
        {
            _logger.LogError(
                ex,
                "Database lookup failed for login identifier {Identifier} from IP {IP}.",
                identifier,
                remoteIp);

            ModelState.AddModelError(string.Empty, "Não foi possível contactar o serviço de autenticação. Tente novamente mais tarde.");
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return Page();
        }

        if (user is null || !IsPasswordValid(user, Password!))
        {
            ModelState.AddModelError(string.Empty, "Usuário ou senha inválidos.");
            var attemptResult = await _loginRateLimiter.RegisterFailedAttemptAsync(throttleKey, cancellationToken);
            _logger.LogWarning(
                "Login failed for identifier {Identifier} from IP {IP}: invalid credentials. Failure count: {Count}, enforced delay: {Delay}.",
                identifier,
                remoteIp,
                attemptResult.FailureCount,
                attemptResult.EnforcedDelay);

            if (attemptResult.FailureCount >= 5)
            {
                _logger.LogWarning(
                    "Suspicious login activity detected for identifier {Identifier} from IP {IP}: {Count} consecutive failures.",
                    identifier,
                    remoteIp,
                    attemptResult.FailureCount);
            }
            return Page();
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "A conta está desativada. Entre em contacto com o suporte.");
            var attemptResult = await _loginRateLimiter.RegisterFailedAttemptAsync(throttleKey, cancellationToken);
            _logger.LogWarning(
                "Login blocked for disabled account with identifier {Identifier} from IP {IP}. Failure count: {Count}.",
                identifier,
                remoteIp,
                attemptResult.FailureCount);
            return Page();
        }

        _loginRateLimiter.ResetAttempts(throttleKey);
        await SignInUserAsync(user);
        _logger.LogInformation("User {UserId} logged in successfully.", user.Id);

        var destination = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : Url.Page("/Index") ?? "/Index";
        return LocalRedirect(destination);
    }

    private bool IsPasswordValid(UserAccount user, string password)
    {
        if (user.Salt is null)
        {
            return false;
        }

        return _passwordHasher.VerifyPassword(password, user.PasswordHash, user.Salt);
    }

    private async Task SignInUserAsync(UserAccount user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);
    }
}