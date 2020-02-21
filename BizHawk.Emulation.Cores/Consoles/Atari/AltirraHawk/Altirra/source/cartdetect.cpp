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
//	Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.

#include <stdafx.h>
#include <vd2/system/binary.h>
#include <at/atdebugger/target.h>
#include "cartridge.h"
#include "disasm.h"
#include "ksyms.h"

namespace {
	enum SystemType {
		kType800,
		kType5200
	};

	enum SizeType {
		kSize2K		= 1,
		kSize4K		= 2,
		kSize8K		= 4,
		kSize16K	= 8,
		kSize32K	= 16,
		kSize40K	= 32,
		kSize64K	= 64,
		kSize128K	= 128,
		kSize256K	= 256,
		kSize512K	= 512,
		kSize1M		= 1024,
		kSize2M		= 2048,
		kSize4M		= 0x1000,
		kSize32M	= 0x2000,
		kSize64M	= 0x4000,
		kSize128M	= 0x8000,
	};

	enum WritableStoreType {
		kWrsNone,
		kWrs256B,
		kWrs8K
	};

	enum BankingType {
		kBankNone,
		kBankData,
		kBankDataSw,
		kBankAddr,
		kBankAddrSw,
		kBankAddr7x,
		kBankAddrDx,
		kBankAddrEx,
		kBankAddrEFx,
		kBankAny,
		kBankBF,
		kBankOther
	};

	enum InitRange {
		kInit2K,
		kInit4K,
		kInit8K,
		kInit8KR,
		kInit16K,
		kInit32K
	};

	enum HeaderType {
		kHeaderFirst4K,
		kHeaderFirst8K,
		kHeaderFirst8K_PreferAll8K,
		kHeaderFirst16K_PreferAll16K,
		kHeaderFirst32K,
		kHeaderLast32K,
		kHeaderLast16B,
		kHeaderLast8K_PreferAll8K
	};
}

static const struct ATCartDetectInfo {
	ATCartridgeMode		mMode;
	SystemType			mSystemType;
	uint32				mSizeTypes;
	WritableStoreType	mWritableStoreType;
	BankingType			mBankingType;
	InitRange			mInitRange;
	HeaderType			mHeaderType;
} kATCartDetectInfo[] = {
{	kATCartridgeMode_2K,						kType800,	kSize2K,	kWrsNone,	kBankNone,		kInit2K,	kHeaderLast16B,					},
{	kATCartridgeMode_4K,						kType800,	kSize4K,	kWrsNone,	kBankNone,		kInit4K,	kHeaderLast16B,					},
{	kATCartridgeMode_8K,						kType800,	kSize2K |
															kSize4K |
															kSize8K,	kWrsNone,	kBankNone,		kInit8K,	kHeaderLast16B,					},
{	kATCartridgeMode_16K,						kType800,	kSize16K,	kWrsNone,	kBankNone,		kInit16K,	kHeaderLast16B,					},
{	kATCartridgeMode_XEGS_32K,					kType800,	kSize32K,	kWrsNone,	kBankData,		kInit8K,	kHeaderLast16B,					},
{	kATCartridgeMode_XEGS_64K,					kType800,	kSize64K,	kWrsNone,	kBankData,		kInit8K,	kHeaderLast16B,					},
{	kATCartridgeMode_XEGS_128K,					kType800,	kSize128K,	kWrsNone,	kBankData,		kInit8K,	kHeaderLast16B,					},
{	kATCartridgeMode_XEGS_256K,					kType800,	kSize256K,	kWrsNone,	kBankData,		kInit8K,	kHeaderLast16B,					},
{	kATCartridgeMode_XEGS_512K,					kType800,	kSize512K,	kWrsNone,	kBankData,		kInit8K,	kHeaderLast16B,					},
{	kATCartridgeMode_XEGS_1M,					kType800,	kSize1M,	kWrsNone,	kBankData,		kInit8K,	kHeaderLast16B,					},
{	kATCartridgeMode_Switchable_XEGS_32K,		kType800,	kSize32K,	kWrsNone,	kBankDataSw,	kInit16K,	kHeaderLast16B,					},
{	kATCartridgeMode_Switchable_XEGS_64K,		kType800,	kSize64K,	kWrsNone,	kBankDataSw,	kInit16K,	kHeaderLast16B,					},
{	kATCartridgeMode_Switchable_XEGS_128K,		kType800,	kSize128K,	kWrsNone,	kBankDataSw,	kInit16K,	kHeaderLast16B,					},
{	kATCartridgeMode_Switchable_XEGS_256K,		kType800,	kSize256K,	kWrsNone,	kBankDataSw,	kInit16K,	kHeaderLast16B,					},
{	kATCartridgeMode_Switchable_XEGS_512K,		kType800,	kSize512K,	kWrsNone,	kBankDataSw,	kInit16K,	kHeaderLast16B,					},
{	kATCartridgeMode_Switchable_XEGS_1M,		kType800,	kSize1M,	kWrsNone,	kBankDataSw,	kInit16K,	kHeaderLast16B,					},
{	kATCartridgeMode_MaxFlash_128K,				kType800,	kSize128K,	kWrsNone,	kBankAddrSw,	kInit8K,	kHeaderFirst8K_PreferAll8K,		},
{	kATCartridgeMode_MaxFlash_128K_MyIDE,		kType800,	kSize128K,	kWrsNone,	kBankAddrSw,	kInit8K,	kHeaderFirst8K_PreferAll8K,		},
{	kATCartridgeMode_MaxFlash_1024K,			kType800,	kSize1M,	kWrsNone,	kBankAddrSw,	kInit8K,	kHeaderLast8K_PreferAll8K,		},
{	kATCartridgeMode_MaxFlash_1024K_Bank0,		kType800,	kSize256K |
															kSize512K |
															kSize1M,	kWrsNone,	kBankAddrSw,	kInit8K,	kHeaderFirst8K_PreferAll8K,		},
{	kATCartridgeMode_MegaCart_16K,				kType800,	kSize16K,	kWrsNone,	kBankDataSw,	kInit16K,	kHeaderFirst16K_PreferAll16K,	},
{	kATCartridgeMode_MegaCart_32K,				kType800,	kSize32K,	kWrsNone,	kBankDataSw,	kInit16K,	kHeaderFirst16K_PreferAll16K,	},
{	kATCartridgeMode_MegaCart_64K,				kType800,	kSize64K,	kWrsNone,	kBankDataSw,	kInit16K,	kHeaderFirst16K_PreferAll16K,	},
{	kATCartridgeMode_MegaCart_128K,				kType800,	kSize128K,	kWrsNone,	kBankDataSw,	kInit16K,	kHeaderFirst16K_PreferAll16K,	},
{	kATCartridgeMode_MegaCart_256K,				kType800,	kSize256K,	kWrsNone,	kBankDataSw,	kInit16K,	kHeaderFirst16K_PreferAll16K,	},
{	kATCartridgeMode_MegaCart_512K,				kType800,	kSize512K,	kWrsNone,	kBankDataSw,	kInit16K,	kHeaderFirst16K_PreferAll16K,	},
{	kATCartridgeMode_MegaCart_1M,				kType800,	kSize1M,	kWrsNone,	kBankDataSw,	kInit16K,	kHeaderFirst16K_PreferAll16K,	},
{	kATCartridgeMode_MegaCart_2M,				kType800,	kSize2M,	kWrsNone,	kBankDataSw,	kInit16K,	kHeaderFirst16K_PreferAll16K,	},
{	kATCartridgeMode_BountyBob800,				kType800,	kSize40K,	kWrsNone,	kBankOther,		kInit16K,	kHeaderLast16B,					},
{	kATCartridgeMode_OSS_034M,					kType800,	kSize16K,	kWrsNone,	kBankAddr,		kInit8K,	kHeaderLast16B,					},
{	kATCartridgeMode_OSS_M091,					kType800,	kSize16K,	kWrsNone,	kBankAddr,		kInit8K,	kHeaderFirst4K,					},
{	kATCartridgeMode_OSS_043M,					kType800,	kSize16K,	kWrsNone,	kBankAddr,		kInit8K,	kHeaderLast16B,					},
{	kATCartridgeMode_OSS_8K,					kType800,	kSize8K,	kWrsNone,	kBankAddr,		kInit8K,	kHeaderFirst4K,					},
{	kATCartridgeMode_Corina_1M_EEPROM,			kType800,	kSize1M,	kWrs8K,		kBankData,		kInit8K,	kHeaderFirst8K,					},
{	kATCartridgeMode_Corina_512K_SRAM_EEPROM,	kType800,	kSize512K,	kWrs8K,		kBankData,		kInit8K,	kHeaderFirst8K,					},
{	kATCartridgeMode_BountyBob5200,				kType5200,	kSize40K,	kWrsNone,	kBankOther,		kInit32K,	kHeaderLast16B,					},
{	kATCartridgeMode_BountyBob5200Alt,			kType5200,	kSize40K,	kWrsNone,	kBankOther,		kInit32K,	kHeaderFirst8K,					},
{	kATCartridgeMode_Williams_32K,				kType800,	kSize32K,	kWrsNone,	kBankAddr,		kInit8K,	kHeaderFirst8K,					},
{	kATCartridgeMode_Williams_64K,				kType800,	kSize64K,	kWrsNone,	kBankAddr,		kInit8K,	kHeaderFirst8K,					},
{	kATCartridgeMode_Diamond_64K,				kType800,	kSize64K,	kWrsNone,	kBankAddrDx,	kInit8K,	kHeaderFirst8K,					},
{	kATCartridgeMode_Express_64K,				kType800,	kSize64K,	kWrsNone,	kBankAddr7x,	kInit8K,	kHeaderFirst8K,					},
{	kATCartridgeMode_SpartaDosX_64K,			kType800,	kSize64K,	kWrsNone,	kBankAddrEx,	kInit8K,	kHeaderFirst8K,					},
{	kATCartridgeMode_SpartaDosX_128K,			kType800,	kSize128K,	kWrsNone,	kBankAddrEFx,	kInit8K,	kHeaderFirst8K,					},
{	kATCartridgeMode_Atrax_SDX_128K,			kType800,	kSize128K,	kWrsNone,	kBankAddrEFx,	kInit8K,	kHeaderFirst8K,					},
{	kATCartridgeMode_Atrax_SDX_64K,				kType800,	kSize64K,	kWrsNone,	kBankAddrEx,	kInit8K,	kHeaderFirst8K,					},
{	kATCartridgeMode_TelelinkII,				kType800,	kSize8K,	kWrs256B,	kBankNone,		kInit8K,	kHeaderFirst8K,					},
{	kATCartridgeMode_RightSlot_4K,				kType800,	kSize4K,	kWrsNone,	kBankNone,		kInit8KR,	kHeaderLast16B,					},
{	kATCartridgeMode_RightSlot_8K,				kType800,	kSize8K,	kWrsNone,	kBankNone,		kInit8KR,	kHeaderLast16B,					},
{	kATCartridgeMode_DB_32K,					kType800,	kSize32K,	kWrsNone,	kBankAddr,		kInit8K,	kHeaderLast16B,					},
{	kATCartridgeMode_Atrax_128K,				kType800,	kSize128K,	kWrsNone,	kBankData,		kInit8K,	kHeaderFirst8K,					},
{	kATCartridgeMode_Phoenix_8K,				kType800,	kSize8K,	kWrsNone,	kBankAny,		kInit8K,	kHeaderLast16B,					},
{	kATCartridgeMode_Blizzard_16K,				kType800,	kSize16K,	kWrsNone,	kBankAny,		kInit16K,	kHeaderLast16B,					},
{	kATCartridgeMode_Blizzard_4K,				kType800,	kSize4K,	kWrsNone,	kBankAny,		kInit8K,	kHeaderLast16B,					},
{	kATCartridgeMode_SIC,						kType800,	kSize128K |
															kSize256K |
															kSize512K,	kWrsNone,	kBankAddr,		kInit8K,	kHeaderFirst16K_PreferAll16K,	},
{	kATCartridgeMode_AST_32K,					kType800,	kSize32K,	kWrsNone,	kBankAny,		kInit8K,	kHeaderFirst8K,					},
{	kATCartridgeMode_Turbosoft_64K,				kType800,	kSize64K,	kWrsNone,	kBankAddr,		kInit8K,	kHeaderFirst8K,					},
{	kATCartridgeMode_Turbosoft_128K,			kType800,	kSize128K,	kWrsNone,	kBankAddr,		kInit8K,	kHeaderFirst8K,					},
{	kATCartridgeMode_MegaCart_1M_2,				kType800,	kSize256K |
															kSize512K |
															kSize1M,	kWrsNone,	kBankDataSw,	kInit8K,	kHeaderFirst8K,					},
{	kATCartridgeMode_MegaCart_512K_3,			kType800,	kSize512K,	kWrsNone,	kBankDataSw,	kInit8K,	kHeaderFirst8K,					},
{	kATCartridgeMode_MegaCart_4M_3,				kType800,	kSize4M,	kWrsNone,	kBankDataSw,	kInit8K,	kHeaderLast16B,					},
{	kATCartridgeMode_MicroCalc,					kType800,	kSize32K,	kWrsNone,	kBankAny,		kInit8K,	kHeaderFirst8K,					},
{	kATCartridgeMode_MegaMax_2M,				kType800,	kSize2M,	kWrsNone,	kBankAny,		kInit16K,	kHeaderFirst16K_PreferAll16K,	},
{	kATCartridgeMode_TheCart_32M,				kType800,	kSize32M,	kWrsNone,	kBankAny,		kInit8K,	kHeaderFirst8K,					},
{	kATCartridgeMode_TheCart_64M,				kType800,	kSize64M,	kWrsNone,	kBankAny,		kInit8K,	kHeaderFirst8K,					},
{	kATCartridgeMode_TheCart_128M,				kType800,	kSize128M,	kWrsNone,	kBankAny,		kInit8K,	kHeaderFirst8K,					},

// 5200 carts
//
// Quite a few carts make use of address mirroring, so the entire cart address range
// is allowed for init.

{	kATCartridgeMode_5200_32K,					kType5200,	kSize32K,	kWrsNone,	kBankNone,		kInit32K,	kHeaderLast16B,					},
{	kATCartridgeMode_5200_16K_TwoChip,			kType5200,	kSize16K,	kWrsNone,	kBankNone,		kInit32K,	kHeaderLast16B,					},
{	kATCartridgeMode_5200_16K_OneChip,			kType5200,	kSize16K,	kWrsNone,	kBankNone,		kInit32K,	kHeaderLast16B,					},
{	kATCartridgeMode_5200_8K,					kType5200,	kSize8K,	kWrsNone,	kBankNone,		kInit32K,	kHeaderLast16B,					},
{	kATCartridgeMode_5200_4K,					kType5200,	kSize4K,	kWrsNone,	kBankNone,		kInit32K,	kHeaderLast16B,					},
{	kATCartridgeMode_5200_64K_32KBanks,			kType5200,	kSize64K,	kWrsNone,	kBankBF,		kInit32K,	kHeaderLast32K,					},
{	kATCartridgeMode_5200_512K_32KBanks,		kType5200,	kSize512K,	kWrsNone,	kBankBF,		kInit32K,	kHeaderLast32K,					},
};

uint32 ATCartridgeAutodetectMode(const void *data, uint32 size, vdfastvector<int>& cartModes) {
	WritableStoreType wrsType = kWrsNone;

	if (size >= 262144 && (size & 8192)) {
		size -= 8192;
		wrsType = kWrs8K;
	} else if (size >= 8192 && (size & 256)) {
		size -= 256;
		wrsType = kWrs256B;
	}

	// map cartridge size
	uint32 sizeType;

		 if (size == 4194304) sizeType = kSize4M;
	else if (size == 2097152) sizeType = kSize2M;
	else if (size == 1048576) sizeType = kSize1M;
	else if (size ==  524288) sizeType = kSize512K;
	else if (size ==  262144) sizeType = kSize256K;
	else if (size ==  131072) sizeType = kSize128K;
	else if (size ==   65536) sizeType = kSize64K;
	else if (size ==   40960) sizeType = kSize40K;
	else if (size ==   32768) sizeType = kSize32K;
	else if (size ==   16384) sizeType = kSize16K;
	else if (size ==    8192) sizeType = kSize8K;
	else if (size ==    4096) sizeType = kSize4K;
	else if (size ==    2048) sizeType = kSize2K;
	else return 0;

	// Attempt to detect 5200 vs. 800 by POKEY and GTIA accesses.
	//
	// 5200: GTIA @ Cxxx, POKEY @ E800-EBFF
	// 800: GTIA @ D0xx, POKEY @ D2xx
	//
	// We just check STA abs instructions for simplicity, as these are the most
	// common.

	const uint8 *const data8 = (const uint8 *)data;

	uint32 count800 = 0;
	uint32 count5200 = 0;

	if (data) {
		for(uint32 i=2; i<size; ++i) {
			uint8 c = data8[i-2];

			if (c == 0x8D && data8[i-1] < 0x20) {
				uint8 hiaddr = data8[i];

				if ((hiaddr & 0xF0) == 0xC0 || (hiaddr & 0xFC) == 0xE8)
					++count5200;
				else if (hiaddr == 0xD0 || hiaddr == 0xD2)
					++count800;
			} else if (c == 0x20) {
				uint32 target = data8[i-1] + 256*data8[i];

				switch(target) {
					case ATKernelSymbols::CIOV:
					case ATKernelSymbols::SIOV:
					case ATKernelSymbols::SETVBV:
					case ATKernelSymbols::XITVBV:
					case ATKernelSymbols::AFP:
					case ATKernelSymbols::FASC:
					case ATKernelSymbols::FPI:
						++count800;
						break;
				}
			}
		}
	}

	// Consider it a hit if we have at least some accesses and at least a 3:1
	// ratio over the other mode.
	const uint32 countAll = count800 + count5200;
	bool reject800 = (count800 * 3 < count5200) && (countAll >= 10);
	bool reject5200 = (count5200 * 3 < count800) && (countAll >= 10);

	// If we're still ambiguous, there are a few more tests we can do. We're
	// particularly interested in 8K carts since these are the most commonly
	// ambiguous (no banking detection possible).
	if (!reject800 && !reject5200 && size == 8192) {
		if (data8[0x1FFD] < 0x08) {
			// $BFFD is a copyright digit for non-diag 5200 carts and so we would
			// expect it to be an IR mode 6/7 digit in INTERNAL encoding. However,
			// it's a flags byte in 800 mode, where only bits 0 and 2 are pertinent.
			// If we see a low byte, we assume it is an 800 cart.
			reject5200 = true;
		}
	}

	// Loop over all of the modes and filter.
	for(size_t i=0; i<sizeof(kATCartDetectInfo)/sizeof(kATCartDetectInfo[0]); ++i) {
		const ATCartDetectInfo& detInfo = kATCartDetectInfo[i];

		// check size type
		if (!(detInfo.mSizeTypes & sizeType))
			continue;

		// check system type
		switch(detInfo.mSystemType) {
			case kType800:
				if (reject800) {
not_recommended:
					cartModes.push_back(detInfo.mMode);
					continue;
				}
				break;

			case kType5200:
				if (reject5200)
					goto not_recommended;
				break;
		}

		// check writable store type (optional, but must be absent if not expected)
		if (wrsType && wrsType != detInfo.mWritableStoreType)
			continue;

		// special casing for troublesome and rare types
		switch(detInfo.mMode) {
			case kATCartridgeMode_TelelinkII:
			case kATCartridgeMode_Atrax_SDX_64K:
			case kATCartridgeMode_Atrax_SDX_128K:
				goto not_recommended;
		}

		bool passAllHeaders = true;
		sint32 score = 0;

		if (data) {
			sint32 headerOffset = -1;
			sint32 headerStep = 0;

			switch(detInfo.mHeaderType) {
				case kHeaderFirst4K:
					headerOffset = 0xFF0;
					break;

				case kHeaderFirst8K:
					headerOffset = 0x1FF0;
					break;

				case kHeaderFirst8K_PreferAll8K:
					headerOffset = 0x1FF0;
					headerStep = 0x2000;
					break;

				case kHeaderFirst16K_PreferAll16K:
					headerOffset = 0x3FF0;
					headerStep = 0x4000;
					break;

				case kHeaderFirst32K:
					headerOffset = 0x7FF0;
					break;

				case kHeaderLast32K:
					headerOffset = size - 0x10;
					break;

				case kHeaderLast16B:
					headerOffset = size - 0x10;
					break;

				case kHeaderLast8K_PreferAll8K:
					headerOffset = size - 0x10;
					headerStep = -0x2000;
					break;
			}

			bool firstHeader = true;

			while(headerOffset >= 0 && headerOffset + 16 <= (sint32)size) {
				const uint8 *header = data8 + headerOffset;
				const uint8 hibyte = header[15];

				bool ok = true;

				switch(detInfo.mInitRange) {
					case kInit2K:
						if ((hibyte & 0xF8) != 0xB8)
							ok = false;
						break;

					case kInit4K:
						if ((hibyte & 0xF0) != 0xB0)
							ok = false;
						break;

					case kInit8K:
						if ((hibyte & 0xE0) != 0xA0)
							ok = false;
						break;

					case kInit8KR:
						if ((hibyte & 0xE0) != 0x80)
							ok = false;
						break;

					case kInit16K:
						if ((hibyte & 0xC0) != 0x80)
							ok = false;
						break;

					case kInit32K:
						if ((uint8)(hibyte - 0x40) >= 0x80)
							ok = false;
						break;
				}

				if (!ok) {
					passAllHeaders = false;

					if (firstHeader)
						goto not_recommended;
				}

				firstHeader = false;

				if (!headerStep)
					break;

				headerOffset += headerStep;
			}

			score += 10;

			if (headerStep && passAllHeaders) {
				score += 10;

				if (abs(headerStep) > 0x2000)
					score += 10;
			}

			// Now scan looking for bank instructions -- we are looking for:
			//		[AC,AD,AE] xx D5	(D5 read)
			//		[8C,8D,8E] xx D5	(D5 write)
			//		A9 yy 8D xx D5		(load A, D5 write)
			//		A2 yy 8E xx D5		(load X, D5 write)
			//		A0 yy 8C xx D5		(load Y, D5 write)
			//
			uint32 goodBank = 0;
			uint32 badBank = 0;

			for(uint32 i=4; i<size; ++i) {
				uint8 c = data8[i-2];
				int val = -1;

				switch(c) {
					case 0xAC:	// LDY
						break;

					case 0xAD:	// LDA
						break;

					case 0xAE:	// LDX
						break;

					case 0x8C:	// STY
						if (data8[i-4] == 0xA0)
							val = data8[i-3];
						break;

					case 0x8D:	// STA
						if (data8[i-4] == 0xA9)
							val = data8[i-3];
						break;

					case 0x8E:	// STX
						if (data8[i-4] == 0xA2)
							val = data8[i-3];
						break;

					default:
						continue;
				}

				const uint8 hiaddr = data8[i];
				const bool write = !(c & 0x20);

				switch(detInfo.mBankingType) {
					case kBankNone:
						if (hiaddr == 0xD5)
							++badBank;
						break;

					case kBankData:
						if (hiaddr == 0xD5) {
							if (write && (val >= 0 && val < 0x80))
								++goodBank;
							else
								++badBank;
						}
						break;

					case kBankDataSw:
						if (hiaddr == 0xD5) {
							if (val >= 0x80)
								++goodBank;
							else if (!write)
								++badBank;
						}
						break;

					case kBankAddr:
						if (hiaddr == 0xD5) {
							if (data8[i-1] < 0x80)
								++goodBank;
							else
								++badBank;
						}
						break;

					case kBankAddrSw:
						if (hiaddr == 0xD5 && data8[i-1] >= 0x80)
							++goodBank;
						break;

					case kBankAddr7x:
						if (hiaddr == 0xD5) {
							if ((data8[i-1] & 0xF0) == 0x70)
								++goodBank;
							else
								++badBank;
						}
						break;

					case kBankAddrDx:
						if (hiaddr == 0xD5) {
							if ((data8[i-1] & 0xF0) == 0xD0)
								++goodBank;
							else
								++badBank;
						}
						break;

					case kBankAddrEx:
						if (hiaddr == 0xD5) {
							if ((data8[i-1] & 0xF0) == 0xE0)
								++goodBank;
							else
								++badBank;
						}
						break;

					case kBankAddrEFx:
						if (hiaddr == 0xD5) {
							if ((data8[i-1] & 0xE0) == 0xE0)
								++goodBank;
							else
								++badBank;
						}
						break;

					case kBankAny:
						if (hiaddr == 0xD5)
							++goodBank;
						break;

					case kBankBF:
						if (hiaddr == 0xBF)
							++goodBank;
						else if (hiaddr == 0xD5)
							++badBank;
						break;
				}
			}

			// if we expected banking and didn't find much, reject
			if (detInfo.mBankingType && goodBank < 2)
				goto not_recommended;

			// if we had significantly more bad banks, reject
			if (badBank >= 32 && badBank * 3 >= goodBank)
				goto not_recommended;

			// apply a bonus based on banking type
			switch(detInfo.mBankingType) {
				case kBankNone:
					break;

				case kBankData:
					score += 10;
					break;

				case kBankDataSw:
					score += 20;
					break;

				case kBankAddr:
					score += 5;
					break;

				case kBankAddrSw:
					score += 15;
					break;

				case kBankAddr7x:
					score += 30;
					break;

				case kBankAddrDx:
					score += 30;
					break;

				case kBankAddrEx:
					score += 40;
					break;

				case kBankAddrEFx:
					score += 30;
					break;

				case kBankAny:
					break;

				case kBankBF:
					score += 20;
					break;
			}
		}

		cartModes.push_back(detInfo.mMode - (score << 16));
	}

	if (cartModes.empty())
		return 0;

	// Check if we have both 5200 one-chip and 5200 two-chip.
	auto itOneChip = std::find_if(cartModes.begin(), cartModes.end(), [](uint32 id) { return (id & 0xffff) == kATCartridgeMode_5200_16K_OneChip; });
	auto itTwoChip = std::find_if(cartModes.begin(), cartModes.end(), [](uint32 id) { return (id & 0xffff) == kATCartridgeMode_5200_16K_TwoChip; });
	if (itOneChip != cartModes.end() && itTwoChip != cartModes.end()) {
		// Do a quick and dirty static trace for 256 bytes starting at the init address
		// and count the number of instructions that are 'interesting' for init code. If we
		// have a >2:1 ratio, drop the score on the lower one.
		uint32 scores[2] = {};

		for(int mode=0; mode<2; ++mode) {
			uint32 hiMask = mode ? 0x8000 : 0x2000;

			uint32 addr = VDReadUnalignedLEU16(data8 + 0x3FFE);

			uint8 buf[256];
			for(int i=0; i<256; ++i, ++addr) {
				uint32 offset = (addr & 0x1FFF) + (addr & hiMask ? 0x2000 : 0);

				buf[i] = data8[offset];
			}

			for(int offset = 0; offset < 254;) {
				int ilen = ATGetOpcodeLength(buf[offset], 0x30, true, kATDebugDisasmMode_6502);

				// check for abs store
				switch(buf[offset]) {
					case 0x8C:	// STY abs
					case 0x8D:	// STA abs
					case 0x8E:	// STX abs
					case 0x9D:	// STA abs,X
					case 0x99:	// STA abs,Y
						{
							uint32 baseAddr = VDReadUnalignedLEU16(&buf[offset + 1]);

							if ((baseAddr - 0xC000) < 0x20		// GTIA (canonical)
								|| (baseAddr - 0xE800) < 0x10	// POKEY (canonical)
								|| (baseAddr - 0xD400) < 0x10	// ANTIC (canonical)
								|| (baseAddr - 0x0200) < 0x0E	// OS Vectors
								)
							{
								++scores[mode];
							}
						}
						break;
				}

				offset += ilen;
			}
		}

		if (scores[0] > 4 && scores[0] > scores[1]*2) {
			// We think it's one-chip.
			*itTwoChip += 0x10000;

		} else if (scores[1] > 4 && scores[1] > scores[0]*2) {
			// We think it's two-chip.
			*itOneChip += 0x10000;
		} else {
			// Ugh. Okay, the init path didn't work. Let's try to partition based on
			// bank usage.
			// CPU			OneChip		TwoChip
			// $4000-5FFF	$0000-1FFF	$0000-1FFF
			// $6000-7FFF	$2000-3FFF	$0000-1FFF
			// $8000-9FFF	$0000-1FFF	$2000-3FFF
			// $A000-BFFF	$2000-3FFF	$2000-3FFF

			int oneChipScore = 0;
			int scores[4] = {0};

			for(int i=0; i<0x3FDD; ++i) {
				const uint8 opcode = data8[i];

				if (opcode != 0x20 && opcode != 0x4C)
					continue;

				const uint16 addr = VDReadUnalignedLEU16(&data8[i+1]);

				if (addr >= 0x4000 && addr < 0xC000)
					++scores[(addr - 0x4000) >> 13];
			}

			if (std::max(scores[0], scores[2]) > std::min(scores[0], scores[2]) * 2
				&& std::max(scores[1], scores[3]) > std::min(scores[1], scores[3]) * 2) {
				// We think it's one-chip.
				*itTwoChip += 0x10000;

			} else if (std::max(scores[0], scores[1]) > std::min(scores[0], scores[1]) * 2
				&& std::max(scores[2], scores[3]) > std::min(scores[2], scores[3]) * 2) {
				// We think it's two-chip.
				*itOneChip += 0x10000;
			}
		}
	}

	// reverse sort
	std::sort(cartModes.begin(), cartModes.end());

	// count recommended types
	int hiscore = cartModes.front() >> 16;

	uint32 recommendedCount = 0;

	for(vdfastvector<int>::iterator it(cartModes.begin()), itEnd(cartModes.end());
		it != itEnd;
		++it)
	{
		if ((*it >> 16) == hiscore)
			++recommendedCount;

		*it &= 0xffff;
	}

	return recommendedCount;
}
