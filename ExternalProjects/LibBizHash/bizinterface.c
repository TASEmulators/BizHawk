#include <stdbool.h>
#include "common.h"

__attribute__((visibility("default")))
crc32_func BizCalcCrcFunc(void) {
	x86_check_features();
	return (x86_cpu_has_pclmulqdq && x86_cpu_has_sse41) ? &crc32_pclmulqdq : &crc32_braid;
}

__attribute__((visibility("default")))
bool BizSupportsShaInstructions(void) {
	x86_check_features();
	return x86_cpu_has_sha && x86_cpu_has_sse41;
}

__attribute__((visibility("default")))
void BizCalcSha1(uint32_t state[5], const uint8_t data[], uint32_t length) {
	x86_check_features();

	uint64_t bit_length = length * 8ULL;

	// hash most of the data, leaving at most 63 bytes left
	sha1_sha(state, data, length);
	data += length & ~0x3F;
	length &= 0x3F;

	// copy all remaining data to a buffer
	uint8_t block[64] = {0};
	memcpy(block, data, length);

	// pad data with '1' bit
	block[length++] = 0x80;

	// the last 8 bytes in the last block contain the data length;
	// if the current block is too full hash it and start a new one (here the old one is cleared and re-used)
	if (__builtin_expect(length > 56, false)) {
		sha1_sha(state, block, 64);
		memset(block, 0, 56);
	}

	// fill the last 8 bytes in the last block with the data length in bits (big endian)
	for (int i = 0; i != 8; i++) {
		block[63 - i] = bit_length >> i * 8;
	}
	// hash the last block
	sha1_sha(state, block, 64);

	// byteswap state (to big endian format)
	state[0] = __builtin_bswap32(state[0]);
	state[1] = __builtin_bswap32(state[1]);
	state[2] = __builtin_bswap32(state[2]);
	state[3] = __builtin_bswap32(state[3]);
	state[4] = __builtin_bswap32(state[4]);
}
