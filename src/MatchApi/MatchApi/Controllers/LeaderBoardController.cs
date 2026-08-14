using System.Text.Json;
using Data;
using MassTransit.Initializers;
using MatchApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DomainModels;
using StackExchange.Redis;

namespace MatchApi.Controllers;

[ApiController]
[Route("api/leaderboards")]
public class LeaderBoardController : Controller
{
    private readonly IDatabase _redisDb;
    private readonly AppDbContext _dbContext;
    
    public LeaderBoardController(AppDbContext dbContext, IDatabase redisDb)
    {
        _dbContext = dbContext;
        _redisDb = redisDb;
    }
    
    

    [HttpGet]
    public async Task<IActionResult> GetLeaderBoard(
        [FromQuery] string? leagueId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromHeader(Name = "X Request-Id")] string? requestId = "")
    {
        
        var reqId = string.IsNullOrEmpty(requestId) ? Guid.NewGuid().ToString() : requestId;

        
        if ( page <= 0 || pageSize <= 0)
        {
            return BadRequest(ApiResponse<object>.Failure(
                reqId,
                "Invalid payload",
                "The page / page number cannot be zero or less than zero "
            ));
        }
        
        string leagueKey = string.IsNullOrEmpty(leagueId) ? "global" : leagueId;
        string responseCacheKey = $"cache:leaderboard:{leagueKey}:p{page}:s{pageSize}";
        
        //String Cache
        var cachedResponse = await _redisDb.StringGetAsync(responseCacheKey);
        if (!cachedResponse.IsNull)
        {
            var cachedData = JsonSerializer.Deserialize<LeaderBoardResponseDto>(cachedResponse.ToString());
            return Ok(ApiResponse<LeaderBoardResponseDto>.Success(3001, reqId, cachedData));
        }
        
        string redisKey = string.IsNullOrEmpty(leagueId)
            ? "leaderboard:global"
            : $"leaderboard:league:{leagueId}";

        var topUsers = await _redisDb.SortedSetRangeByRankWithScoresAsync(
            redisKey, 
            (page -1)*pageSize,
            ((page -1)*pageSize) + pageSize -1 , 
            Order.Descending);
            //Start = (Page - 1) * PageSize
            //End = Start + PageSize - 1
            
        long totalCount = await _redisDb.SortedSetLengthAsync(redisKey);
        
        //Here we filter the IDs
        var userIds = topUsers.Select(x =>x.Element.ToString()).ToList();
        
        
        //REDIS HASH 
        var userDictionary = new Dictionary<string, string>();
        var missingUserIds = new List<string>();

        foreach (var userId in userIds)
        {
            var cachedName = await _redisDb.HashGetAsync($"user:{userId}", "username");
            if (!cachedName.IsNull)
            {
                userDictionary[userId] = cachedName.ToString();
            }

            else
            {
                missingUserIds.Add(userId);
            }
        }

        /*Names that could not be found in Redis were retrieved in bulk from the database,
         added to the dictionary and cached in Redis for subsequent requests.*/
        
        if (missingUserIds.Any())
        {
            var dbUsers = await _dbContext.Users
                .Where(u => missingUserIds.Contains(u.id))
                .Select(u => new { u.id, u.username }).ToListAsync();

            foreach (var user in dbUsers)
            {
                userDictionary[user.id] = user.username;
                await _redisDb.HashSetAsync($"user:{user.id}", "username", user.username);
                
            }
        }
            
       
        var leaderBoardList = topUsers.Select((s, index) => new LeaderBoardItemDto
        {
            Rank = ((page - 1) * pageSize) + index + 1,
            UserId = s.Element.ToString(),
            TotalPoints = (int)s.Score,
            UserName = userDictionary.TryGetValue(s.Element.ToString(), out var uName) ? uName : "Unknown Manager"

        }).ToList();
        

        var responseData = new LeaderBoardResponseDto
        {
            TotalCount = totalCount,
            Entries = leaderBoardList
        };
        
        await _redisDb.StringSetAsync(
            responseCacheKey,
            JsonSerializer.Serialize(responseData),
            TimeSpan.FromSeconds(5));
        
        return Ok(ApiResponse<LeaderBoardResponseDto>.Success(3001, reqId, responseData));
        
    }


    [HttpGet("users/{userId}/rank")]
    public async Task<IActionResult> GetMyRank(
        [FromRoute] string userId,
        [FromQuery] string? leagueId,
        [FromHeader(Name = "X-Request-Id")] string? requestId = "")
    {
        var reqId =  string.IsNullOrEmpty(requestId) ? Guid.NewGuid().ToString() : requestId;

        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(ApiResponse<object>.Failure(reqId,"Invalid Payload", "UserId cannot be empty"));
        }
        
        
        
        string redisKey = string.IsNullOrEmpty(leagueId)
            ? "leaderboard:global"
            : $"leaderboard:league:{leagueId}";

        
        long totalUsers =await _redisDb.SortedSetLengthAsync(redisKey);
        long? rank = await _redisDb.SortedSetRankAsync(redisKey, userId, Order.Descending);
        double? score = await _redisDb.SortedSetScoreAsync(redisKey, userId);

        long userRank = rank.HasValue ? rank.Value+1 : 0;

        decimal percentile = 0;
        if (totalUsers > 0 && userRank > 0)
        {
            percentile = ((decimal)userRank / totalUsers) * 100;
        }

        var responseData = new GetMyRankResponseDto
        {
            Rank = userRank,
            TotalPoints = score.HasValue ? (int )score.Value : 0,
            Percentile = percentile
        };
        
        return Ok(ApiResponse<GetMyRankResponseDto>.Success(3002, reqId, responseData));
        

    }
    
}