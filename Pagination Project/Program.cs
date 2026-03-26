using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Pagination_Project.Components;
using Pagination_Project.Data;
using Pagination_Project.Services;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// ================================
// Razor Components
// ================================
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient();

// ================================
// EF Core + PostgreSQL
// ================================
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

// ================================
// Servicios de aplicación
// ================================
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

builder.Services.AddControllers();

// ================================
// Auth con cookies
// ================================
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

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// ================================
// Pipeline
// ================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

// ================================
// Endpoints de login/logout
// ================================
app.MapPost("/account/login", async (
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
        return Results.Redirect("/login?error=1");

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
        new(ClaimTypes.Name, usuario.Name ?? string.Empty),
        new("Username", usuario.Username ?? string.Empty),
        new(ClaimTypes.Email, usuario.email ?? string.Empty),
        new("LvlId", usuario.lvl_Id.ToString())
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

app.MapPost("/account/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

// ================================
// Controllers API
// ================================
app.MapControllers();

// ================================
// Blazor
// ================================
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();