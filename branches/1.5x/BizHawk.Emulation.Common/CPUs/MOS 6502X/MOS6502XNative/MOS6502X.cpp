#include "MOS6502X.h"

void MOS6502X::FetchDummy()
{
	DummyReadMemory(PC);
}

void MOS6502X::Reset()
{
	A = 0;
	X = 0;
	Y = 0;
	P = 0;
	S = 0;
	PC = 0;
	TotalExecutedCycles = 0;
	mi = 0;
	opcode = 256;
	//MessageBox(NULL,L"Opcode set to 256", NULL, 0);
	iflag_pending = true;
}

void MOS6502X::SetTrampolines(byte (__cdecl *ReadMemory)(ushort), byte (__cdecl *DummyReadMemory)(ushort), void (__cdecl *WriteMemory)(ushort, byte))
{
	this->ReadMemory = ReadMemory;
	this->DummyReadMemory = DummyReadMemory;
	this->WriteMemory = WriteMemory;
}

/*
#include <string>
#define SHOWRA(a) SHOWGA((int)(void *)(a) - ((int)(void *)cpu), #a)

void SHOWGA(int diff, const char *nom)
{
	char buffra[24];
	std::sprintf(buffra, "%08xi", diff);
	MessageBoxA(NULL, buffra, nom, 0);
}*/

void* Create()
{
	MOS6502X* cpu = new MOS6502X();
	/*
	SHOWRA(&cpu->BCD_Enabled);
	SHOWRA(&cpu->debug);
	SHOWRA(&cpu->throw_unhandled);
	SHOWRA(&cpu->A);
	SHOWRA(&cpu->X);
	SHOWRA(&cpu->Y);
	SHOWRA(&cpu->P);
	SHOWRA(&cpu->PC);
	SHOWRA(&cpu->S);
	SHOWRA(&cpu->IRQ);
	SHOWRA(&cpu->NMI);
	SHOWRA(&cpu->TotalExecutedCycles);
	SHOWRA(&cpu->ReadMemory);
	SHOWRA(&cpu->DummyReadMemory);
	SHOWRA(&cpu->WriteMemory);
	SHOWRA(&cpu->opcode);
	SHOWRA(&cpu->opcode2);
	SHOWRA(&cpu->opcode3);
	SHOWRA(&cpu->ea);
	SHOWRA(&cpu->alu_temp);
	SHOWRA(&cpu->mi);
	SHOWRA(&cpu->iflag_pending);
	SHOWRA(&cpu->interrupt_pending);
	SHOWRA(&cpu->branch_irq_hack);
	*/

	return (void *)cpu;
}

void Destroy(void *ptr)
{
	MOS6502X* cpu = (MOS6502X*) ptr;
	delete cpu;
}


