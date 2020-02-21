//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2014 Avery Lee
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

#ifndef f_AT_VIA_H
#define f_AT_VIA_H

#include <vd2/system/function.h>
#include <at/atcore/scheduler.h>

typedef void (*ATVIA6522OutputFn)(void *data, uint32 outputState);

enum ATVIAOutputBit {
	kATVIAOutputBit_CA2 = 0x10000,
	kATVIAOutputBit_CB2 = 0x20000
};

class ATVIA6522Emulator : public IATSchedulerCallback {
public:
	ATVIA6522Emulator();
	~ATVIA6522Emulator();

	void Init(ATScheduler *sch);
	void Shutdown();

	void SetPortAInput(uint8 val);
	void SetPortBInput(uint8 val);
	void SetCA1Input(bool state);
	void SetCA2Input(bool state);
	void SetCB1Input(bool state);
	void SetCB2Input(bool state);

	void SetPortOutputFn(ATVIA6522OutputFn fn, void *data) {
		mpOutputFn = fn;
		mpOutputFnData = data;
	}

	void SetInterruptFn(const vdfunction<void(bool)>& fn);

	void Reset();

	uint8 DebugReadByte(uint8 address) const;
	uint8 ReadByte(uint8 address);
	void WriteByte(uint8 address, uint8 value);

public:
	virtual void OnScheduledEvent(uint32 id);

protected:
	enum {
		kIF_CA2	= 0x01,
		kIF_CA1	= 0x02,
		kIF_SR	= 0x04,
		kIF_CB2	= 0x08,
		kIF_CB1	= 0x10,
		kIF_T2	= 0x20,
		kIF_T1	= 0x40
	};

	enum {
		kEventId_CA2Assert = 1,
		kEventId_CA2Deassert,
		kEventId_CB2Assert,
		kEventId_CB2Deassert
	};

	void SetIF(uint8 mask);
	void ClearIF(uint8 mask);
	void UpdateOutput();

	uint8	mIRB;
	uint8	mIRA;
	uint8	mORB;
	uint8	mORA;
	uint8	mDDRB;
	uint8	mDDRA;
	uint16	mT1C;
	uint16	mT1L;
	uint16	mT2C;
	uint8	mT2L;
	uint8	mSR;
	uint8	mACR;
	uint8	mPCR;
	uint8	mIFR;
	uint8	mIER;
	bool	mCA1Input;
	bool	mCA2Input;
	bool	mCB1Input;
	bool	mCB2Input;
	bool	mCA2;
	bool	mCB2;
	bool	mbIrqState;

	ATScheduler *mpScheduler;
	ATEvent *mpEventCA2Update;
	ATEvent *mpEventCB2Update;

	ATVIA6522OutputFn mpOutputFn;
	void *mpOutputFnData;

	vdfunction<void(bool)> mInterruptFn;
};

#endif
