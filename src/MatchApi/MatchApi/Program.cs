using Microsoft.EntityFrameworkCore;
using Data;
var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();


var app = builder.Build(); 

app.UseAuthorization();
app.MapControllers();

app.Run();





