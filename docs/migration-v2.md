# Migrating from BattleNET 1.3.x to 2.0

v2 is a **breaking rewrite**. There is no compatibility shim.

## Package / project layout

| 1.3.x | 2.0 |
|-------|-----|
| Single `BattleNET` (netstandard2.0) | `BattleNET.Core` + `BattleNET.Commands` (net10.0) |
| `BattleNET client` (.NET Framework 4.6.1) | `BattleNET.Cli` |

```bash
dotnet add package BattleNET.Core
dotnet add package BattleNET.Commands
```

## Connect / disconnect

```csharp
// 1.3.x
var b = new BattlEyeClient(loginCredentials);
b.BattlEyeMessageReceived += ...;
b.Connect();
b.Disconnect();

// 2.0
await using var client = new BattlEyeClient(credentials, options);
client.MessageReceived += ...;
await client.ConnectAsync();
await client.DisconnectAsync();
```

## Credentials

```csharp
// 1.3.x
new BattlEyeLoginCredentials { Host = ip, Port = 2302, Password = "..." };

// 2.0
new BattlEyeCredentials("127.0.0.1", 2302, "...");
// or
new BattlEyeCredentials(ipAddress, 2302, "...");
```

## Commands

```csharp
// 1.3.x
b.SendCommand("players");
b.SendCommand(BattlEyeCommand.Players); // enum + DescriptionAttribute

// 2.0
await client.SendCommandAsync("players");
await client.SendCommandAndWaitAsync("players");
await client.GetPlayersAsync(); // Commands package
BattlEyeCommand.Players;        // string constants, not enum
```

Magic return value `256` (failure) is gone — use exceptions / connect result enums.

## Events

| 1.3.x | 2.0 |
|-------|-----|
| `BattlEyeMessageReceived` | `MessageReceived` |
| `BattlEyeConnected` | `Connected` |
| `BattlEyeDisconnected` | `Disconnected` |

Handler signature changed from custom delegates to `EventHandler<TEventArgs>`.

## Reconnect / connected flag

| 1.3.x | 2.0 |
|-------|-----|
| `ReconnectOnPacketLoss` property | `BattlEyeClientOptions.ReconnectOnPacketLoss` |
| `Connected` | `IsConnected` |

## Removed

- Obsolete enum members (`loadEvents`, `admins`)
- Hex/string packet construction helpers
- Fire-and-forget `async void` receive loop
- Unbounded `ReceiveBufferSize = int.MaxValue`

## Socket cleanup

Always dispose the client:

```csharp
await using var client = new BattlEyeClient(credentials);
```

v1 often leaked UDP handles on reconnect / exit; v2 cancels loops and disposes `UdpClient` on disconnect/dispose.
