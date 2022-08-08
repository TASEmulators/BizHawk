#include <stdbool.h>
#include <string.h>
#include "cpu_features.h"

__attribute__((visibility("default")))
crc32_func BizCalcCrcFunc(void) {
	cpu_check_features();
	return (x86_cpu_has_pclmulqdq && x86_cpu_has_sse41) ? &crc32_pclmulqdq : &crc32_braid;
}

__attribute__((visibility("default")))
bool BizSupportsShaInstructions(void) {
	cpu_check_features();
	return x86_cpu_has_sha && x86_cpu_has_sse41;
}

__attribute__((visibility("default")))
void BizFastCalcSha1(uint32_t state[5], const uint8_t data[], uint32_t length) {
	uint64_t bit_length = length * 8;

	// hash most of the data, leaving at most 63 bytes left
	BizFastCalcSha1Internal(state, data, length);
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
		BizFastCalcSha1Internal(state, block, 64);
		memset(block, 0, 56);
	}

	// fill the last 8 bytes in the last block with the data length in bits (big endian)
	for (int i = 0; i != 8; i++) {
		block[63 - i] = bit_length >> i * 8;
	}
	// hash the last block
	BizFastCalcSha1Internal(state, block, 64);
}
