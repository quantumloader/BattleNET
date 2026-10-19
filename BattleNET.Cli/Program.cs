using System.CommandLine;
using BattleNET;
using BattleNET.Commands;

namespace BattleNET.Cli;

internal static class Program
{
    private static readonly List<string> History = [];

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var hostOption = new Option<string>("--host")
        {
            Description = "RCon host name or IP address",
            Required = false,
        };
        var portOption = new Option<int?>("--port", "-p")
        {
            Description = "RCon UDP port",
            Required = false,
        };
        var passwordOption = new Option<string>("--password")
        {
            Description = "RCon password",
            Required = false,
        };
        var commandOption = new Option<string?>("--command", "-c")
        {
            Description = "Send a single command and exit",
            Required = false,
        };
        var reconnectOption = new Option<bool>("--reconnect")
        {
            Description = "Automatically reconnect after connection loss",
            DefaultValueFactory = _ => false,
        };

        var root = new RootCommand("BattleNET v2 — BattlEye RCon client")
        {
            hostOption,
            portOption,
            passwordOption,
            commandOption,
            reconnectOption,
        };

        root.SetAction(async (parseResult, cancellationToken) =>
        {
            string? host = parseResult.GetValue(hostOption);
            int? port = parseResult.GetValue(portOption);
            string? password = parseResult.GetValue(passwordOption);
            string? command = parseResult.GetValue(commandOption);
            bool reconnect = parseResult.GetValue(reconnectOption);

            if (string.IsNullOrWhiteSpace(host) || port is null or <= 0 || string.IsNullOrEmpty(password))
            {
                (host, port, password) = await PromptCredentialsAsync(host, port, password)
                    .ConfigureAwait(false);
            }

            if (string.IsNullOrWhiteSpace(host) || port is null or <= 0 || string.IsNullOrEmpty(password))
            {
                WriteError("Host, port and password are required.");
                return 1;
            }

            var credentials = new BattlEyeCredentials(host, port.Value, password);
            var options = new BattlEyeClientOptions { ReconnectOnPacketLoss = reconnect };

            await using var client = new BattlEyeClient(credentials, options);
            client.MessageReceived += (_, e) => WriteMessage(e);
            client.Connected += (_, e) => WriteStatus(e.Message, e.Result == BattlEyeConnectionResult.Success);
            client.Disconnected += (_, e) => WriteStatus(e.Message, success: false);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
            };

            BattlEyeConnectionResult result = await client.ConnectAsync(cts.Token).ConfigureAwait(false);
            if (result != BattlEyeConnectionResult.Success)
            {
                return 2;
            }

            Console.Title = $"BattleNET v2 — {host}:{port}";

            if (!string.IsNullOrWhiteSpace(command))
            {
                try
                {
                    string reply = await client.SendCommandAndWaitAsync(command, cancellationToken: cts.Token)
                        .ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(reply))
                    {
                        Console.WriteLine(reply);
                    }
                }
                catch (Exception ex)
                {
                    WriteError(ex.Message);
                    return 3;
                }

                await client.DisconnectAsync(cts.Token).ConfigureAwait(false);
                return 0;
            }

            await RunInteractiveAsync(client, cts.Token).ConfigureAwait(false);
            await client.DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
            return 0;
        });

        ParseResult parseResult = root.Parse(args);
        return await parseResult.InvokeAsync().ConfigureAwait(false);
    }

    private static async Task RunInteractiveAsync(BattlEyeClient client, CancellationToken cancellationToken)
    {
        WriteStatus("Interactive mode. Type a command, or 'exit' / 'logout' to quit.", success: true);
        WriteStatus("Shortcuts: /players, /bans, /history", success: true);

        while (!cancellationToken.IsCancellationRequested && client.IsConnected)
        {
            Console.Write("> ");
            string? line = await Console.In.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                break;
            }

            line = line.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line is "exit" or "logout" or "quit")
            {
                break;
            }

            if (line is "/history")
            {
                foreach (string item in History)
                {
                    Console.WriteLine(item);
                }

                continue;
            }

            try
            {
                if (line is "/players")
                {
                    var players = await client.GetPlayersAsync(cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    foreach (BattlEyePlayer player in players)
                    {
                        Console.WriteLine(
                            $"{player.Id,3}  {player.Name,-24}  {player.Guid}  {player.Ip}:{player.Port}  ping={player.Ping}"
                            + (player.IsLobby ? " (Lobby)" : string.Empty));
                    }

                    History.Add(BattlEyeCommand.Players);
                    continue;
                }

                if (line is "/bans")
                {
                    var bans = await client.GetBansAsync(cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    foreach (BattlEyeBan ban in bans)
                    {
                        string duration = ban.Minutes == 0 ? "perm" : $"{ban.Minutes}m";
                        Console.WriteLine($"{ban.Id,3}  {ban.GuidOrIp}  {duration}  {ban.Reason}");
                    }

                    History.Add(BattlEyeCommand.Bans);
                    continue;
                }

                History.Add(line);
                string reply = await client.SendCommandAndWaitAsync(line, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                if (!string.IsNullOrEmpty(reply))
                {
                    Console.WriteLine(reply);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                WriteError(ex.Message);
                if (!client.IsConnected)
                {
                    break;
                }
            }
        }
    }

    private static async Task<(string Host, int? Port, string Password)> PromptCredentialsAsync(
        string? host,
        int? port,
        string? password)
    {
        while (string.IsNullOrWhiteSpace(host))
        {
            Console.Write("Enter IP address or hostname: ");
            host = (await Console.In.ReadLineAsync().ConfigureAwait(false))?.Trim();
        }

        while (port is null or <= 0)
        {
            Console.Write("Enter port number: ");
            string? input = await Console.In.ReadLineAsync().ConfigureAwait(false);
            if (int.TryParse(input, out int value) && value is > 0 and <= 65535)
            {
                port = value;
            }
        }

        while (string.IsNullOrEmpty(password))
        {
            Console.Write("Enter RCon password: ");
            password = await ReadPasswordAsync().ConfigureAwait(false);
            Console.WriteLine();
        }

        return (host, port, password);
    }

    private static Task<string> ReadPasswordAsync()
    {
        var password = new System.Text.StringBuilder();
        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                break;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0)
                {
                    password.Length--;
                    Console.Write("\b \b");
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                password.Append(key.KeyChar);
                Console.Write('*');
            }
        }

        return Task.FromResult(password.ToString());
    }

    private static void WriteMessage(BattlEyeMessageEventArgs args)
    {
        if (string.IsNullOrEmpty(args.Message))
        {
            return;
        }

        ConsoleColor color = args.Id == BattlEyeMessageEventArgs.ServerMessageId
            ? ConsoleColor.Cyan
            : ConsoleColor.Gray;

        var previous = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(args.Message);
        Console.ForegroundColor = previous;
    }

    private static void WriteStatus(string message, bool success)
    {
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = success ? ConsoleColor.Green : ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ForegroundColor = previous;
    }

    private static void WriteError(string message)
    {
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine(message);
        Console.ForegroundColor = previous;
    }
}
