using Blazored.LocalStorage;
using MudBlazor;
using SSSMCR.Web;
using SSSMCR.Web.Components;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<AuthHandler>();
builder.Services.AddHttpClient("api", c =>
    {
        c.BaseAddress = new Uri("http://localhost:5506/");
        c.DefaultRequestHeaders.Accept.Clear();
        c.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        c.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddHttpMessageHandler<AuthHandler>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.AddServiceDefaults();


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();


app.MapStaticAssets();
app.MapDefaultEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();