"""BattlEye packet CRC-32."""

_POLY = 0xEDB88320
_SEED = 0xFFFFFFFF


def _make_table() -> list[int]:
    table: list[int] = []
    for i in range(256):
        entry = i
        for _ in range(8):
            if entry & 1:
                entry = (entry >> 1) ^ _POLY
            else:
                entry >>= 1
        table.append(entry & 0xFFFFFFFF)
    return table


_TABLE = _make_table()


def crc32(data: bytes | bytearray | memoryview) -> int:
    crc = _SEED
    for b in data:
        crc = (_TABLE[(crc ^ b) & 0xFF] ^ (crc >> 8)) & 0xFFFFFFFF
    return (~crc) & 0xFFFFFFFF
