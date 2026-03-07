using AlgoTrace.Server.Data;
using AlgoTrace.Server.Models;
using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Services;
using AlgoTrace.Server.Algorithms;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentityApiEndpoints<User>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddScoped<ITextAlgorithm, RabinKarpAlgorithm>();
builder.Services.AddScoped<ITextAlgorithm, LevenshteinAlgorithm>();
builder.Services.AddScoped<ITextAlgorithm, NgramAlgorithm>();
builder.Services.AddScoped<ITextAnalysisService, TextAnalysisService>();

var app = builder.Build();

app.MapGroup("/identity").MapIdentityApi<User>();

app.UseDefaultFiles();
app.MapStaticAssets();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("/index.html");

app.Run();