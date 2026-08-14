using LeaderboardWorker;
using LeaderboardWorker.Consumers;
using MassTransit;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

//Redis DI
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = new ConfigurationOptions
    {
        EndPoints = { "localhost:6379" },
        AbortOnConnectFail = false, 
        ConnectTimeout = 10000,     
        SyncTimeout = 10000
    };
    
    return ConnectionMultiplexer.Connect(config);
});

//MassTransit and Consumer Configuration
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer < ScoreCalculatedConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

       
        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();