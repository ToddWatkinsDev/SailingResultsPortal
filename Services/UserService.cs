using SailingResultsPortal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SailingResultsPortal.Services
{
    public static class UserService
    {
        private const string UsersFile = "users.json";

        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public static async Task<List<User>> GetAllUsersAsync()
        {
            return await Helpers.FileStorageHelper.LoadAsync<User>(UsersFile);
        }

        public static async Task<User?> GetUserByUsernameAsync(string username)
        {
            var users = await GetAllUsersAsync();
            return users.FirstOrDefault(u => u.Username == username);
        }

        public static async Task<bool> RegisterUserAsync(User user)
        {
            var users = await GetAllUsersAsync();
            if (users.Any(u => u.Username == user.Username || u.Email == user.Email))
                return false;

            user.Id = Guid.NewGuid().ToString();
            user.PasswordHash = HashPassword(user.PasswordHash); // PasswordHash is actually the plain password here
            user.CreatedAt = DateTime.UtcNow;
            users.Add(user);
            await Helpers.FileStorageHelper.SaveAsync(UsersFile, users);
            return true;
        }

        public static async Task<bool> ValidateUserAsync(string username, string password)
        {
            var user = await GetUserByUsernameAsync(username);
            if (user == null) return false;
            var hash = HashPassword(password);
            return user.PasswordHash == hash;
        }
    }
}