namespace Shared.Events;

public interface IScoreCalculated
{
    string UserId { get; }
    string? LeagueId { get; }
    int TotalPoints { get; }
    DateTime CalculatedAt { get; }
}