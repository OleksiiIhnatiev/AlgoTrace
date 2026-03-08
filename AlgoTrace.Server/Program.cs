using AlgoTrace.Server.Algorithms;
using AlgoTrace.Server.Algorithms.Graph;
using AlgoTrace.Server.Algorithms.Metric;
using AlgoTrace.Server.Data;
using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models;
using AlgoTrace.Server.Models.DTO;
using AlgoTrace.Server.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)
);

builder.Services.AddIdentityApiEndpoints<User>().AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddScoped<ITextAlgorithm, RabinKarpAlgorithm>();
builder.Services.AddScoped<ITextAlgorithm, LevenshteinAlgorithm>();
builder.Services.AddScoped<ITextAlgorithm, NgramAlgorithm>();
builder.Services.AddScoped<ITextAnalysisService, TextAnalysisService>();

builder.Services.AddScoped<IGraphAlgorithm, ControlFlowGraphAlgorithm>();
builder.Services.AddScoped<IGraphAlgorithm, ProgramDependenceGraphAlgorithm>();
builder.Services.AddScoped<IGraphAlgorithm, SubgraphIsomorphismAlgorithm>();
builder.Services.AddScoped<IGraphAnalysisService, GraphAnalysisService>();

builder.Services.AddScoped<IMetricAlgorithm, HalsteadMetricsAlgorithm>();
builder.Services.AddScoped<IMetricAlgorithm, McCabeComplexityAlgorithm>();
builder.Services.AddScoped<IMetricAnalysisService, MetricAnalysisService>();

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
