# Referencia CLI

```bash
dotnet tool install -g BattleNET.Cli
BattleNET --host 127.0.0.1 --port 2302 --password secret
BattleNET --host 127.0.0.1 --port 2302 --password secret --command "say -1 Hola"
```

| Opción | Corta | Descripción |
|--------|-------|-------------|
| `--host` | | Host / IP RCon |
| `--port` | `-p` | Puerto UDP RCon |
| `--password` | | Contraseña RCon |
| `--command` | `-c` | Un comando y salir |
| `--reconnect` | | Reconectar tras pérdida |

Interactivo: `/players` · `/bans` · `/history` · `exit` / `logout` / `quit`

Códigos: `0` OK · `1` faltan credenciales · `2` fallo de login · `3` error de comando
