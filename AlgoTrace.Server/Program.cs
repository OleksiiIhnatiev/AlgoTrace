using AlgoTrace.Server.Algorithms.Graph;
using AlgoTrace.Server.Algorithms.Metric;
using AlgoTrace.Server.Algorithms.Textual;
using AlgoTrace.Server.Algorithms.Token;
using AlgoTrace.Server.Algorithms.Tree;
using AlgoTrace.Server.Data;
using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models;
using AlgoTrace.Server.ParserFactory;
using AlgoTrace.Server.ParserFactory.Parsers;
using AlgoTrace.Server.Services;
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

builder.Services.AddScoped<IUnifiedAnalysisService, UnifiedAnalysisService>();
builder.Services.AddScoped<IDirectoryService, DirectoryService>();
builder.Services.AddScoped<ISubmissionService, SubmissionService>();

builder.Services.AddScoped<ITextAlgorithm, RabinKarpAlgorithm>();
builder.Services.AddScoped<ITextAlgorithm, LevenshteinAlgorithm>();
builder.Services.AddScoped<ITextAlgorithm, NgramAlgorithm>();

builder.Services.AddScoped<ITokenAlgorithm, WinnowingAlgorithm>();
builder.Services.AddScoped<ITokenAlgorithm, JaccardTokenAlgorithm>();

builder.Services.AddScoped<ITreeAlgorithm, AstStructureAlgorithm>();
builder.Services.AddScoped<ITreeAlgorithm, SubtreeAlgorithm>();
builder.Services.AddScoped<ITreeAlgorithm, AstHashingAlgorithm>();

builder.Services.AddScoped<IGraphAlgorithm, ControlFlowGraphAlgorithm>();
builder.Services.AddScoped<IGraphAlgorithm, ProgramDependenceGraphAlgorithm>();
builder.Services.AddScoped<IGraphAlgorithm, SubgraphIsomorphismAlgorithm>();

builder.Services.AddScoped<IMetricAlgorithm, HalsteadMetricsAlgorithm>();
builder.Services.AddScoped<IMetricAlgorithm, McCabeComplexityAlgorithm>();

builder.Services.AddScoped<ICodeParser, CSharpParser>();
builder.Services.AddScoped<ICodeParser, PythonParser>();
builder.Services.AddScoped<ICodeParser, JavaParser>();
builder.Services.AddScoped<ICodeParser, CppParser>();
builder.Services.AddScoped<ICodeParser, CParser>();
builder.Services.AddScoped<ICodeParser, JavaScriptParser>();
builder.Services.AddScoped<ICodeParser, TypeScriptParser>();
builder.Services.AddScoped<ICodeParser, PhpParser>();
builder.Services.AddScoped<ICodeParser, GoParser>();
builder.Services.AddScoped<ICodeParser, RubyParser>();
builder.Services.AddScoped<ICodeParser, HtmlParser>();
builder.Services.AddScoped<ICodeParser, CssParser>();
builder.Services.AddScoped<ICodeParser, KotlinParser>();
builder.Services.AddScoped<ICodeParser, RustParser>();
builder.Services.AddScoped<ICodeParser, SwiftParser>();
builder.Services.AddScoped<ICodeParser, BashParser>();
builder.Services.AddScoped<ICodeParser, SqlParser>();
builder.Services.AddScoped<ICodeParser, XmlParser>();
builder.Services.AddScoped<ICodeParser, JsonParser>();
builder.Services.AddScoped<ICodeParser, YamlParser>();

builder.Services.AddScoped<ParserFactory>();

var app = builder.Build();

// Автоматическое применение миграций базы данных при запуске приложения
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

app.MapGroup("/auth").MapIdentityApi<User>();
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