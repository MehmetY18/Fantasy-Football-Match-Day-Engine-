using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Data;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using StackExchange.Redis;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect("localhost:6379"));

builder.Services.AddScoped<IDatabase>(sp =>
{
    var redis = sp.GetRequiredService<IConnectionMultiplexer>();
    return redis.GetDatabase();
});

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Fantasy Football Match & Leaderboard API",
        Version = "v1",
        Description = "Fantasy Football Platform API Documentation (OPCODE BASED)",
    });
});

builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "postgresql",
        tags: new []{"ready"})
    .AddRedis(
        builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379",
        name: "redis",
        tags: new []{"ready"});

builder.Services.AddRateLimiter(options =>
{
    // 429 Too Many Requests döndüğünde verilecek standart yanıt
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        // Reverse proxy (Nginx, Cloudflare vb.) arkasındaysa gerçek IP'yi almak için:
        string ipAddress = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() 
                           ?? httpContext.Connection.RemoteIpAddress?.ToString() 
                           ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ipAddress,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 60, 
                Window = TimeSpan.FromMinutes(1)
            });
    });
});

var app = builder.Build();

//SWAGGER UI 
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fantasy Football API V1");
        c.RoutePrefix = "swagger"; // http://localhost:/swagger
    });
}


app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false, // Hiçbir altyapı bağımlılığına bakmaz, app ayaktaysa 200 OK döner
});

app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"), // Sadece DB & Redis'i denetler
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseRateLimiter();

app.Run();





