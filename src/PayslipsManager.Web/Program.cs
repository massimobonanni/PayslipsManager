using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using PayslipsManager.Application.Interfaces;
using PayslipsManager.Infrastructure;
using PayslipsManager.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Load local configuration file if it exists (git-ignored, for secrets)
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

// Check if we should bypass authentication (for local development)
var bypassAuth = builder.Configuration.GetValue<bool>("BypassAuthentication");

if (!bypassAuth)
{
    // Add Microsoft Identity authentication with token acquisition for Azure Storage
    string[] initialScopes = ["https://storage.azure.com/user_impersonation"];
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
        .EnableTokenAcquisitionToCallDownstreamApi(initialScopes)
        .AddInMemoryTokenCaches();

    // Add authorization policies
    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = options.DefaultPolicy;
    });

    builder.Services.Configure<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>(
        Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
        options => options.AccessDeniedPath = "/Home/AccessDenied");
}
else
{
    // In development mode without authentication, use cookie authentication
    builder.Services.AddAuthentication("Cookies")
        .AddCookie("Cookies", options =>
        {
            options.LoginPath = "/Home/DevLogin";
            options.AccessDeniedPath = "/Home/AccessDenied";
        });
    
    builder.Services.AddAuthorization();
}

// Add services to the container
var mvcBuilder = builder.Services.AddControllersWithViews();
if (!bypassAuth)
{
    mvcBuilder.AddMicrosoftIdentityUI();
}

// Add Infrastructure services (includes repository and application services)
builder.Services.AddInfrastructure(builder.Configuration);

// Add employee context service (resolves signed-in employee from Entra claims)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IEmployeeContextService, EmployeeContextService>();

// Add logging
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

