using System.Reflection;
using CatalogoJogos.Api.Repositories;
using CatalogoJogos.Api.Services;
using CatalogoJogos.Api.Settings;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Catálogo de Jogos — CP5",
        Version = "v1",
        Description = "Web API .NET 8 com CRUD, filtro e agregação no MongoDB."
    });
    var xml = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xml));
});

builder.Services.AddOptions<MongoDbSettings>()
    .Bind(builder.Configuration.GetSection(MongoDbSettings.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// MongoClient mantém seu pool de conexões e deve ser reutilizado.
builder.Services.AddSingleton<IMongoClient>(provider =>
{
    var options = provider.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    var settings = MongoClientSettings.FromConnectionString(options.ConnectionString);
    settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
    return new MongoClient(settings);
});
builder.Services.AddSingleton<IMongoDatabase>(provider =>
{
    var options = provider.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    return provider.GetRequiredService<IMongoClient>().GetDatabase(options.DatabaseName);
});
builder.Services.AddScoped<IJogoRepository, JogoRepository>();
builder.Services.AddScoped<IJogoService, JogoService>();

var app = builder.Build();
app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Catálogo de Jogos v1"));
app.MapControllers();
app.Run();
