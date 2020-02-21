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
#include <vd2/system/vdstl_vectorview.h>
#include <at/atio/blobimage.h>
#include "hlebasicloader.h"
#include "hleutils.h"
#include "kerneldb.h"
#include "console.h"
#include "cpu.h"
#include "cpuhookmanager.h"
#include "debugger.h"
#include "simeventmanager.h"
#include "simulator.h"
#include "cio.h"

ATHLEBasicLoader::ATHLEBasicLoader() {
}

ATHLEBasicLoader::~ATHLEBasicLoader() {
	Shutdown();
}

void ATHLEBasicLoader::Init(ATCPUEmulator *cpu, ATSimulatorEventManager *simEventMgr, ATSimulator *sim) {
	mpCPU = cpu;
	mpCPUHookMgr = cpu->GetHookManager();
	mpSimEventMgr = simEventMgr;
	mpSim = sim;
	mProgramIOCB = 0;
}

void ATHLEBasicLoader::Shutdown() {
	vdsaferelease <<= mpImage;

	if (mpCPUHookMgr) {
		mpCPUHookMgr->UnsetHook(mpLaunchHook);
		mpCPUHookMgr = NULL;
	}

	mpSim = NULL;
	mpSimEventMgr = NULL;
	mpCPU = NULL;

	mbLaunchPending = false;
}

void ATHLEBasicLoader::LoadProgram(IATBlobImage *image) {
	vdsaferelease <<= mpImage;

	mpImage = image;
	image->AddRef();

	uint32 len = image->GetSize();

	if (len < 14)
		throw MyError("Invalid BASIC program: must be at least 12 bytes.");

	mpCPUHookMgr->SetHookMethod(mpLaunchHook, kATCPUHookMode_KernelROMOnly, ATKernelSymbols::CIOV, 10, this, &ATHLEBasicLoader::OnCIOV);

	mbLaunchPending = true;
	mState = State::RunProgram;
}

void ATHLEBasicLoader::LoadTape(bool pushKeyToLoadTape) {
	vdsaferelease <<= mpImage;

	mpCPUHookMgr->SetHookMethod(mpLaunchHook, kATCPUHookMode_KernelROMOnly, ATKernelSymbols::CIOV, 10, this, &ATHLEBasicLoader::OnCIOV);

	// We don't need to keep the BASIC loader across a cold reset for tape loads since
	// the simulator will re-create it.
	mbLaunchPending = false;

	mbPushKeyToLoadTape = pushKeyToLoadTape;

	mState = State::LoadTape;
}

uint8 ATHLEBasicLoader::OnCIOV(uint16) {
	ATCPUEmulatorMemory *mem = mpCPU->GetMemory();
	ATKernelDatabase kdb(mem);

	mbLaunchPending = false;

	// check for invalid IOCB
	const uint8 iocb = mpCPU->GetX();
	if (iocb & 0x8f)
		return 0;

	const uint8 cmd = mem->ReadByte(ATKernelSymbols::ICCMD + iocb);
	if (mProgramIOCB && iocb == mProgramIOCB) {
		switch(cmd) {
			case ATCIOSymbols::CIOCmdOpen:
				mpCPU->Ldy(ATCIOSymbols::CIOStatIOCBInUse);
				return 0x60;

			case ATCIOSymbols::CIOCmdClose:
				mProgramIOCB = 0;
				mem->WriteByte(ATKernelSymbols::ICHID + iocb, 0xFF);
				mpCPU->Ldy(ATCIOSymbols::CIOStatSuccess);
				mpCPUHookMgr->UnsetHook(mpLaunchHook);
				return 0x60;

			case ATCIOSymbols::CIOCmdGetChars:
				if (mProgramIndex >= mpImage->GetSize()) {
					mpCPU->Ldy(ATCIOSymbols::CIOStatEndOfFile);
				} else {
					uint32 maxRead = (uint32)(mpImage->GetSize() - mProgramIndex);
					uint32 addr = mem->ReadByte(ATKernelSymbols::ICBAL + iocb) + 256*mem->ReadByte(ATKernelSymbols::ICBAH + iocb);
					uint32 len = mem->ReadByte(ATKernelSymbols::ICBLL + iocb) + 256*mem->ReadByte(ATKernelSymbols::ICBLH + iocb);
					bool trunc = false;

					if (len > maxRead) {
						trunc = true;
						len = maxRead;
					}

					const uint8 *src = (const uint8 *)mpImage->GetBuffer();
					for(uint32 i=0; i<len; ++i)
						mem->WriteByte((uint16)(addr++), src[mProgramIndex++]);

					mem->WriteByte(ATKernelSymbols::ICBLL + iocb, (uint8)len);
					mem->WriteByte(ATKernelSymbols::ICBLH + iocb, (uint8)(len >> 8));

					mpCPU->Ldy(trunc ? ATCIOSymbols::CIOStatTruncRecord : ATCIOSymbols::CIOStatSuccess);
				}
				return 0x60;
		
			case ATCIOSymbols::CIOCmdPutChars:
				mpCPU->Ldy(ATCIOSymbols::CIOStatReadOnly);
				return 0x60;

			default:
				mpCPU->Ldy(ATCIOSymbols::CIOStatNotSupported);
				return 0x60;
		}
	} else if (mpImage && cmd == ATCIOSymbols::CIOCmdOpen) {
		uint16 lineAddr = mem->ReadByte(ATKernelSymbols::ICBAL + iocb) + 256*mem->ReadByte(ATKernelSymbols::ICBAH + iocb);

		if (mem->ReadByte(lineAddr) != '*')
			return 0;

		if ((mem->ReadByte(ATKernelSymbols::ICAX1 + iocb) & 0x0c) != 0x04) {
			mpCPU->Ldy(ATCIOSymbols::CIOStatReadOnly);
			return 0x60;
		}

		mProgramIOCB = iocb;
		mProgramIndex = 0;
		mpCPU->Ldy(ATCIOSymbols::CIOStatSuccess);
		return 0x60;
	}

	// check that this is a get chars request on IOCB 0
	if (iocb != 0)
		return 0;

	if (cmd != ATCIOSymbols::CIOCmdGetRecord)
		return 0;

	// write command line
	static const uint8 kRunProgramCmdLine[]={ 'R', 'U', 'N', ' ', '"', '*', '"', 0x9B };
	static const uint8 kLoadTapeCmdLine[]={ 'C', 'L', 'O', 'A', 'D', 0x9B };
	static const uint8 kRunTapeCmdLine[]={ 'R', 'U', 'N', 0x9B };

	uint16 lineAddr = kdb.ICBAL_ICBAH;
	uint32 lineLen = kdb.ICBLL_ICBLH;

	vdvector_view<const uint8> cmdline {};

	switch(mState) {
		default:
			return 0;

		case State::RunProgram:
			cmdline = kRunProgramCmdLine;
			mState = State::None;
			// leave hook attached for program load
			break;

		case State::LoadTape:
			cmdline = kLoadTapeCmdLine;
			mState = State::RunTape;

			if (mbPushKeyToLoadTape)
				kdb.CH = 0x21;
			break;

		case State::RunTape:
			mState = State::None;
			cmdline = kRunTapeCmdLine;
			mpCPUHookMgr->UnsetHook(mpLaunchHook);
			break;
	}

	uint32 cmdlineLen = cmdline.size();
	if (lineLen > cmdlineLen)
		lineLen = cmdlineLen;

	for(uint32 i=0; i<lineLen; ++i)
		mem->WriteByte(lineAddr++, cmdline[i]);

	kdb.ICBLL_ICBLH = lineLen;

	uint8 status = lineLen < cmdlineLen ? ATCIOSymbols::CIOStatTruncRecord : ATCIOSymbols::CIOStatSuccess;
	kdb.STATUS = status;

	mpCPU->Ldy(status);
	return 0x60;
}
