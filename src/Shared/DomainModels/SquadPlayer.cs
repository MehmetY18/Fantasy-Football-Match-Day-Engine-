namespace Shared.DomainModels;

public class SquadPlayer
{
    public int Id { get; set; }
    public string SquadId { get; set; } = string.Empty;
    public string player_id { get; set; } = string.Empty;
    public string position_slot  { get; set; } = string.Empty;
    public bool is_captain { get; set; }
    public bool is_vice_captain { get; set; }
    public bool is_bench { get; set; }
    public int Multiplier { get; set; }
}