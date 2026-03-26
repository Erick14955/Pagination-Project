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

                var usuario = await authService.ValidarLoginAsync(username, password);

                if (usuario is null)
                {
                    return Results.Redirect($"/login?error=1");
                }

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                    new(ClaimTypes.Name, usuario.Name ?? string.Empty),
                    new("Username", usuario.Username ?? string.Empty),
                    new(ClaimTypes.Email, usuario.email ?? string.Empty),
                    new("Lvl_Id", usuario.lvl_Id.ToString())
                };

                if (usuario.Permisos is not null)
                {
                    claims.Add(new("CreateUser", usuario.Permisos.CreateUser.ToString()));
                    claims.Add(new("EditUser", usuario.Permisos.EditUser.ToString()));
                    claims.Add(new("DeleteUser", usuario.Permisos.DeleteUser.ToString()));
                    claims.Add(new("CreateBook", usuario.Permisos.CreateBook.ToString()));
                    claims.Add(new("EditBook", usuario.Permisos.EditBook.ToString()));
                    claims.Add(new("DeleteBook", usuario.Permisos.DeleteBook.ToString()));
                    claims.Add(new("AsignBook", usuario.Permisos.AsignBook.ToString()));
                    claims.Add(new("BooksView", usuario.Permisos.BooksView.ToString()));
                    claims.Add(new("QualifyBook", usuario.Permisos.QualifyBook.ToString()));
                    claims.Add(new("CreateEmployees", usuario.Permisos.CreateEmployees.ToString()));
                    claims.Add(new("EditEmployees", usuario.Permisos.EditEmployees.ToString()));
                    claims.Add(new("DeleteEmployees", usuario.Permisos.DeleteEmployees.ToString()));
                    claims.Add(new("EditPermissionLevels", usuario.Permisos.EditPermissionLevels.ToString()));
                    claims.Add(new("ViewAssignations", usuario.Permisos.ViewAssignations.ToString()));
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

                return Results.Redirect(returnUrl);
            });

            endpoints.MapPost("/account/logout", async (HttpContext httpContext) =>
            {
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect("/login");
            });

            return endpoints;
        }
    }
}