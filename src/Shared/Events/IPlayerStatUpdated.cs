namespace Shared.Events;

public interface IPlayerStatUpdated
{ 
     string Id { get; }
     int TotalPoints { get;}
     string Stats  { get; }
}