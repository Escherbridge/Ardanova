using System;

namespace ArdaNova
{
    [Serializable]
    public class UserProfile
    {
        public string id;
        public string name;
        public string email;
        public string image;
        public string role;
        public string userType;
        public string verificationLevel;
    }
}
