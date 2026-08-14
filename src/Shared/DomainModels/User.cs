namespace Shared.DomainModels;

public class User
{
    public string id { get; set; } = string.Empty;
    public string username { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string league_id { get; set; } = string.Empty;
    public int total_points { get; set; } = 0;

}