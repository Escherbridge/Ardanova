using System;
using UnityEngine;

namespace ArdaNova.Auth
{
    /// <summary>
    /// Manages JWT token storage and refresh for ArdaNova authentication.
    /// </summary>
    public class JwtAuthProvider
    {
        private const string TokenPrefKey = "ardanova_auth_token";

        /// <summary>
        /// Store the JWT token persistently.
        /// </summary>
        public void StoreToken(string token)
        {
            PlayerPrefs.SetString(TokenPrefKey, token);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Retrieve the stored JWT token.
        /// </summary>
        public string GetStoredToken()
        {
            return PlayerPrefs.GetString(TokenPrefKey, null);
        }

        /// <summary>
        /// Clear the stored JWT token.
        /// </summary>
        public void ClearToken()
        {
            PlayerPrefs.DeleteKey(TokenPrefKey);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Check if a token is stored.
        /// </summary>
        public bool HasToken()
        {
            return PlayerPrefs.HasKey(TokenPrefKey);
        }
    }
}
