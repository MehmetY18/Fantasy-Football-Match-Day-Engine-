namespace MatchApi.Models;

public class OpcodeModel
{
    public int code { get; set; }
    public string name { get; set; }
    public string direction { get; set; }
    public string description { get; set; }
    public PayloadSchema payloadSchema { get; set; }
    public List<string> response_fields { get; set; }
}

