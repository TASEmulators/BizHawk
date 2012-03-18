/*
this c++/cli file implements the bridge between PsxHawk.Core and managed code.
currently this is a miserable little pile of hacks.
*/

#using <mscorlib.dll>
#using <System.dll>
#include <vcclr.h>
#include <msclr/marshal.h>
#include <msclr/marshal_cppstd.h>
#include <string>

#include "psx.h"
#include "loader.h"

using namespace msclr::interop;

public ref class PsxApi
{
public:
	PSX *psx;

	//this endeavours to fix some issues with routing stdio between .net and the CRT
	static void StdioFixes()
	{
		HANDLE handle = GetStdHandle(STD_OUTPUT_HANDLE);
		DWORD fileType = GetFileType(handle);
		bool shouldReopen = fileType == FILE_TYPE_CHAR;
		if(shouldReopen)
			freopen("CONOUT$", "w", stdout);

		handle = GetStdHandle(STD_ERROR_HANDLE);
		fileType = GetFileType(handle);
		shouldReopen = fileType == FILE_TYPE_CHAR;
		if(shouldReopen)
			freopen("CONOUT$", "w", stderr);
	}

	PsxApi()
	{
		StdioFixes();

		//initialize the psx instance
		psx = new PSX();
		psx->poweron(PSX::eConsoleType_DTL);	
		Load_BIOS(*psx, "B:\\svn\\bizhawk4\\BizHawk.MultiClient\\output\\SCPH5500.bin"); //JP bios (apparently thats what region our test programs are, or what mednafen ends up using)
		//Load_BIOS(psx, "scph5501.bin"); 
		psx->reset();
	}

	void Load_EXE(System::String^ str)
	{
		::Load_EXE(*psx, marshal_as<std::wstring>(str).c_str());
	}

	void RunForever()
	{
		psx->RunForever();
	}
};
