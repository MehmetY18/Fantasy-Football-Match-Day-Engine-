using Data;
using MatchApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchApi.Controllers;

[ApiController]
[Route("api/players")]
public class PlayerController : Controller
{
    private readonly AppDbContext _dbContext;
    
    public PlayerController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
        
    [HttpGet("{playerId}/stats")]
    public async Task<IActionResult> GetStats(
        [FromRoute] string playerId,
        [FromQuery] int? gameweek,
        [FromHeader(Name = "X-Request-Id")] string? requestId)
    {
        var reqId = string.IsNullOrEmpty(requestId) ? Guid.NewGuid().ToString() : requestId;

        if (string.IsNullOrEmpty(playerId))
        {
            return BadRequest(ApiResponse<object>.Failure(
                reqId, "Invalid Payload",
                "PlayerId must be provided in URL path."
                ));
        }
        
        
        var player = await _dbContext.Players
            .Include(p => p.stats)
            .FirstOrDefaultAsync(p => p.id == playerId);
        
        
       
        if (player == null)
        {
            return NotFound(ApiResponse<object>.Failure(
                reqId,
                "Player not found",
                $"No data was found for player id: {playerId} "
                ));
        }

        var responseData = new GetPlayerStatsResponseDto()
        {
           Player = new PlayerInfoDto
           {
               PlayerId = player.id,
               PlayerName = player.name,
               Position = player.position,
               TeamId =  player.team_id
           },
           
           Stats = new PlayerStatsDto
           {
               GameWeek = gameweek ?? 1,
               Goals = player.stats.goals,
               Assists = player.stats.assists,
               YellowCards =  player.stats.yellow_cards,
               RedCards = player.stats.red_cards,
               MinutesPlayed =  player.stats.minutes_played,
               CleanSheets =  player.stats.clean_sheets,
               OwnGoals = player.stats.own_goals,
               PenaltiesMissed = player.stats.penalties_missed,
               Saves = player.stats.saves
           },
           
           FantasyPoints = player.total_points
        };

        return Ok(ApiResponse<GetPlayerStatsResponseDto>.Success(2003, reqId, responseData));
        

    }
}