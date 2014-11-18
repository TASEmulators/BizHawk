
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

EXPORT void SetRotation(CSystem *s, int value)
{
	s->rotate = value;
}

EXPORT int Advance(CSystem *s, int buttons, uint32 *vbuff, int16 *sbuff, int *sbuffsize)
{
	return s->Advance(buttons, vbuff, sbuff, *sbuffsize);
}

EXPORT int GetSaveRamPtr(CSystem *s, int *size, uint8 **data)
{
	return s->GetSaveRamPtr(*size, *data);
}

EXPORT void GetReadOnlyCartPtrs(CSystem *s, int *s0, uint8 **p0, int *s1, uint8 **p1)
{
	s->GetReadOnlyCartPtrs(*s0, *p0, *s1, *p1);
}

EXPORT int BinStateSize(CSystem *s)
{
	NewStateDummy dummy;
	s->SyncState<false>(&dummy);
	return dummy.GetLength();
}

EXPORT int BinStateSave(CSystem *s, char *data, int length)
{
	NewStateExternalBuffer saver(data, length);
	s->SyncState<false>(&saver);
	return !saver.Overflow() && saver.GetLength() == length;
}
	
EXPORT int BinStateLoad(CSystem *s, const char *data, int length)
{
	NewStateExternalBuffer loader(const_cast<char *>(data), length);
	s->SyncState<true>(&loader);
	return !loader.Overflow() && loader.GetLength() == length;
}

EXPORT void TxtStateSave(CSystem *s, FPtrs *ff)
{
	NewStateExternalFunctions saver(ff);
	s->SyncState<false>(&saver);
}

EXPORT void TxtStateLoad(CSystem *s, FPtrs *ff)
{
	NewStateExternalFunctions loader(ff);
	s->SyncState<true>(&loader);
}

EXPORT void *GetRamPointer(CSystem *s)
{
	return s->GetRamPointer();
}

