using Microsoft.EntityFrameworkCore;
using Shared.DomainModels;
using MassTransit;

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

        modelBuilder.Entity<Squad>()
            .HasMany(s => s.Players)
            .WithOne()
            .HasForeignKey(sp => sp.SquadId)
            .OnDelete(DeleteBehavior.Cascade);// When you delete a squad, player selections are automatically deleted.
        
        modelBuilder.Entity<Squad>()
            .HasOne(s => s.User)
            .WithMany() 
            .HasForeignKey(s => s.user_id) 
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.AddTransactionalOutboxEntities();
        
    }

    public DbSet<Player> Players { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<Match> Matches { get; set; }
    public DbSet<Fixture> Fixtures { get; set; }
    
    public DbSet<Squad> Squads { get; set; }
    public DbSet<SquadPlayer> SquadPlayers { get; set; }
    
    public DbSet<User> Users { get; set; }
}