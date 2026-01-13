using chatterbox_ui.Components;
using chatterbox_ui.Services;
using DotNetEnv;

// Load environment variables from .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure Chatterbox settings from environment variables
builder.Services.Configure<ChatterboxConfig>(config =>
{
    config.ServerUrl = Environment.GetEnvironmentVariable("CHATTERBOX_SERVER_URL") ?? "http://localhost:20480";
    config.ApiKey = Environment.GetEnvironmentVariable("CHATTERBOX_API_KEY") ?? "";
});

// Register Chatterbox service
builder.Services.AddScoped<ChatterboxService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
