package battlenet

const (
	crcPolynomial = 0xEDB88320
	crcSeed       = 0xFFFFFFFF
)

var crcTable = func() [256]uint32 {
	var table [256]uint32
	for i := uint32(0); i < 256; i++ {
		entry := i
		for j := 0; j < 8; j++ {
			if entry&1 != 0 {
				entry = (entry >> 1) ^ crcPolynomial
			} else {
				entry >>= 1
			}
		}
		table[i] = entry
	}
	return table
}()

// CRC32 computes the BattlEye packet checksum over payload bytes.
func CRC32(data []byte) uint32 {
	crc := uint32(crcSeed)
	for _, b := range data {
		crc = (crc >> 8) ^ crcTable[(crc^uint32(b))&0xFF]
	}
	return ^crc
}
