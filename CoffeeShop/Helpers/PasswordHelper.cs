using System.Security.Cryptography;
using System.Text;

namespace CoffeeShop.Helpers
{
    public static class PasswordHelper
    {
        public static string HashSha512(string password)
        {
            var bytes = SHA512.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        public static bool Verify(string password, string storedHash)
        {
            var hash = HashSha512(password);
            return string.Equals(hash, storedHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}