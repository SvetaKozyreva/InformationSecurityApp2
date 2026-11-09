
namespace InformationSecurityApp.Models;

public class User
{
    public string Login { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = "User";

    public DateTime PasswordValidUntil { get; set; }

    // Права доступу до каталогів A, B, C, D та E
    public Dictionary<string, string> Permissions { get; set; } = new();
}
