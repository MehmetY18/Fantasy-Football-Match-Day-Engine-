
using System.Text.Json.Serialization;

namespace MatchApi.Models;


public class OpenApiError
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    public OpenApiError() { }

    public OpenApiError(string code, string message)
    {
        Code = code;
        Message = message;
    }
}



public class ApiResponse<T>
{
    [JsonPropertyName("opcode")]
    public int Opcode  { get; set; }
    
    [JsonPropertyName("requestId")]
    public string RequestId { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = "success";
    
    [JsonPropertyName("data")]
    public T? Data { get; set; }
    
    [JsonPropertyName("error")]
    public OpenApiError? Error { get; set; }
    
    
    

    public static ApiResponse<T> Success(int opcode, string requestId, T data)
    {
        return new ApiResponse<T>
        {
            Opcode = opcode,
            RequestId = requestId,
            Status = "success",
            Data = data,
            Error = null
        };
    }

    public static ApiResponse<T> Failure(string requestId, string errorCode, string errorMessage, int opcode = 9999)
    {

        return new ApiResponse<T>
        {
            Opcode = opcode,
            RequestId = requestId,
            Status = "error",
            Data = default,
            Error = new OpenApiError
            {
                Code = errorCode,
                Message = errorMessage,
            }
        };
    }
}

public class GetMatchesResponseDto
{
    public List<MatchDto> Matches { get; set; } = new();
}

public class MatchScoreDto
{
    public int Home { get; set; }
    public int Away { get; set; }
}

public class MatchDto
{
    public string MatchId { get; set; } = string.Empty;
    public int Gameweek { get; set; }
    public string HomeTeamId { get; set; } = string.Empty;
    public string AwayTeamId { get; set; } = string.Empty;
    public MatchScoreDto? Score { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime KickoffTime { get; set; }
}

public class SquadPlayerInputDto
{
    public string PlayerId { get; set; } = string.Empty;
    public bool IsCaptain { get; set; }
    public bool IsViceCaptain { get; set; }
    
    public string position_string {get; set;} = string.Empty;
}

public class SaveSquadResponseDto
{
    public string SquadId { get; set; } = string.Empty;
    public DateTime SavedAt { get; set; }
}

public class MyScoreResponseDto
{
    public int TotalSquadPoints { get; set; }
    public int GameweekPoints { get; set; }
    public int Rank { get; set; }
}

// public class PlayerScoreDto
// {
//     public int PlayerId { get; set; }
//     public string PlayerName { get; set; } = string.Empty;
//     public int TotalPoints { get; set; }
//     public bool IsCaptain { get; set; } 
//     public bool IsBench { get; set; }  
//     public int Multiplier { get; set; }
// }

public class PlayerInfoDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string TeamId { get; set; } = string.Empty;
}

public class PlayerStatsDto
{
    public int GameWeek {get; set;}
    public int Goals { get; set; }
    public int Assists { get; set; }
    public int YellowCards { get; set; }
    public int RedCards { get; set; }
    public int MinutesPlayed { get; set; }
    public int CleanSheets { get; set; }
    public int OwnGoals { get; set; }
    public int PenaltiesMissed { get; set; }
    public int Saves { get; set; }
}

public class GetPlayerStatsResponseDto
{
    public PlayerInfoDto Player { get; set; } = new();
    public PlayerStatsDto Stats { get; set; } = new();
    
    [JsonPropertyName("fantasy_points")]
    public int FantasyPoints { get; set; }
}

public class LeaderBoardResponseDto
{
    [JsonPropertyName("total_count")]
    public long TotalCount { get; set; }
    
    [JsonPropertyName("entries")]
    public List<LeaderBoardItemDto> Entries { get; set; } = new();
}

public class LeaderBoardItemDto
{
    public int Rank { get; set; }
    
    [JsonPropertyName("user_id")]
    public string UserId { get; set; }
    public string UserName { get; set; }
    public int TotalPoints { get; set; }
}

public class GetMyRankResponseDto
{
    public long Rank { get; set; }
    public double? TotalPoints { get; set; }
    public decimal Percentile  { get; set; }
    
}

public class HeartbeatResponseDto
{
    public DateTime Timestamp { get; set; }
    public string ServerTime { get; set; }
}

