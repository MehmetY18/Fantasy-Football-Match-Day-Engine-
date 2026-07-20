using System.Text.Json;
using Data;
using Microsoft.EntityFrameworkCore;
using Shared.DomainModels;
using MassTransit;

namespace MatchDataIngestion;


public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPublishEndpoint _publishEndpoint;

    public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory, IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _publishEndpoint = publishEndpoint;
    }

    

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(60));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

               
                
                List<Player> rawPlayers = await ReadAndDeserializeAsync<Player>("players.json", stoppingToken);
                List<Match> rawMatches = await ReadAndDeserializeAsync<Match>("matches.json", stoppingToken);
                List<Fixture> rawFixtures = await ReadAndDeserializeAsync<Fixture>("fixtures.json", stoppingToken);
                
                List<Player> normalizePlayersData = new();
                List<Match> normalizeMathcesData = new();
                List<Fixture> normalizeFixturesData = new();
                
                if (rawPlayers != null)
                {
                     normalizePlayersData = rawPlayers.Select(p => new Player()
                    {
                        id = p.id,
                        name = p.name.Trim().ToUpper(),
                        position = p.position,
                        team_id = p.team_id,
                        price = p.price,
                        total_points = p.total_points,
                        stats = p.stats
                    }).ToList();
                }

                if (rawMatches != null)
                {
                     normalizeMathcesData = rawMatches.Select(m => new Match()
                    {
                        id = m.id,
                        home_team_id = m.home_team_id,
                        away_team_id = m.away_team_id,
                        kickoff = m.kickoff,
                        status = m.status,
                        gameweek = m.gameweek,
                        Score = m.Score,
                        minute = m.minute
                    }).ToList();
                }

                if (rawFixtures != null)
                {
                     normalizeFixturesData = rawFixtures.Select(f => new Fixture()
                    {
                        id = f.id,
                        match_id = f.match_id,
                        minute = f.minute,
                        type = f.type,
                        player_id = f.player_id,
                        assist_player_id = f.assist_player_id,
                        team_id = f.team_id
                    }).ToList();
                }

                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                    
                    
                    //Players
                    var incomingPlayerId = normalizePlayersData.Select(p => p.id).ToList();
                    var dbPlayers = await dbContext.Players.Where(p => incomingPlayerId.Contains(p.id)).ToListAsync(stoppingToken);
                    
                    //Matches
                    var incomingMatchId = normalizeMathcesData.Select(m=> m.id).ToList();
                    var dbMatches = await dbContext.Matches.Where(m => incomingMatchId.Contains(m.id)).ToListAsync(stoppingToken);
                    
                    //Fixtures
                    var incomingFixtureId = normalizeFixturesData.Select(f => f.id).ToList();
                    var dbFixtures = await dbContext.Fixtures.Where(f => incomingFixtureId.Contains(f.id)).ToListAsync(stoppingToken);
                    
                    var playersToPublish = new List<Player>();
                    var matchesToPublish = new List<Match>();
                    
                    foreach (var incomingPlayer in normalizePlayersData)
                    {
                        var dbPlayer = dbPlayers.FirstOrDefault(p => p.id == incomingPlayer.id);
                        if(dbPlayer == null)
                        {
                            
                            await dbContext.Players.AddAsync(incomingPlayer, stoppingToken);
                            
                        }
                        
                        
                        else
                        {
                            //Idempotency checking
                            if (dbPlayer.name != incomingPlayer.name || dbPlayer.name != incomingPlayer.name ||
                                dbPlayer.position != incomingPlayer.position ||
                                dbPlayer.team_id != incomingPlayer.team_id ||
                                dbPlayer.price != incomingPlayer.price ||
                                dbPlayer.total_points != incomingPlayer.total_points ||
                                dbPlayer.stats != incomingPlayer.stats)
                            {
                                dbPlayer.name = incomingPlayer.name;
                                dbPlayer.position = incomingPlayer.position;
                                dbPlayer.team_id = incomingPlayer.team_id;
                                dbPlayer.price = incomingPlayer.price;
                                dbPlayer.total_points = incomingPlayer.total_points;
                                dbPlayer.stats = incomingPlayer.stats;
                                
                                
                                
                                playersToPublish.Add(dbPlayer);
                            }
                        }
                        
                        
                    }
                    
                    foreach (var incomingMatch in normalizeMathcesData)
                    {
                        var dbMatch = dbMatches.FirstOrDefault(m => m.id == incomingMatch.id);
                        
                        if(dbMatch == null)
                        {
                            await dbContext.Matches.AddAsync(incomingMatch, stoppingToken);
                            
                        }
                        
                        else
                        {
                            if (dbMatch.home_team_id != incomingMatch.home_team_id ||
                                dbMatch.away_team_id != incomingMatch.away_team_id ||
                                dbMatch.kickoff != incomingMatch.kickoff ||
                                dbMatch.status != incomingMatch.status || dbMatch.gameweek != incomingMatch.gameweek ||
                                dbMatch.Score != incomingMatch.Score || dbMatch.minute != incomingMatch.minute)
                            {
                                dbMatch.home_team_id = incomingMatch.home_team_id;
                                dbMatch.away_team_id = incomingMatch.away_team_id;
                                dbMatch.kickoff = incomingMatch.kickoff;
                                dbMatch.status = incomingMatch.status;
                                dbMatch.gameweek = incomingMatch.gameweek;
                                dbMatch.Score = incomingMatch.Score;
                                dbMatch.minute = incomingMatch.minute;
                                
                                matchesToPublish.Add((dbMatch));
                            }
                                
                        }
                        
                    }
                    
                    
                    
                    foreach (var incomingFixture in normalizeFixturesData)
                    {
                        var dbFixture = dbFixtures.FirstOrDefault(f => f.id == incomingFixture.id);
                        if(dbFixture == null)
                        {
                            await dbContext.Fixtures.AddAsync(incomingFixture, stoppingToken);
                        }

                        else
                        {
                            
                                dbFixture.id = incomingFixture.id;
                                dbFixture.match_id = incomingFixture.match_id;
                                dbFixture.minute = incomingFixture.minute;
                                dbFixture.type = incomingFixture.type;
                                dbFixture.player_id = incomingFixture.player_id;
                                dbFixture.assist_player_id = incomingFixture.assist_player_id;
                                dbFixture.team_id = incomingFixture.team_id;
                            
                        }
                        
                        
                    }
                    
                    
                    await dbContext.SaveChangesAsync(stoppingToken);

                    foreach (var player in playersToPublish)
                    {
                        await _publishEndpoint.Publish<IPlayerStatUpdated>(new
                        {
                            Id = player.id,
                            TotalPoints = player.total_points,
                            Stats = player.stats
                        },stoppingToken);
                        _logger.LogInformation($"RabbitMQ Event Published: PlayerStatUpdated for {player.name}");
                    }

                    foreach (var match in matchesToPublish)
                    {
                        await _publishEndpoint.Publish<IMatchUpdated>(new
                        {
                            MatchId = match.id, 
                            match.Score,
                            Minute = match.minute,
                            Status = match.status
                        },stoppingToken);
                        _logger.LogInformation($"RabbitMQ Event Published: MatchUpdated for Match ID: {match.id}");
                        
                    }
                }
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured during worker run.");

            }
            
        }
    }
    
  

    


    private async Task<List<T>?> ReadAndDeserializeAsync<T>(string fileName, CancellationToken cancellationToken) 
        where T : class, new()
    {
        
        

            try
            {
                string filePath = Path.Combine(AppContext.BaseDirectory, "mock-data", fileName);

                if (!File.Exists(filePath))
                {
                    _logger.LogWarning($"Path {filePath} not found.");
                    return new List<T>();
                }

                string jsonString = await File.ReadAllTextAsync(filePath, cancellationToken);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return  JsonSerializer.Deserialize<List<T>>(jsonString, options);
                
                
                
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"{fileName} file could not be read.");
                return new List<T>();

            }
        }
    }
public interface IPlayerStatUpdated
{
     int Id { get; }
     int TotalPoints { get; }
     string Stats { get; }
}

public interface IMatchUpdated
{
    int MatchId { get; }
    int Minute { get; } 
    string Score { get; }
    string Status { get; }
}

