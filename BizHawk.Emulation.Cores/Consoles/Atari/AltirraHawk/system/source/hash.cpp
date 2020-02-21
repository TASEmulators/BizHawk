//	VirtualDub - Video processing and capture application
//	System library component - hashing module
//	Copyright (C) 1998-2014 Avery Lee, All Rights Reserved.
//
//	Beginning with 1.6.0, the VirtualDub system library is licensed
//	differently than the remainder of VirtualDub.  This particular file is
//	thus licensed as follows (the "zlib" license):
//
//	This software is provided 'as-is', without any express or implied
//	warranty.  In no event will the authors be held liable for any
//	damages arising from the use of this software.
//
//	Permission is granted to anyone to use this software for any purpose,
//	including commercial applications, and to alter it and redistribute it
//	freely, subject to the following restrictions:
//
//	1.	The origin of this software must not be misrepresented; you must
//		not claim that you wrote the original software. If you use this
//		software in a product, an acknowledgment in the product
//		documentation would be appreciated but is not required.
//	2.	Altered source versions must be plainly marked as such, and must
//		not be misrepresented as being the original software.
//	3.	This notice may not be removed or altered from any source
//		distribution.

#include <stdafx.h>
#include <vd2/system/hash.h>
#include <vd2/system/binary.h>
#include <vd2/system/int128.h>

uint32 VDHashString32(const char *s) {
	uint32 len = (uint32)strlen(s);

	return VDHashString32(s, len);
}

uint32 VDHashString32(const char *s, uint32 len) {
	uint32 hash = 2166136261U;

	for(uint32 i=0; i<len; ++i) {
		hash *= 16777619;
		hash ^= (unsigned char)s[i];
	}

	return hash;
}

uint32 VDHashString32(const wchar_t *s) {
	return VDHashString32(s, wcslen(s));
}

uint32 VDHashString32(const wchar_t *s, uint32 len) {
	uint32 hash = 2166136261U;

	for(uint32 i=0; i<len; ++i) {
		hash *= 16777619;
		hash ^= (unsigned)s[i];
	}

	return hash;
}

uint32 VDHashString32I(const char *s) {
	uint32 len = (uint32)strlen(s);

	return VDHashString32I(s, len);
}

uint32 VDHashString32I(const char *s, uint32 len) {
	uint32 hash = 2166136261U;

	for(uint32 i=0; i<len; ++i) {
		hash *= 16777619;
		hash ^= (uint32)tolower((unsigned char)*s++);
	}

	return hash;
}

uint32 VDHashString32I(const wchar_t *s) {
	uint32 len = (uint32)wcslen(s);

	return VDHashString32I(s, len);
}

uint32 VDHashString32I(const wchar_t *s, uint32 len) {
	uint32 hash = 2166136261U;

	for(uint32 i=0; i<len; ++i) {
		hash *= 16777619;
		hash ^= (uint32)towlower(*s++);
	}

	return hash;
}

///////////////////////////////////////////////////////////////////////////
//
// The algorithm used for the 128-bit hashing functions is MurmurHash3,
// originally released as MurmurHash3.cpp with the following statement:
//
//	MurmurHash3 was written by Austin Appleby, and is placed in the public
//	domain. The author hereby disclaims copyright to this source code.
//
// This particular version is the x86 128-bit version. It's a
// non-cryptographic hash function, and isn't intended to be used here for
// persistent purposes either.
//
///////////////////////////////////////////////////////////////////////////

#define ROTL32(value, bits) ((value << bits) | (value >> (32 - bits)))
#define FMIX32(h)	\
  h ^= h >> 16;	\
  h *= 0x85ebca6b;	\
  h ^= h >> 13;	\
  h *= 0xc2b2ae35;	\
  h ^= h >> 16

vduint128 VDHash128(const void *data0, size_t len) {
	const uint8 * data = (const uint8*)data0;
	const int nblocks = len >> 4;

	const uint32 seed = 0;
	uint32 h1 = seed;
	uint32 h2 = seed;
	uint32 h3 = seed;
	uint32 h4 = seed;

	const uint32 c1 = 0x239b961b;
	const uint32 c2 = 0xab0e9789;
	const uint32 c3 = 0x38b34ae5;
	const uint32 c4 = 0xa1e38b93;

	//----------
	// body

	const uint8 * tail = (const uint8*)(data + nblocks*16);

	for(ptrdiff_t offset = -(ptrdiff_t)(nblocks << 4);
		offset;
		offset += 16)
	{
		uint32 k1 = VDReadUnalignedU32(tail + offset + 0);
		uint32 k2 = VDReadUnalignedU32(tail + offset + 4);
		uint32 k3 = VDReadUnalignedU32(tail + offset + 8);
		uint32 k4 = VDReadUnalignedU32(tail + offset + 12);

		k1 *= c1; k1  = ROTL32(k1,15); k1 *= c2; h1 ^= k1;

		h1 = ROTL32(h1,19); h1 += h2; h1 = h1*5+0x561ccd1b;

		k2 *= c2; k2  = ROTL32(k2,16); k2 *= c3; h2 ^= k2;

		h2 = ROTL32(h2,17); h2 += h3; h2 = h2*5+0x0bcaa747;

		k3 *= c3; k3  = ROTL32(k3,17); k3 *= c4; h3 ^= k3;

		h3 = ROTL32(h3,15); h3 += h4; h3 = h3*5+0x96cd1c35;

		k4 *= c4; k4  = ROTL32(k4,18); k4 *= c1; h4 ^= k4;

		h4 = ROTL32(h4,13); h4 += h1; h4 = h4*5+0x32ac3b17;
	}

	//----------
	// tail


	uint32 k1 = 0;
	uint32 k2 = 0;
	uint32 k3 = 0;
	uint32 k4 = 0;

	switch(len & 15) {
	case 15:	k4 ^= tail[14] << 16;
	case 14:	k4 ^= tail[13] << 8;
	case 13:	k4 ^= tail[12] << 0;
				k4 *= c4; k4  = ROTL32(k4,18); k4 *= c1; h4 ^= k4;

	case 12:	k3 ^= tail[11] << 24;
	case 11:	k3 ^= tail[10] << 16;
	case 10:	k3 ^= tail[ 9] << 8;
	case  9:	k3 ^= tail[ 8] << 0;
				k3 *= c3; k3  = ROTL32(k3,17); k3 *= c4; h3 ^= k3;

	case  8:	k2 ^= tail[ 7] << 24;
	case  7:	k2 ^= tail[ 6] << 16;
	case  6:	k2 ^= tail[ 5] << 8;
	case  5:	k2 ^= tail[ 4] << 0;
				k2 *= c2; k2  = ROTL32(k2,16); k2 *= c3; h2 ^= k2;

	case  4:	k1 ^= tail[ 3] << 24;
	case  3:	k1 ^= tail[ 2] << 16;
	case  2:	k1 ^= tail[ 1] << 8;
	case  1:	k1 ^= tail[ 0] << 0;
				k1 *= c1; k1  = ROTL32(k1,15); k1 *= c2; h1 ^= k1;
	};

	//----------
	// finalization

	h1 ^= len; h2 ^= len; h3 ^= len; h4 ^= len;

	h1 += h2; h1 += h3; h1 += h4;
	h2 += h1; h3 += h1; h4 += h1;

	h1 = FMIX32(h1);
	h2 = FMIX32(h2);
	h3 = FMIX32(h3);
	h4 = FMIX32(h4);

	h1 += h2; h1 += h3; h1 += h4;
	h2 += h1; h3 += h1; h4 += h1;

	vduint128 result;
	result.d[0] = h1;
	result.d[1] = h2;
	result.d[2] = h3;
	result.d[3] = h4;

	return result;
}

#undef FMIX32
#undef ROTL32
