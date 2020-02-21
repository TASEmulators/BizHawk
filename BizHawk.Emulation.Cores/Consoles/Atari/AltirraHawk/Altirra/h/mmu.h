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

#ifndef f_AT_MMU_H
#define f_AT_MMU_H

#include <vd2/system/function.h>

class ATMemoryLayer;
class ATMemoryManager;
class IATHLEKernel;

struct ATMemoryMapState {
	bool mbKernelEnabled;
	bool mbBASICEnabled;
	bool mbSelfTestEnabled;
	bool mbGameEnabled;
	bool mbExtendedCPU;
	bool mbExtendedANTIC;
	uint32 mExtendedBank;
	uint8 mAxlonBank;
	uint8 mAxlonBankMask;
};

class ATMMUEmulator {
	ATMMUEmulator(const ATMMUEmulator&) = delete;
	ATMMUEmulator& operator=(const ATMMUEmulator&) = delete;
public:
	ATMMUEmulator();
	~ATMMUEmulator();

	void Init(ATMemoryManager *memman);
	void Shutdown();

	void InitMapping(int hardwareMode,
		int memoryMode,
		void *extBase,
		ATMemoryLayer *extLayer,
		ATMemoryLayer *selfTestLayer,
		ATMemoryLayer *lowerKernelLayer,
		ATMemoryLayer *upperKernelLayer,
		ATMemoryLayer *basicLayer,
		ATMemoryLayer *gameLayer,
		ATMemoryLayer *hiddenRamLayer);
	void ShutdownMapping();

	bool IsKernelROMEnabled() const { return (mCurrentBankInfo & kMapInfo_Kernel) != 0; }
	bool IsSelfTestROMEnabled() const { return (mCurrentBankInfo & kMapInfo_SelfTest) != 0; }
	bool IsBASICROMEnabled() const { return (mCurrentBankInfo & kMapInfo_BASIC) != 0; }
	bool IsBASICOrGameROMEnabled() const { return (mCurrentBankInfo & (kMapInfo_BASIC | kMapInfo_Game)) != 0; }

	void SetROMMappingHook(const vdfunction<void()>& fn);

	void SetHighMemory(uint32 numBanks, void *mem);
	void SetAxlonMemory(uint8 bankbits, bool enableAliasing, void *mem);

	void GetMemoryMapState(ATMemoryMapState& state) const;

	uint32 GetCPUBankBase() const { return mCPUBase; }
	uint32 GetAnticBankBase() const { return mAnticBase; }

	uint32 ExtBankToMemoryOffset(uint8 portbVal) const;

	void ClearModeOverrides();
	void SetModeOverrides(int memoryMode, bool forceBASIC);

	uint8 GetBankRegister() const { return mCurrentBank; }
	void SetBankRegister(uint8 bank);

	void SetAxlonBank(uint8 bank);
	uint8 GetAxlonBank() const { return mAxlonBank; }

protected:
	void RebuildMappingTables();
	void UpdateAxlonBank();
	static bool OnAxlonWrite(void *thisptr, uint32 addr, uint8 value);

	void UpdateROMMappingHook();

	int mHardwareMode;
	int mMemoryMode;
	int mMemoryModeOverride;
	bool mbForceBasic;
	ATMemoryManager *mpMemMan;
	uint8 *mpMemory;
	ATMemoryLayer *mpLayerExtRAM;
	ATMemoryLayer *mpLayerSelfTest;
	ATMemoryLayer *mpLayerLowerKernel;
	ATMemoryLayer *mpLayerUpperKernel;
	ATMemoryLayer *mpLayerBASIC;
	ATMemoryLayer *mpLayerGame;
	ATMemoryLayer *mpLayerHiddenRAM;
	ATMemoryLayer *mpLayerHighRAM;
	ATMemoryLayer *mpLayerAxlonControl1;
	ATMemoryLayer *mpLayerAxlonControl2;
	bool		mbBASICForced;
	uint8		mCurrentBank;
	uint8		mCPUBankMask;		// PORTB bits pertinent to CPU xbanking
	uint8		mAxlonBank;
	uint8		mAxlonBankMask;
	bool		mbAxlonAliasing;
	void		*mpAxlonMemory = nullptr;

	uint32		mCPUBase;
	uint32		mAnticBase;
	uint32		mCurrentBankInfo;

	vdfunction<void()> mpROMMappingChangeFn;

	// bits 0-8: bank number (shr 14) (8MB)
	// bit 9: Hidden RAM enable
	// bit 10: Game ROM enable
	// bit 11: CPU extended RAM enable
	// bit 12: ANTIC extended RAM enable
	// bit 13: Kernel ROM enable
	// bit 14: BASIC ROM enable
	// bit 15: Self-test ROM enable
	enum {
		kMapInfo_BankMask		= 0x01FF,
		kMapInfo_HiddenRAM		= 0x0200,
		kMapInfo_Game			= 0x0400,
		kMapInfo_ExtendedCPU	= 0x0800,
		kMapInfo_ExtendedANTIC	= 0x1000,
		kMapInfo_Kernel			= 0x2000,
		kMapInfo_BASIC			= 0x4000,
		kMapInfo_SelfTest		= 0x8000,
	};

	uint16		mBankMap[256];
};

#endif
