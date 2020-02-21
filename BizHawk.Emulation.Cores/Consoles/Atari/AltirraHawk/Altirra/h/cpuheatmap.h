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

#ifndef f_AT_CPUHEATMAP_H
#define f_AT_CPUHEATMAP_H

#include <vd2/system/vdtypes.h>

class ATCPUEmulator;
class ATSimulatorEventManager;

enum ATCPUHeatMapTrapFlags : uint32 {
	kATCPUHeatMapTrapFlags_Load		= 0x01,
	kATCPUHeatMapTrapFlags_Compute	= 0x02,
	kATCPUHeatMapTrapFlags_Branch	= 0x04,
	kATCPUHeatMapTrapFlags_EffectiveAddress = 0x08,
	kATCPUHeatMapTrapFlags_HwStore	= 0x10,
	kATCPUHeatMapTrapFlags_All		= 0x1F,
};

class ATCPUHeatMap {
public:
	enum : uint32 {
		kTypeUnknown	= 0x00000,
		kTypePreset		= 0x10000,
		kTypeImm		= 0x20000,
		kTypeComputed	= 0x30000,
		kTypeHardware	= 0x80000000,
		kTypeMask		= 0xFFFF0000
	};

	enum {
		kAccessRead		= 0x01,
		kAccessWrite	= 0x02
	};

	ATCPUHeatMap();
	~ATCPUHeatMap();

	void Init(ATSimulatorEventManager *pSimEvtMgr);

	uint32 GetAStatus() const { return mA; }
	uint32 GetXStatus() const { return mX; }
	uint32 GetYStatus() const { return mY; }
	uint8 GetAValidity() const { return mAValid; }
	uint8 GetXValidity() const { return mXValid; }
	uint8 GetYValidity() const { return mYValid; }
	uint8 GetPValidity() const { return mPValid; }

	uint32 GetMemoryStatus(uint16 addr) const { return mMemory[addr]; }
	uint8 GetMemoryAccesses(uint16 addr) const { return mMemAccess[addr]; }
	uint8 GetMemoryValidity(uint16 addr) const { return mMemValid[addr]; }

	void SetEarlyState(bool isEarly);

	ATCPUHeatMapTrapFlags GetEarlyTrapFlags() const { return mTrapFlagsEarly; }
	ATCPUHeatMapTrapFlags GetNormalTrapFlags() const { return mTrapFlagsNormal; }

	void SetEarlyTrapFlags(ATCPUHeatMapTrapFlags trapFlags);
	void SetNormalTrapFlags(ATCPUHeatMapTrapFlags trapFlags);

	void Reset();

	void ResetMemoryRange(uint32 addr, uint32 len);
	void MarkMemoryRangeHardware(uint32 addr, uint32 len);
	void PresetMemoryRange(uint32 addr, uint32 len);

	VDNOINLINE void ProcessInsn(const ATCPUEmulator& cpu, uint8 opcode, uint16 addr, uint16 pc);

protected:
	void TrapOnUninitLoad(uint8 opcode, uint16 addr, uint16 pc);
	void TrapOnUninitCompute(uint8 opcode, uint16 addr, uint16 pc);
	void TrapOnUninitBranch(uint8 opcode, uint16 addr, uint16 pc);
	void TrapOnUninitEffectiveAddress(uint8 opcode, uint16 addr, uint16 pc);
	void TrapOnUninitHwStore(uint8 opcode, uint16 addr, uint16 pc);
	void LogValidityStatus();
	void UpdateTrapFlags();

	uint32	mA;
	uint32	mX;
	uint32	mY;
	uint8	mAValid;
	uint8	mXValid;
	uint8	mYValid;
	uint8	mPValid;

	bool	mbEarlyState = false;
	bool	mbTrapOnUninitLoad = false;
	bool	mbTrapOnUninitCompute = false;
	bool	mbTrapOnUninitBranch = false;
	bool	mbTrapOnUninitEa = false;
	bool	mbTrapOnUninitHwStore = false;

	ATCPUHeatMapTrapFlags mTrapFlagsEarly = (ATCPUHeatMapTrapFlags)0;
	ATCPUHeatMapTrapFlags mTrapFlagsNormal = (ATCPUHeatMapTrapFlags)0;

	ATSimulatorEventManager *mpSimEvtMgr = nullptr;

	uint32	mMemory[0x10000];
	uint8	mMemAccess[0x10000];
	uint8	mMemValid[0x10000];
};

#endif	// f_AT_CPUHEATMAP_H
