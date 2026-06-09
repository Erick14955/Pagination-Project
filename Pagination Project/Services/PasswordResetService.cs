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
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='UTF-8'>
                </head>
                <body style='margin:0;padding:0;background:#f4f7fb;font-family:Arial,Helvetica,sans-serif;color:#1f2937;'>
                    <table width='100%' cellpadding='0' cellspacing='0' style='background:#f4f7fb;padding:32px 12px;'>
                        <tr>
                            <td align='center'>
                                <table width='100%' cellpadding='0' cellspacing='0' style='max-width:620px;background:#ffffff;border-radius:18px;overflow:hidden;border:1px solid #e5e7eb;box-shadow:0 12px 30px rgba(15,23,42,0.08);'>
                    
                                    <tr>
                                        <td style='background:linear-gradient(135deg,#071526,#00796b);padding:28px 32px;color:#ffffff;'>
                                            <div style='font-size:14px;font-weight:700;letter-spacing:0.08em;text-transform:uppercase;opacity:0.9;'>
                                                Evaluations
                                            </div>
                                            <h1 style='margin:10px 0 0;font-size:26px;line-height:1.25;font-weight:800;'>
                                                Password Reset Request
                                            </h1>
                                        </td>
                                    </tr>

                                    <tr>
                                        <td style='padding:32px;'>
                                            <p style='margin:0 0 16px;font-size:16px;line-height:1.6;'>
                                                Hello <strong>{usuario.Name}</strong>,
                                            </p>

                                            <p style='margin:0 0 18px;font-size:15px;line-height:1.7;color:#374151;'>
                                                We received a request to reset the password for your Evaluations account.
                                                To continue, please click the button below.
                                            </p>

                                            <table cellpadding='0' cellspacing='0' style='margin:28px 0;'>
                                                <tr>
                                                    <td>
                                                        <a href='{link}'
                                                           style='display:inline-block;background:#00796b;color:#ffffff;padding:14px 22px;border-radius:12px;text-decoration:none;font-size:15px;font-weight:800;'>
                                                            Reset Password
                                                        </a>
                                                    </td>
                                                </tr>
                                            </table>

                                            <p style='margin:0 0 14px;font-size:14px;line-height:1.7;color:#4b5563;'>
                                                This link will expire in <strong>30 minutes</strong> for your security.
                                            </p>

                                            <p style='margin:0 0 18px;font-size:14px;line-height:1.7;color:#4b5563;'>
                                                If you did not request this password reset, you can safely ignore this email.
                                                Your current password will remain unchanged.
                                            </p>

                                            <div style='margin-top:24px;padding:16px;border-radius:12px;background:#f9fafb;border:1px solid #e5e7eb;'>
                                                <p style='margin:0 0 8px;font-size:13px;color:#6b7280;'>
                                                    If the button does not work, copy and paste this link into your browser:
                                                </p>
                                                <p style='margin:0;font-size:12px;line-height:1.6;color:#00796b;word-break:break-all;'>
                                                    {link}
                                                </p>
                                            </div>
                                        </td>
                                    </tr>

                                    <tr>
                                        <td style='padding:20px 32px;background:#f9fafb;border-top:1px solid #e5e7eb;text-align:center;'>
                                            <p style='margin:0;font-size:12px;color:#6b7280;line-height:1.5;'>
                                                This is an automated message from the Evaluations system. Please do not reply to this email.
                                            </p>
                                        </td>
                                    </tr>

                                </table>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>";

            try
            {
                await _emailService.EnviarCorreoAsync(
                    correo,
                    "Reset your password for evaluation system",
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