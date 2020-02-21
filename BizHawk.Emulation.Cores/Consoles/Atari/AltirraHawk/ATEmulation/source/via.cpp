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

#include <stdafx.h>
#include <at/atcore/scheduler.h>
#include <at/atemulation/via.h>

ATVIA6522Emulator::ATVIA6522Emulator()
	: mIRB(0)
	, mIRA(0)
	, mORB(0)
	, mORA(0)
	, mDDRB(0)
	, mDDRA(0)
	, mT1C(0)
	, mT1L(0)
	, mT2C(0)
	, mT2L(0)
	, mSR(0)
	, mACR(0)
	, mPCR(0)
	, mIFR(0)
	, mIER(0)
	, mCA1Input(true)
	, mCA2Input(true)
	, mCB1Input(true)
	, mCB2Input(true)
	, mCA2(true)
	, mCB2(true)
	, mbIrqState(false)
	, mpScheduler(nullptr)
	, mpEventCA2Update(nullptr)
	, mpEventCB2Update(nullptr)
{
	mpOutputFn = nullptr;
}

ATVIA6522Emulator::~ATVIA6522Emulator() {
	Shutdown();
}

void ATVIA6522Emulator::Init(ATScheduler *sch) {
	mpScheduler = sch;
}

void ATVIA6522Emulator::Shutdown() {
	if (mpScheduler) {
		mpScheduler->UnsetEvent(mpEventCA2Update);
		mpScheduler->UnsetEvent(mpEventCB2Update);
	}
}

void ATVIA6522Emulator::SetPortAInput(uint8 val) {
	mIRA = val;
}

void ATVIA6522Emulator::SetPortBInput(uint8 val) {
	mIRB = val;
}

void ATVIA6522Emulator::SetCA1Input(bool state) {
	if (mCA1Input == state)
		return;

	mCA1Input = state;

	// check if we got the required transition to assert IRQ
	if ((mPCR & 0x01) == (state ? 1 : 0))
		SetIF(kIF_CA1);

	// check for handshake mode on CA2 -- we need to deassert CA2 on CA1
	// transition
	if ((mPCR & 0x0E) == 0x08)
		mpScheduler->SetEvent(1, this, kEventId_CA2Deassert, mpEventCA2Update);
}

void ATVIA6522Emulator::SetCA2Input(bool state) {
	if (mCA2Input != state) {
		mCA2Input = state;

		//	000 = Input, IRQ on negative edge, clear IRQ on read/write
		//	001 = Input, IRQ on negative edge
		//	010 = Input, IRQ on positive edge, clear IRQ on read/write
		//	011 = Input, IRQ on positive edge
		//	100 = Output, set low on read/write, reset on CA1 edge
		//	101 = Output, set low for one cycle on read/write
		//	110 = Output, low
		//	111 = Output, high
		switch(mPCR & 0x0E) {
			case 0x00:
			case 0x02:
			case 0x04:
			case 0x06:
				break;
			case 0x08:

				break;
			case 0x0A:
			case 0x0C:
			case 0x0E:
				break;
			default:
				VDNEVERHERE;
		}
	}
}

void ATVIA6522Emulator::SetCB1Input(bool state) {
	if (mCB1Input == state)
		return;

	mCB1Input = state;

	// check if we got the required transition to assert IRQ
	if ((mPCR & 0x01) == (state ? 1 : 0))
		SetIF(kIF_CB1);

	// check for handshake mode on CB2 -- we need to deassert CB2 on CB1
	// transition
	if ((mPCR & 0xE0) == 0x80)
		mpScheduler->SetEvent(1, this, kEventId_CB2Deassert, mpEventCB2Update);
}

void ATVIA6522Emulator::SetCB2Input(bool state) {
	if (mCB2Input != state) {
		mCB2Input = state;

		//	000 = Input, IRQ on negative edge, clear IRQ on read/write
		//	001 = Input, IRQ on negative edge
		//	010 = Input, IRQ on positive edge, clear IRQ on read/write
		//	011 = Input, IRQ on positive edge
		//	100 = Output, set low on read/write, reset on CA1 edge
		//	101 = Output, set low for one cycle on read/write
		//	110 = Output, low
		//	111 = Output, high
		switch(mPCR & 0xE0) {
			case 0x00:
			case 0x20:
			case 0x40:
			case 0x60:
				break;
			case 0x80:

				break;
			case 0xA0:
			case 0xC0:
			case 0xE0:
				break;
			default:
				VDNEVERHERE;
		}
	}
}

void ATVIA6522Emulator::SetInterruptFn(const vdfunction<void(bool)>& fn) {
	mInterruptFn = fn;
}

void ATVIA6522Emulator::Reset() {
	mORB = 0;
	mORA = 0;
	mDDRB = 0;
	mDDRA = 0;
	mT1C = 0;
	mT1L = 0;
	mT2C = 0;
	mT2L = 0;
	mSR = 0;
	mACR = 0;
	mPCR = 0;
	mIFR = 0;
	mIER = 0;
	mCA2 = true;
	mCB2 = true;
	mbIrqState = false;

	if (mInterruptFn)
		mInterruptFn(false);
	
	mpScheduler->UnsetEvent(mpEventCA2Update);
	mpScheduler->UnsetEvent(mpEventCB2Update);

	UpdateOutput();
}

uint8 ATVIA6522Emulator::DebugReadByte(uint8 address) const {
	switch(address & 15) {
		case 0:
			return (mIRB & ~mDDRB) + (mORB & mDDRB);

		case 1:
			return mIRA;

		case 2:
			return mDDRB;

		case 3:
			return mDDRA;

		case 4:
			return (uint8)mT1C;

		case 5:
			return (uint8)(mT1C >> 8);

		case 6:
			return (uint8)mT1L;

		case 7:
			return (uint8)(mT1L >> 8);

		case 8:
			return (uint8)mT2L;

		case 9:
			return (uint8)(mT2C >> 8);

		case 10:
			return mSR;

		case 11:
			return mACR;

		case 12:
			return mPCR;

		case 13:
			{
				uint8 value = mIER & mIFR;

				if (value)
					value |= 0x80;

				return value;
			}
			break;

		case 14:
			return mIER;

		case 15:
			return mIRA;

		default:
			VDNEVERHERE;
	}
}

uint8 ATVIA6522Emulator::ReadByte(uint8 address) {
	switch(address & 15) {
		case 0:
			// check for read-sensitive modes on CB2
			switch(mPCR & 0xE0) {
				case 0x00:	// input mode, negative transition - clear IFR0
				case 0x40:	// input mode, positive transition - clear IFR0
					ClearIF(kIF_CA2);
					break;

				case 0x80:	// handshake mode - assert CA2
					mpScheduler->SetEvent(1, this, kEventId_CA2Assert, mpEventCA2Update);
					break;
			}

			// clear CB1/CB2 interrupts
			ClearIF(kIF_CB1 | kIF_CB2);

			return (mIRB & ~mDDRB) + (mORB & mDDRB);

		case 1:
			// check for read-sensitive modes on CA2
			switch(mPCR & 0x0E) {
				case 0x00:	// input mode, negative transition - clear IFR0
				case 0x04:	// input mode, positive transition - clear IFR0
					ClearIF(kIF_CA2);
					break;

				case 0x08:	// handshake mode - assert CA2
					mpScheduler->SetEvent(1, this, kEventId_CA2Assert, mpEventCA2Update);
					break;
			}

			// clear CA1/CA2 interrupts
			ClearIF(kIF_CA1 | kIF_CA2);
			return mIRA;

		case 2:
			return mDDRB;

		case 3:
			return mDDRA;

		case 4:
			ClearIF(kIF_T1);
			return (uint8)mT1C;

		case 5:
			return (uint8)(mT1C >> 8);

		case 6:
			return (uint8)mT1L;

		case 7:
			return (uint8)(mT1L >> 8);

		case 8:
			ClearIF(kIF_T2);
			return (uint8)mT2L;

		case 9:
			return (uint8)(mT2C >> 8);

		case 10:
			return mSR;

		case 11:
			return mACR;

		case 12:
			return mPCR;

		case 13:
			return mIFR + ((mIER & mIFR) ? 0x80 : 0x00);

		case 14:
			return mIER;

		case 15:
			return mIRA;

		default:
			VDNEVERHERE;
	}
}

void ATVIA6522Emulator::WriteByte(uint8 address, uint8 value) {
	switch(address & 15) {
		case 0:
			if (mORB != value) {
				uint8 delta = (mORB ^ value) & mDDRB;

				mORB = value;

				if (delta)
					UpdateOutput();
			}

			// check for write-sensitive modes on CB2
			switch(mPCR & 0xE0) {
				case 0x00:	// input mode, negative transition
				case 0x40:	// input mode, positive transition
					ClearIF(kIF_CB2);
					break;

				case 0x80:	// handshake mode
					mpScheduler->SetEvent(1, this, kEventId_CB2Assert, mpEventCB2Update);
					break;
			}

			// clear CB1/CB2 interrupts
			ClearIF(kIF_CB1 | kIF_CB2);
			break;

		case 1:
			if (mORA != value) {
				uint8 delta = (mORA ^ value) & mDDRA;

				mORA = value;

				if (delta)
					UpdateOutput();
			}

			// check for write-sensitive modes on CA2
			switch(mPCR & 0x0E) {
				case 0x00:	// input mode, negative transition
				case 0x04:	// input mode, positive transition
					ClearIF(kIF_CA2);
					break;

				case 0x08:	// handshake mode
					mpScheduler->SetEvent(1, this, kEventId_CA2Assert, mpEventCA2Update);
					break;
			}

			// clear CA1/CA2 interrupts
			ClearIF(kIF_CA1 | kIF_CA2);
			break;

		case 2:
			if (mDDRB != value) {
				uint8 delta = ~mORB & (mDDRB ^ value);

				mDDRB = value;

				if (delta)
					UpdateOutput();
			}
			break;

		case 3:
			if (mDDRA != value) {
				uint8 delta = ~mORA & (mDDRA ^ value);

				mDDRA = value;

				if (delta)
					UpdateOutput();
			}
			break;

		case 4:
		case 6:
			mT1L = (uint16)((mT1L & 0xff00) + value);
			break;

		case 5:
			mT1L = (uint16)((mT1L & 0x00ff) + ((uint32)value << 8));
			mT1C = mT1L;
			ClearIF(kIF_T1);
			break;

		case 7:
			mT1L = (uint16)((mT1L & 0x00ff) + ((uint32)value << 8));
			ClearIF(kIF_T1);
			break;

		case 8:
			mT2L = (uint16)((mT2L & 0xff00) + value);
			break;

		case 9:
			mT2C = (uint16)(mT2L + ((uint32)value << 8));
			ClearIF(kIF_T2);
			break;

		case 10:
			mSR = value;
			break;

		case 11:
			mACR = value;
			break;

		case 12:
			// |  7  |  6  |  5  |  4  |  3  |  2  |  1  |  0  |
			// |       CB2       | CB1 |       CA2       | CA1 |
			//
			// CA2/CB2:
			//	000 = Input, IRQ on negative edge, clear IRQ on read/write
			//	001 = Input, IRQ on negative edge
			//	010 = Input, IRQ on positive edge, clear IRQ on read/write
			//	011 = Input, IRQ on positive edge
			//	100 = Output, set low on read/write, reset on CA1 edge
			//	101 = Output, set low for one cycle on read/write
			//	110 = Output, low
			//	111 = Output, high
			//
			if (uint8 delta = mPCR ^ value) {
				mPCR = value;

				// CB2
				if (delta & 0xE0) {
					switch(value >> 5) {
						case 0:
						case 1:
						case 2:
						case 3:
						case 7:
							mpScheduler->UnsetEvent(mpEventCB2Update);
							mCB2 = true;
							break;

						case 6:
							mpScheduler->UnsetEvent(mpEventCA2Update);
							mCB2 = false;
							break;

						case 4:
						case 5:
						default:
							break;
					}
				}

				// CA2
				if (delta & 0x0E) {
					switch((value >> 1) & 7) {
						case 0:
						case 1:
						case 2:
						case 3:
						case 7:
							mpScheduler->UnsetEvent(mpEventCA2Update);
							mCA2 = true;
							break;

						case 6:
							mpScheduler->UnsetEvent(mpEventCA2Update);
							mCA2 = false;
							break;

						case 4:
						case 5:
						default:
							break;
					}
				}

				UpdateOutput();
			}
			break;

		case 13:
			ClearIF(value);
			break;

		case 14:
			{
				const uint8 mask = (value & 0x7f);

				if (value & 0x80) {
					if (~mIER & mask) {
						mIER |= mask;

						if (!mbIrqState && (mIER & mIFR)) {
							mbIrqState = true;

							if (mInterruptFn)
								mInterruptFn(true);
						}
					}
				} else {
					if (mIER & mask) {
						mIER &= ~mask;

						if (mbIrqState && !(mIER & mIFR)) {
							mbIrqState = false;

							if (mInterruptFn)
								mInterruptFn(false);
						}
					}
				}
			}
			break;

		case 15:
			break;
	}
}

void ATVIA6522Emulator::OnScheduledEvent(uint32 id) {
	switch(id) {
		case kEventId_CA2Assert:
			mpEventCA2Update = nullptr;
			if (mCA2) {
				mCA2 = false;
				UpdateOutput();
			}
			break;

		case kEventId_CA2Deassert:
			mpEventCA2Update = nullptr;
			if (!mCA2) {
				mCA2 = true;
				UpdateOutput();
			}
			break;

		case kEventId_CB2Assert:
			mpEventCB2Update = nullptr;
			if (mCB2) {
				mCB2 = false;
				UpdateOutput();
			}
			break;

		case kEventId_CB2Deassert:
			mpEventCB2Update = nullptr;
			if (!mCB2) {
				mCB2 = true;
				UpdateOutput();
			}
			break;
	}
}

void ATVIA6522Emulator::SetIF(uint8 mask) {
	if (~mIFR & mask) {
		mIFR |= mask;

		if (!mbIrqState && (mIFR & mIER)) {
			mbIrqState = true;

			if (mInterruptFn)
				mInterruptFn(true);
		}
	}
}

void ATVIA6522Emulator::ClearIF(uint8 mask) {
	if (mIFR & mask) {
		mIFR &= ~mask;

		if (mbIrqState && !(mIFR & mIER)) {
			mbIrqState = false;

			if (mInterruptFn)
				mInterruptFn(false);
		}
	}
}

void ATVIA6522Emulator::UpdateOutput() {
	uint32 val = (((mORB | ~mDDRB) & 0xff) << 8) + ((mORA | ~mDDRA) & 0xff);

	if (mCA2)
		val |= kATVIAOutputBit_CA2;

	if (mCB2)
		val |= kATVIAOutputBit_CB2;

	if (mpOutputFn)
		mpOutputFn(mpOutputFnData, val);
}
