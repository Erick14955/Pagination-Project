using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Pagination_Project.Components;
using Pagination_Project.Data;
using Pagination_Project.Models;
using Pagination_Project.Services;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString(
            "SupabaseConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);

            npgsqlOptions.CommandTimeout(60);
        }));

builder.Services
    .AddDataProtection()
    .SetApplicationName("Pagination_Project")
    .PersistKeysToDbContext<AppDbContext>();

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<
    IDashboardService,
    DashboardService>();

builder.Services.AddScoped<
    IUsuarioService,
    UsuarioService>();

builder.Services.AddScoped<
    IEmpleadoService,
    EmpleadoService>();

builder.Services.AddHttpClient<
    IEmailService,
    EmailService>();

builder.Services.AddScoped<
    IPasswordResetService,
    PasswordResetService>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<
    IPaginationChecklistService,
    PaginationChecklistService>();

builder.Services.AddScoped<
    IUserDataScopeService,
    UserDataScopeService>();

builder.Services.AddControllers();

var isDevelopment =
    builder.Environment.IsDevelopment();

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = isDevelopment
        ? "Pagination_Project.Antiforgery"
        : "__Host-Pagination_Project.Antiforgery";

    options.Cookie.HttpOnly = true;

    options.Cookie.SecurePolicy = isDevelopment
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;

    options.Cookie.SameSite =
        SameSiteMode.Strict;

    options.Cookie.Path = "/";
});

builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";

        options.LogoutPath =
            "/account/logout";

        options.AccessDeniedPath =
            "/login";

        options.Cookie.Name = isDevelopment
            ? "Pagination_Project.Auth"
            : "__Host-Pagination_Project.Auth";

        options.Cookie.HttpOnly = true;

        options.Cookie.SameSite =
            SameSiteMode.Lax;

        options.Cookie.SecurePolicy = isDevelopment
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        options.Cookie.Path = "/";

        options.Cookie.IsEssential = true;

        options.SlidingExpiration = true;

        options.ExpireTimeSpan =
            TimeSpan.FromHours(8);

        options.Events =
            new CookieAuthenticationEvents
            {
                OnValidatePrincipal = async context =>
                {
                    if (EsRutaTecnicaAutenticacion(
                            context.Request.Path))
                    {
                        return;
                    }

                    var userIdClaim =
                        context.Principal?
                            .FindFirstValue(
                                ClaimTypes.NameIdentifier);

                    if (!Guid.TryParse(
                            userIdClaim,
                            out var usuarioId))
                    {
                        context.RejectPrincipal();

                        await context.HttpContext
                            .SignOutAsync(
                                CookieAuthenticationDefaults
                                    .AuthenticationScheme);

                        return;
                    }

                    var cache =
                        context.HttpContext
                            .RequestServices
                            .GetRequiredService<IMemoryCache>();

                    var cacheKey =
                        ObtenerClaveCacheUsuario(
                            usuarioId);

                    if (!cache.TryGetValue<bool>(
                            cacheKey,
                            out var usuarioValido))
                    {
                        var authService =
                            context.HttpContext
                                .RequestServices
                                .GetRequiredService<IAuthService>();

                        try
                        {
                            usuarioValido =
                                await authService
                                    .UsuarioSigueActivoAsync(
                                        usuarioId);
                        }
                        catch
                        {
                            context.RejectPrincipal();

                            await context.HttpContext
                                .SignOutAsync(
                                    CookieAuthenticationDefaults
                                        .AuthenticationScheme);

                            return;
                        }

                        cache.Set(
                            cacheKey,
                            usuarioValido,
                            new MemoryCacheEntryOptions
                            {
                                AbsoluteExpirationRelativeToNow =
                                    TimeSpan.FromMinutes(1)
                            });
                    }

                    if (!usuarioValido)
                    {
                        cache.Remove(cacheKey);

                        context.RejectPrincipal();

                        await context.HttpContext
                            .SignOutAsync(
                                CookieAuthenticationDefaults
                                    .AuthenticationScheme);
                    }
                },

                OnRedirectToLogin = context =>
                {
                    if (EsRutaTecnicaAutenticacion(
                            context.Request.Path))
                    {
                        context.Response.StatusCode =
                            StatusCodes
                                .Status401Unauthorized;

                        return Task.CompletedTask;
                    }

                    context.Response.Redirect(
                        context.RedirectUri);

                    return Task.CompletedTask;
                },

                OnRedirectToAccessDenied = context =>
                {
                    if (EsRutaTecnicaAutenticacion(
                            context.Request.Path))
                    {
                        context.Response.StatusCode =
                            StatusCodes
                                .Status403Forbidden;

                        return Task.CompletedTask;
                    }

                    context.Response.Redirect(
                        context.RedirectUri);

                    return Task.CompletedTask;
                }
            };
    });

builder.Services.Configure<ForwardedHeadersOptions>(
    options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor |
            ForwardedHeaders.XForwardedProto;

        options.ForwardLimit = 1;

        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy =
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
});

builder.Services
    .AddCascadingAuthenticationState();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.AddPolicy(
        "login-policy",
        httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey:
                    httpContext
                        .Connection
                        .RemoteIpAddress?
                        .ToString()
                    ?? "unknown",

                factory: _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,

                        Window =
                            TimeSpan.FromMinutes(1),

                        QueueLimit = 0,

                        AutoReplenishment = true
                    }));

    options.AddPolicy(
        "password-reset-policy",
        httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey:
                    httpContext
                        .Connection
                        .RemoteIpAddress?
                        .ToString()
                    ?? "unknown",

                factory: _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,

                        Window =
                            TimeSpan.FromMinutes(10),

                        QueueLimit = 0,

                        AutoReplenishment = true
                    }));
});

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Error",
        createScopeForErrors: true);

    app.UseHsts();
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var headers =
            context.Response.Headers;

        headers["X-Content-Type-Options"] =
            "nosniff";

        headers["Referrer-Policy"] =
            "strict-origin-when-cross-origin";

        headers["X-Frame-Options"] =
            "DENY";

        headers["Permissions-Policy"] =
            "camera=(), microphone=(), geolocation=()";

        headers["Cross-Origin-Opener-Policy"] =
            "same-origin";

        headers["Cross-Origin-Resource-Policy"] =
            "same-origin";

        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "base-uri 'self'; " +
            "object-src 'none'; " +
            "frame-ancestors 'none'; " +
            "form-action 'self'; " +
            "script-src 'self' 'unsafe-inline'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: blob: https:; " +
            "font-src 'self' data:; " +
            "connect-src 'self' https: wss:; " +
            "frame-src 'none'; " +
            "upgrade-insecure-requests;";

        return Task.CompletedTask;
    });

    if (HttpMethods.IsOptions(
            context.Request.Method))
    {
        context.Response.StatusCode =
            StatusCodes.Status405MethodNotAllowed;

        return;
    }

    await next();
});

app.UseCookiePolicy();

app.UseStaticFiles();

app.UseAuthentication();

app.Use(async (context, next) =>
{
    var path =
        context.Request.Path;

    var estaAutenticado =
        context.User?
            .Identity?
            .IsAuthenticated == true;

    var esSolicitudGet =
        HttpMethods.IsGet(
            context.Request.Method);

    var esPaginaLogin =
        path == "/" ||
        path.StartsWithSegments(
            "/login");

    if (esSolicitudGet &&
        estaAutenticado &&
        esPaginaLogin)
    {
        var requiereCambioPassword =
            string.Equals(
                context.User?
                    .FindFirst(
                        "RequirePasswordChange")?
                    .Value,
                "True",
                StringComparison.OrdinalIgnoreCase);

        context.Response.Redirect(
            requiereCambioPassword
                ? "/cambiar-password"
                : "/dashboard");

        return;
    }

    await next();
});

app.Use(async (context, next) =>
{
    var path =
        context.Request.Path
            .Value?
            .ToLowerInvariant()
        ?? string.Empty;

    var estaAutenticado =
        context.User?
            .Identity?
            .IsAuthenticated == true;

    var requiereCambioPassword =
        string.Equals(
            context.User?
                .FindFirst(
                    "RequirePasswordChange")?
                .Value,
            "True",
            StringComparison.OrdinalIgnoreCase);

    var esRutaPermitida =
        path.StartsWith(
            "/cambiar-password") ||

        path.StartsWith(
            "/account/change-password") ||

        path.StartsWith(
            "/account/logout") ||

        path.StartsWith(
            "/account/login") ||

        path.StartsWith(
            "/login") ||

        path.StartsWith(
            "/_framework") ||

        path.StartsWith(
            "/_blazor") ||

        path.StartsWith(
            "/_content") ||

        path.StartsWith(
            "/css") ||

        path.StartsWith(
            "/js") ||

        path.StartsWith(
            "/style") ||

        path.StartsWith(
            "/lib") ||

        path.StartsWith(
            "/images") ||

        path.StartsWith(
            "/favicon") ||

        path.StartsWith(
            "/forgot-password") ||

        path.StartsWith(
            "/reset-password") ||

        path.StartsWith(
            "/account/forgot-password") ||

        path.StartsWith(
            "/account/reset-password") ||

        path.StartsWith(
            "/.well-known/security.txt") ||

        path.StartsWith(
            "/security.txt");

    if (estaAutenticado &&
        requiereCambioPassword &&
        !esRutaPermitida)
    {
        context.Response.Redirect(
            "/cambiar-password");

        return;
    }

    await next();
});

app.UseAuthorization();

app.UseRateLimiter();

app.UseAntiforgery();

app.MapPost(
    "/account/login",
    async (
        HttpContext httpContext,
        IAuthService authService,
        IMemoryCache cache) =>
    {
        var form =
            await httpContext.Request.ReadFormAsync(
                httpContext.RequestAborted);

        var username =
            form["Username"]
                .ToString()
                .Trim()
                .ToLowerInvariant();

        var password =
            form["Password"]
                .ToString();

        var returnUrl =
            NormalizarReturnUrl(
                form["ReturnUrl"]
                    .ToString());

        var rememberMe =
            form.TryGetValue(
                "RememberMe",
                out var rememberValue) &&
            string.Equals(
                rememberValue.ToString(),
                "true",
                StringComparison.OrdinalIgnoreCase);

        var resultado =
            await authService
                .ValidarLoginDetalladoAsync(
                    username,
                    password);

        if (!resultado.Exitoso ||
            resultado.Usuario is null)
        {
            var error =
                resultado.Estado switch
                {
                    LoginEstado.UsuarioNoExiste =>
                        "usuario",

                    LoginEstado.ContrasenaIncorrecta =>
                        "password",

                    LoginEstado.UsuarioInactivo =>
                        "inactivo",

                    LoginEstado.CuentaBloqueada =>
                        "bloqueado",

                    _ =>
                        "general"
                };

            return Results.Redirect(
                $"/login?error={error}");
        }

        var usuario =
            resultado.Usuario;

        var requiereCambioPassword =
            usuario.RequirePasswordChange;

        var claims =
            CrearClaimsUsuario(
                usuario);

        var identity =
            new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults
                    .AuthenticationScheme);

        var principal =
            new ClaimsPrincipal(
                identity);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults
                .AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = rememberMe,

                AllowRefresh = true,

                ExpiresUtc = rememberMe
                    ? DateTimeOffset.UtcNow
                        .AddHours(8)
                    : null
            });

        cache.Set(
            ObtenerClaveCacheUsuario(
                usuario.Id),
            true,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow =
                    TimeSpan.FromMinutes(1)
            });

        if (requiereCambioPassword)
        {
            return Results.Redirect(
                "/cambiar-password");
        }

        return Results.Redirect(
            returnUrl);
    })
    .AllowAnonymous()
    .RequireRateLimiting(
        "login-policy")
    .AddEndpointFilter(
        ValidarAntiforgeryEndpoint);

app.MapPost(
    "/account/change-password",
    async (
        HttpContext httpContext,
        IUsuarioService usuarioService,
        IMemoryCache cache) =>
    {
        if (httpContext.User?
                .Identity?
                .IsAuthenticated != true)
        {
            return Results.Redirect(
                "/login?error=general");
        }

        var userIdClaim =
            httpContext.User
                .FindFirstValue(
                    ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
                userIdClaim,
                out var usuarioId))
        {
            await httpContext.SignOutAsync(
                CookieAuthenticationDefaults
                    .AuthenticationScheme);

            return Results.Redirect(
                "/login?error=general");
        }

        var form =
            await httpContext.Request
                .ReadFormAsync(
                    httpContext.RequestAborted);

        var nuevaPassword =
            form["NewPassword"]
                .ToString();

        var confirmarPassword =
            form["ConfirmPassword"]
                .ToString();

        if (string.IsNullOrWhiteSpace(
                nuevaPassword) ||
            nuevaPassword.Length < 6)
        {
            return Results.Redirect(
                "/cambiar-password?error=short");
        }

        if (!string.Equals(
                nuevaPassword,
                confirmarPassword,
                StringComparison.Ordinal))
        {
            return Results.Redirect(
                "/cambiar-password?error=mismatch");
        }

        var actualizado =
            await usuarioService
                .CambiarPasswordAsync(
                    usuarioId,
                    nuevaPassword);

        if (!actualizado)
        {
            return Results.Redirect(
                "/cambiar-password?error=notfound");
        }

        cache.Remove(
            ObtenerClaveCacheUsuario(
                usuarioId));

        await httpContext.SignOutAsync(
            CookieAuthenticationDefaults
                .AuthenticationScheme);

        return Results.Redirect(
            "/login?changed=1");
    })
    .RequireAuthorization()
    .AddEndpointFilter(
        ValidarAntiforgeryEndpoint);

app.MapPost(
    "/account/logout",
    async (
        HttpContext httpContext,
        IMemoryCache cache) =>
    {
        var userIdClaim =
            httpContext.User
                .FindFirstValue(
                    ClaimTypes.NameIdentifier);

        if (Guid.TryParse(
                userIdClaim,
                out var usuarioId))
        {
            cache.Remove(
                ObtenerClaveCacheUsuario(
                    usuarioId));
        }

        await httpContext.SignOutAsync(
            CookieAuthenticationDefaults
                .AuthenticationScheme);

        return Results.Redirect(
            "/login");
    })
    .RequireAuthorization()
    .AddEndpointFilter(
        ValidarAntiforgeryEndpoint);

app.MapPost(
    "/account/forgot-password",
    async (
        HttpContext httpContext,
        IConfiguration configuration,
        IPasswordResetService passwordResetService) =>
    {
        var form =
            await httpContext.Request
                .ReadFormAsync(
                    httpContext.RequestAborted);

        var username =
            form["Username"]
                .ToString()
                .Trim();

        var configuredBaseUrl =
            configuration[
                "App:BaseUrl"];

        var baseUrl =
            !string.IsNullOrWhiteSpace(
                configuredBaseUrl)
                ? configuredBaseUrl.TrimEnd('/')
                : $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

        await passwordResetService
            .SolicitarRecuperacionAsync(
                username,
                baseUrl);

        return Results.Redirect(
            "/forgot-password?sent=1");
    })
    .AllowAnonymous()
    .RequireRateLimiting(
        "password-reset-policy")
    .AddEndpointFilter(
        ValidarAntiforgeryEndpoint);

app.MapPost(
    "/account/reset-password",
    async (
        HttpContext httpContext,
        IPasswordResetService passwordResetService) =>
    {
        var form =
            await httpContext.Request
                .ReadFormAsync(
                    httpContext.RequestAborted);

        var token =
            form["Token"]
                .ToString();

        var nuevaPassword =
            form["NewPassword"]
                .ToString();

        var confirmarPassword =
            form["ConfirmPassword"]
                .ToString();

        if (string.IsNullOrWhiteSpace(
                nuevaPassword) ||
            nuevaPassword.Length < 6)
        {
            return Results.Redirect(
                $"/reset-password?token={Uri.EscapeDataString(token)}&error=short");
        }

        if (!string.Equals(
                nuevaPassword,
                confirmarPassword,
                StringComparison.Ordinal))
        {
            return Results.Redirect(
                $"/reset-password?token={Uri.EscapeDataString(token)}&error=mismatch");
        }

        var actualizado =
            await passwordResetService
                .RestablecerPasswordAsync(
                    token,
                    nuevaPassword);

        if (!actualizado)
        {
            return Results.Redirect(
                "/reset-password?error=invalid");
        }

        await httpContext.SignOutAsync(
            CookieAuthenticationDefaults
                .AuthenticationScheme);

        return Results.Redirect(
            "/login?changed=1");
    })
    .AllowAnonymous()
    .RequireRateLimiting(
        "password-reset-policy")
    .AddEndpointFilter(
        ValidarAntiforgeryEndpoint);

var securityTxtHandler =
    (
        IConfiguration configuration,
        HttpContext httpContext) =>
    {
        var configuredBaseUrl =
            configuration[
                "App:BaseUrl"];

        var baseUrl =
            !string.IsNullOrWhiteSpace(
                configuredBaseUrl)
                ? configuredBaseUrl.TrimEnd('/')
                : $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

        var contactEmail =
            configuration[
                "Security:ContactEmail"];

        if (string.IsNullOrWhiteSpace(
                contactEmail))
        {
            contactEmail =
                "soporte@tu-dominio.com";
        }

        var expires =
            DateTime.UtcNow
                .AddYears(1)
                .ToString(
                    "yyyy-MM-ddTHH:mm:ss.000Z");

        var content =
$"""
Contact: mailto:{contactEmail}
Preferred-Languages: es,en
Canonical: {baseUrl}/.well-known/security.txt
Expires: {expires}
""";

        return Results.Text(
            content,
            "text/plain");
    };

app.MapGet(
    "/.well-known/security.txt",
    securityTxtHandler);

app.MapGet(
    "/security.txt",
    securityTxtHandler);

app.MapControllers();

app.MapStaticAssets()
    .Add(endpointBuilder =>
    {
        endpointBuilder.Metadata.Add(
            new AllowAnonymousAttribute());
    });

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

static async ValueTask<object?> ValidarAntiforgeryEndpoint(
    EndpointFilterInvocationContext context,
    EndpointFilterDelegate next)
{
    var antiforgery =
        context.HttpContext
            .RequestServices
            .GetRequiredService<IAntiforgery>();

    try
    {
        await antiforgery
            .ValidateRequestAsync(
                context.HttpContext);
    }
    catch (AntiforgeryValidationException)
    {
        return Results.BadRequest(
            "Solicitud inválida.");
    }

    return await next(context);
}

static List<Claim> CrearClaimsUsuario(
    Usuario usuario)
{
    var claims =
        new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                usuario.Id.ToString()),

            new(
                ClaimTypes.Name,
                usuario.Name
                ?? string.Empty),

            new(
                "Username",
                usuario.Username
                ?? string.Empty),

            new(
                ClaimTypes.Email,
                usuario.email
                ?? string.Empty),

            new(
                "LvlId",
                usuario.lvl_Id.ToString()),

            new(
                "RequirePasswordChange",
                usuario.RequirePasswordChange
                    .ToString())
        };

    if (usuario.EmployeeId.HasValue)
    {
        claims.Add(
            new Claim(
                "EmployeeId",
                usuario.EmployeeId
                    .Value
                    .ToString()));
    }

    if (usuario.Empleado is not null)
    {
        claims.Add(
            new Claim(
                "EmployeeCode",
                usuario.Empleado
                    .IdEmpleado
                    .ToString()));

        claims.Add(
            new Claim(
                "EmployeeName",
                usuario.Empleado
                    .Nombre
                ?? string.Empty));

        claims.Add(
            new Claim(
                "EmployeeEmail",
                usuario.Empleado
                    .Email
                ?? string.Empty));

        claims.Add(
            new Claim(
                "EmployeeTypeId",
                usuario.Empleado
                    .EmployeeTypeId
                    .ToString()));

        if (usuario.Empleado.EmployeeType is not null)
        {
            claims.Add(
                new Claim(
                    "EmployeeTypeCode",
                    usuario.Empleado
                        .EmployeeType
                        .Code
                    ?? string.Empty));
        }
    }

    if (usuario.Permisos is not null)
    {
        var permisos =
            usuario.Permisos;

        AgregarPermiso(
            claims,
            "CreateUser",
            permisos.CreateUser);

        AgregarPermiso(
            claims,
            "EditUser",
            permisos.EditUser);

        AgregarPermiso(
            claims,
            "DeleteUser",
            permisos.DeleteUser);

        AgregarPermiso(
            claims,
            "UnlockUsers",
            permisos.UnlockUsers);

        AgregarPermiso(
            claims,
            "CreateBook",
            permisos.CreateBook);

        AgregarPermiso(
            claims,
            "EditBook",
            permisos.EditBook);

        AgregarPermiso(
            claims,
            "DeleteBook",
            permisos.DeleteBook);

        AgregarPermiso(
            claims,
            "BooksView",
            permisos.BooksView);

        AgregarPermiso(
            claims,
            "CreateAssignations",
            permisos.CreateAssignations);

        AgregarPermiso(
            claims,
            "AsignBook",
            permisos.AsignBook);

        AgregarPermiso(
            claims,
            "ViewAssignations",
            permisos.ViewAssignations);

        AgregarPermiso(
            claims,
            "QualifyBook",
            permisos.QualifyBook);

        AgregarPermiso(
            claims,
            "CreateEmployees",
            permisos.CreateEmployees);

        AgregarPermiso(
            claims,
            "EditEmployees",
            permisos.EditEmployees);

        AgregarPermiso(
            claims,
            "DeleteEmployees",
            permisos.DeleteEmployees);

        AgregarPermiso(
            claims,
            "ViewEmployees",
            permisos.ViewEmployees);

        AgregarPermiso(
            claims,
            "LateWork",
            permisos.LateWork);

        AgregarPermiso(
            claims,
            "CreateLateWork",
            permisos.CreateLateWork);

        AgregarPermiso(
            claims,
            "EditLateWork",
            permisos.EditLateWork);

        AgregarPermiso(
            claims,
            "DeleteLateWork",
            permisos.DeleteLateWork);

        AgregarPermiso(
            claims,
            "CompleteLateWork",
            permisos.CompleteLateWork);

        AgregarPermiso(
            claims,
            "EditPermissionLevels",
            permisos.EditPermissionLevels);

        AgregarPermiso(
            claims,
            "ViewAllEmployeeTypes",
            permisos.ViewAllEmployeeTypes);
    }

    return claims;
}

static void AgregarPermiso(
    ICollection<Claim> claims,
    string nombre,
    bool tienePermiso)
{
    if (!tienePermiso)
    {
        return;
    }

    claims.Add(
        new Claim(
            nombre,
            bool.TrueString));
}

static string NormalizarReturnUrl(
    string? returnUrl)
{
    if (string.IsNullOrWhiteSpace(
            returnUrl))
    {
        return "/dashboard";
    }

    returnUrl =
        returnUrl.Trim();

    if (!returnUrl.StartsWith(
            "/",
            StringComparison.Ordinal))
    {
        return "/dashboard";
    }

    if (returnUrl.StartsWith(
            "//",
            StringComparison.Ordinal))
    {
        return "/dashboard";
    }

    if (returnUrl.StartsWith(
            @"\\",
            StringComparison.Ordinal) ||
        returnUrl.StartsWith(
            @"/\",
            StringComparison.Ordinal))
    {
        return "/dashboard";
    }

    return returnUrl;
}

static bool EsRutaTecnicaAutenticacion(
    PathString path)
{
    return
        path.StartsWithSegments(
            "/_framework") ||

        path.StartsWithSegments(
            "/_blazor") ||

        path.StartsWithSegments(
            "/_content") ||

        path.StartsWithSegments(
            "/css") ||

        path.StartsWithSegments(
            "/js") ||

        path.StartsWithSegments(
            "/style") ||

        path.StartsWithSegments(
            "/lib") ||

        path.StartsWithSegments(
            "/images") ||

        path.StartsWithSegments(
            "/favicon") ||

        path.StartsWithSegments(
            "/robots.txt") ||

        path.StartsWithSegments(
            "/.well-known");
}

static string ObtenerClaveCacheUsuario(
    Guid usuarioId)
{
    return
        $"pagination-auth-user-valid-{usuarioId:N}";
}

app.Run();