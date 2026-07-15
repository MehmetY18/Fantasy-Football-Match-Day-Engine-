namespace Shared.DomainModels;

public class Match
{
    public string id { get; set; } 
    public string home_team_id { get; set; }
    public string away_team_id { get; set; }
    public DateTime kickoff  { get; set; }
    public string status { get; set; }
    public int gameweek { get; set; }
    public MatchScore Score { get; set; }
    public int? minute { get; set; }
}