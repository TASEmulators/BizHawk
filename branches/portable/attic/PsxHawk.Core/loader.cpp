/*
this file contains stuff which isn't realistic emulation but is used for loading and bootstrapping things
*/

#include <stdio.h>
#include <string.h>
#include <assert.h>

#include "types.h"
#include "loader.h"
#include "asm.h"

void Load_BIOS(PSX& psx, const char* path)
{
	FILE* inf = fopen(path,"rb");
	fread(psx.bios,1,BIOS_SIZE,inf);
	fclose(inf);
}

bool Load_EXE_Check(const char* fname)
{
	//check for the PSX EXE signature
	FILE* inf = fopen(fname, "rb");
	char tmp[8] = {0};
	fread(tmp,1,8,inf);
	fclose(inf);
	return !memcmp(tmp, "PS-X EXE", 8);
}

//TODO - we could load other format EXEs as well, not just PSX-EXE (psxjin appears to do this? check PSXGetFileType and Load() in misc.cpp)
void Load_EXE(PSX& psx, const wchar_t* fname)
{
	FILE* inf = _wfopen(fname, L"rb");
	PSX_EXE_Header header;
	fread(&header,sizeof(PSX_EXE_Header),1,inf);

	//load the text section to main memory
	u32 text_destination = header.text_load_addr & RAM_MASK; //convert from virtual address to physical
	fseek(inf, header.text_exe_offset + 0x800, SEEK_SET); //image addresses are relative to the image section of the file (past the 0x800 header)
	fread(psx.ram+text_destination,1,header.text_size,inf);

	//now, mednafen patches the bios to run its own routine loaded to PIO which loads the program from fake memory.
	//i have a better idea. lets patch it with a special escape code which will run the bootstrapping code in C
	psx.patch(0xBFC06990, ASM_BREAK(PSX::eFakeBreakOp_BootEXE));
	psx.exeBootHeader = header;

	//patch the kernel image section of the bios with traps for our bios hacks
	psx.patch(0xBFC10000, ASM_BREAK(PSX::eFakeBreakOp_BiosHack)); //im not sure why we have to include these two. they must get chosen for some other reason to get patched into the kernel
	psx.patch(0xBFC10010, ASM_BREAK(PSX::eFakeBreakOp_BiosHack)); //..
	psx.patch(0xBFC10020, ASM_BREAK(PSX::eFakeBreakOp_BiosHack)); //this should correspond to 0xA0 in kernel area
	psx.patch(0xBFC10030, ASM_BREAK(PSX::eFakeBreakOp_BiosHack)); //this should correspond to 0xB0 in kernel area 

}