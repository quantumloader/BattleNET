# CLI 参考

项目：`BattleNET.Cli` · 包：`BattleNET.Cli` · 工具命令：`battlenet`

## 安装

```bash
dotnet tool install -g BattleNET.Cli
dotnet run --project BattleNET.Cli -- [options]
```

## 参数

| 参数 | 短选项 | 说明 |
|------|--------|------|
| `--host` | | RCon 主机或 IP |
| `--port` | `-p` | RCon UDP 端口 |
| `--password` | | RCon 密码 |
| `--command` | `-c` | 发送一条命令后退出 |
| `--reconnect` | | 断线自动重连 |

未提供参数时会交互式询问（密码掩码显示）。

## 示例

```bash
BattleNET --host 127.0.0.1 --port 2302 --password secret
BattleNET --host 127.0.0.1 --port 2302 --password secret --command "say -1 你好"
```

## 交互模式

| 输入 | 作用 |
|------|------|
| 任意 RCon 命令 | 发送并打印回复 |
| `/players` | 解析后的玩家列表 |
| `/bans` | 解析后的封禁列表 |
| `/history` | 本会话命令历史 |
| `exit` / `logout` / `quit` | 退出 |

Ctrl+C 会取消并释放 UDP 端口。

## 退出码

`0` 成功 · `1` 缺少凭据 · `2` 连接/登录失败 · `3` 命令失败
