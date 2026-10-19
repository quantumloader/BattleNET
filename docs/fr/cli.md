# Référence CLI

```bash
dotnet tool install -g BattleNET.Cli
BattleNET --host 127.0.0.1 --port 2302 --password secret
BattleNET --host 127.0.0.1 --port 2302 --password secret --command "say -1 Bonjour"
```

| Option | Court | Description |
|--------|-------|-------------|
| `--host` | | Hôte / IP RCon |
| `--port` | `-p` | Port UDP RCon |
| `--password` | | Mot de passe RCon |
| `--command` | `-c` | Une commande puis sortie |
| `--reconnect` | | Reconnexion après perte |

Interactif : `/players` · `/bans` · `/history` · `exit` / `logout` / `quit`

Codes : `0` OK · `1` identifiants manquants · `2` échec login · `3` erreur commande
