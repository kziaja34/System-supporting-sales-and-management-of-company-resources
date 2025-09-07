using Blazored.LocalStorage;
using MudBlazor;
using SSSMCR.Web;
using SSSMCR.Web.Components;
using MudBlazor.Services;
using SSSMCR.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopRight;
    config.SnackbarConfiguration.PreventDuplicates = true;
    config.SnackbarConfiguration.NewestOnTop = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
    config.SnackbarConfiguration.HideTransitionDuration = 100;
    config.SnackbarConfiguration.ShowTransitionDuration = 100;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddHttpClient("api", c =>
{
    c.BaseAddress = new Uri("http://localhost:5506/");
    c.DefaultRequestHeaders.Accept.Clear();
    c.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    c.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<GenericService>();
builder.Services.AddScoped<ProductsApiService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<BranchesApiService>();   
builder.Services.AddScoped<RolesApiService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<UsersApiService>();
builder.AddServiceDefaults();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapDefaultEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();