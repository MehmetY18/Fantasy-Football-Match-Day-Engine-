using Data;
using MassTransit;
using MatchDataIngestion;
using Microsoft.EntityFrameworkCore;



var builder = Host.CreateApplicationBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

//DbContext 
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

//MassTransit + Transactional Outbox
builder.Services.AddMassTransit(x =>
{
    //Outbox Pattern Activating
    x.AddEntityFrameworkOutbox<AppDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });
    
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
builder.Services.AddMassTransitHostedService();


var host = builder.Build();
host.Run();