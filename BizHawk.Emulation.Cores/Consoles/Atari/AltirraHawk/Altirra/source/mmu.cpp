//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2010 Avery Lee
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
#include <at/atcore/address.h>
#include "mmu.h"
#include "memorymanager.h"
#include "simulator.h"

ATMMUEmulator::ATMMUEmulator()
	: mHardwareMode(0)
	, mMemoryMode(0)
	, mMemoryModeOverride(-1)
	, mbForceBasic(false)
	, mpMemMan(NULL)
	, mpMemory(NULL)
	, mpLayerExtRAM(NULL)
	, mpLayerSelfTest(NULL)
	, mpLayerLowerKernel(NULL)
	, mpLayerUpperKernel(NULL)
	, mpLayerBASIC(NULL)
	, mpLayerGame(NULL)
	, mpLayerHiddenRAM(NULL)
	, mpLayerHighRAM(NULL)
	, mpLayerAxlonControl1(NULL)
	, mpLayerAxlonControl2(NULL)
	, mCurrentBank(0xFF)
	, mCPUBase(0)
	, mAnticBase(0)
	, mCurrentBankInfo(0)
	, mAxlonBank(0)
	, mAxlonBankMask(0)
	, mbAxlonAliasing(false)
{
	Shutdown();
}

ATMMUEmulator::~ATMMUEmulator() {
	Shutdown();
}

void ATMMUEmulator::Init(ATMemoryManager *memman) {
	mpMemMan = memman;
}

void ATMMUEmulator::Shutdown() {
	ShutdownMapping();
	SetAxlonMemory(0, false, nullptr);
	SetHighMemory(0, nullptr);

	mpMemMan = nullptr;
}

void ATMMUEmulator::InitMapping(int hwmode, int memoryMode, void *mem,
						 ATMemoryLayer *extLayer,
						 ATMemoryLayer *selfTestLayer,
						 ATMemoryLayer *lowerKernelLayer,
						 ATMemoryLayer *upperKernelLayer,
						 ATMemoryLayer *basicLayer,
						 ATMemoryLayer *gameLayer,
						 ATMemoryLayer *hiddenRamLayer) {
	mHardwareMode = hwmode;
	mMemoryMode = memoryMode;
	mpMemory = (uint8 *)mem;
	mpLayerExtRAM = extLayer;
	mpLayerSelfTest = selfTestLayer;
	mpLayerLowerKernel = lowerKernelLayer;
	mpLayerUpperKernel = upperKernelLayer;
	mpLayerBASIC = basicLayer;
	mpLayerGame = gameLayer;
	mpLayerHiddenRAM = hiddenRamLayer;

	mCPUBase = 0;
	mAnticBase = 0;

	RebuildMappingTables();
}

void ATMMUEmulator::RebuildMappingTables() {
	const ATMemoryMode memmode = (ATMemoryMode)(mMemoryModeOverride >= 0 ? mMemoryModeOverride : mMemoryMode);
	const ATHardwareMode hwmode = (ATHardwareMode)mHardwareMode;
	uint8 extbankmask = 0;

	if (mpLayerAxlonControl1)
		mpMemMan->EnableLayer(mpLayerAxlonControl1, kATMemoryAccessMode_CPUWrite, hwmode == kATHardwareMode_800);

	switch(memmode) {
		case kATMemoryMode_128K:		// 4 banks * 16K = 64K
			extbankmask = 0x03;
			break;

		case kATMemoryMode_320K:		// 16 banks * 16K = 256K
		case kATMemoryMode_320K_Compy:
		case kATMemoryMode_256K:
			extbankmask = 0x0F;
			break;

		case kATMemoryMode_576K:		// 32 banks * 16K = 512K
		case kATMemoryMode_576K_Compy:
			extbankmask = 0x1F;
			break;

		case kATMemoryMode_1088K:		// 64 banks * 16K = 1024K
			extbankmask = 0x3F;
			break;
	}

	const int kernelBankMask = (hwmode == kATHardwareMode_800XL ||
		hwmode == kATHardwareMode_1200XL ||
		hwmode == kATHardwareMode_XEGS ||
		hwmode == kATHardwareMode_130XE) ? 0x00 : 0x01;

	const int cpuBankBit = (extbankmask != 0) ? 0x10 : 0x00;
	int anticBankBit = 0;

	switch(memmode) {
		case kATMemoryMode_128K:
		case kATMemoryMode_320K_Compy:
		case kATMemoryMode_576K_Compy:
			// Separate ANTIC access (bit 5)
			anticBankBit = 0x20;
			break;

		case kATMemoryMode_256K:
		case kATMemoryMode_320K:
		case kATMemoryMode_576K:
		case kATMemoryMode_1088K:
			// CPU/ANTIC access combined (bit 4)
			anticBankBit = 0x10;
			break;
	}

	uint32 extbankoffset = 0x04;
	switch(memmode) {
		case kATMemoryMode_256K:
			extbankoffset = 0;
			break;

		case kATMemoryMode_128K:
		case kATMemoryMode_320K:
		case kATMemoryMode_320K_Compy:
			// We push up RAMBO memory by 256K in low memory modes to make VBXE
			// emulation easier (it emulates RAMBO in its high 256K).
			extbankoffset = 0x14;
			break;
	}

	switch(memmode) {
		case kATMemoryMode_128K:		mCPUBankMask = 0x1C; break;
		case kATMemoryMode_320K:		mCPUBankMask = 0x7C; break;
		case kATMemoryMode_576K:		mCPUBankMask = 0x7E; break;
		case kATMemoryMode_1088K:		mCPUBankMask = 0xFE; break;
		case kATMemoryMode_320K_Compy:	mCPUBankMask = 0xDC; break;
		case kATMemoryMode_576K_Compy:	mCPUBankMask = 0xDE; break;
		case kATMemoryMode_256K:		mCPUBankMask = 0x7C; break;
		default:						mCPUBankMask = 0x00; break;
	}

	bool blockExtBasic = false;
	switch(memmode) {
		case kATMemoryMode_576K:
		case kATMemoryMode_576K_Compy:
		case kATMemoryMode_1088K:
			blockExtBasic = true;
			break;
	}

	uint8 basicMask = mbForceBasic ? 0 : 0x02;
	switch(hwmode) {
		case kATHardwareMode_800XL:
		case kATHardwareMode_XEGS:
		case kATHardwareMode_130XE:
			basicMask = 0;
			break;
	}

	for(int portb=0; portb<256; ++portb) {
		const bool cpuEnabled = (~portb & cpuBankBit) != 0;
		const bool anticEnabled = (~portb & anticBankBit) != 0;
		uint16 encodedBankInfo = 0;

		if (cpuEnabled || anticEnabled) {
			// Bank bits:
			// 128K:		bits 2, 3
			// 192K:		bits 2, 3, 6
			// 256K Rambo:	bits 2, 3, 5, 6 -- bits 3 and 2 must be LSBs for main memory aliasing
			// 320K:		bits 2, 3, 5, 6
			// 576K:		bits 1, 2, 3, 5, 6
			// 1088K:		bits 1, 2, 3, 5, 6, 7
			// 320K COMPY:	bits 2, 3, 6, 7
			// 576K COMPY:	bits 1, 2, 3, 6, 7
			encodedBankInfo = ((~portb & 0x0c) >> 2);

			switch(memmode) {
				case kATMemoryMode_256K:
					// Bank order is crucial here -- %0000 to %0011 must alias main RAM.
					encodedBankInfo = (portb & 0x0c) >> 2;
					encodedBankInfo += (portb & 0x60) >> 3;
					break;

				case kATMemoryMode_320K_Compy:
				case kATMemoryMode_576K_Compy:
					encodedBankInfo += (uint32)(~portb & 0xc0) >> 4;

					if (!(portb & 0x02))
						encodedBankInfo += 0x10;
					break;

				default:
					encodedBankInfo += (uint32)(~portb & 0x60) >> 3;

					if (!(portb & 0x02))
						encodedBankInfo += 0x10;

					if (!(portb & 0x80))
						encodedBankInfo += 0x20;

					break;
			}

			encodedBankInfo &= extbankmask;
			encodedBankInfo += extbankoffset;

			if (cpuEnabled)
				encodedBankInfo |= kMapInfo_ExtendedCPU;

			if (anticEnabled)
				encodedBankInfo |= kMapInfo_ExtendedANTIC;
		}

		if (mpLayerSelfTest) {
			// NOTE: The kernel ROM must be enabled for the self-test ROM to appear.
			// Storm breaks if this is not checked.
			if ((portb & 0x81) == 0x01) {
				// If bit 7 is reused for banking, it must not enable the self-test
				// ROM when extbanking is enabled.
				switch(memmode) {
					case kATMemoryMode_320K_Compy:
					case kATMemoryMode_576K_Compy:
					case kATMemoryMode_1088K:
						if (cpuEnabled || anticEnabled)
							break;

						// fall through
					default:
						encodedBankInfo += kMapInfo_SelfTest;
						break;
				}
			}
		}

		if (!((portb | basicMask) & 0x02)) {
			if (!blockExtBasic || (!cpuEnabled && !anticEnabled))
				encodedBankInfo += kMapInfo_BASIC;
		}

		if ((portb | kernelBankMask) & 0x01)
			encodedBankInfo += kMapInfo_Kernel;

		if (hwmode == kATHardwareMode_XEGS && !(encodedBankInfo & kMapInfo_BASIC) && !(portb & 0x40))
			encodedBankInfo += kMapInfo_Game;

		if (mpLayerHiddenRAM && (portb & 0xb1) == 0x30)
			encodedBankInfo += kMapInfo_HiddenRAM;

		mBankMap[portb] = encodedBankInfo;
	}

	// force bank reinit
	mCurrentBankInfo = ~mBankMap[mCurrentBank];
	mCurrentBank = ~mCurrentBank;
	SetBankRegister(~mCurrentBank);
}

void ATMMUEmulator::ShutdownMapping() {
	mpMemory = NULL;
	mpLayerExtRAM = NULL;
	mpLayerSelfTest = NULL;
	mpLayerLowerKernel = NULL;
	mpLayerUpperKernel = NULL;
	mpLayerBASIC = NULL;
	mpLayerGame = NULL;
	mpLayerHiddenRAM = NULL;

	mCPUBase = 0;
	mAnticBase = 0;
}

void ATMMUEmulator::SetROMMappingHook(const vdfunction<void()>& fn) {
	mpROMMappingChangeFn = fn;

	UpdateROMMappingHook();
}

void ATMMUEmulator::SetHighMemory(uint32 numBanks, void *mem) {
	if (mpLayerHighRAM) {
		mpMemMan->DeleteLayer(mpLayerHighRAM);
		mpLayerHighRAM = nullptr;
	}

	if (numBanks) {
		mpLayerHighRAM = mpMemMan->CreateLayer(kATMemoryPri_BaseRAM, (const uint8 *)mem, 0x100, numBanks << 8, false);
		mpMemMan->SetLayerFastBus(mpLayerHighRAM, true);
		mpMemMan->SetLayerName(mpLayerHighRAM, "65C816 linear RAM");
		mpMemMan->SetLayerModes(mpLayerHighRAM, kATMemoryAccessMode_RW);
	}
}

void ATMMUEmulator::SetAxlonMemory(uint8 bankbits, bool enableAliasing, void *mem) {
	const uint8 bankMask = (uint8)((1 << bankbits) - 1);

	if (mAxlonBankMask == bankMask && mbAxlonAliasing == enableAliasing && mpAxlonMemory == mem)
		return;

	if (mpLayerAxlonControl1) {
		mpMemMan->DeleteLayer(mpLayerAxlonControl1);
		mpLayerAxlonControl1 = NULL;
	}

	if (mpLayerAxlonControl2) {
		mpMemMan->DeleteLayer(mpLayerAxlonControl2);
		mpLayerAxlonControl2 = NULL;
	}

	mbAxlonAliasing = enableAliasing;
	mAxlonBankMask = bankMask;
	mAxlonBank &= mAxlonBankMask;
	mpAxlonMemory = mem;

	if (bankbits) {
		ATMemoryHandlerTable handler = {};
		handler.mbPassWrites = true;
		handler.mpThis = this;
		handler.mpWriteHandler = OnAxlonWrite;

		if (mbAxlonAliasing) {
			mpLayerAxlonControl1 = mpMemMan->CreateLayer(kATMemoryPri_ExtRAM+1, handler, 0x0F, 0x01);
			mpMemMan->SetLayerName(mpLayerAxlonControl1, "Axlon control (low mirror)");
			mpMemMan->EnableLayer(mpLayerAxlonControl1, kATMemoryAccessMode_CPUWrite, true);
		}

		mpLayerAxlonControl2 = mpMemMan->CreateLayer(kATMemoryPri_ROM+1, handler, 0xCF, 0x01);
		mpMemMan->SetLayerName(mpLayerAxlonControl2, "Axlon control (high mirror)");
		mpMemMan->EnableLayer(mpLayerAxlonControl2, kATMemoryAccessMode_CPUWrite, true);
	}

	if (mpLayerExtRAM)
		mpMemMan->EnableLayer(mpLayerExtRAM, mAxlonBank != 0);

	if (mpMemory)
		RebuildMappingTables();
}

void ATMMUEmulator::GetMemoryMapState(ATMemoryMapState& state) const {
	state.mbKernelEnabled = (mCurrentBankInfo & kMapInfo_Kernel) != 0;
	state.mbBASICEnabled = (mCurrentBankInfo & kMapInfo_BASIC) != 0;
	state.mbSelfTestEnabled = (mCurrentBankInfo & kMapInfo_SelfTest) != 0;
	state.mbGameEnabled = (mCurrentBankInfo & kMapInfo_Game) != 0;
	state.mbExtendedCPU = (mCurrentBankInfo & kMapInfo_ExtendedCPU) != 0;
	state.mbExtendedANTIC = (mCurrentBankInfo & kMapInfo_ExtendedANTIC) != 0;
	state.mExtendedBank = (mCurrentBankInfo & kMapInfo_BankMask);
	state.mAxlonBank = mAxlonBank;
	state.mAxlonBankMask = mAxlonBankMask;
}

uint32 ATMMUEmulator::ExtBankToMemoryOffset(uint8 bank) const {
	const uint32 bankInfo = mBankMap[bank];

	if (!(bankInfo & (kMapInfo_ExtendedCPU | kMapInfo_ExtendedANTIC)))
		return 0x4000;

	return (bankInfo & kMapInfo_BankMask) << 14;
}

void ATMMUEmulator::ClearModeOverrides() {
	SetModeOverrides(-1, false);
}

void ATMMUEmulator::SetModeOverrides(int memoryMode, bool forceBasic) {
	if (mMemoryModeOverride == memoryMode && mbForceBasic == forceBasic)
		return;

	mMemoryModeOverride = memoryMode;
	mbForceBasic = forceBasic;

	RebuildMappingTables();
}

void ATMMUEmulator::SetBankRegister(uint8 bank) {
	if (mCurrentBank == bank)
		return;

	mCurrentBank = bank;

	const uint32 bankInfo = mBankMap[bank];

	if (mCurrentBankInfo == bankInfo)
		return;

	mCurrentBankInfo = bankInfo;

	const uint32 bankOffset = ((bankInfo & kMapInfo_BankMask) << 14);
	uint8 *bankbase = mpMemory + bankOffset;

	if (bankInfo & (kMapInfo_ExtendedCPU | kMapInfo_ExtendedANTIC)) {
		// If settings are in flux, we may temporarily have the mapping mode set to
		// enable extended memory while not having an extRAM layer.
		if (mpLayerExtRAM) {
			uint32 addressSpace = bankOffset < 0x10000
				? kATAddressSpace_RAM + bankOffset
				: kATAddressSpace_PORTB + 0x4000 + ((uint32)(bank | (mCPUBankMask ^ 0xFF)) << 16);
			mpMemMan->SetLayerMemoryAndAddressSpace(mpLayerExtRAM, bankbase, 0x40, 0x40, addressSpace);
		}
	} else
		UpdateAxlonBank();

	mCPUBase = 0;
	mAnticBase = 0;

	bool extcpu = (mAxlonBank > 0);
	bool extantic = (mAxlonBank > 0);

	if (bankInfo & kMapInfo_ExtendedCPU) {
		mCPUBase = bankOffset;
		extcpu = true;
	}

	if (bankInfo & kMapInfo_ExtendedANTIC) {
		mAnticBase = bankOffset;
		extantic = true;
	}

	if (mpLayerExtRAM) {
		mpMemMan->EnableLayer(mpLayerExtRAM, kATMemoryAccessMode_CPURead, extcpu);
		mpMemMan->EnableLayer(mpLayerExtRAM, kATMemoryAccessMode_CPUWrite, extcpu);
		mpMemMan->EnableLayer(mpLayerExtRAM, kATMemoryAccessMode_AnticRead, extantic);
	}

	const bool selfTestEnabled = (bankInfo & kMapInfo_SelfTest) != 0;

	if (mpLayerSelfTest)
		mpMemMan->EnableLayer(mpLayerSelfTest, selfTestEnabled);

	const bool kernelEnabled = (bankInfo & kMapInfo_Kernel) != 0;

	if (mpLayerAxlonControl2)
		mpMemMan->EnableLayer(mpLayerAxlonControl2, kATMemoryAccessMode_CPUWrite, kernelEnabled);

	if (mpLayerLowerKernel) {
		mpMemMan->EnableLayer(mpLayerLowerKernel, kernelEnabled);
	}

	if (mpLayerUpperKernel) {
		mpMemMan->EnableLayer(mpLayerUpperKernel, kernelEnabled);
	}

	if (mpLayerBASIC)
		mpMemMan->EnableLayer(mpLayerBASIC, (bankInfo & kMapInfo_BASIC) != 0);

	if (mpLayerGame)
		mpMemMan->EnableLayer(mpLayerGame, (bankInfo & kMapInfo_Game) != 0);

	if (mpLayerHiddenRAM)
		mpMemMan->EnableLayer(mpLayerHiddenRAM, (bankInfo & kMapInfo_HiddenRAM) != 0);

	UpdateROMMappingHook();
}

void ATMMUEmulator::SetAxlonBank(uint8 bank) {
	bank &= mAxlonBankMask;

	if (mAxlonBank != bank) {
		mAxlonBank = bank;

		UpdateAxlonBank();
	}
}

void ATMMUEmulator::UpdateAxlonBank() {
	if (mpLayerExtRAM && !(mCurrentBankInfo & (kMapInfo_ExtendedCPU | kMapInfo_ExtendedANTIC))) {
		if (mAxlonBank > 0) {
			mpMemMan->SetLayerMemory(mpLayerExtRAM, (uint8 *)mpAxlonMemory + ((uint32)(mAxlonBank - 1) << 14), 0x40, 0x40);
			mpMemMan->EnableLayer(mpLayerExtRAM, true);
		} else {
			mpMemMan->EnableLayer(mpLayerExtRAM, false);
		}
	}
}

bool ATMMUEmulator::OnAxlonWrite(void *thisptr, uint32 addr, uint8 value) {
	if ((addr & 0x0FF0) == 0xFF0) {
		((ATMMUEmulator *)thisptr)->SetAxlonBank(value);
	}

	return false;
}

void ATMMUEmulator::UpdateROMMappingHook() {
	if (mpROMMappingChangeFn)
		mpROMMappingChangeFn();
}
