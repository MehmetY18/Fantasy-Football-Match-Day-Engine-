using MatchDataIngestion;
using Microsoft.EntityFrameworkCore;
using Data;

var builder = Host.CreateApplicationBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();