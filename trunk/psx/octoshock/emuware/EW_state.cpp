#include "EW_state.h"
#include <cstring>
#include <algorithm>
#include <stdarg.h>
#include <stdio.h>
#include <stdlib.h>

namespace EW {

NewStateDummy::NewStateDummy()
	:length(0)
{
}
void NewStateDummy::Save(const void *ptr, size_t size, const char *name)
{
	length += size;
}
void NewStateDummy::Load(void *ptr, size_t size, const char *name)
{
}

NewStateExternalBuffer::NewStateExternalBuffer(char *buffer, long maxlength)
	:buffer(buffer), length(0), maxlength(maxlength)
{
}

void NewStateExternalBuffer::Save(const void *ptr, size_t size, const char *name)
{
	if (maxlength - length >= (long)size)
	{
		std::memcpy(buffer + length, ptr, size);
	}
	length += size;
}

void NewStateExternalBuffer::Load(void *ptr, size_t size, const char *name)
{
	char *dst = static_cast<char *>(ptr);
	if (maxlength - length >= (long)size)
	{
		std::memcpy(dst, buffer + length, size);
	}
	length += size;
}

NewStateExternalFunctions::NewStateExternalFunctions(const FPtrs *ff)
	:Save_(ff->Save_),
	Load_(ff->Load_),
	EnterSection_(ff->EnterSection_),
	ExitSection_(ff->ExitSection_)
{
}

void NewStateExternalFunctions::Save(const void *ptr, size_t size, const char *name)
{
	Save_(ptr, size, name);
}
void NewStateExternalFunctions::Load(void *ptr, size_t size, const char *name)
{
	Load_(ptr, size, name);
}

void NewStateExternalFunctions::EnterSection(const char *name, ...)
{
	//analysis: multiple passes to generate string not ideal, but there arent many sections.. so it should be OK. improvement would be special vararg overload
	va_list ap;
	va_start(ap,name);
	char easybuf[32];
	int size = vsnprintf(easybuf,0,name,ap);
	char *ptr = easybuf;
	if(size>31)
		ptr = (char*)malloc(size+1);
	vsprintf(ptr,name,ap);
	EnterSection_(ptr);
	if(ptr != easybuf)
		free(ptr);
	va_end(ap);
}
void NewStateExternalFunctions::ExitSection(const char *name, ...)
{
	va_list ap;
	va_start(ap,name);
	char easybuf[32];
	int size = vsnprintf(easybuf,0,name,ap);
	char *ptr = easybuf;
	if(size>31)
		ptr = (char*)malloc(size+1);
	vsprintf(ptr,name,ap);
	ExitSection_(ptr);
	if(ptr != easybuf)
		free(ptr);
	va_end(ap);
}


}
