using System.Data.SqlTypes;
using System.Text.Json;
using MassTransit;
using Data;
using Shared.Events;
using MatchDataIngestion;
using Microsoft.EntityFrameworkCore;
using Shared.DomainModels;


namespace ScoringWorker.Consumers;

public class PlayerStatUpdatedConsumer : IConsumer<IPlayerStatUpdated>
{
    private readonly ILogger<PlayerStatUpdatedConsumer> _logger;
    private readonly AppDbContext _dbContext;
    
    
    public PlayerStatUpdatedConsumer(
        ILogger<PlayerStatUpdatedConsumer> logger, 
        AppDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
        
    }

    public async Task Consume(ConsumeContext<IPlayerStatUpdated> context)
    {
        var message = context.Message;
        _logger.LogInformation($"PlayerStatUpdated event recived for Player ID: {message.Id}");


        var player = await _dbContext.Players.FirstOrDefaultAsync(p => p.id == message.Id);
        if (player == null)
        {
            _logger.LogWarning($"Player not found for Player ID: {message.Id}");
            return;
        }

        PlayerStatsDTO stats = ParseStats(message.Stats);
        
        
        player.stats.goals = stats.goals;
        player.stats.assists = stats.assists;
        player.stats.yellow_cards = stats.yellow_cards;
        player.stats.red_cards = stats.red_cards;
        player.stats.minutes_played = stats.minutes_played;
        player.stats.clean_sheets = stats.clean_sheets;
        player.stats.own_goals = stats.own_goals;
        player.stats.penalties_missed = stats.penalties_missed;
        
        

        int calculatedPoints = CalculateFantasyPoints(player.position, message.Stats);
        player.total_points = calculatedPoints;
        
        

        _logger.LogInformation($"Score for Player Name: {player.name} has changed ({calculatedPoints} pts.)");

        await _dbContext.SaveChangesAsync();
        
        var squads = await _dbContext.Squads
            .Include(s => s.User)
            .Include(s => s.Players)
            .Where(s => s.Players.Any(sp => sp.player_id == player.id))
            .ToListAsync();

        if (squads.Any())
        {
            foreach (var squad in squads)
            {
                if(squad.User == null)
                {
                    continue;
                }

               
                
                var squadPlayerIds = squad.Players.Select(sp => sp.player_id).ToList();

                var squadTotalPoints = await _dbContext.Players
                    .Where(p => squadPlayerIds.Contains(p.id))
                    .SumAsync(p => p.total_points);
                
                squad.TotalPoints = squadTotalPoints;
                squad.User.total_points = squadTotalPoints;
                
                //ScoreCalculated Event Publish
                await context.Publish<IScoreCalculated>(new
                {
                    UserId = squad.User.id,
                    LeagueId = squad.User.league_id,
                    TotalPoints = squad.User.total_points,
                    CalculatedAt = DateTime.UtcNow
                });
                _logger.LogInformation($"ScoreCalculated event published for User ID: {squad.User.id}, Total Points: {squad.User.total_points}");
            }
        }
        await _dbContext.SaveChangesAsync();
    }

    private int CalculateFantasyPoints(string position, string statsJson)
    {
        PlayerStatsDTO stats = ParseStats(statsJson);
        
        int points = 0;
        string positionString = position ?? "";
        string assists = stats.assists.ToString();

        if (positionString == "FW")
            points += stats.goals * 6;
        else if (positionString == "MF")
            points += stats.goals * 5;
        else if (positionString == "DF" || positionString == "GK")
            points += stats.goals * 6;
        
        if (stats.assists > 0)
            points += stats.assists * 3;

        if (stats.clean_sheets > 0)
        {
            if (positionString == "GK" || positionString == "DF")
                points += 4;
            else
                points += 1;
        }

        if (stats.yellow_cards > 0)
            points -=  stats.yellow_cards * 1;
        

        if (stats.red_cards > 0)
            points -= stats.red_cards * 3;
        
        
        if(stats.penalties_missed > 0)
            points -= stats.penalties_missed * 2 ;
        
        if (stats.own_goals > 0)
            points -= stats.own_goals * 2;

        if (stats.minutes_played >= 60)
            points += 2;
        
        return points;

    }
    
    public class PlayerStatsDTO
    {
        public int goals {get; set;}
        public int assists { get; set; }
        public int yellow_cards { get; set; }
        public int red_cards { get; set; }
        public int minutes_played { get; set; }
        public int clean_sheets { get; set; }
        public int own_goals {get; set;}
        public int penalties_missed { get; set; }
        
    }

    private PlayerStatsDTO ParseStats(string statjson)
    {
        if (string.IsNullOrWhiteSpace(statjson))
            return new PlayerStatsDTO();

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            return JsonSerializer.Deserialize<PlayerStatsDTO>(statjson, options) ?? new PlayerStatsDTO();
        }
        catch (Exception e)
        {
            _logger.LogError($"Json Deserialization failed {e.Message} ");
            return new PlayerStatsDTO();
        }
    }
}

