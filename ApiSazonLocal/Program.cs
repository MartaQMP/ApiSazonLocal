using ApiSazonLocal.Data;
using ApiSazonLocal.Helpers;
using ApiSazonLocal.Repositories;
using ApiSazonLocal.Services;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using SazonLocalHelpers.Helpers;
using SazonLocalInterfaces.Interfaces;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

/* ---KEY VAULT--- */
builder.Services.AddAzureClients(factory =>
{
    factory.AddSecretClient(builder.Configuration.GetSection("KeyVault"));
});
SecretClient secretClient = builder.Services.BuildServiceProvider().GetService<SecretClient>();

KeyVaultSecret azureconnection = await secretClient.GetSecretAsync("AzureConnection");
KeyVaultSecret jwtToken = await secretClient.GetSecretAsync("JwtToken");
KeyVaultSecret insights = await secretClient.GetSecretAsync("AplicationInsights");

/* ---TOKEN--- */
HelperCifrado.Initialize(jwtToken.Value);

KeyVaultSecret jwtSecret = await secretClient.GetSecretAsync("Jwt--SecretKey");
HelperActionOAuth helperOAuth = new HelperActionOAuth(
    builder.Configuration.GetValue<string>("ApiOAuthToken:Issuer"),
    builder.Configuration.GetValue<string>("ApiOAuthToken:Audience"),
    jwtSecret.Value
);

/* ---BLOB--- */
KeyVaultSecret storageAccount = await secretClient.GetSecretAsync("BlobStorage");
BlobServiceClient blobServiceClient = new BlobServiceClient(storageAccount.Value);
builder.Services.AddTransient<BlobServiceClient>(x => blobServiceClient);
builder.Services.AddTransient<BlobService>();

/* ---APPLICATION INSIGHTS--- */
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = insights.Value;
});

// Add services to the container.
builder.Services.AddSingleton<HelperPath>();
builder.Services.AddSingleton<HelperActionOAuth>(helperOAuth);
builder.Services.AddTransient<HelperToken>();
builder.Services.AddAuthentication(helperOAuth.GetAuthenticationSchema()).AddJwtBearer(helperOAuth.GetJWtBearerOptions());
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<IRepository, Repository>();
builder.Services.AddDbContext<SazonContext>(options => options.UseSqlServer(azureconnection.Value));

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;

    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});


// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    
}
app.MapOpenApi();
app.MapScalarApiReference();
app.MapGet("/", context =>
{
    context.Response.Redirect("/scalar");
    return Task.CompletedTask;
});
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
