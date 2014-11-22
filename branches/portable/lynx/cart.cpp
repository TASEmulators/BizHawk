//
// Copyright (c) 2004 K. Wilkins
//
// This software is provided 'as-is', without any express or implied warranty.
// In no event will the authors be held liable for any damages arising from
// the use of this software.
//
// Permission is granted to anyone to use this software for any purpose,
// including commercial applications, and to alter it and redistribute it
// freely, subject to the following restrictions:
//
// 1. The origin of this software must not be misrepresented; you must not
//    claim that you wrote the original software. If you use this software
//    in a product, an acknowledgment in the product documentation would be
//    appreciated but is not required.
//
// 2. Altered source versions must be plainly marked as such, and must not
//    be misrepresented as being the original software.
//
// 3. This notice may not be removed or altered from any source distribution.
//

//////////////////////////////////////////////////////////////////////////////
//                       Handy - An Atari Lynx Emulator                     //
//                          Copyright (c) 1996,1997                         //
//                                 K. Wilkins                               //
//////////////////////////////////////////////////////////////////////////////
// Lynx Cartridge Class                                                     //
//////////////////////////////////////////////////////////////////////////////
//                                                                          //
// This class emulates the Lynx cartridge interface, given a filename it    //
// will contstruct a cartridge object via the constructor.                  //
//                                                                          //
//    K. Wilkins                                                            //
// August 1997                                                              //
//                                                                          //
//////////////////////////////////////////////////////////////////////////////
// Revision History:                                                        //
// -----------------                                                        //
//                                                                          //
// 01Aug1997 KW Document header added & class documented.                   //
//                                                                          //
//////////////////////////////////////////////////////////////////////////////

#define CART_CPP

#include "system.h"

#include <algorithm>
#include <string.h>
#include "cart.h"

/*
bool CCart::TestMagic(const uint8 *data, uint32 size)
{
	if(size <= HEADER_RAW_SIZE)
		return(FALSE);

	if(memcmp(data, "LYNX", 4) || data[8] != 0x01)
		return(FALSE);

	return(TRUE);
}
*/

CCart::CCart(const uint8 *gamedata, uint32 gamesize, int pagesize0, int pagesize1)
{
	CTYPE banktype0,banktype1;

	switch(pagesize0)
	{
	default:
		// warn?
	case 0x000:
		banktype0=UNUSED;
		mMaskBank0=0;
		mShiftCount0=0;
		mCountMask0=0;
		break;
	case 0x100:
		banktype0=C64K;
		mMaskBank0=0x00ffff;
		mShiftCount0=8;
		mCountMask0=0x0ff;
		break;
	case 0x200:
		banktype0=C128K;
		mMaskBank0=0x01ffff;
		mShiftCount0=9;
		mCountMask0=0x1ff;
		break;
	case 0x400:
		banktype0=C256K;
		mMaskBank0=0x03ffff;
		mShiftCount0=10;
		mCountMask0=0x3ff;
		break;
	case 0x800:
		banktype0=C512K;
		mMaskBank0=0x07ffff;
		mShiftCount0=11;
		mCountMask0=0x7ff;
		break;
	}

	switch(pagesize1)
	{
	default:
		// warn?
	case 0x000:
		banktype1=UNUSED;
		mMaskBank1=0;
		mShiftCount1=0;
		mCountMask1=0;
		break;
	case 0x100:
		banktype1=C64K;
		mMaskBank1=0x00ffff;
		mShiftCount1=8;
		mCountMask1=0x0ff;
		break;
	case 0x200:
		banktype1=C128K;
		mMaskBank1=0x01ffff;
		mShiftCount1=9;
		mCountMask1=0x1ff;
		break;
	case 0x400:
		banktype1=C256K;
		mMaskBank1=0x03ffff;
		mShiftCount1=10;
		mCountMask1=0x3ff;
		break;
	case 0x800:
		banktype1=C512K;
		mMaskBank1=0x07ffff;
		mShiftCount1=11;
		mCountMask1=0x7ff;
		break;
	}

	// Make some space for the new carts
	mCartBank0 = new uint8[mMaskBank0+1];
	mCartBank1 = new uint8[mMaskBank1+1];

	// Set default bank
	mBank=bank0;

	// Initialiase
	std::memset(mCartBank0, DEFAULT_CART_CONTENTS, mMaskBank0 + 1);
	std::memset(mCartBank1, DEFAULT_CART_CONTENTS, mMaskBank1 + 1);

	// Read in the BANK0 bytes
	if(mMaskBank0)
	{
		int size = std::min(gamesize, mMaskBank0+1);
		std::memcpy(mCartBank0, gamedata, size);
		gamedata += size;
		gamesize -= size;
	}

	// Read in the BANK1 bytes
	if(mMaskBank1)
	{
		int size = std::min(gamesize, mMaskBank1+1);
		std::memcpy(mCartBank1, gamedata, size);
		gamedata += size;
	}

	// As this is a cartridge boot unset the boot address
	// mSystem.gCPUBootAddress=0;

	// Dont allow an empty Bank1 - Use it for shadow SRAM/EEPROM
	if(banktype1==UNUSED)
	{
		// Delete the single byte allocated  earlier
		delete[] mCartBank1;
		// Allocate some new memory for us
		banktype1=C64K;
		mMaskBank1=0x00ffff;
		mShiftCount1=8;
		mCountMask1=0x0ff;
		mCartBank1 = (uint8*) new uint8[mMaskBank1+1];
		std::memset(mCartBank1, DEFAULT_RAM_CONTENTS, mMaskBank1 + 1);
		mWriteEnableBank1=TRUE;
		mCartRAM=TRUE;
	}
}

CCart::~CCart()
{
	delete[] mCartBank0;
	delete[] mCartBank1;
}


void CCart::Reset()
{
	mCounter = 0;
	mShifter = 0;
	mAddrData = 0;
	mStrobe = 0;
	last_strobe = 0;
}

INLINE void CCart::Poke(uint32 addr, uint8 data)
{
	if(mBank==bank0)
	{
		if(mWriteEnableBank0 && false) // can never write as there is no ram
			mCartBank0[addr&mMaskBank0]=data;
	}
	else
	{
		if(mWriteEnableBank1 && mCartRAM) // can only write if it's actually ram
			mCartBank1[addr&mMaskBank1]=data;
	}
}


INLINE uint8 CCart::Peek(uint32 addr)
{
	if(mBank==bank0)
	{
		return(mCartBank0[addr&mMaskBank0]);
	}
	else
	{
		return(mCartBank1[addr&mMaskBank1]);
	}
}


void CCart::CartAddressStrobe(bool strobe)
{
	mStrobe=strobe;

	if(mStrobe) mCounter=0;

	//
	// Either of the two below seem to work OK.
	//
	// if(!strobe && last_strobe)
	//
	if(mStrobe && !last_strobe)
	{
		// Clock a bit into the shifter
		mShifter=mShifter<<1;
		mShifter+=mAddrData?1:0;
		mShifter&=0xff;
	}
	last_strobe=mStrobe;
}

void CCart::CartAddressData(bool data)
{
	mAddrData=data;
}


void CCart::Poke0(uint8 data)
{
	if(mWriteEnableBank0 && false) // can never write as there is no ram
	{
		uint32 address=(mShifter<<mShiftCount0)+(mCounter&mCountMask0);
		mCartBank0[address&mMaskBank0]=data;		
	}
	if(!mStrobe)
	{
		mCounter++;
		mCounter&=0x07ff;
	}
}

void CCart::Poke1(uint8 data)
{
	if(mWriteEnableBank1 && mCartRAM) // can only write if it's actually ram
	{
		uint32 address=(mShifter<<mShiftCount1)+(mCounter&mCountMask1);
		mCartBank1[address&mMaskBank1]=data;		
	}
	if(!mStrobe)
	{
		mCounter++;
		mCounter&=0x07ff;
	}
}


uint8 CCart::Peek0(void)
{
	uint32 address=(mShifter<<mShiftCount0)+(mCounter&mCountMask0);
	uint8 data=mCartBank0[address&mMaskBank0];		

	if(!mStrobe)
	{
		mCounter++;
		mCounter&=0x07ff;
	}

	return data;
}

uint8 CCart::Peek1(void)
{
	uint32 address=(mShifter<<mShiftCount1)+(mCounter&mCountMask1);
	uint8 data=mCartBank1[address&mMaskBank1];		

	if(!mStrobe)
	{
		mCounter++;
		mCounter&=0x07ff;
	}

	return data;
}



bool CCart::GetSaveRamPtr(int &size, uint8 *&data)
{
	if (mCartRAM)
	{
		size = mMaskBank1 + 1;
		data = mCartBank1;
		return true;
	}
	else
	{
		return false;
	}
}

void CCart::GetReadOnlyPtrs(int &s0, uint8 *&p0, int &s1, uint8 *&p1)
{
	s0 = mMaskBank0 + 1;
	s1 = mMaskBank1 + 1;
	p0 = mCartBank0;
	p1 = mCartBank1;
}

SYNCFUNC(CCart)
{
	NSS(mWriteEnableBank0);
	NSS(mWriteEnableBank1);
	NSS(mCartRAM);

	EBS(mBank, 0);
	EVS(mBank, bank0, 0);
	EVS(mBank, bank1, 1);
	EVS(mBank, ram, 2);
	EVS(mBank, cpu, 3);
	EES(mBank, bank0);

	NSS(mMaskBank0);
	NSS(mMaskBank1);
	if (false)
		PSS(mCartBank0, mMaskBank0 + 1);
	if (mCartRAM)
		PSS(mCartBank1, mMaskBank1 + 1);

	NSS(mCounter);
	NSS(mShifter);
	NSS(mAddrData);
	NSS(mStrobe);

	NSS(mShiftCount0);
	NSS(mCountMask0);
	NSS(mShiftCount1);
	NSS(mCountMask1);

	NSS(last_strobe);
}
