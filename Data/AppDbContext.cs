using Microsoft.EntityFrameworkCore;
using Shared.DomainModels;

namespace Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>().OwnsOne(p => p.stats);

        modelBuilder.Entity<Match>().OwnsOne(m => m.Score);

    }

    public DbSet<Player> Players { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<Match> Matches { get; set; }
    public DbSet<Fixture> Fixtures { get; set; }
    
    
}