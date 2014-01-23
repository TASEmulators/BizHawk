#ifndef MOS6502X_H
#define MOS6502X_H

#include "ints.h"

class MOS6502X
{
public:
	bool BCD_Enabled;
	bool debug;
	bool throw_unhandled;

	byte A;
	byte X;
	byte Y;
	byte P;
	ushort PC;
	byte S;

	bool IRQ;
	bool NMI;

	int TotalExecutedCycles;

	byte (__cdecl *ReadMemory)(ushort);
	byte (__cdecl *DummyReadMemory)(ushort);
	void (__cdecl *WriteMemory)(ushort, byte);

	//opcode bytes.. theoretically redundant with the temp variables? who knows.
	int opcode;
	byte opcode2, opcode3;

	int ea, alu_temp; //cpu internal temp variables
	int mi; //microcode index
	bool iflag_pending; //iflag must be stored after it is checked in some cases (CLI and SEI).

	//tracks whether an interrupt condition has popped up recently.
	//not sure if this is real or not but it helps with the branch_irq_hack
	bool interrupt_pending; 
	bool branch_irq_hack; //see Uop.RelBranch_Stage3 for more details


	__declspec(dllexport) void ExecuteOne();
	void FetchDummy();
	__declspec(dllexport) void Reset();
	__declspec(dllexport) void NESSoftReset();
	__declspec(dllexport) void SetTrampolines(byte (__cdecl *ReadMemory)(ushort), byte (__cdecl *DummyReadMemory)(ushort), void (__cdecl *WriteMemory)(ushort, byte));
};

extern "C" __declspec(dllexport) void* __cdecl Create();
extern "C" __declspec(dllexport) void __cdecl Destroy(void *);

#endif // MOS6502X_H
