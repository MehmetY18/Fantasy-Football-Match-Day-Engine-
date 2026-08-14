using MassTransit;
using Shared.Events;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace LeaderboardWorker.Consumers;

public class ScoreCalculatedConsumer : IConsumer<IScoreCalculated>
{
    private readonly IDatabase _redisDb;
    private readonly  ILogger<ScoreCalculatedConsumer> _logger;
    
    public ScoreCalculatedConsumer(IConnectionMultiplexer redis, ILogger<ScoreCalculatedConsumer> logger)
    {
        _redisDb = redis.GetDatabase();
        _logger = logger;
    }
    
    public async Task Consume(ConsumeContext<IScoreCalculated> context)
    {
        var msg = context.Message;
        
        _logger.LogInformation($"[Redis Update] User: {msg.UserId} | League: {msg.LeagueId} | Total Points: {msg.TotalPoints}");

        
        //Redis ZSET
        //Global Leaderboard Table
        await _redisDb.SortedSetAddAsync("leaderboard:global", msg.UserId, msg.TotalPoints);
        
        //League Leaderboard Table
        if (!string.IsNullOrEmpty(msg.LeagueId))
        {
            var leagueKey = $"leaderboard:league:{msg.LeagueId}";
            await _redisDb.SortedSetAddAsync(leagueKey, msg.UserId, msg.TotalPoints);
        }
        


    }
}