using Data;
using MatchApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DomainModels;

namespace MatchApi.Controllers;

[ApiController]
[Route("api/matches")]
public class MatchesController(AppDbContext dbContext) : Controller
{
    private readonly AppDbContext _dbContext = dbContext;
    

    [HttpGet]
    public async Task<IActionResult> GetMatches(
        [FromQuery] int? gameweek,
        [FromQuery] string? status,
        [FromHeader(Name = "X Request-Id")] string? requestId)
    {
        var reqId = string.IsNullOrEmpty(requestId) ? Guid.NewGuid().ToString() : requestId;
        
        var query = _dbContext.Matches.AsQueryable();

        if (gameweek.HasValue)
        {
            query = query.Where(m => m.gameweek == gameweek);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(m => m.status == status);
        }
        
        var dbMatches = await query.ToListAsync();
        var responseData = new GetMatchesResponseDto
        {
            Matches = dbMatches.Select(m=> new MatchDto
            {
                MatchId = m.id.ToString(),
                Gameweek = m.gameweek,
                HomeTeamId = m.home_team_id,
                AwayTeamId = m.away_team_id,
                KickoffTime = m.kickoff,
                Status = m.status,
                Score = m.Score != null ? new MatchScoreDto
                {
                    Home = m.Score.home,
                    Away = m.Score.away
                } : null
            }).ToList()
        };
        
        return Ok(ApiResponse<GetMatchesResponseDto>.Success(1001, reqId, responseData));
    }

    [HttpPost("{matchId}/subscriptions")]
    public async Task<IActionResult> SubscribeLiveScore(
         string matchId,
         [FromHeader(Name = "X-Request-Id")] string? requestId)
    {
        var reqId = string.IsNullOrEmpty(requestId) ? Guid.NewGuid().ToString() : requestId;

        if (string.IsNullOrEmpty(matchId))
        {
            return BadRequest(ApiResponse<object>.Failure(reqId, "Invalid Payload", "MatchId cannot be empty."));
        }
        
        var match = await _dbContext.Matches.FirstOrDefaultAsync(m=> m.id == matchId);

        if (match == null)
        {
            return NotFound(ApiResponse<object>.Failure(reqId, "Match not found" ,"Specified Match not found"));
        }

        var eventType = await _dbContext.Fixtures
            .Where(e => e.match_id == matchId && e.minute <= match.minute)
            .OrderByDescending(e => e.minute)
            .FirstOrDefaultAsync();

        var updateData = new
        {
            subscribed = true,
            matchId = match.id,
            minute = match.minute,
            score = new
            {
                home = match.Score.home,
                away = match.Score.away
            },
            event_type = eventType?.type ?? "NONE",
            player_id = eventType?.player_id ?? "NONE"
        };
        
        return Ok(ApiResponse<object>.Success(1004, reqId, updateData));
    }

    [HttpDelete("{matchId}/subscriptions")]
    public async Task<IActionResult> UnsubscribeLiveScore(
        string matchId,
        [FromHeader(Name = "X-Request-Id")] string? requestId)
    {
        var reqId = string.IsNullOrEmpty(requestId) ? Guid.NewGuid().ToString() : requestId;

        var unsubscribedData = new
        {
            unsubscribed = true,
        };
        
        return Ok(ApiResponse<object>.Success(1004, reqId, unsubscribedData));
        
    }
    
}    


