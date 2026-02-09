using UnityEngine;

namespace ArdaNova.Auth
{
    /// <summary>
    /// Manages session token storage for ArdaNova authentication.
    /// Tokens are obtained by exchanging an auth code via the SDK API.
    /// </summary>
    public class SessionProvider
    {
        private const string TokenPrefKey = "ardanova_session_token";

        /// <summary>
        /// Store the session token persistently.
        /// </summary>
        public void StoreToken(string token)
        {
            PlayerPrefs.SetString(TokenPrefKey, token);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Retrieve the stored session token.
        /// </summary>
        public string GetStoredToken()
        {
            return PlayerPrefs.GetString(TokenPrefKey, null);
        }

        /// <summary>
        /// Clear the stored session token (logout).
        /// </summary>
        public void ClearToken()
        {
            PlayerPrefs.DeleteKey(TokenPrefKey);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Check if a session token is stored.
        /// </summary>
        public bool HasToken()
        {
            return PlayerPrefs.HasKey(TokenPrefKey);
        }
    }
}
