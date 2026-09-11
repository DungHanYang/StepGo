using Microsoft.AspNetCore.Components.Authorization;
using StepGo.ApiClient.Auth;
using StepGo.Marketing.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Registered here too (not just in StepGo.Marketing.Client/Program.cs) because interactive
// islands are prerendered once on the server before hydrating client-side, and prerendering
// resolves DI from this project's container, not the WASM host's.
builder.Services.AddSingleton<IAuthTokenStore, InMemoryAuthTokenStore>();
builder.Services.AddScoped<StepGoAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<StepGoAuthenticationStateProvider>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(StepGo.Marketing.Client._Imports).Assembly);

app.Run();
