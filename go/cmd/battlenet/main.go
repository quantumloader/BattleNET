package main

import (
	"bufio"
	"context"
	"flag"
	"fmt"
	"os"
	"os/signal"
	"strings"
	"syscall"
	"time"

	battlenet "github.com/quantumloader/BattleNET/go"
)

func main() {
	host := flag.String("host", "", "RCon host or IP")
	port := flag.Int("port", 0, "RCon UDP port")
	password := flag.String("password", "", "RCon password")
	command := flag.String("command", "", "Send a single command and exit")
	reconnect := flag.Bool("reconnect", false, "Reconnect after connection loss")
	flag.Parse()

	h, p, pw := *host, *port, *password
	if h == "" || p <= 0 || pw == "" {
		h, p, pw = promptCredentials(h, p, pw)
	}
	if h == "" || p <= 0 || pw == "" {
		fmt.Fprintln(os.Stderr, "host, port and password are required")
		os.Exit(1)
	}

	client := battlenet.NewClient(h, p, pw, &battlenet.Options{
		ReconnectOnPacketLoss: *reconnect,
	})
	defer client.Close()

	client.OnMessage = func(ev battlenet.MessageEvent) {
		if ev.Message == "" {
			return
		}
		fmt.Println(ev.Message)
	}
	client.OnConnected = func(r battlenet.ConnectionResult) {
		fmt.Fprintf(os.Stderr, "connect: %s\n", r)
	}
	client.OnDisconnected = func(t battlenet.DisconnectionType) {
		fmt.Fprintf(os.Stderr, "disconnected: %v\n", t)
	}

	ctx, stop := signal.NotifyContext(context.Background(), os.Interrupt, syscall.SIGTERM)
	defer stop()

	result, err := client.Connect(ctx)
	if result != battlenet.ConnectionSuccess {
		if err != nil {
			fmt.Fprintln(os.Stderr, err)
		}
		os.Exit(2)
	}

	if *command != "" {
		reply, err := client.SendCommandAndWait(ctx, *command, 0)
		if err != nil {
			fmt.Fprintln(os.Stderr, err)
			os.Exit(3)
		}
		if reply != "" {
			fmt.Println(reply)
		}
		_ = client.Disconnect(battlenet.DisconnectManual)
		return
	}

	fmt.Fprintln(os.Stderr, "Interactive mode. Type a command, or exit/logout to quit.")
	reader := bufio.NewReader(os.Stdin)
	for client.Connected() {
		fmt.Print("> ")
		line, err := reader.ReadString('\n')
		if err != nil {
			break
		}
		line = strings.TrimSpace(line)
		if line == "" {
			continue
		}
		if line == "exit" || line == "logout" || line == "quit" {
			break
		}

		select {
		case <-ctx.Done():
			return
		default:
		}

		reply, err := client.SendCommandAndWait(ctx, line, 15*time.Second)
		if err != nil {
			fmt.Fprintln(os.Stderr, err)
			if !client.Connected() {
				break
			}
			continue
		}
		if reply != "" {
			fmt.Println(reply)
		}
	}

	_ = client.Disconnect(battlenet.DisconnectManual)
}

func promptCredentials(host string, port int, password string) (string, int, string) {
	in := bufio.NewReader(os.Stdin)
	for host == "" {
		fmt.Print("Enter IP address or hostname: ")
		host, _ = in.ReadString('\n')
		host = strings.TrimSpace(host)
	}
	for port <= 0 {
		fmt.Print("Enter port number: ")
		var raw string
		raw, _ = in.ReadString('\n')
		fmt.Sscanf(strings.TrimSpace(raw), "%d", &port)
	}
	for password == "" {
		fmt.Print("Enter RCon password: ")
		password, _ = in.ReadString('\n')
		password = strings.TrimSpace(password)
	}
	return host, port, password
}
