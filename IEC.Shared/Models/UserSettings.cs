using System.Collections.Generic;

namespace IEC.Shared.Models
{
    public class UserSettings
    {
        public List<UserAccount> Users { get; set; } = new List<UserAccount>();

        // Optional: default role assigned to newly created users
        public UserRole DefaultRole { get; set; } = UserRole.Operator;
    }
}