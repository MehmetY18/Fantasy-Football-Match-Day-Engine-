using Data;
using MatchApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace MatchApi.Controllers;

[ApiController]
[Route("api/health")]
public class BaseController : Controller
{
   private readonly AppDbContext _dbContext;
   
   public BaseController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetHeartbeat(
        [FromQuery] DateTime? timestamp,
        [FromHeader(Name = "X-Request-Id")] string? requestId)
    {
        var reqId = string.IsNullOrEmpty(requestId) ? Guid.NewGuid().ToString() : requestId;

        if (!timestamp.HasValue || timestamp.Value == default)
        {
            return BadRequest(ApiResponse<object>.Failure(
                reqId,
                "Invalid payload.",
                "Timestamp cannot be null or empty"
            ));
        }
        
        var response = new HeartbeatResponseDto
        {
            Timestamp = timestamp.Value,
            ServerTime = DateTime.UtcNow.ToString("O")
        };



        return Ok(ApiResponse<HeartbeatResponseDto>.Success(9000, reqId, response));
    }
    
    
}