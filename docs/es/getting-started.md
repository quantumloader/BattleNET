# Primeros pasos

## Requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download) (compilar) o [.NET 9 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) (CLI)
- Visual Studio 2022 17.12+ (o CLI `dotnet`). El SDK .NET 10 requiere VS 2026 / MSBuild 18
- Servidor DayZ / Arma con RCon de BattlEye (`RConPort`, `RConPassword`)

## Instalar paquetes

```bash
dotnet add package BattleNET.Core
dotnet add package BattleNET.Commands
```

O descarga las DLL desde [Releases](https://github.com/quantumloader/BattleNET/releases): `BattleNET.Libraries-*-net9.0.zip`.

## Ejemplo mínimo

```csharp
using BattleNET;
using BattleNET.Commands;

var credentials = new BattlEyeCredentials("127.0.0.1", 2302, "secret");
await using var client = new BattlEyeClient(credentials);

client.MessageReceived += (_, e) => Console.WriteLine($"[{e.Id}] {e.Message}");

if (await client.ConnectAsync() != BattlEyeConnectionResult.Success)
{
    Console.WriteLine("Error de conexión");
    return;
}

foreach (var player in await client.GetPlayersAsync())
    Console.WriteLine($"{player.Id}: {player.Name}");

await client.SayAllAsync("Hola desde BattleNET");
await client.DisconnectAsync();
```

`await using` cierra siempre el socket UDP.

| Resultado | Significado |
|-----------|------------|
| `Success` | Conectado |
| `InvalidLogin` | Contraseña RCon incorrecta |
| `ConnectionFailed` | Host inalcanzable / timeout |
| `Cancelled` | Cancelado |

Siguiente: [API](api.md) · [CLI](cli.md)
