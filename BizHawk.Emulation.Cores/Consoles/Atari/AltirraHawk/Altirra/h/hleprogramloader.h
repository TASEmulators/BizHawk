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

#ifndef f_AT_HLEPROGRAMLOADER_H
#define f_AT_HLEPROGRAMLOADER_H

#include <at/atcore/devicesio.h>
#include <at/atcore/devicesioimpl.h>
#include "constants.h"

class ATCPUEmulator;
class ATCPUHookManager;
class ATSimulator;
class ATSimulatorEventManager;
struct ATCPUHookNode;
class IATBlobImage;

class ATHLEProgramLoader : public ATDeviceSIO {
	ATHLEProgramLoader(const ATHLEProgramLoader&) = delete;
	ATHLEProgramLoader& operator=(const ATHLEProgramLoader&) = delete;
public:
	ATHLEProgramLoader();
	~ATHLEProgramLoader();

	void Init(ATCPUEmulator *cpu, ATSimulatorEventManager *simEventMan, ATSimulator *sim, IATDeviceSIOManager *siomgr);
	void Shutdown();

	void SetRandomizeMemoryOnLoad(bool enable) { mbRandomizeMemoryOnLoad = enable; }

	IATBlobImage *GetCurrentImage() const { return mpImage; }
	bool IsLaunchPending() const { return mbLaunchPending; }

	void LoadProgram(const wchar_t *symbolHintPath, IATBlobImage *image, ATHLEProgramLoadMode launchMode);

public:
	void InitSIO(IATDeviceSIOManager *mgr) override;
	CmdResponse OnSerialBeginCommand(const ATDeviceSIOCommand& cmd) override;

protected:
	uint8 OnDSKINV(uint16);
	uint8 OnDeferredLaunch(uint16);
	uint8 StartLoad();
	uint8 OnLoadContinue(uint16);

	void UnloadProgramSymbols();

	ATCPUEmulator *mpCPU = nullptr;
	ATCPUHookManager *mpCPUHookMgr = nullptr;
	ATSimulatorEventManager *mpSimEventMgr = nullptr;
	IATDeviceSIOManager *mpSIOMgr = nullptr;
	ATSimulator *mpSim = nullptr;
	ATCPUHookNode *mpLaunchHook = nullptr;
	ATCPUHookNode *mpLoadContinueHook = nullptr;

	IATBlobImage *mpImage = nullptr;
	uint32		mProgramLoadIndex = 0;

	bool		mbLastKernelEnabledState = false;
	bool		mbType3PollActive = false;
	bool		mbType3PollEnabled = false;
	bool		mbDiskBootEnabled = true;

	bool		mbRandomizeMemoryOnLoad = false;
	bool		mbLaunchPending = false;
	uint32		mProgramModuleIds[4] = {};

	static const uint8 kHandlerData[];
	static const uint8 kBootSector[];
};

#endif	// f_AT_HLEPROGRAMLOADER_H
