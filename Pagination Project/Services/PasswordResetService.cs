using System.Security.Cryptography;
using System.Text;
using Isopoh.Cryptography.Argon2;
using Microsoft.EntityFrameworkCore;
using Pagination_Project.Data;
using Pagination_Project.Models;

namespace Pagination_Project.Services
{
    public class PasswordResetService : IPasswordResetService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly IEmailService _emailService;

        public PasswordResetService(
            IDbContextFactory<AppDbContext> dbFactory,
            IEmailService emailService)
        {
            _dbFactory = dbFactory;
            _emailService = emailService;
        }

        public async Task SolicitarRecuperacionAsync(string username, string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(username))
                return;

            await using var db = await _dbFactory.CreateDbContextAsync();

            var usernameLimpio = username.Trim();

            var usuario = await db.Users
                .Include(u => u.Empleado)
                .FirstOrDefaultAsync(u => u.Username == usernameLimpio);

            if (usuario is null)
                return;

            if (usuario.Activo == false)
                return;

            var correo = !string.IsNullOrWhiteSpace(usuario.email)
                ? usuario.email
                : usuario.Empleado?.Email;

            if (string.IsNullOrWhiteSpace(correo))
                return;

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var tokenHash = GenerarHashToken(token);

            var reset = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuario.Id,
                TokenHash = tokenHash,
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddMinutes(30),
                Used = false
            };

            db.PasswordResetTokens.Add(reset);
            await db.SaveChangesAsync();

            var link = $"{baseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}";

            var cuerpo = $@"
                <div style='font-family:Arial,sans-serif;max-width:600px;margin:auto;padding:24px;border:1px solid #e5e7eb;border-radius:14px;'>
                    <h2 style='color:#00796b;'>Password Recovery</h2>
                    <p>Hello {usuario.Name},</p>
                    <p>We received a request to reset your password for the Evaluations system.</p>
                    <p>
                        <a href='{link}'
                           style='display:inline-block;background:#00796b;color:white;padding:12px 18px;border-radius:10px;text-decoration:none;font-weight:bold;'>
                           Reset Password
                        </a>
                    </p>
                    <p>This link will expire in 30 minutes.</p>
                    <p>If you did not request this, you can ignore this email.</p>
                </div>";

            try
            {
                await _emailService.EnviarCorreoAsync(
                    correo,
                    "Evaluations - Password Recovery",
                    cuerpo);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public async Task<bool> RestablecerPasswordAsync(string token, string nuevaPassword)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            if (string.IsNullOrWhiteSpace(nuevaPassword) || nuevaPassword.Length < 6)
                return false;

            var tokenHash = GenerarHashToken(token);

            await using var db = await _dbFactory.CreateDbContextAsync();

            var reset = await db.PasswordResetTokens
                .Include(x => x.Usuario)
                .FirstOrDefaultAsync(x =>
                    x.TokenHash == tokenHash &&
                    x.Used == false &&
                    x.ExpiresAt >= DateTime.Now);

            if (reset is null || reset.Usuario is null)
                return false;

            reset.Usuario.password = Argon2.Hash(nuevaPassword);
            reset.Usuario.RequirePasswordChange = false;
            reset.Used = true;

            await db.SaveChangesAsync();

            return true;
        }

        private static string GenerarHashToken(string token)
        {
            var bytes = Encoding.UTF8.GetBytes(token);
            var hash = SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}