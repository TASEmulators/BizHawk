//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2015 Avery Lee
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

#ifndef f_AT_VERONICA_H
#define f_AT_VERONICA_H

#include <vd2/system/function.h>
#include <vd2/system/refcount.h>
#include <vd2/system/vdstl.h>
#include <at/atcore/deviceimpl.h>
#include <at/atcore/deviceprinter.h>
#include <at/atcore/deviceserial.h>
#include <at/atcore/devicecart.h>
#include <at/atcpu/co65802.h>
#include <at/atcpu/breakpoints.h>
#include <at/atcpu/history.h>
#include <at/atcpu/memorymap.h>
#include <at/atdebugger/breakpointsimpl.h>
#include <at/atdebugger/target.h>
#include <at/atcore/scheduler.h>

class ATMemoryLayer;
class ATIRQController;

class ATVeronicaEmulator final : public ATDevice
	, public IATDeviceMemMap
	, public IATDeviceScheduling
	, public IATDeviceDebugTarget
	, public IATDeviceCartridge
	, public IATDebugTarget
	, public IATDebugTargetHistory
	, public IATDebugTargetExecutionControl
	, public IATSchedulerCallback
	, public IATCPUBreakpointHandler
{
public:
	ATVeronicaEmulator();
	~ATVeronicaEmulator();

	void *AsInterface(uint32 iid);

	virtual void GetDeviceInfo(ATDeviceInfo& info);
	virtual void GetSettings(ATPropertySet& settings);
	virtual bool SetSettings(const ATPropertySet& settings);
	virtual void Init();
	virtual void Shutdown();
	virtual void WarmReset();
	virtual void ColdReset();

public:
	virtual void InitMemMap(ATMemoryManager *memmap);
	virtual bool GetMappedRange(uint32 index, uint32& lo, uint32& hi) const;

public:
	virtual void InitScheduling(ATScheduler *sch, ATScheduler *slowsch);

public:
	void InitCartridge(IATDeviceCartridgePort *port) override;
	bool IsLeftCartActive() const override;
	void SetCartEnables(bool leftEnable, bool rightEnable, bool cctlEnable) override;
	void UpdateCartSense(bool leftActive) override {}

public:	// IATDeviceDebugTarget
	IATDebugTarget *GetDebugTarget(uint32 index) override;

public:	// IATDebugTarget
	const char *GetName() override;
	ATDebugDisasmMode GetDisasmMode() override;

	void GetExecState(ATCPUExecState& state) override;
	void SetExecState(const ATCPUExecState& state) override;

	sint32 GetTimeSkew() override;

	uint8 ReadByte(uint32 address) override;
	void ReadMemory(uint32 address, void *dst, uint32 n) override;

	uint8 DebugReadByte(uint32 address) override;
	void DebugReadMemory(uint32 address, void *dst, uint32 n) override;

	void WriteByte(uint32 address, uint8 value) override;
	void WriteMemory(uint32 address, const void *src, uint32 n) override;

public:	// IATDebugTargetHistory
	bool GetHistoryEnabled() const override;
	void SetHistoryEnabled(bool enable) override;

	std::pair<uint32, uint32> GetHistoryRange() const override;
	uint32 ExtractHistory(const ATCPUHistoryEntry **hparray, uint32 start, uint32 n) const override;
	uint32 ConvertRawTimestamp(uint32 rawTimestamp) const override;
	double GetTimestampFrequency() const override;

public:	// IATDebugTargetExecutionControl
	void Break() override;
	bool StepInto(const vdfunction<void(bool)>& fn) override;
	bool StepOver(const vdfunction<void(bool)>& fn) override;
	bool StepOut(const vdfunction<void(bool)>& fn) override;
	void StepUpdate() override;
	void RunUntilSynced() override;

public:	// IATCPUBreakpointHandler
	bool CheckBreakpoint(uint32 pc) override;

public:	// IATSchedulerCallback
	void OnScheduledEvent(uint32 id) override;

protected:
	void CancelStep();

	static sint32 OnDebugRead(void *thisptr, uint32 addr);
	static sint32 OnRead(void *thisptr, uint32 addr);
	static bool OnWrite(void *thisptr, uint32 addr, uint8 value);

	static uint8 OnCorruptableDebugRead(uint32 addr, void *thisptr);
	static uint8 OnCorruptableRead(uint32 addr, void *thisptr);
	static void OnCorruptableWrite(uint32 addr, uint8 value, void *thisptr);

	void WriteVControl(uint8 val);
	void UpdateCoProcWindowDormant();
	void UpdateCoProcWindowActive();
	void UpdateWindowBase();
	void UpdateLeftWindowMapping();
	void UpdateRightWindowMapping();
	void Sync();
	void AccumSubCycles();
	void RunSubCycles(uint32 subCycles);
	uint32 PeekRand16() const;
	uint32 Rand16();

	ATScheduler *mpScheduler;
	ATScheduler *mpSlowScheduler;
	ATEvent *mpRunEvent;
	ATMemoryManager *mpMemMan;
	ATMemoryLayer *mpMemLayerLeftWindow;
	ATMemoryLayer *mpMemLayerRightWindow;
	ATMemoryLayer *mpMemLayerControl;

	IATDeviceCartridgePort *mpCartridgePort = nullptr;
	uint32 mCartId = 0;
	bool mbLeftWindowEnabled = false;
	bool mbRightWindowEnabled = false;
	bool mbCCTLEnabled = false;

	uint32 mLastSync = 0;
	uint32 mSubCyclesLeft = 0;
	uint8 mAControl = 0;
	uint8 mVControl = 0;
	bool mbVersion1 = false;
	bool mbCorruptNextCycle = false;
	uint8 *mpCoProcWinBase = nullptr;

	uint32	mPRNG = 0;

	ATCoProcWriteMemNode mWriteNode = {};
	ATCoProcReadMemNode mCorruptedReadNode = {};
	ATCoProcWriteMemNode mCorruptedWriteNode = {};
	ATCoProc65802 mCoProc;

	vdfastvector<ATCPUHistoryEntry> mHistory;

	VDALIGN(4) uint8 mRAM[0x20000] = {};
	
	vdfunction<void(bool)> mpStepHandler = {};
	bool mbStepOut = false;
	bool mbStepNotifyPending = false;
	bool mbStepNotifyPendingBP = false;
	uint32 mStepStartSubCycle = 0;
	uint16 mStepOutS = 0;

	ATDebugTargetBreakpointsImpl mBreakpointsImpl;
};

#endif
