# Referencja CLI

```bash
dotnet tool install -g BattleNET.Cli
BattleNET --host 127.0.0.1 --port 2302 --password secret
BattleNET --host 127.0.0.1 --port 2302 --password secret --command "say -1 Cześć"
```

| Opcja | Krótka | Opis |
|-------|--------|------|
| `--host` | | Host / IP RCon |
| `--port` | `-p` | Port UDP RCon |
| `--password` | | Hasło RCon |
| `--command` | `-c` | Jedna komenda i wyjście |
| `--reconnect` | | Ponowne połączenie po utracie |

Interaktywnie: `/players` · `/bans` · `/history` · `exit` / `logout` / `quit`

Kody: `0` OK · `1` brak danych · `2` błąd logowania · `3` błąd komendy
