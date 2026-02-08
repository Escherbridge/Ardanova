using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ArdaNova
{
    /// <summary>
    /// Main client for interacting with the ArdaNova platform API.
    /// </summary>
    public class ArdaNovaClient
    {
        private readonly string _baseUrl;
        private string _authToken;

        public ArdaNovaClient(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
        }

        /// <summary>
        /// Set the JWT authentication token for API requests.
        /// </summary>
        public void SetAuthToken(string token)
        {
            _authToken = token;
        }

        /// <summary>
        /// Authenticate with email and token, returning user profile.
        /// </summary>
        public async Task<UserProfile> AuthenticateAsync(string token)
        {
            SetAuthToken(token);
            return await GetAsync<UserProfile>("/api/Users/me");
        }

        /// <summary>
        /// Get the current user's profile.
        /// </summary>
        public async Task<UserProfile> GetProfileAsync()
        {
            return await GetAsync<UserProfile>("/api/Users/me");
        }

        /// <summary>
        /// Get all credentials for a user.
        /// </summary>
        public async Task<List<MembershipCredential>> GetCredentialsAsync(string userId)
        {
            return await GetAsync<List<MembershipCredential>>($"/api/MembershipCredentials/user/{userId}");
        }

        /// <summary>
        /// Get token balance for a project.
        /// </summary>
        public async Task<TokenBalance> GetTokenBalanceAsync(string userId, string projectId)
        {
            return await GetAsync<TokenBalance>($"/api/TokenBalances/user/{userId}/project/{projectId}");
        }

        private async Task<T> GetAsync<T>(string path)
        {
            var url = _baseUrl + path;
            using var request = UnityWebRequest.Get(url);

            if (!string.IsNullOrEmpty(_authToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {_authToken}");
            }
            request.SetRequestHeader("Content-Type", "application/json");

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ArdaNova] GET {path} failed: {request.error}");
                throw new ArdaNovaException(request.error, (int)request.responseCode);
            }

            return JsonUtility.FromJson<T>(request.downloadHandler.text);
        }

        private async Task<T> PostAsync<T>(string path, object body)
        {
            var url = _baseUrl + path;
            var json = JsonUtility.ToJson(body);
            var bodyBytes = Encoding.UTF8.GetBytes(json);

            using var request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();

            if (!string.IsNullOrEmpty(_authToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {_authToken}");
            }
            request.SetRequestHeader("Content-Type", "application/json");

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ArdaNova] POST {path} failed: {request.error}");
                throw new ArdaNovaException(request.error, (int)request.responseCode);
            }

            return JsonUtility.FromJson<T>(request.downloadHandler.text);
        }
    }
}
