namespace SailingResultsPortal.Models
{
    public class User
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // TODO: Implement secure password hashing
        public string Role { get; set; } = string.Empty; // Roles: Sudo, Organiser, Official, User
        public DateTime CreatedAt { get; set; }
    }
}
