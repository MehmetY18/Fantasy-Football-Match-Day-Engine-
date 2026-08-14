using System.Text.Json.Serialization;

namespace MatchApi.Models;

public class ApiRequest<T>
{
    [JsonPropertyName("opcode")]
    public int OpCode { get; set; }
    
    [JsonPropertyName("requestId")]
    public Guid RequestId { get; set; } 
    
    [JsonPropertyName("payload")]
    public T? Payload { get; set; }
    
   
    
}

public class GetMatchesPayload
{
    public int? Gameweek { get; set; }
    public string? Status { get; set; }
}

public class LiveScorePayload
{
    public string MatchId { get; set; } = string.Empty;
}


public class SaveSquadPayload
{
    [JsonPropertyName("user_id")]
    public string userId { get; set; } 
    
    [JsonPropertyName("players")]
    public List<SquadPlayerInputDto> Players { get; set; } = new();
    
    [JsonPropertyName("gameweek")]
    public int Gameweek { get; set; }
    
}

public class GetMyScorePayload
{ 
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;
    public int? Gameweek {get; set;}
    
}

public class GetPlayerStatsPayload
{
    [JsonPropertyName("player_id")]
    public string PlayerId { get; set; } = string.Empty;
    public int? Gameweek {get; set;}
}

public class GetLeaderBoardPayload
{
    [JsonPropertyName("league_id")]
    public string? LeagueId { get; set; }
    
    [JsonPropertyName("page")]
    public int Page { get; set; }
    
    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }
}

public class GetMyRankPayload
{
    public string UserId { get; set; }
    public string? LeagueId { get; set; }
    
}

public class GetHeartbeatPayload
{
    public DateTime Timestamp { get; set; }
}