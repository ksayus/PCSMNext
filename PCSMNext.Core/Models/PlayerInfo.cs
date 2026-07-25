namespace PCSMNext.Core.Models;

public class PlayerList
{
    public int OnlineCount { get; set; }
    public int MaxCount { get; set; }
    public List<string> Players { get; set; } = new();
}
