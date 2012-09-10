#include "cinterface.h"
#include "gambatte.h"
#include <cstdlib>

using namespace gambatte;


__declspec(dllexport) void *gambatte_create()
{
	GB *g = new GB();
	return (void *) g;
}

__declspec(dllexport) void gambatte_destroy(void *core)
{
	GB *g = (GB *) core;
	delete g;
}

__declspec(dllexport) int gambatte_load(void *core, const char *romfiledata, unsigned romfilelength, unsigned flags)
{
	GB *g = (GB *) core;
	int ret = g->load(romfiledata, romfilelength, flags);
	return ret;
}

__declspec(dllexport) long gambatte_runfor(void *core, unsigned long *videobuf, int pitch, short *soundbuf, unsigned *samples)
{
	GB *g = (GB *) core;
	unsigned sampv = *samples;
	long ret = g->runFor(videobuf, pitch, (unsigned long *) soundbuf, sampv);
	*samples = sampv;
	return ret;
}

__declspec(dllexport) void gambatte_reset(void *core)
{
	GB *g = (GB *) core;
	g->reset();
}

__declspec(dllexport) void gambatte_setdmgpalettecolor(void *core, unsigned palnum, unsigned colornum, unsigned rgb32)
{
	GB *g = (GB *) core;
	g->setDmgPaletteColor(palnum, colornum, rgb32);
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

__declspec(dllexport) void gambatte_setinputgetter(void *core, unsigned (*getinput)(void))
{
	GB *g = (GB *) core;
	CInputGetter *cig = new CInputGetter();
	cig->inputfunc = getinput;
	// how do i manage the lifetime of cig?
	g->setInputGetter(cig);
}

__declspec(dllexport) void gambatte_setsavedir(void *core, const char *sdir)
{
	GB *g = (GB *) core;
	g->setSaveDir(std::string(sdir));
}

__declspec(dllexport) int gambatte_iscgb(void *core)
{
	GB *g = (GB *) core;
	return g->isCgb();
}

__declspec(dllexport) int gambatte_isloaded(void *core)
{
	GB *g = (GB *) core;
	return g->isLoaded();
}

/*
__declspec(dllexport) void gambatte_savesavedata(void *core)
{
	GB *g = (GB *) core;
	g->saveSavedata();
}
*/

__declspec(dllexport) void gambatte_savesavedata(void *core, char *dest)
{
	GB *g = (GB *) core;
	g->saveSavedata(dest);
}

__declspec(dllexport) void gambatte_loadsavedata(void *core, const char *data)
{
	GB *g = (GB *) core;
	g->loadSavedata(data);
}

__declspec(dllexport) int gambatte_savesavedatalength(void *core)
{
	GB *g = (GB *) core;
	return g->saveSavedataLength();
}

/*
__declspec(dllexport) int gambatte_savestate(void *core, const unsigned long *videobuf, int pitch)
{
	GB *g = (GB *) core;
	return g->saveState(videobuf, pitch);
}

__declspec(dllexport) int gambatte_loadstate(void *core)
{
	GB *g = (GB *) core;
	return g->loadState();
}
*/

__declspec(dllexport) int gambatte_savestate(void *core, const unsigned long *videobuf, int pitch, char **data, unsigned *len)
{
	GB *g = (GB *) core;

	std::ostringstream os = std::ostringstream(std::ios_base::binary | std::ios_base::out);
	if (!g->saveState(videobuf, pitch, os))
		return 0;

	os.flush();
	std::string s = os.str();
	char *ret = (char *) std::malloc(s.length());
	std::memcpy(ret, s.data(), s.length());
	*len = s.length();
	*data = ret;
	return 1;
}

__declspec(dllexport) void gambatte_savestate_destroy(char *data)
{
	std::free(data);
}

__declspec(dllexport) int gambatte_loadstate(void *core, const char *data, unsigned len)
{
	GB *g = (GB *) core;
	return g->loadState(std::istringstream(std::string(data, len), std::ios_base::binary | std::ios_base::in));
}

/*
__declspec(dllexport) void gambatte_selectstate(void *core, int n)
{
	GB *g = (GB *) core;
	g->selectState(n);
}

__declspec(dllexport) int gambatte_currentstate(void *core)
{
	GB *g = (GB *) core;
	return g->currentState();
}
*/

static char horriblebuff[64];
__declspec(dllexport) const char *gambatte_romtitle(void *core)
{
	GB *g = (GB *) core;
	const char *s = g->romTitle().c_str();
	std::strncpy(horriblebuff, s, 63);
	horriblebuff[63] = 0;
	return horriblebuff;
}

__declspec(dllexport) void gambatte_setgamegenie(void *core, const char *codes)
{
	GB *g = (GB *) core;
	g->setGameGenie(std::string(codes));
}

__declspec(dllexport) void gambatte_setgameshark(void *core, const char *codes)
{
	GB *g = (GB *) core;
	g->setGameShark(std::string(codes));
}

