using System;

namespace ArdaNova
{
    [Serializable]
    public class CredentialCheckResult
    {
        public bool hasCredential;
        public string tier;
        public string status;
        public bool meetsMinTier;
    }
}
