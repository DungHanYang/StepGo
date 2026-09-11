using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using StepGo.ApiClient.Auth;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddSingleton<IAuthTokenStore, InMemoryAuthTokenStore>();
builder.Services.AddScoped<StepGoAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<StepGoAuthenticationStateProvider>());

await builder.Build().RunAsync();
