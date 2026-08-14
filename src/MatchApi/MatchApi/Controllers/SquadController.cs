using Data;
using MatchApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DomainModels;

namespace MatchApi.Controllers;

[ApiController]
[Route("api/squads")]
public class SquadController : Controller
{
    private readonly AppDbContext _dbContext;
    
    public SquadController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost]
    public async Task<IActionResult> SaveSquad(
        [FromBody] SaveSquadPayload payload,
        [FromHeader(Name = "X-Request-Id")] string? requestId)
    {
        var reqId = string.IsNullOrEmpty(requestId) ? Guid.NewGuid().ToString() : requestId;

        if (payload == null || payload.Players == null || !payload.Players.Any())
        {
            return BadRequest(ApiResponse<object>.Failure(reqId, "Invalid payload.","Players list cannot be empty."));
        }
        
        
        var playerIds = payload.Players.Select(p => p.PlayerId).ToList();

        var playerPositionsdict = await _dbContext.Players
            .Where(p => playerIds.Contains(p.id))
            .ToDictionaryAsync(p => p.id, p => p.position);
        
        var newSquad = new Squad()
        {
            Id = Guid.NewGuid().ToString(),
            SquadName = $"FC {payload.userId}",
            gameweek = payload.Gameweek,
            CreatedAt = DateTime.UtcNow,
            TotalPoints = 0,
            user_id =  payload.userId,
            Players = payload.Players.Select((p,index) => new SquadPlayer
            {
                player_id = p.PlayerId,
                is_captain = p.IsCaptain,
                is_vice_captain = p.IsViceCaptain,
                Multiplier = p.IsCaptain ? 2:1,
                is_bench = index >= 11,
                position_slot = playerPositionsdict.GetValueOrDefault(p.PlayerId, "DEFAULT")
            }).ToList()
        };
        
        _dbContext.Squads.Add(newSquad);
        await _dbContext.SaveChangesAsync();
        
        var responseData = new SaveSquadResponseDto
        {
            SquadId = newSquad.Id,
            SavedAt = DateTime.UtcNow
        };
        
        return Ok(ApiResponse<object>.Success(2001, reqId, responseData));
    }

    [HttpGet("users/{userId}/scores")]
    public async Task<IActionResult> GetMyScore(
         [FromRoute] string userId,
        [FromQuery] int? gameweek,
        [FromHeader(Name =  "X-Request-Id")] string? requestId)
    {
       
        var reqId = string.IsNullOrEmpty(requestId) ? Guid.NewGuid().ToString() : requestId;

        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(ApiResponse<object>.Failure(reqId, "Invalid payload", "userId cannot be empty"));
        }

        var query = _dbContext.Squads.Where(s => s.user_id == userId);

        if (gameweek.HasValue && gameweek.Value > 0)
        {
            query = query.Where(s => s.gameweek == gameweek.Value);
        }

        var squad = await query
            .Include(s => s.Players)
            .FirstOrDefaultAsync();        
        
        if (squad == null)
        {
            return NotFound(ApiResponse<object>.Failure(
                reqId,
                "Squad Not found.",
                "No data was found for the specified week."
                ));
        }


        var playerIds = squad.Players.Select(s => s.player_id).ToList();
        var playerPointDict = await _dbContext.Players
            .Where(p=> playerIds.Contains(p.id))
            .ToDictionaryAsync(p=> p.id, p => p.total_points);
     
        
        int calculatedGameweekPoints = 0;

        foreach (var sp in squad.Players)
        {
            if(sp.is_bench)
                continue;


            if (playerPointDict.TryGetValue(sp.player_id, out int playerPoints))
            {
                int multiplier = sp.is_captain ? 2 : 1;
                calculatedGameweekPoints += playerPoints * multiplier;
            }
        }
        
        var calculatedRank = await _dbContext.Squads
            .CountAsync(s=>s.TotalPoints > squad.TotalPoints) + 1;
        
        var responseData = new MyScoreResponseDto
        {
            TotalSquadPoints =  squad.TotalPoints,
            GameweekPoints = calculatedGameweekPoints,
            Rank = calculatedRank
        };
        return Ok(ApiResponse<MyScoreResponseDto>.Success(2002, reqId, responseData));

    }
}