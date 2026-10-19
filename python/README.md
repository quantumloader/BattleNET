# BattleNET for Python

Python port of the BattleNET BattlEye RCon client.

Requires **Python 3.10+**.

## Install

```bash
cd python
pip install -e .
```

## Library

```python
from battlenet import BattlEyeClient, ConnectionResult

with BattlEyeClient("127.0.0.1", 2302, "password") as client:
    client.on_message = lambda e: print(e.message)
    if client.connect() is not ConnectionResult.SUCCESS:
        raise SystemExit(2)
    print(client.send_command_and_wait("players"))
    client.send_command("say -1 Hello from Python")
```

`close()` / context manager always releases the UDP socket.

## CLI

```bash
battlenet --host 127.0.0.1 --port 2302 --password secret
python -m battlenet.cli --host 127.0.0.1 --port 2302 --password secret -c "players"
```

## Tests

```bash
cd python
python -m pytest
# or:
python -m unittest discover -s tests -v
```
