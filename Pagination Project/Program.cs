using Microsoft.AspNetCore.Components;
using Pagination_Project.Components;
using Pagination_Project.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuración de servicios
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Para usar controladores tipo /api/auth/login
builder.Services.AddControllers();

// HttpClient para usar dentro de componentes Blazor
builder.Services.AddScoped(sp =>
{
    var navigation = sp.GetRequiredService<NavigationManager>();
    return new HttpClient
    {
        BaseAddress = new Uri(navigation.BaseUri)
    };
});

// HttpClient para el servicio que consulta Supabase
builder.Services.AddHttpClient<SupabaseAuthService>();

var app = builder.Build();

// Pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Mapear controladores API
app.MapControllers();

// Mapear componentes Blazor
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();