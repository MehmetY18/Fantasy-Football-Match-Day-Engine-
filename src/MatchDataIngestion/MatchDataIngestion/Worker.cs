using System.Text.Json;
using System.Text.Json.Serialization;
using Data;
using Shared.Events;
using Microsoft.EntityFrameworkCore;
using Shared.DomainModels;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace MatchDataIngestion;


public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
   

    public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
       
    }

    

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(60));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                
                var playersWrapper = await ReadAndDeserializeAsync<PlayersWrapper>("players.json", stoppingToken);
                var matchsesWrapper = await ReadAndDeserializeAsync<MatchesWrapper>("matches.json", stoppingToken);
                var fixturesWrapper = await ReadAndDeserializeAsync<FixturesWrapper>("fixtures.json", stoppingToken);
                var usersWrapper = await ReadAndDeserializeAsync<UsersWrapper>("users.json", stoppingToken);
                var squadsWrapper = await  ReadAndDeserializeAsync<SquadsWrapper>("squads.json", stoppingToken);
                var teamsWrapper = await ReadAndDeserializeAsync<TeamsWrapper>("teams.json", stoppingToken);
                
                
                List<Player> rawPlayers = playersWrapper?.Players ?? new List<Player>();
                List<Match> rawMatches = matchsesWrapper?.Matches ?? new List<Match>();
                List<Fixture> rawFixtures = fixturesWrapper?.Fixtures ?? new List<Fixture>();
                List<User> rawUsers = usersWrapper?.Users ?? new List<User>();
                List<Squad> rawSquads = squadsWrapper?.Squads ?? new List<Squad>();
                List<Team> rawTeams = teamsWrapper?.Teams ?? new List<Team>();
                
                
                List<Player> normalizePlayersData = rawPlayers.Select(p => new Player()
                {
                    id = p.id,
                    name = p.name,
                    position = p.position,
                    team_id = p.team_id,
                    price = p.price,
                    total_points = p.total_points,
                    stats = p.stats ?? new PlayerStats()
                }).ToList();
                
                
                List<Match> normalizeMathcesData = rawMatches.Select(m=> new Match()
                {
                    id = m.id,
                    home_team_id = m.home_team_id,
                    away_team_id = m.away_team_id,
                    kickoff = m.kickoff,
                    gameweek = m.gameweek,
                    minute = m.minute,
                    Score = m.Score,
                    status = m.status ?? "SCHEDULED"
                }).ToList();
                
                List<Fixture> normalizeFixturesData = rawFixtures.Select(f=>new Fixture()
                {
                    id = f.id,
                    assist_player_id = f.assist_player_id ?? "",
                    match_id = f.match_id,
                    minute = f.minute,
                    player_id = f.player_id ??"",
                    team_id = f.team_id,
                    type = f.type ??""
                }).ToList();
                
                
                

                foreach (var p in normalizePlayersData)
                {
                    p.stats.goals = 0;
                    p.stats.assists = 0;
                    p.stats.yellow_cards = 0;
                    p.stats.red_cards = 0;
                    p.stats.own_goals = 0;
                    p.stats.penalties_missed = 0;
                    p.stats.clean_sheets = 0;
                    p.stats.saves = 0;
                }
                
                foreach (var incomingFixture in normalizeFixturesData)
                {
                    var player = normalizePlayersData.FirstOrDefault(p => p.id == incomingFixture.player_id);
                    if (player != null && player.stats != null)
                    {
                        switch (incomingFixture.type?.ToLower())
                        {
                            case "goal": player.stats.goals++; break;
                            case "yellow_card": player.stats.yellow_cards++; break;
                            case "red_card": player.stats.red_cards++; break;
                            case "own_goal": player.stats.own_goals++; break;
                            case "penalty_miss": player.stats.penalties_missed++; break;
                        }
                    }

                    if (incomingFixture.minute > player.stats.minutes_played)
                    {
                        player.stats.minutes_played = incomingFixture.minute;
                    }
                    
                    if (!string.IsNullOrEmpty(incomingFixture.assist_player_id))
                    {
                        var assistPlayer = normalizePlayersData.FirstOrDefault(p => p.id == incomingFixture.assist_player_id);
                        if (assistPlayer != null && assistPlayer.stats != null)
                            assistPlayer.stats.assists++;
                    }
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
                    
                    //Users
                    var incomingUserIds = rawUsers.Select(u=> u.id).ToList();
                    var dbUsers = await dbContext.Users.Where(u=>incomingUserIds.Contains(u.id)).ToListAsync(stoppingToken);
                    
                    //Squads
                    var incomingSquadIds = rawSquads.Select(s => s.Id).ToList();
                    var dbSquads = await dbContext.Squads.Where(s => incomingSquadIds.Contains(s.Id)).ToListAsync(stoppingToken);
                    
                    //Teams
                    var incomingTeamIds = rawTeams.Select(t => t.id).ToList();
                    var dbTeams = await dbContext.Teams.Where(t => incomingTeamIds.Contains(t.id)).ToListAsync(stoppingToken);
                    
                    var playersToPublish = new List<Player>();
                    var matchesToPublish = new List<Match>();
                    
                    
                    foreach (var incomingPlayer in normalizePlayersData)
                    {
                        var dbPlayer = dbPlayers.FirstOrDefault(p => p.id == incomingPlayer.id);
                        if(dbPlayer == null)
                        {
                            await dbContext.Players.AddAsync(incomingPlayer, stoppingToken);
                            playersToPublish.Add(incomingPlayer);
                            _logger.LogInformation($"A new player has been added to the list: {incomingPlayer.name}");
                        }
                        
                        
                        else
                        {
                            //Idempotency checking
                             if (dbPlayer.name != incomingPlayer.name ||
                                 dbPlayer.position != incomingPlayer.position ||
                                 dbPlayer.team_id != incomingPlayer.team_id ||
                                 dbPlayer.price != incomingPlayer.price ||
                                 dbPlayer.total_points != incomingPlayer.total_points ||
                                 dbPlayer.stats.minutes_played != incomingPlayer.stats.minutes_played ||
                                 dbPlayer.stats.clean_sheets != incomingPlayer.stats.clean_sheets ||
                                 dbPlayer.stats.yellow_cards != incomingPlayer.stats.yellow_cards ||
                                 dbPlayer.stats.red_cards != incomingPlayer.stats.red_cards ||
                                 dbPlayer.stats.saves != incomingPlayer.stats.saves ||
                                 dbPlayer.stats.assists != incomingPlayer.stats.assists ||
                                 dbPlayer.stats.penalties_missed != incomingPlayer.stats.penalties_missed||
                                 dbPlayer.stats.goals != incomingPlayer.stats.goals || dbPlayer.stats.own_goals != incomingPlayer.stats.own_goals ||
                                 dbPlayer.total_points == 0)
                             {
                                 dbPlayer.name = incomingPlayer.name;
                                 dbPlayer.position = incomingPlayer.position;
                                 dbPlayer.team_id = incomingPlayer.team_id;
                                 dbPlayer.price = incomingPlayer.price;
                                 dbPlayer.total_points = incomingPlayer.total_points;
                                 dbPlayer.stats = incomingPlayer.stats;
                                 
                                 playersToPublish.Add(dbPlayer);
                                 _logger.LogInformation($"The player has been updated and added to the list: {dbPlayer.name}");

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
                                
                                matchesToPublish.Add(dbMatch);
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
                                dbFixture.type = incomingFixture.type ?? "";
                                dbFixture.player_id = incomingFixture.player_id ?? "";
                                dbFixture.assist_player_id = incomingFixture.assist_player_id ?? "" ;
                                dbFixture.team_id = incomingFixture.team_id;
                            
                        }
                        
                    }
                    
                    
                    foreach (var incomingUser in rawUsers)
                    {
                        var dbUser = dbUsers.FirstOrDefault(u => u.id == incomingUser.id);
                        if (dbUser == null)
                        {
                            await dbContext.Users.AddAsync(incomingUser, stoppingToken);
                        }

                        else if (dbUser.username != incomingUser.username)
                        {
                            dbUser.username = incomingUser.username;
                        }
                    }

                    foreach (var incomingSquad in rawSquads )
                    {
                        var dbSquad = dbSquads.FirstOrDefault(s => s.user_id == incomingSquad.user_id);
                        if (dbSquad == null)
                        {
                            
                            if (string.IsNullOrWhiteSpace(incomingSquad.SquadName))
                            {   
                                incomingSquad.SquadName = $"FC {incomingSquad.user_id}";
                            }
                            await dbContext.Squads.AddAsync(incomingSquad, stoppingToken);
                        }

                        else
                        {
                            if (dbSquad.SquadName != incomingSquad.SquadName ||
                                dbSquad.TotalPoints != incomingSquad.TotalPoints)
                            {
                                dbSquad.SquadName = incomingSquad.SquadName;
                                dbSquad.TotalPoints = incomingSquad.TotalPoints;
                            }
                        }
                    }
                    
                    foreach (var incomingTeam in rawTeams)
                    {
                        var dbTeam = dbTeams.FirstOrDefault(t => t.id == incomingTeam.id);
                        if (dbTeam == null)
                        {
                            await dbContext.Teams.AddAsync(incomingTeam, stoppingToken);
                        }
                        else if (dbTeam.name != incomingTeam.name)
                        {
                            dbTeam.name = incomingTeam.name;
                        }
                    }
                    
                    
                    await dbContext.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation($"Publishing {playersToPublish.Count} player stats.");
                    
                    foreach (var player in playersToPublish)
                    {
                        await publishEndpoint.Publish<IPlayerStatUpdated>(new
                        {
                            Id = player.id.ToString(),
                            TotalPoints = player.total_points,
                            Stats = JsonSerializer.Serialize(player.stats)
                        },stoppingToken);
                        _logger.LogInformation($"RabbitMQ Event Published: PlayerStatUpdated for {player.name}");
                    }

                    foreach (var match in matchesToPublish)
                    {
                        await publishEndpoint.Publish<IMatchUpdated>(new
                        {
                            MatchId = match.id, 
                            MatchScore = match.Score,
                            Minute = match.minute,
                            Status = match.status
                        },stoppingToken);
                        _logger.LogInformation($"RabbitMQ Event Published: MatchUpdated for Match ID: {match.id}");
                        
                    }
                    
                    await dbContext.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Outbox events have been committed to the database. They are being sent to RabbitMQ...");
                    
                }
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured during worker run.");
                

            }
            
        }
    }
    
  

    


    private async Task <T?> ReadAndDeserializeAsync<T>(string fileName, CancellationToken cancellationToken) 
        where T : class, new()
    {
        
        

            try
            {
                string filePath = Path.Combine(AppContext.BaseDirectory, "mock-data", fileName);

                if (!File.Exists(filePath))
                {
                    _logger.LogWarning($"Path {filePath} not found.");
                    return new T();
                }

                string jsonString = await File.ReadAllTextAsync(filePath, cancellationToken);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return  JsonSerializer.Deserialize<T>(jsonString, options);
                
                
                
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"{fileName} file could not be read.");
                return new T();

            }
    }
    }



public class PlayersWrapper
{
    [JsonPropertyName("players")]
    public List<Player>? Players { get; set; } = new();
}

public class MatchesWrapper
{
    [JsonPropertyName("matches")]
    public List<Match>? Matches { get; set; } = new();
}

public class FixturesWrapper
{
    [JsonPropertyName("fixtures")]
    public List<Fixture>? Fixtures { get; set; } = new();
}

public class UsersWrapper
{
    [JsonPropertyName("users")]
    public List<User>? Users { get; set; } = new();
}

public class SquadsWrapper
{
    [JsonPropertyName("squads")]
    public List<Squad>? Squads { get; set; } = new();
}

public class TeamsWrapper
{
    [JsonPropertyName("teams")] 
    public List<Team>? Teams { get; set; } = new();

}