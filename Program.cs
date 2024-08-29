using Microsoft.OpenApi.Models;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

string redisConnection = builder.Configuration.GetConnectionString("RedisConnection")!;

// Configuração do Redis para StackExchange.Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
    //options.InstanceName = "RedisDemo_"; // Nome da instância para diferenciar caches
});

// configuração do redis para Microsoft.Extensions.Caching.StackExchangeRedis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse(redisConnection, true);
    return ConnectionMultiplexer.Connect(configuration);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RedisDemo API",
        Version = "v1",
        Description = "API para gerenciar o cache Redis",
        Contact = new OpenApiContact
        {
            Name = "Danielle Bassetto",
            Email = "danibbassetto@hotmail.com",
            Url = new Uri("https://github.com/danibassetto")
        }
    });

    var xmlFile = $"{AppDomain.CurrentDomain.FriendlyName}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "RedisDemo API v1");
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();