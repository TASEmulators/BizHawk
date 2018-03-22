#ifndef NEWSTATE_H
#define NEWSTATE_H

#include <cstring>
#include <cstddef>

namespace gambatte {

class NewState
{
public:
	virtual void Save(const void *ptr, size_t size, const char *name) = 0;
	virtual void Load(void *ptr, size_t size, const char *name) = 0;
	virtual void EnterSection(const char *name) { }
	virtual void ExitSection(const char *name) { }
};

class NewStateDummy : public NewState
{
private:
	long length;
public:
	NewStateDummy();
	long GetLength() { return length; }
	void Rewind() { length = 0; }
	virtual void Save(const void *ptr, size_t size, const char *name);
	virtual void Load(void *ptr, size_t size, const char *name);
};

class NewStateExternalBuffer : public NewState
{
private:
	char *const buffer;
	long length;
	const long maxlength;
public:
	NewStateExternalBuffer(char *buffer, long maxlength);
	long GetLength() { return length; }
	void Rewind() { length = 0; }
	bool Overflow() { return length > maxlength; }
	virtual void Save(const void *ptr, size_t size, const char *name);
	virtual void Load(void *ptr, size_t size, const char *name);
};

struct FPtrs
{
	void (*Save_)(const void *ptr, size_t size, const char *name);
	void (*Load_)(void *ptr, size_t size, const char *name);
	void (*EnterSection_)(const char *name);
	void (*ExitSection_)(const char *name);
};

class NewStateExternalFunctions : public NewState
{
private:
	void (*Save_)(const void *ptr, size_t size, const char *name);
	void (*Load_)(void *ptr, size_t size, const char *name);
	void (*EnterSection_)(const char *name);
	void (*ExitSection_)(const char *name);
public:
	NewStateExternalFunctions(const FPtrs *ff);
	virtual void Save(const void *ptr, size_t size, const char *name);
	virtual void Load(void *ptr, size_t size, const char *name);
	virtual void EnterSection(const char *name);
	virtual void ExitSection(const char *name);
};

// defines and explicitly instantiates 
#define SYNCFUNC(x)\
	template void x::SyncState<false>(NewState *ns);\
	template void x::SyncState<true>(NewState *ns);\
	template<bool isReader>void x::SyncState(NewState *ns)

// N = normal variable
// P = pointer to fixed size data
// S = "sub object"
// T = "ptr to sub object"
// R = pointer, store its offset from some other pointer
// E = general purpose cased value "enum"


// first line is default value in converted enum; last line is default value in argument x
#define EBS(x,d) do { int _ttmp = (d); if (isReader) ns->Load(&_ttmp, sizeof(_ttmp), #x); if (0)
#define EVS(x,v,n) else if (!isReader && (x) == (v)) _ttmp = (n); else if (isReader && _ttmp == (n)) (x) = (v)
#define EES(x,d) else if (isReader) (x) = (d); if (!isReader) ns->Save(&_ttmp, sizeof(_ttmp), #x); } while (0)

#define RSS(x,b) do { if (isReader)\
{ ptrdiff_t _ttmp; ns->Load(&_ttmp, sizeof(_ttmp), #x); (x) = (_ttmp == (ptrdiff_t)0xdeadbeef ? 0 : (b) + _ttmp); }\
	else\
{ ptrdiff_t _ttmp = (x) == 0 ? 0xdeadbeef : (x) - (b); ns->Save(&_ttmp, sizeof(_ttmp), #x); } } while (0)

#define PSS(x,s) do { if (isReader) ns->Load((x), (s), #x); else ns->Save((x), (s), #x); } while (0)

#define NSS(x) do { if (isReader) ns->Load(&(x), sizeof(x), #x); else ns->Save(&(x), sizeof(x), #x); } while (0)

#define SSS(x) do { ns->EnterSection(#x); (x).SyncState<isReader>(ns); ns->ExitSection(#x); } while (0)

#define TSS(x) do { ns->EnterSection(#x); (x)->SyncState<isReader>(ns); ns->ExitSection(#x); } while (0)

}

#endif
