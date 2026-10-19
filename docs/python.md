# BattleNET for Python

Code: [`/python`](../python/)

```bash
cd python
pip install -e .
battlenet --host 127.0.0.1 --port 2302 --password secret
```

```python
from battlenet import BattlEyeClient, ConnectionResult

with BattlEyeClient("127.0.0.1", 2302, "password") as client:
    assert client.connect() is ConnectionResult.SUCCESS
    print(client.send_command_and_wait("players"))
```

See [python/README.md](../python/README.md).
