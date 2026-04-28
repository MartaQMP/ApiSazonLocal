using ApiSazonLocal.Data;
using ApiSazonLocal.Helpers;
using ApiSazonLocal.Repositories;
using Azure.Security.KeyVault.Secrets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using SazonLocalHelpers.Helpers;
using SazonLocalInterfaces.Interfaces;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

/* ---KEY VAULT--- */
builder.Services.AddAzureClients(factory =>
{
    factory.AddSecretClient(builder.Configuration.GetSection("KeyVault"));
});
SecretClient secretClient = builder.Services.BuildServiceProvider().GetService<SecretClient>();

KeyVaultSecret azureconnection = await secretClient.GetSecretAsync("AzureConnection");
KeyVaultSecret jwtToken = await secretClient.GetSecretAsync("JwtToken");

/* ---TOKEN--- */
HelperCifrado.Initialize(jwtToken.Value);

// Add services to the container.
builder.Services.AddSingleton<HelperPath>();
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
