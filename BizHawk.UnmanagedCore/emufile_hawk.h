#ifndef _EMUFILE_HAWK_H_
#define _EMUFILE_HAWK_H_

#include "emufile.h"
#include "core.h"

class EMUFILE_HAWK : public EMUFILE
{
	void* ManagedOpaque;


	struct {
		FUNC<int()> fgetc;
		FUNC<int(const void* ptr, size_t bytes)> fread;
		FUNC<void(const void* ptr, size_t bytes)> fwrite;
		FUNC<int(int offset, int origin)> fseek;
		FUNC<int()> ftell;
		FUNC<int()> size;
		FUNC<void()> dispose;
	} _;

public:
	~EMUFILE_HAWK()
	{
		_.dispose();
	}

	EMUFILE_HAWK(void* _ManagedOpaque)
		: ManagedOpaque(_ManagedOpaque)
	{
	}

	void* Construct(void* ManagedOpaque);

	void Delete()
	{
		delete this;
	}

	void Set_fp(const char* param, void* value)
	{
		if(!strcmp(param,"fgetc")) _.fgetc.set(value);
		if(!strcmp(param,"fread")) _.fread.set(value);
		if(!strcmp(param,"fwrite")) _.fwrite.set(value);
		if(!strcmp(param,"fseek")) _.fseek.set(value);
		if(!strcmp(param,"ftell")) _.ftell.set(value);
		if(!strcmp(param,"size")) _.size.set(value);
		if(!strcmp(param,"dispose")) _.dispose.set(value);
	}

	virtual int fgetc() { return _.fgetc(); }
	virtual FILE *get_fp() { return NULL; }
	virtual int fputc(int c) { return -1; }
	virtual int fprintf(const char *format, ...);
	virtual size_t _fread(const void *ptr, size_t bytes) { return _.fread(ptr,bytes); }
	virtual void fwrite(const void *ptr, size_t bytes) { return _.fwrite(ptr,bytes); }
	virtual int fseek(int offset, int origin) { return _.fseek(offset,origin); }
	virtual int ftell() { return _.ftell(); }
	virtual int size() { return _.size(); }
};


#endif //_EMUFILE_HAWK_H_
