namespace Shared.DomainModels;

public class Player
{
    public string id { get; set; }
    public string name { get; set; }
    public string position { get; set; }
    public string team_id  { get; set; }
    public  decimal price { get; set; }
    public int total_points { get; set; }
    public PlayerStats stats { get; set; }
}