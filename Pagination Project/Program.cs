using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Pagination_Project.Components;
using Pagination_Project.Data;
using Pagination_Project.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient();

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("SupabaseConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);

            npgsqlOptions.CommandTimeout(60);
        }));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IEmpleadoService, EmpleadoService>();

builder.Services.AddControllers();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/account/logout";
        options.AccessDeniedPath = "/login";
        options.Cookie.Name = "Pagination_Project.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower() ?? string.Empty;

    var estaAutenticado = context.User?.Identity?.IsAuthenticated == true;

    var requiereCambioPassword =
        string.Equals(
            context.User?.FindFirst("RequirePasswordChange")?.Value,
            "True",
            StringComparison.OrdinalIgnoreCase);

    var esRutaPermitida =
        path.StartsWith("/cambiar-password") ||
        path.StartsWith("/account/change-password") ||
        path.StartsWith("/account/logout") ||
        path.StartsWith("/account/login") ||
        path.StartsWith("/login") ||
        path.StartsWith("/_framework") ||
        path.StartsWith("/_blazor") ||
        path.StartsWith("/_content") ||
        path.StartsWith("/css") ||
        path.StartsWith("/js") ||
        path.StartsWith("/style") ||
        path.StartsWith("/lib") ||
        path.StartsWith("/images") ||
        path.StartsWith("/favicon");

    if (estaAutenticado && requiereCambioPassword && !esRutaPermitida)
    {
        context.Response.Redirect("/cambiar-password");
        return;
    }

    await next();
});

app.UseAuthorization();

app.MapPost("/account/login", async (
    HttpContext httpContext,
    IAuthService authService) =>
{
    var form = await httpContext.Request.ReadFormAsync();

    var username = form["Username"].ToString();
    var password = form["Password"].ToString();
    var returnUrl = form["ReturnUrl"].ToString();

    if (string.IsNullOrWhiteSpace(returnUrl))
    {
        returnUrl = "/dashboard";
    }

    if (!returnUrl.StartsWith("/") || returnUrl.StartsWith("//"))
    {
        returnUrl = "/dashboard";
    }

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

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
        new(ClaimTypes.Name, usuario.Name ?? string.Empty),
        new("Username", usuario.Username ?? string.Empty),
        new(ClaimTypes.Email, usuario.email ?? string.Empty),
        new("LvlId", usuario.lvl_Id.ToString()),
        new("RequirePasswordChange", requiereCambioPassword.ToString())
    };

    if (usuario.EmployeeId.HasValue)
    {
        claims.Add(new("EmployeeId", usuario.EmployeeId.Value.ToString()));
    }

    if (usuario.Empleado is not null)
    {
        claims.Add(new("EmployeeCode", usuario.Empleado.IdEmpleado.ToString()));
        claims.Add(new("EmployeeName", usuario.Empleado.Nombre ?? string.Empty));
        claims.Add(new("EmployeeEmail", usuario.Empleado.Email ?? string.Empty));
    }

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
})
.DisableAntiforgery();

app.MapPost("/account/change-password", async (
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
})
.RequireAuthorization()
.DisableAntiforgery();

app.MapPost("/account/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
})
.DisableAntiforgery();

app.MapControllers();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();