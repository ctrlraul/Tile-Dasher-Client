using Godot;

namespace TD.Connection;

public abstract class TokenManager
{
    private static string ApplicationName => ProjectSettings.GetSetting("application/config/name").AsString();

    private static string _tokenCache;

    public static bool UseTokenStorage { get; set; } = true;
    
    public static string Token
    {
        get
        {
            if (_tokenCache is null && UseTokenStorage)
                _tokenCache = CredentialManager.ReadCredential(ApplicationName)?.Password;
            
            return _tokenCache;
        }
        set
        {
            _tokenCache = value;
            
            if (UseTokenStorage)
                CredentialManager.WriteCredential(ApplicationName, System.Environment.UserName, value);
        }
    }
}