using IASD_Magnolia.Models;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Text.Json;
using Microsoft.Extensions.Logging;


namespace IASD_Magnolia.Services;

/// <summary>
/// Servicio para gestionar el estado de autenticación del usuario
/// </summary>
public class AuthStateService
{
    private readonly ProtectedSessionStorage _sessionStorage;
    private User? _currentUser;
    private bool _isInitialized = false;
    public event Action? OnAuthStateChanged;
    private readonly ILogger<AuthStateService> _logger;


    private const string USER_KEY = "current_user";

    public AuthStateService(
     ProtectedSessionStorage sessionStorage,
     ILogger<AuthStateService> logger)
    {
        _sessionStorage = sessionStorage;
        _logger = logger;
    }

    public User? CurrentUser
    {
        get => _currentUser;
        private set
        {
            _currentUser = value;
            OnAuthStateChanged?.Invoke();
        }
    }

    public bool IsAuthenticated => CurrentUser != null;

    public bool IsAdmin => CurrentUser?.RoleId == 1;

    public Guid UserId => CurrentUser?.Id ?? Guid.Empty;

    public async Task InitializeAsync()
    {

        try
        {
            var result = await _sessionStorage.GetAsync<User>(USER_KEY);
            if (result.Success && result.Value != null)
            {
                _currentUser = result.Value;
            }
            else
            {
                _currentUser = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al leer el ProtectedSessionStorage.");
            _currentUser = null;
        }

        _isInitialized = true;
        OnAuthStateChanged?.Invoke();
    }

    public async Task SetUserAsync(User? user)
    {
        CurrentUser = user;

        if (user != null)
        {
            await _sessionStorage.SetAsync(USER_KEY, user);
        }
        else
        {
            await _sessionStorage.DeleteAsync(USER_KEY);
        }
    }

    public async Task LogoutAsync()
    {
        CurrentUser = null;
        await _sessionStorage.DeleteAsync(USER_KEY);
    }
}
