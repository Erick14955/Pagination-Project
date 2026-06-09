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
                <body style='margin:0;padding:0;background-color:#f4f7fb;font-family:Arial,Helvetica,sans-serif;color:#1f2937;'>

                    <table width='100%' cellpadding='0' cellspacing='0' border='0' style='background-color:#f4f7fb;padding:32px 12px;'>
                        <tr>
                            <td align='center'>

                                <table width='620' cellpadding='0' cellspacing='0' border='0' style='width:100%;max-width:620px;background-color:#ffffff;border:1px solid #e5e7eb;'>

                                    <tr>
                                        <td bgcolor='#071526' style='background-color:#071526;padding:28px 32px;color:#ffffff;'>
                                            <div style='font-size:14px;font-weight:bold;letter-spacing:1px;text-transform:uppercase;color:#ffffff;'>
                                                Evaluations
                                            </div>

                                            <h1 style='margin:10px 0 0;font-size:26px;line-height:32px;font-weight:bold;color:#ffffff;'>
                                                Password Reset Request
                                            </h1>
                                        </td>
                                    </tr>

                                    <tr>
                                        <td style='padding:32px;background-color:#ffffff;'>

                                            <p style='margin:0 0 16px;font-size:16px;line-height:26px;color:#1f2937;'>
                                                Hello <strong>{usuario.Name}</strong>,
                                            </p>

                                            <p style='margin:0 0 18px;font-size:15px;line-height:26px;color:#374151;'>
                                                We received a request to reset the password for your Evaluations account.
                                                To continue, please click the button below.
                                            </p>

                                            <table cellpadding='0' cellspacing='0' border='0' style='margin:28px 0;'>
                                                <tr>
                                                    <td bgcolor='#00796b' style='background-color:#00796b;'>
                                                        <a href='{link}'
                                                           style='display:inline-block;background-color:#00796b;color:#ffffff;padding:14px 24px;text-decoration:none;font-size:15px;font-weight:bold;font-family:Arial,Helvetica,sans-serif;'>
                                                            Reset Password
                                                        </a>
                                                    </td>
                                                </tr>
                                            </table>

                                            <p style='margin:0 0 14px;font-size:14px;line-height:24px;color:#4b5563;'>
                                                This link will expire in <strong>30 minutes</strong> for your security.
                                            </p>

                                            <p style='margin:0 0 18px;font-size:14px;line-height:24px;color:#4b5563;'>
                                                If you did not request this password reset, you can safely ignore this email.
                                                Your current password will remain unchanged.
                                            </p>

                                            <table width='100%' cellpadding='0' cellspacing='0' border='0' style='margin-top:24px;background-color:#f9fafb;border:1px solid #e5e7eb;'>
                                                <tr>
                                                    <td style='padding:16px;'>
                                                        <p style='margin:0 0 10px;font-size:13px;line-height:22px;color:#6b7280;'>
                                                            If the reset button does not work, please contact your system administrator.
                                                        </p>

                                                        <p style='margin:0;font-size:13px;line-height:22px;color:#6b7280;'>
                                                            If your browser displays a security warning when opening the link,
                                                            please contact your system administrator before proceeding.
                                                        </p>
                                                    </td>
                                                </tr>
                                            </table>

                                        </td>
                                    </tr>

                                    <tr>
                                        <td style='padding:20px 32px;background-color:#f9fafb;border-top:1px solid #e5e7eb;text-align:center;'>
                                            <p style='margin:0;font-size:12px;line-height:20px;color:#6b7280;'>
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
                Console.WriteLine("ERROR ENVIANDO CORREO:");
                Console.WriteLine(ex.ToString());
                throw;
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