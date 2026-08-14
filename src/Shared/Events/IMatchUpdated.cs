namespace Shared.Events;

public interface IMatchUpdated
{
     string MatchId { get;}
     int Minute { get; }
     int Score { get; }
     string Status { get; }
}