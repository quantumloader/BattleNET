package battlenet

import (
	"context"
	"errors"
	"fmt"
	"net"
	"sync"
	"sync/atomic"
	"time"
)

// ConnectionResult is the outcome of Connect.
type ConnectionResult int

const (
	ConnectionSuccess ConnectionResult = iota
	ConnectionFailed
	ConnectionInvalidLogin
	ConnectionCancelled
)

func (r ConnectionResult) String() string {
	switch r {
	case ConnectionSuccess:
		return "success"
	case ConnectionFailed:
		return "connection_failed"
	case ConnectionInvalidLogin:
		return "invalid_login"
	case ConnectionCancelled:
		return "cancelled"
	default:
		return fmt.Sprintf("ConnectionResult(%d)", int(r))
	}
}

// DisconnectionType explains why the client disconnected.
type DisconnectionType int

const (
	DisconnectManual DisconnectionType = iota
	DisconnectConnectionLost
	DisconnectSocketError
	DisconnectDisposed
)

// MessageEvent is raised for command replies and server messages.
type MessageEvent struct {
	Message string
	ID      int // 0-255 sequence, or ServerMessageID
}

// ServerMessageID is used for unsolicited server messages.
const ServerMessageID = -1

// Options configures Client behaviour.
type Options struct {
	ReceiveTimeout        time.Duration
	KeepAliveInterval     time.Duration
	ConnectionLostTimeout time.Duration
	CommandTimeout        time.Duration
	ReconnectOnPacketLoss bool
	MaxReconnectAttempts  int
	InitialReconnectDelay time.Duration
	MaxReconnectDelay     time.Duration
	ReadBufferSize        int
}

func (o *Options) withDefaults() Options {
	out := Options{
		ReceiveTimeout:        5 * time.Second,
		KeepAliveInterval:     5 * time.Second,
		ConnectionLostTimeout: 20 * time.Second,
		CommandTimeout:        10 * time.Second,
		MaxReconnectAttempts:  10,
		InitialReconnectDelay: time.Second,
		MaxReconnectDelay:     30 * time.Second,
		ReadBufferSize:        64 * 1024,
	}
	if o == nil {
		return out
	}
	if o.ReceiveTimeout > 0 {
		out.ReceiveTimeout = o.ReceiveTimeout
	}
	if o.KeepAliveInterval > 0 {
		out.KeepAliveInterval = o.KeepAliveInterval
	}
	if o.ConnectionLostTimeout > 0 {
		out.ConnectionLostTimeout = o.ConnectionLostTimeout
	}
	if o.CommandTimeout > 0 {
		out.CommandTimeout = o.CommandTimeout
	}
	out.ReconnectOnPacketLoss = o.ReconnectOnPacketLoss
	if o.MaxReconnectAttempts >= 0 {
		out.MaxReconnectAttempts = o.MaxReconnectAttempts
	}
	if o.InitialReconnectDelay > 0 {
		out.InitialReconnectDelay = o.InitialReconnectDelay
	}
	if o.MaxReconnectDelay > 0 {
		out.MaxReconnectDelay = o.MaxReconnectDelay
	}
	if o.ReadBufferSize > 0 {
		out.ReadBufferSize = o.ReadBufferSize
	}
	return out
}

var (
	ErrNotConnected = errors.New("battlenet: not connected")
	ErrDisconnected = errors.New("battlenet: disconnected before reply")
)

// Client is a BattlEye RCon UDP client.
type Client struct {
	host     string
	port     int
	password string
	opts     Options

	mu           sync.Mutex
	conn         *net.UDPConn
	connected    atomic.Bool
	disposed     atomic.Bool
	seq          atomic.Uint32
	lastSent     atomic.Int64
	lastReceived atomic.Int64

	lifeCancel context.CancelFunc
	wg         sync.WaitGroup

	pendingMu sync.Mutex
	pending   map[byte]chan string
	partsMu   sync.Mutex
	parts     map[byte]*multipartBuffer

	OnMessage      func(MessageEvent)
	OnConnected    func(ConnectionResult)
	OnDisconnected func(DisconnectionType)
}

type multipartBuffer struct {
	total    int
	received int
	parts    []string
}

// NewClient creates a client for host:port with the given RCon password.
func NewClient(host string, port int, password string, opts *Options) *Client {
	o := opts.withDefaults()
	now := time.Now().UnixNano()
	c := &Client{
		host:     host,
		port:     port,
		password: password,
		opts:     o,
		pending:  make(map[byte]chan string),
		parts:    make(map[byte]*multipartBuffer),
	}
	c.lastSent.Store(now)
	c.lastReceived.Store(now)
	return c
}

// Connected reports whether the client is logged in.
func (c *Client) Connected() bool {
	return c.connected.Load()
}

// Connect dials the RCon endpoint and logs in.
func (c *Client) Connect(ctx context.Context) (ConnectionResult, error) {
	if c.disposed.Load() {
		return ConnectionFailed, errors.New("battlenet: client disposed")
	}

	_ = c.Disconnect(DisconnectManual)

	addr, err := net.ResolveUDPAddr("udp", net.JoinHostPort(c.host, fmt.Sprintf("%d", c.port)))
	if err != nil {
		c.raiseConnected(ConnectionFailed)
		return ConnectionFailed, err
	}

	conn, err := net.DialUDP("udp", nil, addr)
	if err != nil {
		c.raiseConnected(ConnectionFailed)
		return ConnectionFailed, err
	}
	_ = conn.SetReadBuffer(c.opts.ReadBufferSize)
	_ = conn.SetWriteBuffer(c.opts.ReadBufferSize)

	c.mu.Lock()
	c.conn = conn
	c.mu.Unlock()

	login := BuildLoginPacket(c.password)
	if _, err := conn.Write(login); err != nil {
		c.closeConn()
		c.raiseConnected(ConnectionFailed)
		return ConnectionFailed, err
	}

	_ = conn.SetReadDeadline(time.Now().Add(c.opts.ReceiveTimeout))
	buf := make([]byte, c.opts.ReadBufferSize)
	n, err := conn.Read(buf)
	_ = conn.SetReadDeadline(time.Time{})
	if err != nil {
		if errors.Is(err, context.Canceled) || ctx.Err() != nil {
			c.closeConn()
			c.raiseConnected(ConnectionCancelled)
			return ConnectionCancelled, ctx.Err()
		}
		c.closeConn()
		c.raiseConnected(ConnectionFailed)
		return ConnectionFailed, err
	}

	parsed, err := ParsePacket(buf[:n])
	if err != nil || parsed.Type != PacketLogin {
		c.closeConn()
		c.raiseConnected(ConnectionFailed)
		return ConnectionFailed, fmt.Errorf("battlenet: invalid login response: %w", err)
	}
	if parsed.LoginSuccess == nil || !*parsed.LoginSuccess {
		c.closeConn()
		c.raiseConnected(ConnectionInvalidLogin)
		return ConnectionInvalidLogin, errors.New("battlenet: invalid login")
	}

	now := time.Now().UnixNano()
	c.lastSent.Store(now)
	c.lastReceived.Store(now)
	c.seq.Store(0)
	c.connected.Store(true)

	lifeCtx, cancel := context.WithCancel(context.Background())
	c.lifeCancel = cancel
	c.wg.Add(2)
	go c.receiveLoop(lifeCtx)
	go c.keepAliveLoop(lifeCtx)

	c.raiseConnected(ConnectionSuccess)
	return ConnectionSuccess, nil
}

// SendCommand sends a command without waiting for a reply. Returns the sequence number.
func (c *Client) SendCommand(ctx context.Context, command string) (byte, error) {
	if !c.connected.Load() {
		return 0, ErrNotConnected
	}
	seq := byte(c.seq.Add(1) - 1)
	packet := BuildCommandPacket(seq, command)
	if err := c.write(packet); err != nil {
		return 0, err
	}
	return seq, ctx.Err()
}

// SendCommandAndWait sends a command and waits for the matching reply.
func (c *Client) SendCommandAndWait(ctx context.Context, command string, timeout time.Duration) (string, error) {
	if !c.connected.Load() {
		return "", ErrNotConnected
	}
	if timeout <= 0 {
		timeout = c.opts.CommandTimeout
	}

	seq := byte(c.seq.Add(1) - 1)
	ch := make(chan string, 1)
	c.pendingMu.Lock()
	c.pending[seq] = ch
	c.pendingMu.Unlock()
	defer func() {
		c.pendingMu.Lock()
		delete(c.pending, seq)
		c.pendingMu.Unlock()
	}()

	packet := BuildCommandPacket(seq, command)
	if err := c.write(packet); err != nil {
		return "", err
	}

	waitCtx, cancel := context.WithTimeout(ctx, timeout)
	defer cancel()

	select {
	case reply := <-ch:
		return reply, nil
	case <-waitCtx.Done():
		return "", waitCtx.Err()
	}
}

// Disconnect stops loops and closes the UDP socket.
func (c *Client) Disconnect(reason DisconnectionType) error {
	was := c.connected.Swap(false)
	if c.lifeCancel != nil {
		c.lifeCancel()
		c.lifeCancel = nil
	}
	c.closeConn()
	c.wg.Wait()

	c.failPending()
	c.partsMu.Lock()
	c.parts = make(map[byte]*multipartBuffer)
	c.partsMu.Unlock()

	if was && c.OnDisconnected != nil {
		c.OnDisconnected(reason)
	}
	return nil
}

// Close disposes the client permanently.
func (c *Client) Close() error {
	if !c.disposed.CompareAndSwap(false, true) {
		return nil
	}
	return c.Disconnect(DisconnectDisposed)
}

func (c *Client) write(packet []byte) error {
	c.mu.Lock()
	conn := c.conn
	c.mu.Unlock()
	if conn == nil {
		return ErrNotConnected
	}
	_, err := conn.Write(packet)
	if err == nil {
		c.lastSent.Store(time.Now().UnixNano())
	}
	return err
}

func (c *Client) closeConn() {
	c.mu.Lock()
	conn := c.conn
	c.conn = nil
	c.mu.Unlock()
	if conn != nil {
		_ = conn.Close()
	}
}

func (c *Client) receiveLoop(ctx context.Context) {
	defer c.wg.Done()
	buf := make([]byte, c.opts.ReadBufferSize)

	for {
		select {
		case <-ctx.Done():
			return
		default:
		}

		c.mu.Lock()
		conn := c.conn
		c.mu.Unlock()
		if conn == nil {
			return
		}

		_ = conn.SetReadDeadline(time.Now().Add(time.Second))
		n, err := conn.Read(buf)
		if err != nil {
			if ne, ok := err.(net.Error); ok && ne.Timeout() {
				continue
			}
			if ctx.Err() != nil || !c.connected.Load() {
				return
			}
			c.handleLost(DisconnectSocketError)
			return
		}

		c.lastReceived.Store(time.Now().UnixNano())
		c.handleDatagram(buf[:n])
	}
}

func (c *Client) keepAliveLoop(ctx context.Context) {
	defer c.wg.Done()
	ticker := time.NewTicker(time.Second)
	defer ticker.Stop()

	for {
		select {
		case <-ctx.Done():
			return
		case <-ticker.C:
			if !c.connected.Load() {
				return
			}
			now := time.Now().UnixNano()
			if time.Duration(now-c.lastReceived.Load()) >= c.opts.ConnectionLostTimeout {
				c.handleLost(DisconnectConnectionLost)
				return
			}
			c.pendingMu.Lock()
			idle := len(c.pending) == 0
			c.pendingMu.Unlock()
			if idle && time.Duration(now-c.lastSent.Load()) >= c.opts.KeepAliveInterval {
				if _, err := c.SendCommand(ctx, ""); err != nil {
					c.handleLost(DisconnectSocketError)
					return
				}
			}
		}
	}
}

func (c *Client) handleDatagram(data []byte) {
	parsed, err := ParsePacket(data)
	if err != nil {
		return
	}

	switch parsed.Type {
	case PacketServerMessage:
		if parsed.Sequence != nil {
			_ = c.write(BuildAcknowledgePacket(*parsed.Sequence))
		}
		c.raiseMessage(MessageEvent{Message: parsed.Message, ID: ServerMessageID})
	case PacketCommand:
		c.handleCommand(parsed)
	}
}

func (c *Client) handleCommand(parsed ParsedPacket) {
	if parsed.Sequence == nil {
		return
	}
	seq := *parsed.Sequence
	msg := parsed.Message

	if parsed.IsMultipart {
		c.partsMu.Lock()
		buf, ok := c.parts[seq]
		if !ok {
			total := int(parsed.MultipartTotal)
			if total < 1 {
				total = 1
			}
			buf = &multipartBuffer{total: total, parts: make([]string, total)}
			c.parts[seq] = buf
		}
		idx := int(parsed.MultipartIndex)
		if idx >= 0 && idx < len(buf.parts) && buf.parts[idx] == "" {
			buf.parts[idx] = parsed.Message
			buf.received++
		}
		complete := buf.received >= buf.total
		if complete {
			combined := ""
			for _, p := range buf.parts {
				combined += p
			}
			msg = combined
			delete(c.parts, seq)
		}
		c.partsMu.Unlock()
		if !complete {
			return
		}
	}

	c.pendingMu.Lock()
	ch := c.pending[seq]
	if ch != nil {
		delete(c.pending, seq)
	}
	c.pendingMu.Unlock()
	if ch != nil {
		select {
		case ch <- msg:
		default:
		}
	}

	c.raiseMessage(MessageEvent{Message: msg, ID: int(seq)})
}

func (c *Client) handleLost(reason DisconnectionType) {
	if !c.connected.Load() {
		return
	}
	_ = c.Disconnect(reason)

	if !c.opts.ReconnectOnPacketLoss || c.disposed.Load() {
		return
	}

	delay := c.opts.InitialReconnectDelay
	attempts := c.opts.MaxReconnectAttempts
	for try := 0; !c.disposed.Load() && (attempts == 0 || try < attempts); try++ {
		time.Sleep(delay)
		ctx, cancel := context.WithTimeout(context.Background(), c.opts.ReceiveTimeout+2*time.Second)
		result, _ := c.Connect(ctx)
		cancel()
		if result == ConnectionSuccess {
			return
		}
		next := delay * 2
		if next > c.opts.MaxReconnectDelay {
			next = c.opts.MaxReconnectDelay
		}
		delay = next
	}
}

func (c *Client) failPending() {
	c.pendingMu.Lock()
	defer c.pendingMu.Unlock()
	for seq, ch := range c.pending {
		delete(c.pending, seq)
		close(ch)
	}
}

func (c *Client) raiseConnected(result ConnectionResult) {
	if c.OnConnected != nil {
		c.OnConnected(result)
	}
}

func (c *Client) raiseMessage(ev MessageEvent) {
	if c.OnMessage != nil {
		c.OnMessage(ev)
	}
}
