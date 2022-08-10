/* crc32_braid.c -- compute the CRC-32 of a data stream
 * Copyright (C) 1995-2022 Mark Adler
 * For conditions of distribution and use, see copyright notice in README.md
 *
 * This interleaved implementation of a CRC makes use of pipelined multiple
 * arithmetic-logic units, commonly found in modern CPU cores. It is due to
 * Kadatch and Jenkins (2010). See doc/crc-doc.1.0.pdf in upstream.
 */

#include "common.h"
#include "crc32_braid_tbl.h"

#define DO1 c = crc_table[(c ^ *buf++) & 0xff] ^ (c >> 8)
#define DO8 DO1; DO1; DO1; DO1; DO1; DO1; DO1; DO1

static uint32_t crc_word(uint64_t data) {
	unsigned k;
	for (k = 0; k < sizeof(uint64_t); k++)
		data = (data >> 8) ^ crc_table[data & 0xff];
	return (uint32_t)data;
}

__attribute__((visibility("hidden")))
uint32_t crc32_braid(uint32_t crc, const uint8_t *buf, uint32_t len) {
	register uint32_t c;

	c = crc;

	/* If provided enough bytes, do a braided CRC calculation. */
	if (len >= 5 * sizeof(uint64_t) + sizeof(uint64_t) - 1) {
		uint64_t blks;
		uint64_t const *words;
		unsigned k;

		/* Compute the CRC up to a uint64_t boundary. */
		while (len && ((uint64_t)buf & (sizeof(uint64_t) - 1)) != 0) {
			len--;
			DO1;
		}

		/* Compute the CRC on as many 5 uint64_t blocks as are available. */
		blks = len / (5 * sizeof(uint64_t));
		len -= blks * 5 * sizeof(uint64_t);
		words = (uint64_t const *)buf;

		uint64_t crc0, word0, comb;
		uint64_t crc1, word1;
		uint64_t crc2, word2;
		uint64_t crc3, word3;
		uint64_t crc4, word4;

		/* Initialize the CRC for each braid. */
		crc0 = c;
		crc1 = 0;
		crc2 = 0;
		crc3 = 0;
		crc4 = 0;

		/* Process the first blks-1 blocks, computing the CRCs on each braid independently. */
		while (--blks) {
			/* Load the word for each braid into registers. */
			word0 = crc0 ^ words[0];
			word1 = crc1 ^ words[1];
			word2 = crc2 ^ words[2];
			word3 = crc3 ^ words[3];
			word4 = crc4 ^ words[4];

			words += 5;

			/* Compute and update the CRC for each word. The loop should get unrolled. */
			crc0 = crc_braid_table[0][word0 & 0xff];
			crc1 = crc_braid_table[0][word1 & 0xff];
			crc2 = crc_braid_table[0][word2 & 0xff];
			crc3 = crc_braid_table[0][word3 & 0xff];
			crc4 = crc_braid_table[0][word4 & 0xff];

			for (k = 1; k < sizeof(uint64_t); k++) {
				crc0 ^= crc_braid_table[k][(word0 >> (k << 3)) & 0xff];
				crc1 ^= crc_braid_table[k][(word1 >> (k << 3)) & 0xff];
				crc2 ^= crc_braid_table[k][(word2 >> (k << 3)) & 0xff];
				crc3 ^= crc_braid_table[k][(word3 >> (k << 3)) & 0xff];
				crc4 ^= crc_braid_table[k][(word4 >> (k << 3)) & 0xff];
			}
		}

		/* Process the last block, combining the CRCs of the 5 braids at the same time. */
		comb = crc_word(crc0 ^ words[0]);
		comb = crc_word(crc1 ^ words[1] ^ comb);
		comb = crc_word(crc2 ^ words[2] ^ comb);
		comb = crc_word(crc3 ^ words[3] ^ comb);
		comb = crc_word(crc4 ^ words[4] ^ comb);

		words += 5;
		c = comb;

		/* Update the pointer to the remaining bytes to process. */
		buf = (const uint8_t *)words;
	}

	/* Complete the computation of the CRC on any remaining bytes. */
	while (len >= 8) {
		len -= 8;
		DO8;
	}
	while (len) {
		len--;
		DO1;
	}

	return c;
}
