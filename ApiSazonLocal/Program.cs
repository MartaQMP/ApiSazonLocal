using ApiSazonLocal.Data;
using ApiSazonLocal.Repositories;
using Microsoft.EntityFrameworkCore;
using SazonLocalHelpers.Helpers;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<HelperPath>();
builder.Services.AddTransient<IRepository, Repository>();
string connection = builder.Configuration.GetConnectionString("Sql");
builder.Services.AddDbContext<SazonContext>(options => options.UseSqlServer(connection));

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
