using Pagination_Project.Models;

public class PasswordResetToken
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    public bool Used { get; set; }
}