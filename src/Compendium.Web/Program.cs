using Compendium.Web.Components;
using Compendium.Web.Services;
using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllers();

builder.Services.AddSingleton<LlmConfigService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var service = new LlmConfigService();
    // Load from persistent storage first — this is the store shared with
    // `compendium init` and the CLI, so either one configures both surfaces.
    service.LoadAsync().GetAwaiter().GetResult();
    // Fallback to appsettings.json if nothing was persisted yet.
    service.LoadFromConfiguration(config);
    return service;
});

builder.Services.AddSingleton<BundleService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var bundlePath = config["Compendium:BundlePath"];
    var service = new BundleService();
    if (!string.IsNullOrEmpty(bundlePath) && Directory.Exists(bundlePath))
    {
        service.LoadBundle(bundlePath);
    }
    return service;
});

builder.Services.AddSingleton<IngestionService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllers();

app.Run();
