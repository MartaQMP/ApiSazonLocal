using ApiSazonLocal.Data;
using ApiSazonLocal.Services;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

string azureconnection = builder.Configuration["AzureConnection"];
string storageAccount = builder.Configuration["StorageAccount"];

builder.Services.AddDbContext<SazonContext>(options => options.UseSqlServer(azureconnection));
builder.Services.AddSingleton(x => new BlobServiceClient(storageAccount));

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddTransient<BlobService>();

builder.Build().Run();