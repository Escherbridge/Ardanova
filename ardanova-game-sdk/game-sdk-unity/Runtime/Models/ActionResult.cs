using System;

namespace ArdaNova
{
    [Serializable]
    public class ActionResult
    {
        public bool awarded;
        public int tokensEarned;
        public int newBalance;
        public string message;
    }
}
