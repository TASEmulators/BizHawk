//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2012 Avery Lee
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
#include "cpu.h"
#include "cpuheatmap.h"
#include "simeventmanager.h"
#include "console.h"

#define TRAP_LOAD(memstat) if (mbTrapOnUninitLoad && (memstat) < kTypePreset) TrapOnUninitLoad(opcode, addr, pc); else ((void)0)
#define TRAP_COMPUTE_IF(cond) if (mbTrapOnUninitCompute && (cond)) TrapOnUninitCompute(opcode, addr, pc); else ((void)0)
#define TRAP_BRANCH_IF(cond) if (mbTrapOnUninitBranch && (cond)) TrapOnUninitBranch(opcode, addr, pc); else ((void)0)
#define TRAP_HWSTORE_IF(cond) if (mbTrapOnUninitCompute && (cond)) TrapOnUninitHwStore(opcode, addr, pc); else ((void)0)

ATCPUHeatMap::ATCPUHeatMap() {
	Reset();
}

ATCPUHeatMap::~ATCPUHeatMap() {
}

void ATCPUHeatMap::Init(ATSimulatorEventManager *pSimEvtMgr) {
	mpSimEvtMgr = pSimEvtMgr;
}

void ATCPUHeatMap::SetEarlyState(bool isEarly) {
	if (mbEarlyState == isEarly)
		return;

	mbEarlyState = isEarly;
	UpdateTrapFlags();
}

void ATCPUHeatMap::SetEarlyTrapFlags(ATCPUHeatMapTrapFlags trapFlags) {
	if (mTrapFlagsEarly == trapFlags)
		return;

	mTrapFlagsEarly = trapFlags;

	if (mbEarlyState)
		UpdateTrapFlags();
}

void ATCPUHeatMap::SetNormalTrapFlags(ATCPUHeatMapTrapFlags trapFlags) {
	if (mTrapFlagsNormal == trapFlags)
		return;

	mTrapFlagsNormal = trapFlags;

	if (!mbEarlyState)
		UpdateTrapFlags();
}

void ATCPUHeatMap::Reset() {
	mA = kTypeUnknown;
	mX = kTypeUnknown;
	mY = kTypeUnknown;

	mAValid = 0;
	mXValid = 0;
	mYValid = 0;
	mPValid = 0x34;

	for(uint32 i=0; i<0x10000; ++i)
		mMemory[i] = kTypePreset + i;

	for(uint32 i=0; i<0x10000; ++i)
		mMemAccess[i] = 0;

	for(uint32 i=0; i<0x10000; ++i)
		mMemValid[i] = 0;
}

void ATCPUHeatMap::ResetMemoryRange(uint32 addr, uint32 len) {
	if (addr >= 0x10000)
		return;

	if (0x10000 - addr < len)
		len = 0x10000 - addr;

	while(len--) {
		mMemory[addr] = kTypeUnknown;
		mMemAccess[addr] = 0;
		mMemValid[addr] = 0;
		++addr;
	}
}

void ATCPUHeatMap::MarkMemoryRangeHardware(uint32 addr, uint32 len) {
	if (addr >= 0x10000)
		return;

	if (0x10000 - addr < len)
		len = 0x10000 - addr;

	while(len--) {
		mMemory[addr] = kTypeHardware + addr;
		mMemAccess[addr] = 0;
		mMemValid[addr] = 0xFF;
		++addr;
	}
}

void ATCPUHeatMap::PresetMemoryRange(uint32 addr, uint32 len) {
	if (addr >= 0x10000)
		return;

	if (0x10000 - addr < len)
		len = 0x10000 - addr;

	while(len--) {
		mMemory[addr] = kTypePreset + addr;
		mMemAccess[addr] = 0;
		mMemValid[addr] = 0xFF;
		++addr;
	}
}

namespace {
	enum : uint8 {
		kIndexUsage_N,
		kIndexUsage_X,
		kIndexUsage_Y,
	};

	const uint8 kIndexUsageByOpcode[256] = {
#define N kIndexUsage_N
#define X kIndexUsage_X
#define Y kIndexUsage_Y
		N,X,N,X,N,N,N,N,N,N,N,N,N,N,N,N,
		N,Y,N,Y,X,X,X,X,N,Y,N,Y,X,X,X,X,
		N,X,N,X,N,N,N,N,N,N,N,N,N,N,N,N,
		N,Y,N,Y,X,X,X,X,N,Y,N,Y,X,X,X,X,
		N,X,N,X,N,N,N,N,N,N,N,N,N,N,N,N,
		N,Y,N,Y,X,X,X,X,N,Y,N,Y,X,X,X,X,
		N,X,N,X,N,N,N,N,N,N,N,N,N,N,N,N,
		N,Y,N,Y,X,X,X,X,N,Y,N,Y,X,X,X,X,
		N,X,N,X,N,N,N,N,N,N,N,N,N,N,N,N,
		N,Y,N,Y,X,X,Y,Y,N,Y,N,Y,X,X,Y,Y,
		N,X,N,X,N,N,N,N,N,N,N,N,N,N,N,N,
		N,Y,N,Y,X,X,Y,Y,N,Y,N,N,X,X,Y,Y,
		N,X,N,X,N,N,N,N,N,N,N,N,N,N,N,N,
		N,Y,N,Y,X,X,X,X,N,Y,N,Y,X,X,X,X,
		N,X,N,X,N,N,N,N,N,N,N,N,N,N,N,N,
		N,Y,N,Y,X,X,X,X,N,Y,N,Y,X,X,X,X,
#undef Y
#undef X
#undef N
	};
}

void ATCPUHeatMap::ProcessInsn(const ATCPUEmulator& cpu, uint8 opcode, uint16 addr, uint16 pc) {
	if (mbTrapOnUninitEa) {
		switch(kIndexUsageByOpcode[opcode]) {
			case kIndexUsage_N:
			default:
				break;

			case kIndexUsage_X:
				if (mXValid != 0xFF)
					TrapOnUninitEffectiveAddress(opcode, addr, pc);
				break;

			case kIndexUsage_Y:
				if (mYValid != 0xFF)
					TrapOnUninitEffectiveAddress(opcode, addr, pc);
				break;
		}
	}

	switch(opcode) {
		// stack operations
		// stack ops don't set addr, so we need to fix
		case 0x00:	// BRK
		case 0x08:	// PHP
			addr = 0x100 + (0xFF & (cpu.GetS() + 1));
			mMemValid[addr] = mPValid;
			break;

		case 0x28:	// PLP
			mPValid = 0xFF;
			break;

		case 0x48:	// PHA
			addr = 0x100 + (0xFF & (cpu.GetS() + 1));
			mMemory[addr] = mA;
			mMemAccess[addr] |= kAccessWrite;
			mMemValid[addr] = mAValid;
			break;

		case 0x68:	// PLA
			addr = 0x100 + cpu.GetS();
			mA = mMemory[addr];
			mAValid = mMemValid[addr];
			mMemAccess[addr] |= kAccessRead;

			mPValid &= 0x7D;
			mPValid |= (mAValid & 0x80);

			if (mAValid == 0xFF)
				mPValid |= 0x02;
			break;

		// load immediates
		case 0xA9:	// LDA imm
			mA = kTypeImm + pc;
			mAValid = 0xFF;
			mPValid |= 0x82;
			break;

		case 0xA2:	// LDX imm
			mX = kTypeImm + pc;
			mXValid = 0xFF;
			mPValid |= 0x82;
			break;

		case 0xA0:	// LDY imm
			mY = kTypeImm + pc;
			mYValid = 0xFF;
			mPValid |= 0x82;
			break;

		// transfer
		case 0x8A:	// TXA
			mA = mX;
			mAValid = mXValid;
			mPValid &= 0x7D;
			mPValid |= (mAValid & 0x80);
			if (mAValid == 0xFF)
				mPValid |= 0x02;
			break;

		case 0x98:	// TYA
			mA = mY;
			mAValid = mYValid;
			mPValid &= 0x7D;
			mPValid |= (mAValid & 0x80);
			if (mAValid == 0xFF)
				mPValid |= 0x02;
			break;

		case 0xA8:	// TAY
			mY = mA;
			mYValid = mAValid;
			mPValid &= 0x7D;
			mPValid |= (mAValid & 0x80);
			if (mAValid == 0xFF)
				mPValid |= 0x02;
			break;

		case 0xAA:	// TAX
			mX = mA;
			mXValid = mAValid;
			mPValid &= 0x7D;
			mPValid |= (mAValid & 0x80);
			if (mAValid == 0xFF)
				mPValid |= 0x02;
			break;

		case 0x9A:	// TXS
			break;

		case 0xBA:	// TSX
			mX = kTypeComputed;
			mXValid = 0xFF;
			break;

		// load A from memory
		case 0xA1:	// LDA (zp,X)
		case 0xA5:	// LDA zp
		case 0xAD:	// LDA abs
		case 0xB1:	// LDA (zp),Y
		case 0xB5:	// LDA zp,X
		case 0xB9:	// LDA abs,Y
		case 0xBD:	// LDA abs,X
			mA = mMemory[addr];
			mAValid = mMemValid[addr];
			TRAP_LOAD(mA);
			mMemAccess[addr] |= kAccessRead;

			mPValid &= 0x7D;
			mPValid |= (mAValid & 0x80);
			if (mAValid == 0xFF)
				mPValid |= 0x02;
			break;

		// load X from memory
		case 0xA6:	// LDX zp
		case 0xAE:	// LDX abs
		case 0xB6:	// LDX zp,Y
		case 0xBE:	// LDX abs,Y
			mX = mMemory[addr];
			mXValid = mMemValid[addr];
			TRAP_LOAD(mX);
			mMemAccess[addr] |= kAccessRead;

			mPValid &= 0x7D;
			mPValid |= (mXValid & 0x80);
			if (mAValid == 0xFF)
				mPValid |= 0x02;
			break;

		// load Y
		case 0xA4:	// LDY zp
		case 0xAC:	// LDY abs
		case 0xB4:	// LDY zp,X
		case 0xBC:	// LDY abs,X
			mY = mMemory[addr];
			mYValid = mMemValid[addr];
			TRAP_LOAD(mY);
			mMemAccess[addr] |= kAccessRead;

			mPValid &= 0x7D;
			mPValid |= (mYValid & 0x80);
			if (mYValid == 0xFF)
				mPValid |= 0x02;
			break;

		// store A
		case 0x81:	// STA (zp,X)
		case 0x85:	// STA zp
		case 0x8D:	// STA abs
		case 0x91:	// STA (zp),Y
		case 0x95:	// STA zp,X
		case 0x99:	// STA abs,Y
		case 0x9D:	// STA abs,X
			if (mMemory[addr] & kTypeHardware) {
				TRAP_HWSTORE_IF(mAValid != 0xFF);
			} else {
				mMemory[addr] = mA;
				mMemValid[addr] = mAValid;
			}

			mMemAccess[addr] |= kAccessWrite;
			break;

		// store X
		case 0x86:	// STX zp
		case 0x8E:	// STX abs
		case 0x96:	// STX zp,Y
			if (mMemory[addr] & kTypeHardware) {
				TRAP_HWSTORE_IF(mXValid != 0xFF);
			} else {
				mMemory[addr] = mX;
				mMemValid[addr] = mXValid;
			}

			mMemAccess[addr] |= kAccessWrite;
			break;

		// store Y
		case 0x84:	// STY zp
		case 0x8C:	// STY abs
		case 0x94:	// STY zp,X
			if (mMemory[addr] & kTypeHardware) {
				TRAP_HWSTORE_IF(mYValid != 0xFF);
			} else {
				mMemory[addr] = mY;
				mMemValid[addr] = mYValid;
			}

			mMemAccess[addr] |= kAccessWrite;
			break;

		// update A
		case 0x09:	// ORA imm
		case 0x29:	// AND imm
		case 0x49:	// EOR imm
			mPValid &= 0x7D;

			if (mAValid == 0xFF)
				mPValid |= 0x02;

			mPValid |= mAValid & 0x80;
			break;

		case 0x0A:	// ASL A
			mPValid &= 0x7C;
			
			if (mAValid & 0x80)
				++mPValid;

			if (mAValid & 0x40)
				mPValid |= 0x80;
			
			if (mAValid == 0xFF)
				mPValid |= 0x02;

			mAValid = (mAValid << 1) + 1;
			break;

		case 0x2A:	// ROL A
			{
				const uint8 oldAValid = mAValid;
				const uint8 oldPValid = mPValid;

				mPValid &= 0x7C;

				if (oldAValid & 0x80)
					++mPValid;

				if (oldAValid & 0x40)
					mPValid |= 0x80;

				mAValid = (oldAValid << 1) + (oldPValid & 0x01);

				if (mAValid == 0xFF)
					mPValid |= 0x02;
			}
			break;

		case 0x4A:	// LSR A
			mPValid &= 0xFC;
			mPValid |= 0x80;
			
			if (mAValid & 0x01)
				++mPValid;

			if (mAValid == 0xFF)
				mPValid |= 0x02;

			mAValid = (mAValid >> 1) + 0x80;
			break;

		case 0x6A:	// ROR A
			{
				const uint8 oldAValid = mAValid;
				const uint8 oldPValid = mPValid;

				mPValid &= 0x7C;

				if (oldAValid & 0x01)
					++mPValid;

				if (oldPValid & 0x01)
					mPValid |= 0x80;

				mAValid = (oldAValid >> 1) + (oldPValid << 7);

				if (mAValid == 0xFF)
					mPValid |= 0x02;
			}
			break;

		case 0x69:	// ADC imm
		case 0xE9:	// SBC imm
			mAValid = (mAValid == 0xFF) && (mPValid & 0x01) ? 0xFF : 0x00;
			mPValid = (mPValid & 0xFE) + (mAValid & 0x01);
			break;

		// update A from memory
		case 0x01:	// ORA (zp,X)
		case 0x05:	// ORA zp
		case 0x0D:	// ORA abs
		case 0x11:	// ORA (zp),Y
		case 0x15:	// ORA zp,X
		case 0x19:	// ORA abs,Y
		case 0x1D:	// ORA abs,X
		case 0x21:	// AND (zp,X)
		case 0x25:	// AND zp
		case 0x2D:	// AND abs
		case 0x31:	// AND (zp),Y
		case 0x35:	// AND zp,X
		case 0x39:	// AND abs,Y
		case 0x3D:	// AND abs,X
		case 0x41:	// EOR (zp,X)
		case 0x45:	// EOR zp
		case 0x4D:	// EOR abs
		case 0x51:	// EOR (zp),Y
		case 0x55:	// EOR zp,X
		case 0x59:	// EOR abs,Y
		case 0x5D:	// EOR abs,X
			TRAP_LOAD(mMemory[addr]);
			mA = kTypeComputed + pc;
			mAValid &= mMemValid[addr];
			mPValid &= 0x7D;

			if (mAValid == 0xFF)
				mPValid |= 0x02;

			mPValid |= mAValid & 0x80;
			break;

		case 0x6D:	// ADC abs
		case 0x61:	// ADC (zp,X)
		case 0x65:	// ADC zp
		case 0x71:	// ADC (zp),Y
		case 0x75:	// ADC zp,X
		case 0x79:	// ADC abs,Y
		case 0x7D:	// ADC abs,X
		case 0xE1:	// SBC (zp,X)
		case 0xE5:	// SBC zp
		case 0xED:	// SBC abs
		case 0xF1:	// SBC (zp),Y
		case 0xF5:	// SBC zp,X
		case 0xF9:	// SBC abs,Y
		case 0xFD:	// SBC abs,X
			TRAP_COMPUTE_IF(mAValid != 0xFF || mMemValid[addr] != 0xFF);
			TRAP_LOAD(mMemory[addr]);
			mA = kTypeComputed + pc;
			mAValid = (mAValid & mMemValid[addr]) == 0xFF && (mPValid & 0x01) ? 0xFF : 0x00;
			mPValid = (mPValid & 0xFE) + (mAValid & 0x01);
			break;

		// update X
		case 0xCA:	// DEX
		case 0xE8:	// INX
			TRAP_COMPUTE_IF(mXValid != 0xFF);
			mX = kTypeComputed + pc;
			mXValid = (mXValid == 0xFF) ? 0xFF : 0x00;
			mPValid &= 0x7D;
			if (mXValid == 0xFF)
				mPValid |= 0x82;
			break;

		// update Y
		case 0x88:	// DEY
		case 0xC8:	// INY
			TRAP_COMPUTE_IF(mYValid != 0xFF);
			mY = kTypeComputed + pc;
			mYValid = (mYValid == 0xFF) ? 0xFF : 0x00;
			mPValid &= 0x7D;
			if (mYValid == 0xFF)
				mPValid |= 0x82;
			break;

		// update P with memory load
		case 0x24:	// BIT zp
		case 0x2C:	// BIT abs
			TRAP_LOAD(mMemory[addr]);
			mPValid &= 0x3D;
			mPValid |= mMemValid[addr] & 0xC0;
			if ((mAValid & mMemValid[addr]) == 0xFF)
				mPValid |= 0x02;
			break;

		case 0xC1:	// CMP (zp,X)
		case 0xC5:	// CMP zp
		case 0xCD:	// CMP abs
		case 0xD1:	// CMP (zp),Y
		case 0xD5:	// CMP zp,X
		case 0xD9:	// CMP abs,Y
		case 0xDD:	// CMP abs,X
			TRAP_LOAD(mMemory[addr]);
			mPValid &= 0x3C;
			if ((mMemValid[addr] & mAValid) == 0xFF)
				mPValid |= 0xC3;
			break;

		case 0xC4:	// CPY zp
		case 0xCC:	// CPY abs
			TRAP_LOAD(mMemory[addr]);
			mPValid &= 0x3C;
			if ((mMemValid[addr] & mYValid) == 0xFF)
				mPValid |= 0xC3;
			break;

		case 0xE4:	// CPX zp
		case 0xEC:	// CPX abs
			TRAP_LOAD(mMemory[addr]);
			mPValid &= 0x3C;
			if ((mMemValid[addr] & mXValid) == 0xFF)
				mPValid |= 0xC3;
			break;

		// update P, no memory load (ignorable)
		case 0xC0:	// CPY imm
			TRAP_COMPUTE_IF(mYValid != 0xFF);
			if (mYValid == 0xFF)
				mPValid |= 0xC3;
			else
				mPValid &= 0x3C;
			break;

		case 0xC9:	// CMP imm
			TRAP_COMPUTE_IF(mAValid != 0xFF);
			if (mAValid == 0xFF)
				mPValid |= 0xC3;
			else
				mPValid &= 0x3C;
			break;

		case 0xE0:	// CPX imm
			TRAP_COMPUTE_IF(mXValid != 0xFF);
			if (mXValid == 0xFF)
				mPValid |= 0xC3;
			else
				mPValid &= 0x3C;
			break;

		case 0x18:	// CLC
		case 0x38:	// SEC
			mPValid |= 0x01;
			break;

		case 0x58:	// CLI
		case 0x78:	// SEI
			mPValid |= 0x04;
			break;

		case 0xB8:	// CLV
			mPValid |= 0x40;
			break;

		case 0xD8:	// CLD
		case 0xF8:	// SED
			mPValid |= 0x08;
			break;

		// branch instructions (ignorable)
		case 0x20:	// JSR abs
		case 0x4C:	// JMP abs
		case 0x60:	// RTS
			break;

		case 0x40:	// RTI
			mPValid = mMemValid[addr];
			break;

		case 0x10:	// BPL rel8
		case 0x30:	// BMI rel8
			TRAP_BRANCH_IF(!(mPValid & 0x80));
			break;

		case 0x50:	// BVC rel8
		case 0x70:	// BVS rel8
			TRAP_BRANCH_IF(!(mPValid & 0x40));
			break;

		case 0x90:	// BCC rel8
		case 0xB0:	// BCS rel8
			TRAP_BRANCH_IF(!(mPValid & 0x01));
			break;

		case 0xD0:	// BNE rel8
		case 0xF0:	// BEQ rel8
			TRAP_BRANCH_IF(!(mPValid & 0x02));
			break;

		// indirect branch instructions
		case 0x6C:	// JMP (abs)
			TRAP_LOAD(mMemory[addr]);
			TRAP_LOAD(mMemory[(addr+1) & 0xffff]);
			break;

		// ignorable operations
		case 0x42:	// HLE (emulator escape insn)
		case 0xEA:	// NOP
			break;

		// read-modify-write instructions
		case 0x06:	// ASL zp
		case 0x0E:	// ASL abs
		case 0x16:	// ASL zp,X
		case 0x1E:	// ASL abs,X
			TRAP_LOAD(mMemory[addr]);
			mMemAccess[addr] |= kAccessRead | kAccessWrite;
			{
				const uint8 oldPValid = mPValid;
				const uint8 oldMemValid = mMemValid[addr];

				mPValid &= 0x7C;

				const uint8 newMemValid = (mMemValid[addr] << 1) + 1;

				if (mMemValid[addr] & kTypeHardware) {
					TRAP_HWSTORE_IF(newMemValid != 0xFF);
				} else {
					mMemory[addr] = kTypeComputed + pc;
					mMemValid[addr] = newMemValid;
				}

				if (newMemValid == 0xFF)
					mPValid |= 0x82;

				if (oldMemValid & 0x80)
					mPValid |= 0x01;
			}
			break;

		case 0x26:	// ROL zp
		case 0x2E:	// ROL abs
		case 0x36:	// ROL zp,X
		case 0x3E:	// ROL abs,X
			TRAP_LOAD(mMemory[addr]);
			mMemAccess[addr] |= kAccessRead | kAccessWrite;
			{
				const uint8 oldPValid = mPValid;
				const uint8 oldMemValid = mMemValid[addr];

				mPValid &= 0x7C;

				const uint8 newMemValid = (oldPValid & 1) + (mMemValid[addr] << 1);

				if (mMemValid[addr] & kTypeHardware) {
					TRAP_HWSTORE_IF(newMemValid != 0xFF);
				} else {
					mMemory[addr] = kTypeComputed + pc;
					mMemValid[addr] = newMemValid;
				}

				if (newMemValid == 0xFF)
					mPValid |= 0x82;

				if (oldMemValid & 0x80)
					mPValid |= 0x01;
			}
			break;

		case 0x46:	// LSR zp
		case 0x4E:	// LSR abs
		case 0x56:	// LSR zp,X
		case 0x5E:	// LSR abs,X
			TRAP_LOAD(mMemory[addr]);
			mMemAccess[addr] |= kAccessRead | kAccessWrite;
			{
				const uint8 oldPValid = mPValid;
				const uint8 oldMemValid = mMemValid[addr];

				mPValid &= 0xFC;
				mPValid |= 0x80;

				const uint8 newMemValid = 0x80 + (mMemValid[addr] >> 1);

				if (mMemValid[addr] & kTypeHardware) {
					TRAP_HWSTORE_IF(newMemValid != 0xFF);
				} else {
					mMemory[addr] = kTypeComputed + pc;
					mMemValid[addr] = newMemValid;
				}

				if (newMemValid == 0xFF)
					mPValid |= 0x02;

				if (oldMemValid & 0x01)
					mPValid |= 0x01;
			}
			break;

		case 0x66:	// ROR zp
		case 0x6E:	// ROR abs
		case 0x76:	// ROR zp,X
		case 0x7E:	// ROR abs,X
			TRAP_LOAD(mMemory[addr]);
			mMemAccess[addr] |= kAccessRead | kAccessWrite;
			{
				const uint8 oldPValid = mPValid;
				const uint8 oldMemValid = mMemValid[addr];

				mPValid &= 0x7C;

				const uint8 newMemValid = (oldPValid << 7) + (mMemValid[addr] >> 1);

				if (mMemValid[addr] & kTypeHardware) {
					TRAP_HWSTORE_IF(newMemValid != 0xFF);
				} else {
					mMemory[addr] = kTypeComputed + pc;
					mMemValid[addr] = newMemValid;
				}

				if (newMemValid == 0xFF)
					mPValid |= 0x82;

				if (oldMemValid & 0x01)
					mPValid |= 0x01;
			}
			break;

		case 0xC6:	// DEC zp
		case 0xD6:	// DEC zp,X
		case 0xCE:	// DEC abs
		case 0xDE:	// DEC abs,X
		case 0xE6:	// INC zp
		case 0xEE:	// INC abs
		case 0xF6:	// INC zp,X
		case 0xFE:	// INC abs,X
			TRAP_LOAD(mMemory[addr]);
			mMemAccess[addr] |= kAccessRead | kAccessWrite;
			
			if (mMemValid[addr] == 0xFF) {
				mPValid |= 0x82;
			} else {
				mPValid &= 0x7D;
				mMemValid[addr] = 0;

				if (mMemValid[addr] & kTypeHardware) {
					TRAP_HWSTORE_IF(true);
				} else {
					mMemory[addr] = kTypeComputed + pc;
					mMemValid[addr] = 0;
				}
			}
			break;
	}
}

void ATCPUHeatMap::TrapOnUninitLoad(uint8 opcode, uint16 addr, uint16 pc) {
	ATConsolePrintf("\n");
	ATConsolePrintf("VERIFIER: Load from uninitialized memory.\n");
	mpSimEvtMgr->NotifyEvent(kATSimEvent_VerifierFailure);
}

void ATCPUHeatMap::TrapOnUninitCompute(uint8 opcode, uint16 addr, uint16 pc) {
	ATConsolePrintf("\n");
	ATConsolePrintf("VERIFIER: Using uninitialized data in computation.\n");
	LogValidityStatus();
	mpSimEvtMgr->NotifyEvent(kATSimEvent_VerifierFailure);
}

void ATCPUHeatMap::TrapOnUninitBranch(uint8 opcode, uint16 addr, uint16 pc) {
	ATConsolePrintf("\n");
	ATConsolePrintf("VERIFIER: Using uninitialized data in branch.\n");
	LogValidityStatus();
	mpSimEvtMgr->NotifyEvent(kATSimEvent_VerifierFailure);
}

void ATCPUHeatMap::TrapOnUninitEffectiveAddress(uint8 opcode, uint16 addr, uint16 pc) {
	ATConsolePrintf("\n");
	ATConsolePrintf("VERIFIER: Using uninitialized data in effective address.\n");
	LogValidityStatus();
	mpSimEvtMgr->NotifyEvent(kATSimEvent_VerifierFailure);
}

void ATCPUHeatMap::TrapOnUninitHwStore(uint8 opcode, uint16 addr, uint16 pc) {
	ATConsolePrintf("\n");
	ATConsolePrintf("VERIFIER: Writing to hardware register with uninitialized data.\n");
	LogValidityStatus();
	mpSimEvtMgr->NotifyEvent(kATSimEvent_VerifierFailure);
}

void ATCPUHeatMap::LogValidityStatus() {
	ATConsolePrintf("  Register validity: A~%02X X~%02X Y~%02X P~(%c%c%c%c%c%c)\n"
		, mAValid
		, mXValid
		, mYValid
		, (mPValid & 0x80) ? 'N' : '-'
		, (mPValid & 0x80) ? 'V' : '-'
		, (mPValid & 0x08) ? 'D' : '-'
		, (mPValid & 0x04) ? 'I' : '-'
		, (mPValid & 0x02) ? 'Z' : '-'
		, (mPValid & 0x01) ? 'C' : '-'
		);
}

void ATCPUHeatMap::UpdateTrapFlags() {
	const ATCPUHeatMapTrapFlags trapFlags = mbEarlyState ? mTrapFlagsEarly : mTrapFlagsNormal;

	mbTrapOnUninitLoad		= 0 != (trapFlags & kATCPUHeatMapTrapFlags_Load);
	mbTrapOnUninitCompute	= 0 != (trapFlags & kATCPUHeatMapTrapFlags_Compute);
	mbTrapOnUninitBranch	= 0 != (trapFlags & kATCPUHeatMapTrapFlags_Branch);
	mbTrapOnUninitEa		= 0 != (trapFlags & kATCPUHeatMapTrapFlags_EffectiveAddress);
	mbTrapOnUninitHwStore	= 0 != (trapFlags & kATCPUHeatMapTrapFlags_HwStore);
}