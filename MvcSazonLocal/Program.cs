using dotenv.net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MvcSazonLocal.Services;
using SazonLocalHelpers.Helpers;
using SazonLocalInterfaces.Services;
using SazonLocalModels.Models;
using Stripe;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<HelperPath>();
builder.Services.AddTransient<SazonApiService>();

string connection = builder.Configuration.GetConnectionString("ApiSazon");

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
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
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

DotEnv.Load();

var stripeKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");
StripeConfiguration.ApiKey = stripeKey;

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
