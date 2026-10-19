# CLI リファレンス

```bash
dotnet tool install -g BattleNET.Cli
BattleNET --host 127.0.0.1 --port 2302 --password secret
BattleNET --host 127.0.0.1 --port 2302 --password secret --command "say -1 こんにちは"
```

| オプション | 短縮 | 説明 |
|------------|------|------|
| `--host` | | RCon ホスト / IP |
| `--port` | `-p` | RCon UDP ポート |
| `--password` | | RCon パスワード |
| `--command` | `-c` | 1 コマンド送信して終了 |
| `--reconnect` | | 切断後に再接続 |

対話: `/players` · `/bans` · `/history` · `exit` / `logout` / `quit`

終了コード: `0` OK · `1` 認証情報不足 · `2` ログイン失敗 · `3` コマンド失敗
