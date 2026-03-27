using System;

namespace ArdaNova
{
    [Serializable]
    public class TokenBalance
    {
        public string id;
        public string userId;
        public string projectTokenConfigId;
        public bool isPlatformToken;
        public int balance;
        public int lockedBalance;
    }
}
