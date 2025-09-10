namespace SailingResultsPortal.Models
{
    public class User
    {
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // TODO: Implement secure password hashing
        public string Role { get; set; } = string.Empty; // Roles like Sudo, Organiser, Official, User
    }
}
