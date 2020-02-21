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
#include <vd2/system/binary.h>
#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include <vd2/system/filesys.h>
#include <at/atcore/memoryutils.h>
#include <at/atcore/vfs.h>
#include <at/atio/blobimage.h>
#include "hleprogramloader.h"
#include "hleutils.h"
#include "kerneldb.h"
#include "console.h"
#include "cpu.h"
#include "cpuheatmap.h"
#include "cpuhookmanager.h"
#include "debugger.h"
#include "simeventmanager.h"
#include "simulator.h"

namespace {
	bool IsDOSExecutable(const uint8 *buf, uint32 len) {
		if (len <= 28)
			return false;

		// Check for MZ signature.
		if (buf[0] != 'M' || buf[1] != 'Z')
			return false;

		// Check for last block size being <= 512. This also rules out a false positive as this
		// requirement ensures that the first four bytes would be an invalid Atari DOS 2 segment.
		if (VDReadUnalignedLEU16(&buf[2]) > 512)
			return false;

		return true;
	}

	bool IsNEExecutable(const uint8 *buf, uint32 len) {
		VDASSERT(IsDOSExecutable(buf, len));

		if (len < 64)
			return false;

		// extract NE header offset
		uint32 neOffset = VDReadUnalignedLEU32(&buf[60]);

		// check if NE header can actually fit at offset
		if (neOffset > len || len - neOffset < 64)
			return false;

		// check for NE signature
		return buf[neOffset] == 'N' && buf[neOffset+1] == 'E';
	}

	bool IsPEExecutable(const uint8 *buf, uint32 len) {
		VDASSERT(IsDOSExecutable(buf, len));

		if (len < 64)
			return false;

		// extract PE header offset
		uint32 peOffset = VDReadUnalignedLEU32(&buf[60]);

		// check if PE header can actually fit at offset
		if (peOffset > len || len - peOffset < 64)
			return false;

		// check for PE signature
		return buf[peOffset] == 'P' && buf[peOffset+1] == 'E';

	}
}

const uint8 ATHLEProgramLoader::kHandlerData[]={
	// text record
	0x00, 0x11,
		0x00, 0x00,			//	relative load address (0)
		0x00, 0x00,			//			dta		a(DevOpen-1)
		0x00, 0x00,			//			dta		a(DevClose-1)
		0x00, 0x00,			//			dta		a(DevGetByte-1)
		0x00, 0x00,			//			dta		a(DevPutByte-1)
		0x00, 0x00,			//			dta		a(DevGetStatus-1)
		0x00, 0x00,			//			dta		a(DevSpecial-1)
		0x4C, 0xFF, 0x01,	//			jmp		$01FF		;initiate launch through hook

	// end
	0x0B, 0x00, 0x00, 0x00
};

const uint8 ATHLEProgramLoader::kBootSector[]={
	0x00,				// flags
	0x01,				// boot sector count
	0x00, 0x07,			// load address
	0xC0, 0xE4,			// init address (KnownRTS)
	0x4C, 0xFF, 0x01,	// initiate launch process
};

ATHLEProgramLoader::ATHLEProgramLoader() {
}

ATHLEProgramLoader::~ATHLEProgramLoader() {
	Shutdown();
}

void ATHLEProgramLoader::Init(ATCPUEmulator *cpu, ATSimulatorEventManager *simEventMgr, ATSimulator *sim, IATDeviceSIOManager *sioMgr) {
	mpCPU = cpu;
	mpCPUHookMgr = cpu->GetHookManager();
	mpSimEventMgr = simEventMgr;
	mpSim = sim;
	mpSIOMgr = sioMgr;
}

void ATHLEProgramLoader::Shutdown() {
	UnloadProgramSymbols();

	if (mpSIOMgr) {
		mpSIOMgr->RemoveDevice(this);
		mpSIOMgr = nullptr;
	}

	if (mpCPUHookMgr) {
		mpCPUHookMgr->UnsetHook(mpLoadContinueHook);
		mpCPUHookMgr->UnsetHook(mpLaunchHook);
		mpCPUHookMgr = NULL;
	}

	mpSim = nullptr;
	mpSimEventMgr = nullptr;
	mpCPU = nullptr;

	mbLaunchPending = false;

	vdsaferelease <<= mpImage;
}

void ATHLEProgramLoader::LoadProgram(const wchar_t *symbolHintPath, IATBlobImage *image, ATHLEProgramLoadMode launchMode) {
	vdsaferelease <<= mpImage;

	uint32 len = image->GetSize();
	const uint8 *const buf = (const uint8 *)image->GetBuffer();
	mProgramLoadIndex = 0;

	// check if this is a SpartaDOS executable by looking for a reloc block
	if (len >= 4 && (buf[0] == 0xfe || buf[0] == 0xfa) && buf[1] == 0xff)
		throw MyError("Program load failed: this program must be loaded under SpartaDOS X.");

	// check if this is a DOS or Windows executable -- we need to be conservative about this to
	// avoid false positives
	if (IsDOSExecutable(buf, len)) {
		if (IsPEExecutable(buf, len) || IsNEExecutable(buf, len))
			throw MyError("Program load failed: this program is written for Windows.");
		else
			throw MyError("Program load failed: this program is written for MS-DOS.");
	}

	mpSIOMgr->RemoveDevice(this);
	mbType3PollActive = false;
	mbType3PollEnabled = false;
	mbDiskBootEnabled = false;

	if (launchMode == kATHLEProgramLoadMode_Default)
		mpCPUHookMgr->SetHookMethod(mpLaunchHook, kATCPUHookMode_KernelROMOnly, ATKernelSymbols::DSKINV, 10, this, &ATHLEProgramLoader::OnDSKINV);
	else {
		mpCPUHookMgr->SetHookMethod(mpLaunchHook, kATCPUHookMode_Always, 0x1FF, 0, this, &ATHLEProgramLoader::OnDeferredLaunch);
		mpSIOMgr->AddDevice(this);

		if (launchMode == kATHLEProgramLoadMode_DiskBoot)
			mbDiskBootEnabled = true;
		else if (launchMode == kATHLEProgramLoadMode_Type3Poll) {
			mbType3PollActive = true;
			mbType3PollEnabled = true;
		}
	}

	// try to load symbols
	UnloadProgramSymbols();

	IATDebugger *d = ATGetDebugger();

	if (d && d->IsSymbolLoadingEnabled() && symbolHintPath) {
		static const wchar_t *const kSymExts[]={
			L".lst", L".lab", L".lbl", L".dbg"
		};

		VDASSERTCT(sizeof(kSymExts)/sizeof(kSymExts[0]) == sizeof(mProgramModuleIds)/sizeof(mProgramModuleIds[0]));

		const wchar_t *symbolHintPathExt = VDFileSplitExt(symbolHintPath);

		VDStringW sympath;
		for(int i=0; i<sizeof(mProgramModuleIds)/sizeof(mProgramModuleIds[0]); ++i) {
			sympath.assign(symbolHintPath, symbolHintPathExt);
			sympath += kSymExts[i];

			try {
				const uint32 target0 = 0;
				uint32 moduleId = d->LoadSymbols(sympath.c_str(), false, &target0);

				if (moduleId) {
					mProgramModuleIds[i] = moduleId;

					ATConsolePrintf("Loaded symbols %ls\n", sympath.c_str());
				}
			} catch(const MyError&) {
				// ignore
			}
		}

		// process directives AFTER all symbols have been loaded
		for(int i=0; i<sizeof(mProgramModuleIds)/sizeof(mProgramModuleIds[0]); ++i) {
			if (mProgramModuleIds[i])
				d->ProcessSymbolDirectives(mProgramModuleIds[i]);
		}

		// load debugger script
		sympath.assign(symbolHintPath, symbolHintPathExt);
		sympath += L".atdbg";

		d->QueueAutoLoadBatchFile(sympath.c_str());
	}

	mbLaunchPending = true;

	image->AddRef();
	mpImage = image;
}

void ATHLEProgramLoader::InitSIO(IATDeviceSIOManager *mgr) {
}

ATHLEProgramLoader::CmdResponse ATHLEProgramLoader::OnSerialBeginCommand(const ATDeviceSIOCommand& cmd) {
	if (!cmd.mbStandardRate)
		return kCmdResponse_NotHandled;

	// check for type 3 poll
	uint8 buf[128];

	if (mbType3PollEnabled && cmd.mDevice == 0x4F && cmd.mCommand == 0x40 && cmd.mAUX[0] == cmd.mAUX[1]) {
		if (cmd.mAUX[0] == 0x00 && cmd.mPollCount == 1 && mbType3PollActive) {
			mbType3PollActive = false;

			// send back handler info
			buf[0] = (uint8)(sizeof(kHandlerData) >> 0);
			buf[1] = (uint8)(sizeof(kHandlerData) >> 8);
			buf[2] = 0x7D;
			buf[3] = 0x00;

			mpSIOMgr->BeginCommand();
			mpSIOMgr->SendACK();
			mpSIOMgr->SendComplete();
			mpSIOMgr->SendData(buf, 4, true);
			mpSIOMgr->EndCommand();

			return kCmdResponse_Start;
		} else if (cmd.mAUX[0] == 0x4F) {
			// Poll Reset -- do not respond, but re-enable poll
			mbType3PollActive = true;
			return kCmdResponse_NotHandled;
		}
	}

	if (cmd.mDevice == 0x31 && mbDiskBootEnabled) {
		if (cmd.mCommand == 0x52) {		// read sector
			// get sector number
			const uint32 sector = VDReadUnalignedLEU16(cmd.mAUX);

			// fail with NAK if outside of SD disk
			if (sector == 0 || sector > 720)
				return kCmdResponse_Fail_NAK;

			// send back sector
			memset(buf, 0, sizeof buf);

			if (sector == 1) {
				static_assert(sizeof(kBootSector) <= sizeof(buf));
				memcpy(buf, kBootSector, sizeof kBootSector);
			}

			mpSIOMgr->BeginCommand();
			mpSIOMgr->SendACK();
			mpSIOMgr->SendComplete();
			mpSIOMgr->SendData(buf, 128, true);
			mpSIOMgr->EndCommand();

			return kCmdResponse_Start;
		} else if (cmd.mCommand == 0x53) {	// status
			buf[0] = 0x00;		// drive status
			buf[1] = 0xFF;		// inverted FDC status
			buf[2] = 0xE0;		// format timeout
			buf[3] = 0;

			mpSIOMgr->BeginCommand();
			mpSIOMgr->SendACK();
			mpSIOMgr->SendComplete();
			mpSIOMgr->SendData(buf, 4, true);
			mpSIOMgr->EndCommand();

			return kCmdResponse_Start;
		}

		return kCmdResponse_Fail_NAK;
	}

	if (cmd.mDevice == 0x7D) {
		if (cmd.mCommand == 0x26) {
			// compute block number and check if it is valid
			const uint32 block = cmd.mAUX[0];

			if (block * 128 >= sizeof kHandlerData)
				return kCmdResponse_Fail_NAK;

			// return handler data
			memset(buf, 0, sizeof buf);

			const uint32 offset = block * 128;
			memcpy(buf, kHandlerData + offset, std::min<uint32>(128, sizeof(kHandlerData) - offset));

			mpSIOMgr->BeginCommand();
			mpSIOMgr->SendACK();
			mpSIOMgr->SendComplete();
			mpSIOMgr->SendData(buf, 128, true);
			mpSIOMgr->EndCommand();

			return kCmdResponse_Start;
		}

		// eh, we don't know this command
		return kCmdResponse_Fail_NAK;
	}

	return kCmdResponse_NotHandled;
}

uint8 ATHLEProgramLoader::OnDSKINV(uint16) {
	return StartLoad();
}

uint8 ATHLEProgramLoader::OnDeferredLaunch(uint16) {
	return StartLoad();
}

uint8 ATHLEProgramLoader::StartLoad() {
	ATKernelDatabase kdb(mpCPU->GetMemory());

	mbLaunchPending = false;

	if (mbRandomizeMemoryOnLoad) {
		const uint16 memlo = kdb.MEMLO;
		const uint16 memtop = kdb.MEMTOP;

		uint32 seed = mpSim->RandomizeRawMemory(0x80, 0x80, 0x73b1b086);
		if (memlo < memtop)
			mpSim->RandomizeRawMemory(memlo, (uint32)(memtop - memlo) + 1, seed);
	}

	// reset run/init addresses to hook location near top of stack
	kdb.INITAD = 0x1FE;
	kdb.RUNAD = 0x1FE;

	// push DSKINV address on stack as return address
	mpCPU->PushWord(ATKernelSymbols::DSKINV - 1);

	// turn on load continuation hook
	mpCPUHookMgr->SetHookMethod(mpLoadContinueHook, kATCPUHookMode_Always, 0x01FE, 0, this, &ATHLEProgramLoader::OnLoadContinue);

	// reset DSKINV stub
	mpCPUHookMgr->UnsetHook(mpLaunchHook);

	// detach serial device
	if (mpSIOMgr) {
		mpSIOMgr->RemoveDevice(this);
	}

	// set COLDST so kernel doesn't think we were in the middle of init (required for
	// some versions of Alley Cat)
	kdb.COLDST = 0;

	// Set diskette boot flag in BOOT? so Alex's DOSINI handler runs.
	// Note that we need valid code pointed to by the DOSINI vector, since Stealth
	// crashes otherwise.
	kdb.DOSINI = 0xE4C0;		// KnownRTS

	kdb.BOOT_ |= 0x01;

	mpSimEventMgr->NotifyEvent(kATSimEvent_EXELoad);

	// load next segment
	return OnLoadContinue(0);
}

uint8 ATHLEProgramLoader::OnLoadContinue(uint16 pc) {
	ATCPUEmulatorMemory *mem = mpCPU->GetMemory();
	ATKernelDatabase kdb(mem);

	// check for bad init segments
	bool kernelEnabled = mpSim->IsKernelROMLocation(0xFFFF);

	if (pc) {
		if (mpCPU->GetP() & AT6502::kFlagI)
			ATConsoleWrite("EXE: Warning: Init segment returned with I flag set (DOS compatibility hazard).\n");

		if (!kernelEnabled && mbLastKernelEnabledState)
			ATConsoleWrite("EXE: Warning: Kernel ROM disabled by init segment.\n");
	}

	mbLastKernelEnabledState = kernelEnabled;

	// set INITAD to known RTS
	kdb.INITAD = 0xE4C0;

	// resume loading segments
	const uint8 *src0 = (const uint8 *)mpImage->GetBuffer();
	const uint8 *src = src0 + mProgramLoadIndex;
	const uint8 *srcEnd = src0 + mpImage->GetSize();
	for(;;) {
		// check if we're done
		if (srcEnd - src < 4) {
launch:
			// Looks like we're done. Push DSKINV onto the stack and execute the run routine.

			// remove hooks
			mpCPUHookMgr->UnsetHook(mpLoadContinueHook);
 
			// Last of the Dragons requires X to be set to a particular value to avoid stomping CRITIC.
			// This is what DOS launches with, since it calls SIO right before doing the run.
			mpCPU->SetX(0x20);
			kdb.STATUS = 0x01;

			// retrieve run address
			uint16 runAddr = kdb.RUNAD;

			mpCPU->Jump(runAddr);
			ATConsolePrintf("EXE: Launching at %04X\n", runAddr);

			mpSimEventMgr->NotifyEvent(kATSimEvent_EXERunSegment);

			vdsaferelease <<= mpImage;
			return 0x4C;
		}

		// read start/end addresses for this segment
		uint16 start = VDReadUnalignedLEU16(src+0);
		if (start == 0xFFFF) {
			src += 2;
			continue;
		}

		uint16 end = VDReadUnalignedLEU16(src+2);
		src += 4;

		uint32 len = (uint32)(end - start) + 1;
		if (end < start || (uint32)(srcEnd - src) < len) {
			if (end >= start) {
				len = (uint32)(srcEnd - src);
				ATConsoleWrite("WARNING: Invalid Atari executable: bad start/end range.\n");
			} else {
				ATConsoleWrite("ERROR: Invalid Atari executable: bad start/end range.\n");
				goto launch;
			}
		}

		// if this is the first segment, set RUNAD to this segment as a default
		if (kdb.RUNAD == 0x01FE)
			kdb.RUNAD = start;

		// load segment data into memory
		ATConsolePrintf("EXE: Loading program %04X-%04X to %04X-%04X\n", src-src0, (src-src0)+len-1, start, end);

		for(uint32 i=0; i<len; ++i)
			mem->WriteByte(start + i, *src++);

		if (auto *hm = mpSim->GetHeatMap())
			hm->PresetMemoryRange(start, len);

		// fake SIO read
		kdb.PBCTL = 0x3C;
		kdb.PBCTL = 0x34;
		kdb.PBCTL = 0x3C;

		ATClearPokeyTimersOnDiskIo(kdb);

		// check if INITAD has changed
		uint16 initAddr = kdb.INITAD;
		if (initAddr != 0xE4C0) {
			ATConsolePrintf("EXE: Jumping to %04X\n", initAddr);
			break;
		}
	}

	mProgramLoadIndex = src - src0;

	// push virtual load hook ($01FE-1) onto stack and jsr through (INITAD)
	mpCPU->PushWord(0x01FD);
	mpCPU->PushWord(kdb.INITAD - 1);

	mpSimEventMgr->NotifyEvent(kATSimEvent_EXEInitSegment);
	return 0x60;
}

void ATHLEProgramLoader::UnloadProgramSymbols() {
	IATDebugger *d = ATGetDebugger();

	if (!d)
		return;

	for(uint32& moduleId : mProgramModuleIds) {
		if (!moduleId)
			continue;

		d->UnloadSymbols(moduleId);	
		moduleId = 0;
	}
}
