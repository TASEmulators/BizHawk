//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2012 Avery Lee
//
//	This program is free software; you can redistribute it and/or modify
//	it under the terms of the GNU General Public License as published by
//	the Free Software Foundation; either version 2 of the License, or
//	(at your option) any later version.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with this program; if not, write to the Free Software
//	Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.

#include <stdafx.h>
#include <vd2/system/vdalloc.h>
#include "cpu.h"
#include "cpumemory.h"
#include "cpuhookmanager.h"
#include "ksyms.h"

class ATHLEFastBootHook {
public:
	~ATHLEFastBootHook();

	void Init(ATCPUEmulator *cpu);
	void Shutdown();

protected:
	void OnInitHooks(const uint8 *lowerROM, const uint8 *upperROM);
	void Unhook();
	uint8 OnHookMemCheck(uint16);
	uint8 OnHookMemClear(uint16);
	uint8 OnHookChecksum(uint16);

	ATCPUEmulator *mpCPU = nullptr;
	ATCPUHookInitNode *mpInitHook = nullptr;
	ATCPUHookNode *mpChecksumHook = nullptr;
	ATCPUHookNode *mpMemClearHook = nullptr;
	ATCPUHookNode *mpMemCheckHook = nullptr;
};

ATHLEFastBootHook::~ATHLEFastBootHook() {
	Shutdown();
}

void ATHLEFastBootHook::Init(ATCPUEmulator *cpu) {
	mpCPU = cpu;

	ATCPUHookManager& hookmgr = *cpu->GetHookManager();

	mpInitHook = hookmgr.AddInitHook([this](auto lowerROM, auto upperROM) { OnInitHooks(lowerROM, upperROM); });
}

void ATHLEFastBootHook::OnInitHooks(const uint8 *lowerROM, const uint8 *upperROM) {
	Unhook();
	
	ATCPUHookManager& hookmgr = *mpCPU->GetHookManager();

	// Check if we can find and accelerate the kernel boot routines.
	static const uint8 kMemCheckRoutine[]={
		0xA9, 0xFF,	// lda #$ff
		0x91, 0x04,	// sta ($04),y
		0xD1, 0x04,	// cmp ($04),y
		0xF0, 0x02,	// beq $+4
		0x46, 0x01,	// lsr $01
		0xA9, 0x00,	// lda #0
		0x91, 0x04,	// sta ($04),y
		0xD1, 0x04,	// cmp ($04),y
		0xF0, 0x02,	// beq $+4
		0x46, 0x01,	// lsr $01
		0xC8,		// iny
		0xD0, 0xE9	// bne $c2e4
	};

	if (lowerROM && !memcmp(lowerROM + 0x02E4, kMemCheckRoutine, sizeof kMemCheckRoutine))
		hookmgr.SetHookMethod(mpMemCheckHook, kATCPUHookMode_KernelROMOnly, 0xC2E4, 0, this, &ATHLEFastBootHook::OnHookMemCheck);

	// This is AltirraOS's fill routine.
	static const uint8 kMemClearRoutine[]={
		0x91, 0x66,		// sta (toadr),y
		0xC8,			// iny
		0xD0, 0xFB,		// bne *-5
		0xE6, 0x67,		// inc toadr+1
		0xCA,			// dex
		0xD0, 0xF6,		// bne *-10
	};

	if (upperROM) {
		for(uint32 i=0; i<0x1C00 - sizeof(kMemClearRoutine); ++i) {
			if (upperROM[i + 0x0C00] == kMemClearRoutine[0] && !memcmp(upperROM + 0xC00 + i + 1, kMemClearRoutine + 1, sizeof kMemClearRoutine - 1)) {
				hookmgr.SetHookMethod(mpMemClearHook, kATCPUHookMode_KernelROMOnly, 0xE400 + i, 0, this, &ATHLEFastBootHook::OnHookMemClear);
				break;
			}
		}
	}

	static const uint8 kChecksumRoutine[]={
		0xa0, 0x00,	// ldy #0
		0x18,       // clc
		0xB1, 0x9E, // lda ($9e),y
		0x65, 0x8B, // adc $8b
		0x85, 0x8B, // sta $8b
		0x90, 0x02, // bcc $ffc4
		0xE6, 0x8C, // inc $8c
		0xE6, 0x9E, // inc $9e
		0xD0, 0x02, // bne $ffca
		0xE6, 0x9F, // inc $9f
		0xA5, 0x9E, // lda $9e
		0xC5, 0xA0, // cmp $a0
		0xD0, 0xE9, // bne $ffb9
		0xA5, 0x9F, // lda $9f
		0xC5, 0xA1, // cmp $a1
		0xD0, 0xE3, // bne $ffb9
	};

	if (upperROM && !memcmp(upperROM + (0xFFB7 - 0xD800), kChecksumRoutine, sizeof kChecksumRoutine))
		hookmgr.SetHookMethod(mpChecksumHook, kATCPUHookMode_KernelROMOnly, 0xFFB7, 0, this, &ATHLEFastBootHook::OnHookChecksum);
}

void ATHLEFastBootHook::Shutdown() {
	if (mpCPU) {
		Unhook();

		ATCPUHookManager& hookmgr = *mpCPU->GetHookManager();
		if (mpInitHook) {
			hookmgr.RemoveInitHook(mpInitHook);
			mpInitHook = nullptr;
		}

		mpCPU = nullptr;
	}
}

void ATHLEFastBootHook::Unhook() {
	if (mpCPU) {
		ATCPUHookManager& hookmgr = *mpCPU->GetHookManager();

		hookmgr.UnsetHook(mpMemCheckHook);
		hookmgr.UnsetHook(mpMemClearHook);
		hookmgr.UnsetHook(mpChecksumHook);
	}
}

uint8 ATHLEFastBootHook::OnHookMemCheck(uint16) {
	ATCPUEmulatorMemory& mem = *mpCPU->GetMemory();

	// XL kernel memory test
	//
	// C2E4: A9 FF  LDA #$FF
	// C2E6: 91 04  STA ($04),Y
	// C2E8: D1 04  CMP ($04),Y
	// C2EA: F0 02  BEQ $C2EE
	// C2EC: 46 01  LSR $01
	// C2EE: A9 00  LDA #$00
	// C2F0: 91 04  STA ($04),Y
	// C2F2: D1 04  CMP ($04),Y
	// C2F4: F0 02  BEQ $C2F8
	// C2F6: 46 01  LSR $01
	// C2F8: C8     INY
	// C2F9: D0 E9  BNE $C2E4

	uint32 addr = mem.ReadByte(0x04) + 256*(uint32)mem.ReadByte(0x05);

	bool success = true;

	addr += mpCPU->GetY();
	for(uint32 i=mpCPU->GetY(); i<256; ++i) {
		mem.WriteByte(addr, 0xFF);
		if (mem.ReadByte(addr) != 0xFF)
			success = false;

		mem.WriteByte(addr, 0x00);
		if (mem.ReadByte(addr) != 0x00)
			success = false;

		++addr;
	}

	if (!success)
		mem.WriteByte(0x01, mem.ReadByte(0x01) >> 1);

	mpCPU->SetY(0);
	mpCPU->SetA(0);
	mpCPU->SetP((mpCPU->GetP() & ~AT6502::kFlagN) | AT6502::kFlagZ);
	mpCPU->Jump(0xC2FB - 1);
	return 0xEA;
}

uint8 ATHLEFastBootHook::OnHookMemClear(uint16 pc) {
	ATCPUEmulatorMemory& mem = *mpCPU->GetMemory();

	// AltirraOS memory clear routine
	//
	// sta (toadr),y
	// iny
	// bne *-5
	// inc toadr+1
	// dex
	// bne *-10

	uint32 addr = mem.ReadByte(ATKernelSymbols::TOADR) + 256*(uint32)mem.ReadByte(ATKernelSymbols::TOADR + 1);

	bool success = true;

	const uint8 fillByte = mpCPU->GetA();
	uint8 x = mpCPU->GetX();
	uint8 y = mpCPU->GetY();

	do {
		for(uint32 i=y; i<256; ++i)
			mem.WriteByte(addr + i, fillByte);

		addr += 0x100;
		y = 0;
	} while(--x);

	mem.WriteByte(ATKernelSymbols::TOADR + 1, (uint8)(addr >> 8));

	mpCPU->SetX(0);
	mpCPU->SetY(0);
	mpCPU->SetP((mpCPU->GetP() & ~AT6502::kFlagN) | AT6502::kFlagZ);
	mpCPU->Jump(pc + 10 - 1);
	return 0xEA;
}

uint8 ATHLEFastBootHook::OnHookChecksum(uint16) {
	ATCPUEmulatorMemory& mem = *mpCPU->GetMemory();

	// XL kernel checksum routine
	//
	// FFB7: A0 00   LDY #$00
	// FFB9: 18      CLC
	// FFBA: B1 9E   LDA ($9E),Y
	// FFBC: 65 8B   ADC $8B
	// FFBE: 85 8B   STA $8B
	// FFC0: 90 02   BCC $FFC4
	// FFC2: E6 8C   INC $8C
	// FFC4: E6 9E   INC $9E
	// FFC6: D0 02   BNE $FFCA
	// FFC8: E6 9F   INC $9F
	// FFCA: A5 9E   LDA $9E
	// FFCC: C5 A0   CMP $A0
	// FFCE: D0 E9   BNE $FFB9
	// FFD0: A5 9F   LDA $9F
	// FFD2: C5 A1   CMP $A1
	// FFD4: D0 E3   BNE $FFB9

	uint16 addr = mem.ReadByte(0x9E) + 256*(uint32)mem.ReadByte(0x9F);
	uint32 checksum = mem.ReadByte(0x8B) + 256*(uint32)mem.ReadByte(0x8C);
	uint16 limit = mem.ReadByte(0xA0) + 256*(uint32)mem.ReadByte(0xA1);

	do {
		checksum += mem.ReadByte(addr++);
	} while(addr != limit);

	mem.WriteByte(0x9E, (uint8)addr);
	mem.WriteByte(0x9F, (uint8)(addr >> 8));
	mem.WriteByte(0x8B, (uint8)checksum);
	mem.WriteByte(0x8C, (uint8)(checksum >> 8));

	mpCPU->SetY(0);
	mpCPU->Jump(0xFFD6 - 1);
	return 0xEA;
}

///////////////////////////////////////////////////////////////////////////

ATHLEFastBootHook *ATCreateHLEFastBootHook(ATCPUEmulator *cpu) {
	vdautoptr<ATHLEFastBootHook> hook(new ATHLEFastBootHook);

	hook->Init(cpu);

	return hook.release();
}

void ATDestroyHLEFastBootHook(ATHLEFastBootHook *hook) {
	delete hook;
}
