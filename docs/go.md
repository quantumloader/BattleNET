# BattleNET for Go

Go implementation lives in [`/go`](../go/).

```bash
go get github.com/quantumloader/BattleNET/go@latest
```

```go
client := battlenet.NewClient("127.0.0.1", 2302, "password", nil)
defer client.Close()

ctx := context.Background()
client.Connect(ctx)
reply, _ := client.SendCommandAndWait(ctx, "players", 10*time.Second)
```

CLI:

```bash
cd go
go run ./cmd/battlenet --host 127.0.0.1 --port 2302 --password secret
```

See [go/README.md](../go/README.md).
