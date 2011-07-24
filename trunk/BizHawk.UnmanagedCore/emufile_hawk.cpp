#include "emufile_hawk.h"
#include "core.h"

static FunctionRecord records[] = {
	FUNC("EmuFile.Construct", &EMUFILE_HAWK::Construct),
	FUNC("EmuFile.Set_fp", &EMUFILE_HAWK::Set_fp),
	FUNC("EmuFile.Delete", &EMUFILE_HAWK::Delete),
};


int EMUFILE_HAWK::fprintf(const char *format, ...)
{ 
	va_list argptr;
	va_start(argptr, format);
	
	//could use a small static buf here optionally for quickness's sake but we may regret it if we multithread later

	int amt = vsnprintf(0,0,format,argptr);
	char* tempbuf = new char[amt+1];
	vsprintf(tempbuf,format,argptr);
	fwrite(tempbuf,amt);
	delete[] tempbuf;
	va_end(argptr);
	return amt;
}


void* EMUFILE_HAWK::Construct(void* ManagedOpaque)
{
	return new EMUFILE_HAWK(ManagedOpaque);
}

