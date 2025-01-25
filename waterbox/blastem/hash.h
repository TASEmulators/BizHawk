#ifndef HASH_H_
#define HASH_H_

#include <stdint.h>

//NOTE: This is only intended for use in file identification
//Please do not use this in a cryptographic setting as no attempts have been
//made at avoiding side channel attacks

void sha1(uint8_t *data, uint64_t size, uint8_t *out);

#endif //HASH_H_
