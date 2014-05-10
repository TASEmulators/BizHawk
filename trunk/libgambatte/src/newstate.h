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

class NewStateExternalFunctions : public NewState
{
private:
	void (*Save_)(const void *ptr, size_t size, const char *name);
	void (*Load_)(void *ptr, size_t size, const char *name);
	void (*EnterSection_)(const char *name);
	void (*ExitSection_)(const char *name);
public:
	NewStateExternalFunctions(
		void (*Save_)(const void *ptr, size_t size, const char *name),
		void (*Load_)(void *ptr, size_t size, const char *name),
		void (*EnterSection_)(const char *name),
		void (*ExitSection_)(const char *name));
	virtual void Save(const void *ptr, size_t size, const char *name);
	virtual void Load(void *ptr, size_t size, const char *name);
	virtual void EnterSection(const char *name);
	virtual void ExitSection(const char *name);
};


// N = normal variable
// P = pointer to fixed size data
// S = "sub object"
// T = "ptr to sub object"
// R = pointer, store its offset from some other pointer
// E = general purpose cased value "enum"


// first line is default value in converted enum; last line is default value in argument x
#define EBS(x,d) do { int _ttmp = (d); if (0)
#define EVS(x,v,n) else if ((x) == (v)) _ttmp = (n)
#define EES(x,d) ns->Save(&_ttmp, sizeof(_ttmp), #x); } while (0)

#define EBL(x,d) do { int _ttmp = (d); ns->Load(&_ttmp, sizeof(_ttmp), #x); if (0)
#define EVL(x,v,n) else if (_ttmp == (n)) (x) = (v)
#define EEL(x,d) else (x) = (d); } while (0)


#define RSS(x,b) do { ptrdiff_t _ttmp = (x) == 0 ? 0xdeadbeef : (x) - (b); ns->Save(&_ttmp, sizeof(_ttmp), #x); } while (0)
#define RSL(x,b) do { ptrdiff_t _ttmp; ns->Load(&_ttmp, sizeof(_ttmp), #x); (x) = (_ttmp == 0xdeadbeef ? 0 : (b) + _ttmp); } while (0)

#define PSS(x,s) ns->Save((x), (s), #x)
#define PSL(x,s) ns->Load((x), (s), #x)

#define NSS(x) ns->Save(&(x), sizeof(x), #x)
#define NSL(x) ns->Load(&(x), sizeof(x), #x)

#define SSS(x) do { ns->EnterSection(#x); (x).SaveS(ns); ns->ExitSection(#x); } while (0)
#define SSL(x) do { ns->EnterSection(#x); (x).LoadS(ns); ns->ExitSection(#x); } while (0)

#define TSS(x) do { ns->EnterSection(#x); (x)->SaveS(ns); ns->ExitSection(#x); } while (0)
#define TSL(x) do { ns->EnterSection(#x); (x)->LoadS(ns); ns->ExitSection(#x); } while (0)

}

#endif
