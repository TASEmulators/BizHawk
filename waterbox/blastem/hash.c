#include <stdint.h>
#include <string.h>

//NOTE: This is only intended for use in file identification
//Please do not use this in a cryptographic setting as no attempts have been
//made at avoiding side channel attacks

static uint32_t rotleft(uint32_t val, uint32_t shift)
{
	return val << shift | val >> (32-shift);
}

static void sha1_step(uint32_t *state, uint32_t f, uint32_t k, uint32_t w)
{
	uint32_t tmp = rotleft(state[0], 5) + f + state[4] + k + w;
	state[4] = state[3];
	state[3] = state[2];
	state[2] = rotleft(state[1], 30);
	state[1] = state[0];
	state[0] = tmp;
}

static void sha1_chunk(uint8_t *chunk, uint32_t *hash)
{
	uint32_t state[5], w[80];
	memcpy(state, hash, sizeof(state));
	for (uint32_t src = 0; src < 64; src += 4)
	{
		w[src >> 2] = chunk[src] << 24 | chunk[src+1] << 16 | chunk[src+2] << 8 | chunk[src+3];
	}
	for (uint32_t cur = 16; cur < 80; cur++)
	{
		w[cur] = rotleft(w[cur-3] ^ w[cur-8] ^ w[cur-14] ^ w[cur-16], 1);
	}
	for (uint32_t cur = 0; cur < 20; cur++)
	{
		sha1_step(state, (state[1] & state[2]) | ((~state[1]) & state[3]), 0x5A827999, w[cur]);
	}
	for (uint32_t cur = 20; cur < 40; cur++)
	{
		sha1_step(state, state[1] ^ state[2] ^ state[3], 0x6ED9EBA1, w[cur]);
	}
	for (uint32_t cur = 40; cur < 60; cur++)
	{
		sha1_step(state, (state[1] & state[2]) | (state[1] & state[3]) | (state[2] & state[3]), 0x8F1BBCDC, w[cur]);
	}
	for (uint32_t cur = 60; cur < 80; cur++)
	{
		sha1_step(state, state[1] ^ state[2] ^ state[3], 0xCA62C1D6, w[cur]);
	}
	for (uint32_t i = 0; i < 5; i++)
	{
		hash[i] += state[i];
	}
}

void sha1(uint8_t *data, uint64_t size, uint8_t *out)
{
	uint32_t hash[5] = {0x67452301, 0xEFCDAB89, 0x98BADCFE, 0x10325476, 0xC3D2E1F0};
	uint8_t last[128];
	uint32_t last_size = 0;
	if ((size & 63) != 0) {
		for (uint32_t src = size - (size & 63); src < size; src++)
		{
			last[last_size++] = data[src];
		}
	}
	uint64_t bitsize = size * 8;
	size -= last_size;
	last[last_size++] = 0x80;
	while ((last_size & 63) != 56)
	{
		last[last_size++] = 0;
	}
	
	last[last_size++] = bitsize >> 56;
	last[last_size++] = bitsize >> 48;
	last[last_size++] = bitsize >> 40;
	last[last_size++] = bitsize >> 32;
	last[last_size++] = bitsize >> 24;
	last[last_size++] = bitsize >> 16;
	last[last_size++] = bitsize >> 8;
	last[last_size++] = bitsize;
	
	for (uint64_t cur = 0; cur < size; cur += 64)
	{
		sha1_chunk(data + cur, hash);
	}
	for (uint64_t cur = 0; cur < last_size; cur += 64)
	{
		sha1_chunk(last + cur, hash);
	}
	for (uint32_t cur = 0; cur < 20; cur += 4)
	{
		uint32_t val = hash[cur >> 2];
		out[cur] = val >> 24;
		out[cur+1] = val >> 16;
		out[cur+2] = val >> 8;
		out[cur+3] = val;
	}
}
