# CLI-Referenz

```bash
dotnet tool install -g BattleNET.Cli
BattleNET --host 127.0.0.1 --port 2302 --password secret
BattleNET --host 127.0.0.1 --port 2302 --password secret --command "say -1 Hallo"
```

| Option | Kurz | Beschreibung |
|--------|------|--------------|
| `--host` | | RCon-Host/IP |
| `--port` | `-p` | RCon-UDP-Port |
| `--password` | | RCon-Passwort |
| `--command` | `-c` | Ein Befehl, dann Exit |
| `--reconnect` | | Nach Verbindungsverlust neu verbinden |

### Interaktiv

`/players` · `/bans` · `/history` · `exit` / `logout` / `quit`

Exit-Codes: `0` OK · `1` fehlende Zugangsdaten · `2` Login fehlgeschlagen · `3` Befehlsfehler
