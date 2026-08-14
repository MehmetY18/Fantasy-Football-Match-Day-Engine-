namespace Shared.DomainModels;

public class Squad
{
    public string? SquadName { get; set; }   
    public virtual User? User { get; set; } 
    public int TotalPoints { get; set; }
    public string Id { get; set; } = string.Empty;
    public string user_id { get; set; } = string.Empty;
    public int gameweek { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<SquadPlayer> Players { get; set; } = new();
}