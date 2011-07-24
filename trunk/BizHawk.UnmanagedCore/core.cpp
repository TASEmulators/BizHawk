#include <string>
#include "core.h"
#include "emufile.h"
#include "emufile_hawk.h"


//TODO
class DISC_INTERFACE
{
};


//TODO - setup a null file to use as the default console, so we dont have to check whether its set to null everywhere
class EMUFILE_HAWK;
EMUFILE_HAWK* con = NULL;

static void* (*ClientSignal_cb)(const char*,void*,const char*,void*);
void* ClientSignal(const char* type, void* obj, const char* _param, void* value)
{
	return ClientSignal_cb(type,obj,_param,value);
}


//core objects function pointers get registered here
class FunctionRegistry
{
private:

	typedef std::map<std::string, void*> TFunctionMap;
	TFunctionMap map;

public:
	static FunctionRegistry& Instance() {
		static FunctionRegistry inst;
		return inst;
	}

	void Register(const char* _name, void* _funcptr)
	{
		map[_name] = _funcptr;
	}

	void* Lookup(const char* name)
	{
		TFunctionMap::iterator it(map.find(name));
		if(it == map.end()) return NULL;
		else return it->second;
	}

private:
	FunctionRegistry() {}
};

void _registerFunction(const char* _name, void* _funcptr)
{
	FunctionRegistry::Instance().Register(_name,_funcptr);
}

//maybe youll need this some day... but probably not.
//#pragma comment(linker, "/include:_Core_signal")
extern "C" __declspec(dllexport) void* Core_signal(const char* type, void* obj, const char* param, void* value)
{
	//use this to log signals
	if(con) con->fprintf("core signal: %s : %s\n",type?type:"n/a",param?param:"n/a");

	if(!strcmp(type,"SET_CLIENT_SIGNAL"))
	{
		ClientSignal_cb = (void *(*)(const char*,void*,const char*,void*))value;
		return 0;
	}

	if(!strcmp(type,"SET_CONSOLE"))
	{
		con = (EMUFILE_HAWK*)value;
		return 0;
	}

	//query a function pointer for later blazing fast reuse
	if(!strcmp(type,"QUERY_FUNCTION"))
		return FunctionRegistry::Instance().Lookup(param);

	//TODO - custom core static operations?

	//force a reference to our core types. a bit annoying but if its this easy i guess i dont mind
	if(!strcmp(type,"IMPOSSIBLE"))
	{
		return new EMUFILE_HAWK(0);
	}

	return 0;
}
