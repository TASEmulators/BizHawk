#pragma once
#include <stdint.h>

#define SDL_SwapLE16(X) (X)
#define SDL_SwapLE32(X) (X)
#define SDL_SwapLE64(X) (X)
#define SDL_SwapFloatLE(X) (X)
#define SDL_SwapBE16(X) SDL_Swap16(X)
#define SDL_SwapBE32(X) SDL_Swap32(X)
#define SDL_SwapBE64(X) SDL_Swap64(X)
#define SDL_SwapFloatBE(X)  SDL_SwapFloat(X)

__inline uint16_t SDL_Swap16(uint16_t v)
{
	return v << 8 | v >> 8;
}
__inline uint32_t SDL_Swap32(uint32_t v)
{
	return v << 24 & 0xff000000
		| v << 8 & 0x00ff0000
		| v >> 8 & 0x0000ff00
		| v >> 24 & 0x000000ff;
}
