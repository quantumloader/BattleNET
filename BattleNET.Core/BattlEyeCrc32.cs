namespace BattleNET;

/// <summary>
/// CRC-32 used by the BattlEye RCon packet checksum.
/// </summary>
internal static class BattlEyeCrc32
{
    private const uint Polynomial = 0xEDB88320u;
    private const uint Seed = 0xFFFFFFFFu;

    private static readonly uint[] Table = CreateTable();

    public static uint Compute(ReadOnlySpan<byte> data)
    {
        uint crc = Seed;
        foreach (byte b in data)
        {
            crc = (crc >> 8) ^ Table[(crc ^ b) & 0xFF];
        }

        return ~crc;
    }

    private static uint[] CreateTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < table.Length; i++)
        {
            uint entry = i;
            for (int j = 0; j < 8; j++)
            {
                entry = (entry & 1) != 0
                    ? (entry >> 1) ^ Polynomial
                    : entry >> 1;
            }

            table[i] = entry;
        }

        return table;
    }
}
