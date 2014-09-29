
#include <cstdlib>

#include "system.h"

void *operator new(std::size_t n)
{
	void *p = std::malloc(n);
	std::memset(p, 0, n);
	return p;
}

void operator delete(void *p)
{
	std::free(p);
}

#define EXPORT extern "C" __declspec(dllexport)

EXPORT CSystem *Create(const uint8 *game, uint32 gamesize, const uint8 *bios, uint32 biossize, int pagesize0, int pagesize1, int lowpass)
{
	return new CSystem(game, gamesize, bios, biossize, pagesize0, pagesize1, lowpass);
}

EXPORT void Destroy(CSystem *s)
{
	delete s;
}

EXPORT void Reset(CSystem *s)
{
	s->Reset();
}

EXPORT void Advance(CSystem *s, int buttons, uint32 *vbuff, int16 *sbuff, int *sbuffsize)
{
	s->Advance(buttons, vbuff, sbuff, *sbuffsize);
}

EXPORT int GetSaveRamPtr(CSystem *s, int *size, uint8 **data)
{
	return s->GetSaveRamPtr(*size, *data);
}
