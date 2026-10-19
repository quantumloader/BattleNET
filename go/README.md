# BattleNET for Go

Go port of the BattleNET BattlEye RCon client (same protocol as the .NET libraries).

Module: `github.com/quantumloader/BattleNET/go`

## Install

```bash
go get github.com/quantumloader/BattleNET/go@latest
```

## Library usage

```go
package main

import (
	"context"
	"fmt"
	"time"

	battlenet "github.com/quantumloader/BattleNET/go"
)

func main() {
	client := battlenet.NewClient("127.0.0.1", 2302, "password", &battlenet.Options{
		ReconnectOnPacketLoss: true,
	})
	defer client.Close()

	client.OnMessage = func(ev battlenet.MessageEvent) {
		fmt.Println(ev.Message)
	}

	ctx := context.Background()
	if result, err := client.Connect(ctx); result != battlenet.ConnectionSuccess {
		panic(err)
	}

	reply, err := client.SendCommandAndWait(ctx, "players", 10*time.Second)
	if err != nil {
		panic(err)
	}
	fmt.Println(reply)

	_, _ = client.SendCommand(ctx, "say -1 Hello from Go")
	_ = client.Disconnect(battlenet.DisconnectManual)
}
```

`Close()` / `Disconnect()` always release the UDP socket.

## CLI

```bash
cd go
go run ./cmd/battlenet --host 127.0.0.1 --port 2302 --password secret
go run ./cmd/battlenet --host 127.0.0.1 --port 2302 --password secret --command "players"
```

## Test

```bash
cd go
go test ./...
```

## Docs

- English: [../docs/getting-started.md](../docs/getting-started.md)
- API ideas match the .NET client: connect, send, wait, keepalive, reconnect, dispose
