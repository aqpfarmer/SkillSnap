using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Frontend;
using Frontend.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5282") }); // Adjust the port to match your backend

// Register authentication services
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AuthenticatedHttpClientService>();

// Register services with interfaces
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ISkillService, SkillService>();
builder.Services.AddScoped<IPortfolioUserService, PortfolioUserService>();
builder.Services.AddScoped<Frontend.Services.IMetricsService, Frontend.Services.MetricsService>();

// Register state management service as Scoped (not Singleton due to HttpClient dependency)
builder.Services.AddScoped<IUserSessionService, UserSessionService>();

await builder.Build().RunAsync();
