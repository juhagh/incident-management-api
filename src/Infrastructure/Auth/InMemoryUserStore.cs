using Domain.Entities;

namespace Infrastructure.Auth;

public class InMemoryUserStore
{
    private static readonly List<User> _users = new()
    {
        // User plain text pwd just initially, proper implementation with Argon2id/bcrypt 
        new User(username: "User1", passwordHash: "VerySecretPassword1!", role: "Admin"),
        new User(username: "User2", passwordHash: "blahblahblah", role: "Operator")
    };

    public User? FindByUsername(string username)
    {
        return _users.FirstOrDefault(s => string.Equals(s.Username, username, StringComparison.OrdinalIgnoreCase));
    }
}