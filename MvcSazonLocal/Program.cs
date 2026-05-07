using Azure.Security.KeyVault.Secrets;
using dotenv.net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;
using MvcSazonLocal.Services;
using SazonLocalHelpers.Helpers;
using SazonLocalInterfaces.Interfaces;
using SazonLocalModels.Models;
using Stripe;
using static Azure.Core.HttpHeader;


var builder = WebApplication.CreateBuilder(args);

/* ---KEY VAULT--- */
builder.Services.AddAzureClients(factory =>
{
    factory.AddSecretClient(builder.Configuration.GetSection("KeyVault"));
});
SecretClient secretClient = builder.Services.BuildServiceProvider().GetService<SecretClient>();

KeyVaultSecret apiconnection = await secretClient.GetSecretAsync("ApiConnection");

KeyVaultSecret server = await secretClient.GetSecretAsync("EmailSettings--Server");
KeyVaultSecret port = await secretClient.GetSecretAsync("EmailSettings--Port");
KeyVaultSecret senderName = await secretClient.GetSecretAsync("EmailSettings--SenderName");
KeyVaultSecret senderEmail = await secretClient.GetSecretAsync("EmailSettings--SenderEmail");
KeyVaultSecret username = await secretClient.GetSecretAsync("EmailSettings--Username");
KeyVaultSecret password = await secretClient.GetSecretAsync("EmailSettings--Password");

KeyVaultSecret insights = await secretClient.GetSecretAsync("ApplicationInsights");

KeyVaultSecret strikeyKey = await secretClient.GetSecretAsync("StripeSecretKey");

/* ---APPLICATION INSIGHTS--- */
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = insights.Value;
});

// Add services to the container.
builder.Services.AddSingleton<HelperPath>();
builder.Services.AddTransient(sp =>
{
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    return new SazonApiService(apiconnection.Value, httpContextAccessor);
});
builder.Services.AddHttpContextAccessor();

builder.Services.AddSession();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
}
).AddCookie(
    CookieAuthenticationDefaults.AuthenticationScheme, config =>
    {
        config.AccessDeniedPath = "/Auth/ErrorAcceso";
    }
    );

builder.Services.AddControllersWithViews(options => options.EnableEndpointRouting = false).AddSessionStateTempDataProvider();

/* --- EMAIL --- */
builder.Services.Configure<EmailSettings>(options =>
{
    options.Server = server.Value;
    options.Port = int.Parse(port.Value);
    options.SenderName = senderName.Value;
    options.SenderEmail = senderEmail.Value;
    options.Username = username.Value;
    options.Password = password.Value;
});
builder.Services.AddScoped<IEmailService, EmailService>();

/* --- PDF --- */
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
builder.Services.AddTransient<IPdfService, PdfService>();

/* --- CONVERSION NUMEROS --- */
var supportedCultures = new[] { "en-US", "es-ES" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("en-US")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

StripeConfiguration.ApiKey = strikeyKey.Value;

app.UseRequestLocalization(localizationOptions);

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.UseMvc(routes =>
{
    routes.MapRoute(
        name: "default",
        template: "{controller=Productos}/{action=Productos}/{id?}");
});

app.Run();
