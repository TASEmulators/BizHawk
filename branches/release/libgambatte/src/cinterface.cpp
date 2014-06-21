#include "cinterface.h"
#include "gambatte.h"
#include <cstdlib>
#include "newstate.h"

using namespace gambatte;


GBEXPORT void *gambatte_create()
{
	GB *g = new GB();
	return (void *) g;
}

GBEXPORT void gambatte_destroy(void *core)
{
	GB *g = (GB *) core;
	delete g;
}

GBEXPORT int gambatte_load(void *core, const char *romfiledata, unsigned romfilelength, long long now, unsigned flags)
{
	GB *g = (GB *) core;
	int ret = g->load(romfiledata, romfilelength, now, flags);
	return ret;
}

GBEXPORT long gambatte_runfor(void *core, short *soundbuf, unsigned *samples)
{
	GB *g = (GB *) core;
	unsigned sampv = *samples;
	long ret = g->runFor((unsigned int *) soundbuf, sampv);
	*samples = sampv;
	return ret;
}

GBEXPORT void gambatte_blitto(void *core, unsigned long *videobuf, int pitch)
{
	GB *g = (GB *) core;
	g->blitTo((unsigned int *)videobuf, pitch);
}

GBEXPORT void gambatte_reset(void *core, long long now)
{
	GB *g = (GB *) core;
	g->reset(now);
}

GBEXPORT void gambatte_setdmgpalettecolor(void *core, unsigned palnum, unsigned colornum, unsigned rgb32)
{
	GB *g = (GB *) core;
	g->setDmgPaletteColor(palnum, colornum, rgb32);
}

GBEXPORT void gambatte_setcgbpalette(void *core, unsigned *lut)
{
	GB *g = (GB *) core;
	g->setCgbPalette(lut);
}

class CInputGetter: public InputGetter
{
public: 
	
	unsigned (*inputfunc)(void);
	unsigned operator()()
	{
		return inputfunc ();
	}
};

GBEXPORT void gambatte_setinputgetter(void *core, unsigned (*getinput)(void))
{
	GB *g = (GB *) core;
	CInputGetter *cig = new CInputGetter();
	cig->inputfunc = getinput;
	// how do i manage the lifetime of cig?
	g->setInputGetter(cig);
}

GBEXPORT void gambatte_setreadcallback(void *core, void (*callback)(unsigned))
{
	GB *g = (GB *) core;
	g->setReadCallback(callback);
}

GBEXPORT void gambatte_setwritecallback(void *core, void (*callback)(unsigned))
{
	GB *g = (GB *) core;
	g->setWriteCallback(callback);
}

GBEXPORT void gambatte_setexeccallback(void *core, void (*callback)(unsigned))
{
	GB *g = (GB *) core;
	g->setExecCallback(callback);
}

GBEXPORT void gambatte_settracecallback(void *core, void (*callback)(void *))
{
	GB *g = (GB *) core;
	g->setTraceCallback(callback);
}

GBEXPORT void gambatte_setscanlinecallback(void *core, void (*callback)(), int sl)
{
	GB *g = (GB *) core;
	g->setScanlineCallback(callback, sl);
}

GBEXPORT void gambatte_setrtccallback(void *core, unsigned int (*callback)())
{
	GB *g = (GB *) core;
	g->setRTCCallback(callback);
}

GBEXPORT int gambatte_iscgb(void *core)
{
	GB *g = (GB *) core;
	return g->isCgb();
}

GBEXPORT int gambatte_isloaded(void *core)
{
	GB *g = (GB *) core;
	return g->isLoaded();
}

GBEXPORT void gambatte_savesavedata(void *core, char *dest)
{
	GB *g = (GB *) core;
	g->saveSavedata(dest);
}

GBEXPORT void gambatte_loadsavedata(void *core, const char *data)
{
	GB *g = (GB *) core;
	g->loadSavedata(data);
}

GBEXPORT int gambatte_savesavedatalength(void *core)
{
	GB *g = (GB *) core;
	return g->saveSavedataLength();
}

GBEXPORT long gambatte_newstatelen(void *core)
{
	GB *g = (GB *) core;
	NewStateDummy dummy;
	g->SyncState<false>(&dummy);
	return dummy.GetLength();
}

GBEXPORT int gambatte_newstatesave(void *core, char *data, long len)
{
	GB *g = (GB *) core;
	NewStateExternalBuffer saver(data, len);
	g->SyncState<false>(&saver);
	return !saver.Overflow() && saver.GetLength() == len;
}

GBEXPORT int gambatte_newstateload(void *core, const char *data, long len)
{
	GB *g = (GB *) core;
	NewStateExternalBuffer loader((char *)data, len);
	g->SyncState<true>(&loader);
	return !loader.Overflow() && loader.GetLength() == len;
}

GBEXPORT void gambatte_newstatesave_ex(void *core, FPtrs *ff)
{
	GB *g = (GB *) core;
	NewStateExternalFunctions saver(ff);
	g->SyncState<false>(&saver);
}

GBEXPORT void gambatte_newstateload_ex(void *core, FPtrs *ff)
{
	GB *g = (GB *) core;
	NewStateExternalFunctions loader(ff);
	g->SyncState<true>(&loader);
}

static char horriblebuff[64];
GBEXPORT const char *gambatte_romtitle(void *core)
{
	GB *g = (GB *) core;
	const char *s = g->romTitle().c_str();
	std::strncpy(horriblebuff, s, 63);
	horriblebuff[63] = 0;
	return horriblebuff;
}

GBEXPORT int gambatte_getmemoryarea(void *core, int which, unsigned char **data, int *length)
{
	GB *g = (GB *) core;
	return g->getMemoryArea(which, data, length);
}

GBEXPORT unsigned char gambatte_cpuread(void *core, unsigned short addr)
{
	GB *g = (GB *) core;
	return g->ExternalRead(addr);
}

GBEXPORT void gambatte_cpuwrite(void *core, unsigned short addr, unsigned char val)
{
	GB *g = (GB *) core;
	g->ExternalWrite(addr, val);
}

GBEXPORT int gambatte_linkstatus(void *core, int which)
{
	GB *g = (GB *) core;
	return g->LinkStatus(which);
}

GBEXPORT void gambatte_getregs(void *core, int *dest)
{
	GB *g = (GB *) core;
	g->GetRegs(dest);
}
