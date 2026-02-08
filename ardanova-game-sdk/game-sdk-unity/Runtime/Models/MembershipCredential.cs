using System;

namespace ArdaNova
{
    [Serializable]
    public class MembershipCredential
    {
        public string id;
        public string projectId;
        public string guildId;
        public string userId;
        public string assetId;
        public string status;
        public bool isTransferable;
        public string tier;
        public string grantedVia;
        public string mintTxHash;
        public string createdAt;
    }
}
