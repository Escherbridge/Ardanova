using System.Threading.Tasks;

namespace ArdaNova
{
    /// <summary>
    /// High-level content gating helpers. Use these to check whether the
    /// current player meets credential or token requirements before
    /// unlocking game content.
    /// </summary>
    public class CredentialGate
    {
        private readonly ArdaNovaClient _client;

        internal CredentialGate(ArdaNovaClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Check if the current user holds any active credential for the given project.
        /// </summary>
        public async Task<bool> HasCredential(string projectId = null, string guildId = null)
        {
            var result = await _client.CheckCredentialAsync(projectId: projectId, guildId: guildId);
            return result.hasCredential;
        }

        /// <summary>
        /// Check if the current user holds a credential at or above the given tier.
        /// Tier order: BRONZE < SILVER < GOLD < PLATINUM < DIAMOND
        /// </summary>
        public async Task<bool> HasMinTier(string minTier, string projectId = null, string guildId = null)
        {
            var result = await _client.CheckCredentialAsync(
                projectId: projectId,
                guildId: guildId,
                minTier: minTier);
            return result.hasCredential && result.meetsMinTier;
        }

        /// <summary>
        /// Check if the current user has at least the given token balance for a project.
        /// </summary>
        public async Task<bool> HasTokenBalance(string projectId, int minBalance)
        {
            try
            {
                var balance = await _client.GetTokenBalanceAsync(projectId);
                return balance.balance >= minBalance;
            }
            catch (ArdaNovaException)
            {
                return false;
            }
        }
    }
}
