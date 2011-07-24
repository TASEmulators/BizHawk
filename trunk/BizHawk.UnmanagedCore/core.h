#ifndef _CORE_H_
#define _CORE_H_

#include <map>
#include <string>

#ifndef CTASSERT
#define CTASSERT(x)  typedef char __assert ## y[(x) ? 1 : -1];
#endif

//use this to send a signal to the client.
//it may not be completely fully-baked yet though
void* ClientSignal(const char* type, void* obj, const char* _param, void* value);

class EMUFILE_HAWK;

//use this to print output to the client
extern EMUFILE_HAWK* con;


//this is supposedly illegal. but i say its perfectly legal, as long as im not using virtual functions. so stuff it.
//well, since we're doing it illegally, we need to resist the urge to generalize the function pointer system (to emufile and disc)
//since we may need to change all these later to work un-generalized
//but seriously. before doing that, i would rather return sizeof(functionpointer) bytes as a token to the managed code and pass that back in
//(MP stands for MEMBER POINTER)
template<typename T> void* MP(const T& a)
{
	union U{
		void* vp;
		T t;
	} u;
	u.t = a;
	return u.vp;
	CTASSERT(sizeof(U)==4||(sizeof(U)==8&&sizeof(void*)==8));
}

//this is a function pointer which can be assigned without having to type the function protoype again to cast it.
template<typename T> class FP
{
private:
	template<typename T> T MPX(void* a)
	{
		union U{
			void* vp;
			T t;
		} u;
		u.vp = a;
		return u.t;
		CTASSERT(sizeof(U)==4||(sizeof(U)==8&&sizeof(void*)==8));
	}
public:
	T func;
	void set(void* val) { func = MPX<T>(val); }
};

//nothing important
void _registerFunction(const char* _name, void* _funcptr);
struct FunctionRecord
{
	FunctionRecord(const char* _name, void* _funcptr)
	{
		_registerFunction(_name,_funcptr);
	}
};

//register a core object member function. put it in a global static array
template<typename T> FunctionRecord FUNC(const char* name, const T& a)
{
	return FunctionRecord(name,MP(a));
}

#endif //_CORE_H_
