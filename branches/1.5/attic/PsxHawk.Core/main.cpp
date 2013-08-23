//#include <windows.h>
//#include <stdio.h>
//
//#include "psx.h"
//#include "loader.h"
//
//PSX psx;
//
//void main(int argc, char **argv)
//{
//	//const char* target = argv[1];
//	const char* target = "C:\\psxsdk\\projects\\helloworld\\main-sdk.exe";
//	
//	//can we load it as a PSX EXE? thats the only format we understand so far
//	if(!Load_EXE_Check(target)) return;
//
//	//initialize the psx 
//	psx.poweron(PSX::eConsoleType_DTL);
//	Load_BIOS(psx, "SCPH5500.bin"); //JP bios (apparently thats what region our test programs are, or what mednafen ends up using)
//	//Load_BIOS(psx, "scph5501.bin"); 
//	//Load_BIOS(psx, "DTLH1100.bin"); 
//	Load_EXE(psx, target);
//	psx.reset();
//
//	static const int work = 33*1024*1024*10;
//	DWORD a = timeGetTime();
//	for(;;)
//	{
//		psx.cpu_exec_cycle();
//		if(psx.counter == work) break;
//	}
//	DWORD b = timeGetTime();
//	printf("%d ms\n",b-a);
//}