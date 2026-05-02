using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ArdaNova
{
    /// <summary>
    /// Main client for interacting with the ArdaNova platform.
    /// Routes through the Next.js SDK API layer (/api/sdk/*) which handles
    /// session authorization. Does NOT hit the .NET backend directly.
    /// </summary>
    public class ArdaNovaClient
    {
        private readonly string _baseUrl;
        private string _sessionToken;

        /// <summary>
        /// Content gating helpers for credential and token checks.
        /// </summary>
        public CredentialGate Gate { get; }

        /// <param name="baseUrl">
        /// The base URL of the Next.js application (e.g., https://app.ardanova.io).
        /// SDK requests are sent to /api/sdk/* routes on this host.
        /// </param>
        public ArdaNovaClient(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            Gate = new CredentialGate(this);
        }

        /// <summary>
        /// Set the session token for authenticated requests.
        /// </summary>
        public void SetSessionToken(string token)
        {
            _sessionToken = token;
        }

        /// <summary>
        /// Authenticate by exchanging an auth code for a session token.
        /// The auth code is obtained through the ArdaNova web login flow.
        /// </summary>
        public async Task<UserProfile> AuthenticateAsync(string authCode)
        {
            var result = await PostAsync<AuthSessionResponse>("/api/sdk/auth/session", new AuthSessionRequest { authCode = authCode });
            SetSessionToken(result.sessionToken);
            return result.profile;
        }

        /// <summary>
        /// Get the current authenticated user's profile.
        /// </summary>
        public async Task<UserProfile> GetProfileAsync()
        {
            return await GetAsync<UserProfile>("/api/sdk/me");
        }

        /// <summary>
        /// Get all membership credentials for the current user.
        /// </summary>
        public async Task<List<MembershipCredential>> GetCredentialsAsync()
        {
            return await GetAsync<List<MembershipCredential>>("/api/sdk/me/credentials");
        }

        /// <summary>
        /// Check if the current user holds a credential matching the given criteria.
        /// </summary>
        public async Task<CredentialCheckResult> CheckCredentialAsync(
            string projectId = null,
            string guildId = null,
            string minTier = null)
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(projectId)) queryParams.Add($"projectId={projectId}");
            if (!string.IsNullOrEmpty(guildId)) queryParams.Add($"guildId={guildId}");
            if (!string.IsNullOrEmpty(minTier)) queryParams.Add($"minTier={minTier}");

            var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            return await GetAsync<CredentialCheckResult>($"/api/sdk/me/credentials/check{query}");
        }

        /// <summary>
        /// Get all token balances for the current user.
        /// </summary>
        public async Task<List<TokenBalance>> GetTokenBalancesAsync()
        {
            return await GetAsync<List<TokenBalance>>("/api/sdk/me/token-balances");
        }

        /// <summary>
        /// Get token balance for a specific project.
        /// </summary>
        public async Task<TokenBalance> GetTokenBalanceAsync(string projectId)
        {
            return await GetAsync<TokenBalance>($"/api/sdk/me/token-balances/{projectId}");
        }

        /// <summary>
        /// Report an in-game action to earn equity/XP.
        /// The platform determines the reward based on task configuration.
        /// </summary>
        public async Task<ActionResult> ReportActionAsync(string actionType, string taskId, string metadata = null)
        {
            return await PostAsync<ActionResult>("/api/sdk/actions", new GameActionRequest
            {
                actionType = actionType,
                taskId = taskId,
                metadata = metadata
            });
        }

        internal async Task<T> GetAsync<T>(string path)
        {
            var url = _baseUrl + path;
            using var request = UnityWebRequest.Get(url);

            if (!string.IsNullOrEmpty(_sessionToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {_sessionToken}");
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

        internal async Task<T> PostAsync<T>(string path, object body)
        {
            var url = _baseUrl + path;
            var json = JsonUtility.ToJson(body);
            var bodyBytes = Encoding.UTF8.GetBytes(json);

            using var request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();

            if (!string.IsNullOrEmpty(_sessionToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {_sessionToken}");
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

    [Serializable]
    internal class AuthSessionRequest
    {
        public string authCode;
    }

    [Serializable]
    internal class AuthSessionResponse
    {
        public string sessionToken;
        public UserProfile profile;
    }

    [Serializable]
    internal class GameActionRequest
    {
        public string actionType;
        public string taskId;
        public string metadata;
    }
}
