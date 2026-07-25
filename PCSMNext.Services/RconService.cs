using CoreRCON;
using PCSMNext.Core;
using PCSMNext.Core.Models;
using Serilog;
using System.Net;
using System.Text.RegularExpressions;

namespace PCSMNext.Services;

public class RconService
{
    /// <summary>
    /// Calculate the RCON port
    /// </summary>
    /// <param name="serverPort"></param>
    /// <param name="portOffset"></param>
    /// <returns>RCON port</returns>
    public int CalculateRconPort(int serverPort, int portOffset = 10)
    {
        var port = serverPort + portOffset;
        if (port > 65535)
        {
            port = serverPort - portOffset;
            if ((port > 65535) || (port < 1))
            {
                Log.Warning("Can`t auto calculate RCON port, server-port={SP}", serverPort);
                // Use default RCON port.
                port = 25575;
            }
        }
        return port;
    }

    /// <summary>
    /// Execute RCON command.
    /// </summary>
    /// <param name="host"></param>
    /// <param name="port"></param>
    /// <param name="password"></param>
    /// <param name="command"></param>
    /// <returns>RCON execute result.</returns>
    public async Task<RconResult> ExecuteCommandAsync(
        string host, int port, string password, string command)
    {
        try
        {
            // Parameter check
            if (port < 1 || port > 65535)
            {
                return new RconResult { Success = false, Error = $"Port illegally: {port}" };
            }
            if (string.IsNullOrEmpty(password))
            {
                return new RconResult { Success = false, Error = "Password can`t be empty." };
            }

            using var rcon = new RCON(IPAddress.Parse(host), (ushort)port, password);
            await rcon.ConnectAsync();

            var response = await rcon.SendCommandAsync(command);
            Log.Debug("RCON command executed: {Cmd} -> {Resp}", command, response);

            return new RconResult { Success = true, Response = response };
        }
        catch(Exception ex)
        {
            Log.Warning(ex, "Failed to execute RCON command:{Host}:{Port}{Cmd}", host, port, command);
            return new RconResult { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Get online players list.
    /// </summary>
    /// <param name="host"></param>
    /// <param name="port"></param>
    /// <param name="password"></param>
    /// <returns>Players list, also contain online players count and max players count.</returns>
    public async Task<PlayerList> GetOnlinePlayersAsync(
        string host, int port, string password)
    {
        var result = await ExecuteCommandAsync(host, port, password, "list");

        if (!result.Success)
            return new PlayerList();

        // Match format: "There are X of a max of Y players online: player1, player2"
        var match = Regex.Match(result.Response,
            @"There are (\d+) of a max of (\d+) players online:?\s*(.*)");

        var playerList = new PlayerList();
        if (match.Success)
        {
            playerList.OnlineCount = int.Parse(match.Groups[1].Value);
            playerList.MaxCount = int.Parse(match.Groups[2].Value);
            // Trim() can remove empty str.
            var playersStr = match.Groups[3].Value.Trim();
            if (!string.IsNullOrEmpty(playersStr))
            {
                playerList.Players = playersStr
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .ToList();
            }
        }

        return playerList;
    }

    public async Task<bool> IsServerOnlineAsync(
        string host, int port, string password, string customCmd = "list")
    {
        try
        {
            using var rcon = new RCON(IPAddress.Parse(host), (ushort)port, password);
            await rcon.ConnectAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}