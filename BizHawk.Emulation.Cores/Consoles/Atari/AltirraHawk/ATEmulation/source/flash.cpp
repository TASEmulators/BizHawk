//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2011 Avery Lee
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
#include <at/atcore/logging.h>
#include <at/atemulation/flash.h>

ATLogChannel g_ATLCFlash(false, false, "FLASH", "Flash memory erase operations");
ATLogChannel g_ATLCFlashWrite(false, false, "FLASHWR", "Flash memory write operations");

namespace {
	// 150us timeout for writes to Atmel devices
	const uint32 kAtmelWriteTimeoutCycles = 268;

	// 80us timeout for multiple sector erase (AMD)
	const uint32 kAMDSectorEraseTimeoutCycles = 143;

	// 80us timeout for multiple sector erase (BMI)
	const uint32 kBRIGHTSectorEraseTimeoutCycles = 143;

	// 50us timeout for multiple sector erase (Amic)
	const uint32 kAmicSectorEraseTimeoutCycles = 89;

	const uint32 kSectorEraseTimeoutCycles50us = 89;
}

ATFlashEmulator::ATFlashEmulator()
	: mpMemory(NULL)
	, mpScheduler(NULL)
	, mpWriteEvent(NULL)
{
}

ATFlashEmulator::~ATFlashEmulator() {
	Shutdown();
}

void ATFlashEmulator::Init(void *mem, ATFlashType type, ATScheduler *sch) {
	mpMemory = (uint8 *)mem;
	mpScheduler = sch;
	mFlashType = type;
	mpWriteEvent = NULL;

	mbDirty = false;
	mbWriteActivity = false;
	mbAtmelSDP = (type == kATFlashType_AT29C010A || type == kATFlashType_AT29C040);

	// check if this flash chip checks 11 or 15 address bits during the unlock sequence
	mbA11Unlock = false;
	mbA12iUnlock = false;

	switch(mFlashType) {
		case kATFlashType_A29040:
		case kATFlashType_Am29F010B:
		case kATFlashType_Am29F040B:
		case kATFlashType_Am29F016D:
		case kATFlashType_Am29F032B:
		case kATFlashType_M29F010B:
		case kATFlashType_HY29F040A:
			mbA11Unlock = true;
			break;

		case kATFlashType_S29GL01P:
		case kATFlashType_S29GL512P:
		case kATFlashType_S29GL256P:
			mbA12iUnlock = true;
			break;
	}

	switch(mFlashType) {
		case kATFlashType_A29040:
			mSectorEraseTimeoutCycles = kAmicSectorEraseTimeoutCycles;
			break;

		case kATFlashType_Am29F010:
		case kATFlashType_Am29F010B:
		case kATFlashType_M29F010B:
		case kATFlashType_HY29F040A:
			mSectorEraseTimeoutCycles = kSectorEraseTimeoutCycles50us;
			break;

		case kATFlashType_Am29F040:
		case kATFlashType_Am29F040B:
			mSectorEraseTimeoutCycles = kAMDSectorEraseTimeoutCycles;
			break;

		case kATFlashType_BM29F040:
			mSectorEraseTimeoutCycles = kBRIGHTSectorEraseTimeoutCycles;
			break;

		default:
			mSectorEraseTimeoutCycles = 0;
			break;
	}

	ColdReset();
}

void ATFlashEmulator::Shutdown() {
	if (mpWriteEvent)
		mpScheduler->UnsetEvent(mpWriteEvent);

	mpScheduler = NULL;
	mpMemory = NULL;
}

void ATFlashEmulator::ColdReset() {
	if (mpWriteEvent)
		mpScheduler->UnsetEvent(mpWriteEvent);

	mReadMode = kReadMode_Normal;
	mCommandPhase = 0;

	mToggleBits = 0;
}

bool ATFlashEmulator::ReadByte(uint32 address, uint8& data) {
	bool result = DebugReadByte(address, data);

	if (mReadMode == kReadMode_SectorEraseStatus) {
		// Toggle DQ2 and DQ6 after the read.
		mToggleBits ^= 0x44;
	}

	return result;
}

bool ATFlashEmulator::DebugReadByte(uint32 address, uint8& data) const {
	data = 0xFF;

	switch(mReadMode) {
		case kReadMode_Normal:
			data = mpMemory[address];
			return true;

		case kReadMode_Autoselect:
			if (mFlashType == kATFlashType_S29GL01P ||
				mFlashType == kATFlashType_S29GL512P ||
				mFlashType == kATFlashType_S29GL256P) {
				// The S29GL01 is a 16-bit device, so this is a bit more annoying:
				static const uint8 kIDData[]={
					0x01, 0x00,		// Manufacturer ID
					0x7E, 0x22,		// Device ID
					0x00, 0x00,		// Protection Verification (stubbed)
					0x3F, 0xFF,		// Indicator Bits
					0xFF, 0xFF,		// RFU / Reserved
					0xFF, 0xFF,		// RFU / Reserved
					0xFF, 0xFF,		// RFU / Reserved
					0xFF, 0xFF,		// RFU / Reserved
					0xFF, 0xFF,		// RFU / Reserved
					0xFF, 0xFF,		// RFU / Reserved
					0xFF, 0xFF,		// RFU / Reserved
					0xFF, 0xFF,		// RFU / Reserved
					0x04, 0x00,		// Lower Software Bits
					0xFF, 0xFF,		// Upper Software Bits
					0x28, 0x22,		// Device ID
					0x01, 0x22,		// Device ID

					0x51, 0x00,		// Q
					0x52, 0x00,		// R
					0x59, 0x00,		// Y
					0x02, 0x00,		// Primary OEM Command Set
					0x00, 0x00,		// (cont.)
					0x40, 0x00,		// Address for Primary Extended Table
					0x00, 0x00,		// (cont.)
					0x00, 0x00,		// Alternate OEM Command Set
					0x00, 0x00,		// (cont.)
					0x00, 0x00,		// Address for Alternate OEM Command Set
					0x00, 0x00,		// (cont.)
					0x27, 0x00,		// Vcc Min. (erase/program)
					0x36, 0x00,		// Vcc Max. (erase/program)
					0x00, 0x00,		// Vpp Min. voltage
					0x00, 0x00,		// Vpp Max. voltage
					0x06, 0x00,		// Typical timeout per single word

					0x06, 0x00,		// Typical timeout for max multi-byte program
					0x09, 0x00,		// Typical timeout per individual block erase
					0x13, 0x00,		// Typical timeout for full chip erase
					0x03, 0x00,		// Max. timeout for single word write
					0x05, 0x00,		// Max. timeout for buffer write
					0x03, 0x00,		// Max. timeout per individual block erase
					0x02, 0x00,		// Max. timeout for full chip erase
					0x1B, 0x00,		// Device size
					0x01, 0x00,		// Flash Device Interface Description (x16 only)
					0x00, 0x00,		// (cont.)
					0x09, 0x00,		// Max number of byte in multi-byte write
					0x00, 0x00,		// (cont.)
					0x01, 0x00,		// Number of Erase Block Regions
					0xFF, 0x00,		// Erase Block Region 1 Information
					0x03, 0x00,		// (cont.)
					0x00, 0x00,		// (cont.)

					0x02, 0x00,		// (cont.)
					0x00, 0x00,		// Erase Block Region 2 Information
					0x00, 0x00,		// (cont.)
					0x00, 0x00,		// (cont.)
					0x00, 0x00,		// (cont.)
					0x00, 0x00,		// Erase Block Region 3 Information
					0x00, 0x00,		// (cont.)
					0x00, 0x00,		// (cont.)
					0x00, 0x00,		// (cont.)
					0x00, 0x00,		// Erase Block Region 4 Information
					0x00, 0x00,		// (cont.)
					0x00, 0x00,		// (cont.)
					0x00, 0x00,		// (cont.)
					0xFF, 0xFF,		// Reserved
					0xFF, 0xFF,		// Reserved
					0xFF, 0xFF,		// Reserved

					0x50, 0x00,		// P
					0x52, 0x00,		// R
					0x49, 0x00,		// I
					0x31, 0x00,		// Major version number (1.3)
					0x33, 0x00,		// Minor version number
					0x14, 0x00,		// Address Sensitive Unlock, Process Technology (90nm MirrorBit)
					0x02, 0x00,		// Erase Suspend
					0x01, 0x00,		// Sector Protect
					0x00, 0x00,		// Temporary Sector Unprotect
					0x08, 0x00,		// Sector Protect/Unprotect Scheme
					0x00, 0x00,		// Simultaneous Operation
					0x00, 0x00,		// Burst Mode Type
					0x02, 0x00,		// Page Mode Type
					0xB5, 0x00,		// ACC Supply Minimum
					0xC5, 0x00,		// ACC Supply Maximum
					0x05, 0x00,		// WP# Protection (top)

					0x01, 0x00,		// Program Suspend
				};

				VDASSERTCT(sizeof(kIDData) == 0x51 * 2);

				size_t address8 = address & 0xff;
				if (address8 == 0x0E*2) {		// Device ID
					switch(mFlashType) {
						case kATFlashType_S29GL256P:
							data = 0x22;
							break;
						case kATFlashType_S29GL512P:
							data = 0x23;
							break;
						case kATFlashType_S29GL01P:
							data = 0x28;
							break;
					}
				} else if (address8 == 0x22*2) {		// Typical Timeout for Full Chip Erase
					switch(mFlashType) {
						case kATFlashType_S29GL256P:
							data = 0x13;
							break;
						case kATFlashType_S29GL512P:
							data = 0x12;
							break;
						case kATFlashType_S29GL01P:
							data = 0x11;
							break;
					}
				} else if (address8 == 0x27*2) {		// Device Size
					switch(mFlashType) {
						case kATFlashType_S29GL256P:
							data = 0x19;
							break;
						case kATFlashType_S29GL512P:
							data = 0x1A;
							break;
						case kATFlashType_S29GL01P:
							data = 0x1B;
							break;
					}
				} else if (address8 == 0x2E*2) {		// Erase Block Region 1
					switch(mFlashType) {
						case kATFlashType_S29GL256P:
							data = 0x00;
							break;
						case kATFlashType_S29GL512P:
							data = 0x01;
							break;
						case kATFlashType_S29GL01P:
							data = 0x03;
							break;
					}
				} else if (address8 < sizeof(kIDData))
					data = kIDData[address];
				else
					data = 0xFF;
			} else switch(address & 0xFF) {
				case 0x00:
					switch(mFlashType) {
						case kATFlashType_Am29F010:
						case kATFlashType_Am29F010B:
						case kATFlashType_Am29F040:
						case kATFlashType_Am29F040B:
						case kATFlashType_Am29F016D:
						case kATFlashType_Am29F032B:
							data = 0x01;	// XX00 Manufacturer ID: AMD/Spansion
							break;

						case kATFlashType_AT29C010A:
						case kATFlashType_AT29C040:
							data = 0x1F;	// XX00 Manufacturer ID: Atmel
							break;

						case kATFlashType_SST39SF040:
							data = 0xBF;	// XX00 Manufacturer ID: SST
							break;

						case kATFlashType_A29040:
							data = 0x37;	// XX00 Manufacturer ID: AMIC
							break;

						case kATFlashType_BM29F040:
							data = 0xAD;	// XX00 Manufacturer ID: Bright Microelectronics Inc. (same as Hyundai)
							break;

						case kATFlashType_M29F010B:
							data = 0x20;
							break;

						case kATFlashType_HY29F040A:
							data = 0xAD;	// XX00 Manufacturer ID: Hynix (Hyundai)
							break;
					}
					break;

				case 0x01:
					switch(mFlashType) {
						case kATFlashType_Am29F010:
						case kATFlashType_Am29F010B:
							data = 0x20;
							break;
					
						case kATFlashType_Am29F040:
						case kATFlashType_Am29F040B:
							// Yes, the 29F040 and 29F040B both have the same code even though
							// the 040 validates A0-A14 and the 040B only does A0-A10.
							data = 0xA4;
							break;

						case kATFlashType_Am29F016D:
							data = 0xAD;
							break;

						case kATFlashType_Am29F032B:
							data = 0x41;
							break;

						case kATFlashType_AT29C010A:
							data = 0xD5;
							break;

						case kATFlashType_AT29C040:
							data = 0x5B;
							break;

						case kATFlashType_SST39SF040:
							data = 0xB7;
							break;

						case kATFlashType_A29040:
							data = 0x86;
							break;

						case kATFlashType_BM29F040:
							data = 0x40;
							break;

						case kATFlashType_M29F010B:
							data = 0x20;
							break;

						case kATFlashType_HY29F040A:
							data = 0xA4;
							break;
					}
					break;

				default:
					data = 0x00;	// XX02 Sector Protect Verify: 00 not protected
					break;
			}
			break;

		case kReadMode_WriteStatusPending:
			data = ~mpMemory[address] & 0x80;
			break;

		case kReadMode_SectorEraseStatus:
			// During sector erase timeout:
			//	DQ7 = 0 (complement of erased data)
			//	DQ6 = toggle
			//	DQ5 = 0 (not exceeded timing limits)
			//	DQ3 = 0 (additional commands accepted)
			//	DQ2 = toggle
			data = mToggleBits;
			break;
	}

	return false;
}

bool ATFlashEmulator::WriteByte(uint32 address, uint8 value) {
	uint32 address15 = address & 0x7fff;
	uint32 address11 = address & 0x7ff;

	g_ATLCFlashWrite("Write[$%05X] = $%02X\n", address, value);

	switch(mCommandPhase) {
		case 0:
			// $F0 written at phase 0 deactivates autoselect mode
			if (value == 0xF0) {
				if (!mReadMode)
					break;

				g_ATLCFlash("Exiting autoselect mode.\n");
				mReadMode = kReadMode_Normal;

				mpScheduler->UnsetEvent(mpWriteEvent);
				return true;
			}

			switch(mFlashType) {
				case kATFlashType_Am29F010:
				case kATFlashType_Am29F040:
				case kATFlashType_SST39SF040:
				case kATFlashType_BM29F040:
					if (address15 == 0x5555 && value == 0xAA)
						mCommandPhase = 1;
					break;

				case kATFlashType_AT29C010A:
				case kATFlashType_AT29C040:
					if (address == 0x5555 && value == 0xAA)
						mCommandPhase = 7;
					break;

				case kATFlashType_A29040:
				case kATFlashType_Am29F010B:
				case kATFlashType_Am29F040B:
				case kATFlashType_Am29F016D:
				case kATFlashType_Am29F032B:
				case kATFlashType_M29F010B:
				case kATFlashType_HY29F040A:
					if ((address & 0x7FF) == 0x555 && value == 0xAA)
						mCommandPhase = 1;
					break;

				case kATFlashType_S29GL01P:
				case kATFlashType_S29GL512P:
				case kATFlashType_S29GL256P:
					if ((address & 0xFFF) == 0xAAA && value == 0xAA)
						mCommandPhase = 1;
					break;
			}

			if (mCommandPhase)
				g_ATLCFlash("Unlock: step 1 OK.\n");
			else
				g_ATLCFlash("Unlock: step 1 FAILED [($%05X) = $%02X].\n", address, value);
			break;

		case 1:
			mCommandPhase = 0;

			if (value == 0x55) {
				switch(mFlashType) {
					case kATFlashType_S29GL01P:
					case kATFlashType_S29GL512P:
					case kATFlashType_S29GL256P:
						if ((address & 0xFFF) == 0x555)
							mCommandPhase = 2;
						break;

					default:
						if (mbA11Unlock) {
							if ((address & 0x7FF) == 0x2AA)
								mCommandPhase = 2;
						} else {
							if (address15 == 0x2AAA)
								mCommandPhase = 2;
						}
						break;
				}
			}

			if (mCommandPhase)
				g_ATLCFlash("Unlock: step 2 OK.\n");
			else
				g_ATLCFlash("Unlock: step 2 FAILED [($%05X) = $%02X].\n", address, value);
			break;

		case 2:
			switch(mFlashType) {
				case kATFlashType_S29GL256P:
				case kATFlashType_S29GL512P:
				case kATFlashType_S29GL01P:
					if (value == 0x25) {
						g_ATLCFlash("Entering write buffer load mode.\n");
						mPendingWriteAddress = address;
						mCommandPhase = 15;
						return false;
					}
					break;
			}

			if (mbA12iUnlock ? (address & 0xFFF) != 0xAAA :
				mbA11Unlock ? (address & 0x7FF) != 0x555
							: address15 != 0x5555) {
				g_ATLCFlash("Unlock: step 3 FAILED [($%05X) = $%02X].\n", address, value);
				mCommandPhase = 0;
				break;	
			}

			// A non-erase command aborts a multiple sector erase in timeout phase.
			if (value != 0x80 && mReadMode == kReadMode_SectorEraseStatus) {
				mpScheduler->UnsetEvent(mpWriteEvent);
				mReadMode = kReadMode_Autoselect;
			}

			switch(value) {
				case 0x80:	// $80: sector or chip erase
					g_ATLCFlash("Entering sector erase mode.\n");
					mCommandPhase = 3;
					break;

				case 0x90:	// $90: autoselect mode
					g_ATLCFlash("Entering autoselect mode.\n");
					mReadMode = kReadMode_Autoselect;
					mCommandPhase = 0;
					return true;

				case 0xA0:	// $A0: program mode
					g_ATLCFlash("Entering program mode.\n");
					mCommandPhase = 6;
					break;

				case 0xF0:	// $F0: reset
					g_ATLCFlash("Exiting autoselect mode.\n");
					mCommandPhase = 0;
					mReadMode = kReadMode_Normal;
					return true;

				default:
					g_ATLCFlash("Unknown command $%02X.\n", value);
					mCommandPhase = 0;
					break;
			}

			break;

		case 3:		// 5555[AA] 2AAA[55] 5555[80]
			mCommandPhase = 0;
			if (value == 0xAA) {
				if (mbA12iUnlock) {
					if ((address & 0xFFF) == 0xAAA)
						mCommandPhase = 4;
				} else if (mbA11Unlock) {
					if (address11 == 0x555)
						mCommandPhase = 4;
				} else {
					if (address15 == 0x5555)
						mCommandPhase = 4;
				}
			}

			if (mCommandPhase)
				g_ATLCFlash("Erase: step 4 OK.\n");
			else
				g_ATLCFlash("Erase: step 4 FAILED [($%05X) = $%02X].\n", address, value);
			break;

		case 4:		// 5555[AA] 2AAA[55] 5555[80] 5555[AA]
			mCommandPhase = 0;
			if (value == 0x55) {
				if (mbA12iUnlock) {
					if ((address & 0xFFF) == 0x555)
						mCommandPhase = 5;
				} else if (mbA11Unlock) {
					if (address11 == 0x2AA)
						mCommandPhase = 5;
				} else {
					if (address15 == 0x2AAA)
						mCommandPhase = 5;
				}
			}

			if (mCommandPhase)
				g_ATLCFlash("Erase: step 5 OK.\n");
			else
				g_ATLCFlash("Erase: step 5 FAILED [($%05X) = $%02X].\n", address, value);
			break;

		case 5:		// 5555[AA] 2AAA[55] 5555[80] 5555[AA] 2AAA[55]
			// A non-sector-erase command aborts a multiple sector erase in timeout phase.
			if (value != 0x80 && mReadMode == kReadMode_SectorEraseStatus) {
				mpScheduler->UnsetEvent(mpWriteEvent);
				mReadMode = kReadMode_Autoselect;
			}

			if (value == 0x10 && (mbA12iUnlock ? (address & 0xFFF) == 0xAAA : mbA11Unlock ? address11 == 0x555 : address15 == 0x5555)) {
				// full chip erase
				switch(mFlashType) {
					case kATFlashType_Am29F010:
					case kATFlashType_Am29F010B:
					case kATFlashType_M29F010B:
						memset(mpMemory, 0xFF, 0x20000);
						break;

					case kATFlashType_Am29F040:
					case kATFlashType_Am29F040B:
					case kATFlashType_SST39SF040:
					case kATFlashType_A29040:
					case kATFlashType_BM29F040:
					case kATFlashType_HY29F040A:
						memset(mpMemory, 0xFF, 0x80000);
						break;

					case kATFlashType_Am29F016D:
						memset(mpMemory, 0xFF, 0x200000);
						break;

					case kATFlashType_Am29F032B:
						memset(mpMemory, 0xFF, 0x400000);
						break;

					case kATFlashType_S29GL01P:
						memset(mpMemory, 0xFF, 0x8000000);
						break;

					case kATFlashType_S29GL512P:
						memset(mpMemory, 0xFF, 0x4000000);
						break;

					case kATFlashType_S29GL256P:
						memset(mpMemory, 0xFF, 0x2000000);
						break;
				}

				g_ATLCFlash("Erasing entire flash chip.\n");
				mbWriteActivity = true;
				mbDirty = true;

			} else if (value == 0x30) {
				// sector erase
				switch(mFlashType) {
					case kATFlashType_Am29F010:
					case kATFlashType_Am29F010B:
					case kATFlashType_M29F010B:
						address &= 0x1C000;
						memset(mpMemory + address, 0xFF, 0x4000);
						g_ATLCFlash("Erasing sector $%05X-%05X\n", address, address + 0x3FFF);
						break;
					case kATFlashType_Am29F040:
					case kATFlashType_Am29F040B:
					case kATFlashType_A29040:
					case kATFlashType_BM29F040:
					case kATFlashType_HY29F040A:
						address &= 0x70000;
						memset(mpMemory + address, 0xFF, 0x10000);
						g_ATLCFlash("Erasing sector $%05X-%05X\n", address, address + 0xFFFF);
						break;
					case kATFlashType_SST39SF040:
						address &= 0x7F000;
						memset(mpMemory + address, 0xFF, 0x1000);
						g_ATLCFlash("Erasing sector $%05X-%05X\n", address, address + 0xFFF);
						break;
					case kATFlashType_Am29F016D:
						address &= 0x1F0000;
						memset(mpMemory + address, 0xFF, 0x10000);
						g_ATLCFlash("Erasing sector $%06X-%06X\n", address, address + 0xFFFF);
						break;
					case kATFlashType_Am29F032B:
						address &= 0x3F0000;
						memset(mpMemory + address, 0xFF, 0x10000);
						g_ATLCFlash("Erasing sector $%06X-%06X\n", address, address + 0xFFFF);
						break;
					case kATFlashType_S29GL01P:
						address &= 0x7FF0000;
						memset(mpMemory + address, 0xFF, 0x20000);
						g_ATLCFlash("Erasing sector $%07X-%07X\n", address, address + 0x1FFFF);
						break;
					case kATFlashType_S29GL512P:
						address &= 0x3FF0000;
						memset(mpMemory + address, 0xFF, 0x20000);
						g_ATLCFlash("Erasing sector $%07X-%07X\n", address, address + 0x1FFFF);
						break;
					case kATFlashType_S29GL256P:
						address &= 0x1FF0000;
						memset(mpMemory + address, 0xFF, 0x20000);
						g_ATLCFlash("Erasing sector $%07X-%07X\n", address, address + 0x1FFFF);
						break;
				}

				mbWriteActivity = true;
				mbDirty = true;

				if (mSectorEraseTimeoutCycles) {
					// Once a sector erase has happened, other sector erase commands
					// may be issued... but ONLY sector erase commands. This window is
					// only guaranteed to last between 50us and 80us.
					mpScheduler->SetEvent(mSectorEraseTimeoutCycles, this, 2, mpWriteEvent);
					mCommandPhase = 14;
					mReadMode = kReadMode_SectorEraseStatus;
					return true;
				}
			} else {
				g_ATLCFlash("Erase: step 6 FAILED [($%05X) = $%02X].\n", address, value);
			}

			// unknown command
			mCommandPhase = 0;
			mReadMode = kReadMode_Normal;
			return true;

		case 6:		// 5555[AA] 2AAA[55] 5555[A0]
			mpMemory[address] &= value;
			mbDirty = true;
			mbWriteActivity = true;

			mCommandPhase = 0;
			mReadMode = kReadMode_Normal;
			return true;

		case 7:		// Atmel 5555[AA]
			if (address == 0x2AAA && value == 0x55)
				mCommandPhase = 8;
			else
				mCommandPhase = 0;
			break;

		case 8:		// Atmel 5555[AA] 2AAA[55]
			if (address != 0x5555) {
				mCommandPhase = 0;
				break;
			}

			switch(value) {
				case 0x80:	// $80: chip erase
					mCommandPhase = 9;
					break;

				case 0x90:	// $90: autoselect mode
					mReadMode = kReadMode_Autoselect;
					mCommandPhase = 0;
					break;

				case 0xA0:	// $A0: program mode
					mCommandPhase = 12;
					mbAtmelSDP = true;
					break;

				case 0xF0:	// $F0: reset
					mCommandPhase = 0;
					mReadMode = kReadMode_Normal;
					return true;

				default:
					mCommandPhase = 0;
					break;
			}
			break;

		case 9:		// Atmel 5555[AA] 2AAA[55] 5555[80]
			if (address == 0x5555 && value == 0xAA)
				mCommandPhase = 10;
			else
				mCommandPhase = 0;
			break;

		case 10:	// Atmel 5555[AA] 2AAA[55] 5555[80] 2AAA[55]
			if (address == 0x2AAA && value == 0x55)
				mCommandPhase = 11;
			else
				mCommandPhase = 0;
			break;

		case 11:	// Atmel 5555[AA] 2AAA[55] 5555[80] 2AAA[55]
			if (address == 0x5555) {
				switch(value) {
					case 0x10:		// chip erase
						switch(mFlashType) {
							case kATFlashType_AT29C010A:
								memset(mpMemory, 0xFF, 0x20000);
								break;

							case kATFlashType_AT29C040:
								memset(mpMemory, 0xFF, 0x80000);
								break;
						}
						mbDirty = true;
						mbWriteActivity = true;
						break;

					case 0x20:		// software data protection disable
						mbAtmelSDP = false;
						mCommandPhase = 12;
						return false;
				}
			}

			mReadMode = kReadMode_Normal;
			mCommandPhase = 0;
			break;

		case 12:	// Atmel SDP program mode - initial sector write
			mWriteSector = address & 0xFFF00;
			mReadMode = kReadMode_WriteStatusPending;
			memset(mpMemory, 0xFF, 256);
			mCommandPhase = 13;
			// fall through

		case 13:	// Atmel program mode - sector write
			mpMemory[address] = value;
			mbDirty = true;
			mbWriteActivity = true;
			mpScheduler->SetEvent(kAtmelWriteTimeoutCycles, this, 1, mpWriteEvent);
			return true;

		case 14:	// Multiple sector erase mode (AMD/Amic)
			if (value == 0x30) {
				// sector erase
				switch(mFlashType) {
					case kATFlashType_Am29F010:
					case kATFlashType_Am29F010B:
					case kATFlashType_M29F010B:
						address &= 0x1C000;
						memset(mpMemory + address, 0xFF, 0x4000);
						g_ATLCFlash("Erasing sector $%05X-%05X\n", address, address + 0x3FFF);
						break;
					case kATFlashType_Am29F040:
					case kATFlashType_Am29F040B:
					case kATFlashType_A29040:
					case kATFlashType_BM29F040:
					case kATFlashType_HY29F040A:
						address &= 0x70000;
						memset(mpMemory + address, 0xFF, 0x10000);
						g_ATLCFlash("Erasing sector $%05X-%05X\n", address, address + 0xFFFF);
						break;
					case kATFlashType_SST39SF040:
						address &= 0x7F000;
						memset(mpMemory + address, 0xFF, 0x1000);
						g_ATLCFlash("Erasing sector $%05X-%05X\n", address, address + 0xFFF);
						break;
					case kATFlashType_Am29F016D:
						address &= 0x1F0000;
						memset(mpMemory + address, 0xFF, 0x10000);
						g_ATLCFlash("Erasing sector $%06X-%06X\n", address, address + 0xFFFF);
						break;
					case kATFlashType_Am29F032B:
						address &= 0x3F0000;
						memset(mpMemory + address, 0xFF, 0x10000);
						g_ATLCFlash("Erasing sector $%06X-%06X\n", address, address + 0xFFFF);
						break;
				}

				mbWriteActivity = true;
				mbDirty = true;

				mpScheduler->SetEvent(mSectorEraseTimeoutCycles, this, 2, mpWriteEvent);
			}
			return true;

		case 15:	// Write buffer load - word count byte
			if (value > 31) {
				g_ATLCFlash("Aborting write buffer load - invalid word count ($%02X + 1)\n", value);
				mCommandPhase = 0;
			} else if ((address ^ mPendingWriteAddress) & ~(uint32)0x1ffff) {
				g_ATLCFlash("Aborting write buffer load - received word count outside of initial sector\n");
			} else {
				memset(mWriteBufferData, 0xFF, sizeof mWriteBufferData);
				mPendingWriteCount = value + 1;
				mCommandPhase = 16;
			}
			break;

		case 16:	// Write buffer load - receiving first byte
			if ((address ^ mPendingWriteAddress) & ~(uint32)0x1ffff) {
				g_ATLCFlash("Aborting write buffer load - received write outside of initial sector\n");
				mCommandPhase = 0;
			} else {
				mPendingWriteAddress = address;
				mWriteBufferData[address & 31] &= value;
				--mPendingWriteCount;

				if (mPendingWriteCount)
					mCommandPhase = 17;
				else
					mCommandPhase = 18;
			}
			break;

		case 17:	// Write buffer load - receiving subsequent bytes
			if ((address ^ mPendingWriteAddress) & ~(uint32)31) {
				g_ATLCFlash("Aborting write buffer load - received write outside of write page\n");
				mCommandPhase = 0;
			} else {
				mWriteBufferData[address & 31] &= value;
				--mPendingWriteCount;

				if (!mPendingWriteCount)
					mCommandPhase = 18;
			}
			break;

		case 18:	// Write buffer load -- waiting for program command
			if (value != 0x29) {
				g_ATLCFlash("Aborting buffered write -- received command other than program buffer command ($%02X)\n", value);
			} else {
				switch(mFlashType) {
					case kATFlashType_S29GL256P:
						mPendingWriteAddress &= 0x1FFFFE0;
						break;

					case kATFlashType_S29GL512P:
						mPendingWriteAddress &= 0x3FFFFE0;
						break;

					case kATFlashType_S29GL01P:
						mPendingWriteAddress &= 0x7FFFFE0;
						break;
				}

				g_ATLCFlash("Programming write page at $%07X\n", mPendingWriteAddress);

				for(int i=0; i<32; ++i)
					mpMemory[mPendingWriteAddress + i] &= mWriteBufferData[i];

				mbWriteActivity = true;
				mbDirty = true;
			}

			mCommandPhase = 0;
			break;
	}

	return false;
}

void ATFlashEmulator::OnScheduledEvent(uint32 id) {
	mpWriteEvent = NULL;

	g_ATLCFlash("Ending multiple sector timeout.\n");
	mReadMode = kReadMode_Normal;

	switch(mFlashType) {
		case kATFlashType_AT29C010A:
		case kATFlashType_AT29C040:
			if (mbAtmelSDP)
				mCommandPhase = 0;
			else
				mCommandPhase = 12;
			break;

		default:
			mCommandPhase = 0;
			break;
	}
}
