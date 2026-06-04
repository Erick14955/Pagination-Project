using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Pagination_Project.Services;

namespace Pagination_Project.Components.Account
{
    public static class LoginEndpoints
    {
        public static IEndpointRouteBuilder MapLoginEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPost("/account/login", async (
                HttpContext httpContext,
                IAuthService authService) =>
            {
                var form = await httpContext.Request.ReadFormAsync();

                var username = form["Username"].ToString();
                var password = form["Password"].ToString();
                var returnUrl = form["ReturnUrl"].ToString();

                if (string.IsNullOrWhiteSpace(returnUrl))
                    returnUrl = "/dashboard";

                var resultado = await authService.ValidarLoginDetalladoAsync(username, password);

                if (!resultado.Exitoso || resultado.Usuario is null)
                {
                    var error = resultado.Estado switch
                    {
                        LoginEstado.UsuarioNoExiste => "usuario",
                        LoginEstado.ContrasenaIncorrecta => "password",
                        LoginEstado.UsuarioInactivo => "inactivo",
                        _ => "general"
                    };

                    return Results.Redirect($"/login?error={error}");
                }

                var usuario = resultado.Usuario;

                var requiereCambioPassword = usuario.RequirePasswordChange;

                var themePreference = string.IsNullOrWhiteSpace(usuario.ThemePreference)
                    ? "light"
                    : usuario.ThemePreference;

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                    new(ClaimTypes.Name, usuario.Name ?? string.Empty),
                    new("Username", usuario.Username ?? string.Empty),
                    new(ClaimTypes.Email, usuario.email ?? string.Empty),
                    new("LvlId", usuario.lvl_Id.ToString()),
                    new("RequirePasswordChange", requiereCambioPassword.ToString()),
                    new("ThemePreference", themePreference)
                };

                if (usuario.Permisos is not null)
                {
                    claims.Add(new("CreateUser", usuario.Permisos.CreateUser.ToString()));
                    claims.Add(new("EditUser", usuario.Permisos.EditUser.ToString()));
                    claims.Add(new("DeleteUser", usuario.Permisos.DeleteUser.ToString()));

                    claims.Add(new("CreateBook", usuario.Permisos.CreateBook.ToString()));
                    claims.Add(new("EditBook", usuario.Permisos.EditBook.ToString()));
                    claims.Add(new("DeleteBook", usuario.Permisos.DeleteBook.ToString()));
                    claims.Add(new("BooksView", usuario.Permisos.BooksView.ToString()));

                    claims.Add(new("CreateAssignations", usuario.Permisos.CreateAssignations.ToString()));
                    claims.Add(new("AsignBook", usuario.Permisos.AsignBook.ToString()));
                    claims.Add(new("ViewAssignations", usuario.Permisos.ViewAssignations.ToString()));

                    claims.Add(new("QualifyBook", usuario.Permisos.QualifyBook.ToString()));

                    claims.Add(new("CreateEmployees", usuario.Permisos.CreateEmployees.ToString()));
                    claims.Add(new("EditEmployees", usuario.Permisos.EditEmployees.ToString()));
                    claims.Add(new("DeleteEmployees", usuario.Permisos.DeleteEmployees.ToString()));
                    claims.Add(new("ViewEmployees", usuario.Permisos.ViewEmployees.ToString()));

                    claims.Add(new("LateWork", usuario.Permisos.LateWork.ToString()));
                    claims.Add(new("CreateLateWork", usuario.Permisos.CreateLateWork.ToString()));
                    claims.Add(new("EditLateWork", usuario.Permisos.EditLateWork.ToString()));
                    claims.Add(new("DeleteLateWork", usuario.Permisos.DeleteLateWork.ToString()));
                    claims.Add(new("CompleteLateWork", usuario.Permisos.CompleteLateWork.ToString()));

                    claims.Add(new("EditPermissionLevels", usuario.Permisos.EditPermissionLevels.ToString()));
                }

                var identity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                var principal = new ClaimsPrincipal(identity);

                await httpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        AllowRefresh = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                    });

                if (requiereCambioPassword)
                {
                    return Results.Redirect("/cambiar-password");
                }

                return Results.Redirect(returnUrl);
            });

            endpoints.MapPost("/account/change-password", async (
                HttpContext httpContext,
                IUsuarioService usuarioService) =>
            {
                if (httpContext.User?.Identity?.IsAuthenticated != true)
                {
                    return Results.Redirect("/login?error=general");
                }

                var userIdClaim = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!Guid.TryParse(userIdClaim, out var usuarioId))
                {
                    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return Results.Redirect("/login?error=general");
                }

                var form = await httpContext.Request.ReadFormAsync();

                var nuevaPassword = form["NewPassword"].ToString();
                var confirmarPassword = form["ConfirmPassword"].ToString();

                if (string.IsNullOrWhiteSpace(nuevaPassword) || nuevaPassword.Length < 6)
                {
                    return Results.Redirect("/cambiar-password?error=short");
                }

                if (nuevaPassword != confirmarPassword)
                {
                    return Results.Redirect("/cambiar-password?error=mismatch");
                }

                var actualizado = await usuarioService.CambiarPasswordAsync(usuarioId, nuevaPassword);

                if (!actualizado)
                {
                    return Results.Redirect("/cambiar-password?error=notfound");
                }

                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                return Results.Redirect("/login?changed=1");
            }).RequireAuthorization();

            endpoints.MapPost("/account/logout", async (HttpContext httpContext) =>
            {
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect("/login");
            });

            return endpoints;
        }
    }
}