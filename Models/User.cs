namespace SailingResultsPortal.Models
{
    public class User
    {
        public string Username { get; set; }
        public string PasswordHash { get; set; } // TODO: Implement secure password hashing
        public string Role { get; set; } // Sudo, Organiser, Official, User
    }
}
