using ScoringWorker;
using Microsoft.EntityFrameworkCore;
using Data;
using MassTransit;
using ScoringWorker.Consumers;


var builder = Host.CreateApplicationBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PlayerStatUpdatedConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", host =>
        {
            host.Username("guest");
            host.Password("guest");
        });
        
        cfg.ConfigureEndpoints(context);

    });
});
    

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();