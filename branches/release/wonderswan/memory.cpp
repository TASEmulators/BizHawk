/* Cygne
*
* Copyright notice for this file:
*  Copyright (C) 2002 Dox dox@space.pl
*
* This program is free software; you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation; either version 2 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program; if not, write to the Free Software
* Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
*/

#include "system.h"

#include <math.h>
#include <cstring>
#include <cstdlib>


namespace MDFN_IEN_WSWAN
{
	void Memory::Write20(uint32 A, uint8 V)
	{
		uint32 offset, bank;

		offset = A & 0xffff;
		bank = (A>>16) & 0xF;

		if(!bank) /*RAM*/
		{
			sys->sound.CheckRAMWrite(offset);
			wsRAM[offset] = V;

			sys->gfx.InvalidByAddr(offset);

			if(offset>=0xfe00) /*WSC palettes*/
				sys->gfx.PaletteRAMWrite(offset, V);
		}
		else if(bank == 1) /* SRAM */
		{	 
			if(sram_size)
			{
				wsSRAM[(offset | (BankSelector[1] << 16)) & (sram_size - 1)] = V;
			}
		}
	}



	uint8 Memory::Read20(uint32 A)
	{
		uint32	offset, bank;

		offset = A & 0xFFFF;
		bank = (A >> 16) & 0xF;

		switch(bank)
		{
		case 0:  return wsRAM[offset];
		case 1:  if(sram_size)
				 {
					 return wsSRAM[(offset | (BankSelector[1] << 16)) & (sram_size - 1)];
				 }
				 else
					 return(0);

		case 2:
		case 3:  return wsCartROM[offset+((BankSelector[bank]&((rom_size>>16)-1))<<16)];

		default: 
			{
				uint8 bank_num = ((BankSelector[0] & 0xF) << 4) | (bank & 0xf);
				bank_num &= (rom_size >> 16) - 1;
				return(wsCartROM[(bank_num << 16) | offset]);
			}
		}
	}

	void Memory::CheckDMA()
	{
		if(DMAControl & 0x80)
		{
			while(DMALength)
			{
				Write20(DMADest, Read20(DMASource));

				DMASource++; // = ((DMASource + 1) & 0xFFFF) | (DMASource & 0xFF0000);
				//if(!(DMASource & 0xFFFF)) puts("Warning: DMA source bank crossed.");
				DMADest = ((DMADest + 1) & 0xFFFF) | (DMADest & 0xFF0000);
				DMALength--;
			}
		}
		DMAControl &= ~0x80;
	}

	void Memory::CheckSoundDMA()
	{
		if(SoundDMAControl & 0x80)
		{
			if(SoundDMALength)
			{
				uint8 zebyte = Read20(SoundDMASource);

				if(SoundDMAControl & 0x08)
					zebyte ^= 0x80;

				if(SoundDMAControl & 0x10)
					sys->sound.Write(0x95, zebyte); // Pick a port, any port?!
				else
					sys->sound.Write(0x89, zebyte);

				SoundDMASource++; // = ((SoundDMASource + 1) & 0xFFFF) | (SoundDMASource & 0xFF0000);
				//if(!(SoundDMASource & 0xFFFF)) puts("Warning:  Sound DMA source bank crossed.");
				SoundDMALength--;
			}
			if(!SoundDMALength)
				SoundDMAControl &= ~0x80;
		}
	}

	uint8 Memory::readport(uint32 number)
	{
		number &= 0xFF;

		if(number >= 0x80 && number <= 0x9F)
			return(sys->sound.Read(number));
		else if(number <= 0x3F || (number >= 0xA0 && number <= 0xAF) || (number == 0x60))
			return(sys->gfx.Read(number));
		else if((number >= 0xBA && number <= 0xBE) || (number >= 0xC4 && number <= 0xC8))
			return(sys->eeprom.Read(number));
		else if(number >= 0xCA && number <= 0xCB)
			return(sys->rtc.Read(number));
		else switch(number)
		{
			//default: printf("Read: %04x\n", number); break;
		case 0x40: return(DMASource >> 0);
		case 0x41: return(DMASource >> 8);
		case 0x42: return(DMASource >> 16);

		case 0x43: return(DMADest >> 16);
		case 0x44: return(DMADest >> 0);
		case 0x45: return(DMADest >> 8);

		case 0x46: return(DMALength >> 0);
		case 0x47: return(DMALength >> 8);

		case 0x48: return(DMAControl);

		case 0xB0:
		case 0xB2:
		case 0xB6: return(sys->interrupt.Read(number));

		case 0xC0: return(BankSelector[0] | 0x20);
		case 0xC1: return(BankSelector[1]);
		case 0xC2: return(BankSelector[2]);
		case 0xC3: return(BankSelector[3]);

		case 0x4a: return(SoundDMASource >> 0);
		case 0x4b: return(SoundDMASource >> 8);
		case 0x4c: return(SoundDMASource >> 16);
		case 0x4e: return(SoundDMALength >> 0);
		case 0x4f: return(SoundDMALength >> 8);
		case 0x52: return(SoundDMAControl);

		case 0xB1: return(CommData);

		case 0xb3: 
			{
				uint8 ret = CommControl & 0xf0;

				if(CommControl & 0x80)
					ret |= 0x4; // Send complete

				return(ret);
			}
		case 0xb5: 
			{
				Lagged = false;
				if (ButtonHook)
					ButtonHook();
				uint8 ret = (ButtonWhich << 4) | ButtonReadLatch;
				return(ret);
			}
		}

		if(number >= 0xC8)
			return language ? 0xD1 : 0xD0;
			//return(0xD0 | language); // is this right?

		return(0);
	}

	void Memory::writeport(uint32 IOPort, uint8 V)
	{
		IOPort &= 0xFF;

		if(IOPort >= 0x80 && IOPort <= 0x9F)
		{
			sys->sound.Write(IOPort, V);
		}
		else if((IOPort >= 0x00 && IOPort <= 0x3F) || (IOPort >= 0xA0 && IOPort <= 0xAF) || (IOPort == 0x60))
		{
			sys->gfx.Write(IOPort, V);
		}
		else if((IOPort >= 0xBA && IOPort <= 0xBE) || (IOPort >= 0xC4 && IOPort <= 0xC8))
			sys->eeprom.Write(IOPort, V);
		else if(IOPort >= 0xCA && IOPort <= 0xCB)
			sys->rtc.Write(IOPort, V);
		else switch(IOPort)
		{
			//default: printf("%04x %02x\n", IOPort, V); break;

		case 0x40: DMASource &= 0xFFFF00; DMASource |= (V << 0); break;
		case 0x41: DMASource &= 0xFF00FF; DMASource |= (V << 8); break;
		case 0x42: DMASource &= 0x00FFFF; DMASource |= ((V & 0x0F) << 16); break;

		case 0x43: DMADest &= 0x00FFFF; DMADest |= ((V & 0x0F) << 16); break;
		case 0x44: DMADest &= 0xFFFF00; DMADest |= (V << 0); break;
		case 0x45: DMADest &= 0xFF00FF; DMADest |= (V << 8); break;

		case 0x46: DMALength &= 0xFF00; DMALength |= (V << 0); break;
		case 0x47: DMALength &= 0x00FF; DMALength |= (V << 8); break;

		case 0x48: DMAControl = V;
			//if(V&0x80) 
			// printf("DMA%02x: %08x %08x %08x\n", V, DMASource, DMADest, DMALength); 
			CheckDMA(); 
			break;

		case 0x4a: SoundDMASource &= 0xFFFF00; SoundDMASource |= (V << 0); break;
		case 0x4b: SoundDMASource &= 0xFF00FF; SoundDMASource |= (V << 8); break;
		case 0x4c: SoundDMASource &= 0x00FFFF; SoundDMASource |= (V << 16); break;
			//case 0x4d: break; // Unused?
		case 0x4e: SoundDMALength &= 0xFF00; SoundDMALength |= (V << 0); break;
		case 0x4f: SoundDMALength &= 0x00FF; SoundDMALength |= (V << 8); break;
			//case 0x50: break; // Unused?
			//case 0x51: break; // Unused?
		case 0x52: SoundDMAControl = V; 
			//if(V & 0x80) printf("Sound DMA: %02x, %08x %08x\n", V, SoundDMASource, SoundDMALength);
			break;

		case 0xB0:
		case 0xB2:
		case 0xB6: sys->interrupt.Write(IOPort, V); break;

		case 0xB1: CommData = V; break;
		case 0xB3: CommControl = V & 0xF0; break;

		case 0xb5: ButtonWhich = V >> 4;
			// Lagged = false; // why was this being set here?
			ButtonReadLatch = 0;

			if(ButtonWhich & 0x4) /*buttons*/
				ButtonReadLatch |= ((WSButtonStatus >> 8) << 1) & 0xF;

			if(ButtonWhich & 0x2) /* H/X cursors */
				ButtonReadLatch |= WSButtonStatus & 0xF;

			if(ButtonWhich & 0x1) /* V/Y cursors */
				ButtonReadLatch |= (WSButtonStatus >> 4) & 0xF;
			break;

		case 0xC0: BankSelector[0] = V & 0xF; break;
		case 0xC1: BankSelector[1] = V; break;
		case 0xC2: BankSelector[2] = V; break;
		case 0xC3: BankSelector[3] = V; break;
		}
	}

	Memory::~Memory()
	{
		if (wsCartROM)
		{
			std::free(wsCartROM);
			wsCartROM = nullptr;
		}
		if (wsSRAM)
		{
			std::free(wsSRAM);
			wsSRAM = nullptr;
		}
	}
	
	void Memory::Init(const SyncSettings &settings)
	{
		char tmpname[17];
		std::memcpy(tmpname, settings.name, 16);
		tmpname[16] = 0;

		language = settings.language;

		// WSwan_EEPROMInit() will also clear wsEEPROM
		sys->eeprom.Init(tmpname, settings.byear, settings.bmonth, settings.bday, settings.sex, settings.blood);

		if(sram_size)
		{
			wsSRAM = (uint8*)malloc(sram_size);
			memset(wsSRAM, 0, sram_size);
		}
	}

	void Memory::Reset()
	{
		memset(wsRAM, 0, 65536);

		wsRAM[0x75AC] = 0x41;
		wsRAM[0x75AD] = 0x5F;
		wsRAM[0x75AE] = 0x43;
		wsRAM[0x75AF] = 0x31;
		wsRAM[0x75B0] = 0x6E;
		wsRAM[0x75B1] = 0x5F;
		wsRAM[0x75B2] = 0x63;
		wsRAM[0x75B3] = 0x31;

		std::memset(BankSelector, 0, sizeof(BankSelector));
		ButtonWhich = 0;
		ButtonReadLatch = 0;
		DMASource = 0;
		DMADest = 0;
		DMALength = 0;
		DMAControl = 0;

		SoundDMASource = 0;
		SoundDMALength = 0;
		SoundDMAControl = 0;

		CommControl = 0;
		CommData = 0;
	}

	SYNCFUNC(Memory)
	{
		NSS(wsRAM);
		//NSS(rom_size);
		//PSS(wsCartROM, rom_size);
		NSS(sram_size);
		PSS(wsSRAM, sram_size);

		NSS(WSButtonStatus);
		NSS(Lagged);

		NSS(ButtonWhich);
		NSS(ButtonReadLatch);

		NSS(DMASource);
		NSS(DMADest);
		NSS(DMALength);
		NSS(DMAControl);

		NSS(SoundDMASource);
		NSS(SoundDMALength);
		NSS(SoundDMAControl);

		NSS(BankSelector);

		NSS(CommControl);
		NSS(CommData);

		NSS(language);
	}
}
