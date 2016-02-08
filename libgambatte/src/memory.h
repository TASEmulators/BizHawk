/***************************************************************************
 *   Copyright (C) 2007 by Sindre Aamås                                    *
 *   aamas@stud.ntnu.no                                                    *
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License version 2 as     *
 *   published by the Free Software Foundation.                            *
 *                                                                         *
 *   This program is distributed in the hope that it will be useful,       *
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 *   GNU General Public License version 2 for more details.                *
 *                                                                         *
 *   You should have received a copy of the GNU General Public License     *
 *   version 2 along with this program; if not, write to the               *
 *   Free Software Foundation, Inc.,                                       *
 *   59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.             *
 ***************************************************************************/
#ifndef MEMORY_H
#define MEMORY_H

#include "mem/cartridge.h"
#include "video.h"
#include "sound.h"
#include "interrupter.h"
#include "tima.h"
#include "newstate.h"
#include "gambatte.h"

namespace gambatte {
class InputGetter;
class FilterInfo;

class Memory {
	Cartridge cart;
	unsigned char ioamhram[0x200];
	
	void (*readCallback)(unsigned);
	void (*writeCallback)(unsigned);
	void (*execCallback)(unsigned);
	CDCallback cdCallback;

	unsigned (*getInput)();
	unsigned long divLastUpdate;
	unsigned long lastOamDmaUpdate;
	
	InterruptRequester intreq;
	Tima tima;
	LCD display;
	PSG sound;
	Interrupter interrupter;
	
	unsigned short dmaSource;
	unsigned short dmaDestination;
	unsigned char oamDmaPos;
	unsigned char serialCnt;
	bool blanklcd;

	bool LINKCABLE;
	bool linkClockTrigger;

	void decEventCycles(MemEventId eventId, unsigned long dec);

	void oamDmaInitSetup();
	void updateOamDma(unsigned long cycleCounter);
	void startOamDma(unsigned long cycleCounter);
	void endOamDma(unsigned long cycleCounter);
	const unsigned char * oamDmaSrcPtr() const;
	
	unsigned nontrivial_ff_read(unsigned P, unsigned long cycleCounter);
	unsigned nontrivial_read(unsigned P, unsigned long cycleCounter);
	void nontrivial_ff_write(unsigned P, unsigned data, unsigned long cycleCounter);
	void nontrivial_write(unsigned P, unsigned data, unsigned long cycleCounter);
	
	unsigned nontrivial_peek(unsigned P);
	unsigned nontrivial_ff_peek(unsigned P);

	void updateSerial(unsigned long cc);
	void updateTimaIrq(unsigned long cc);
	void updateIrqs(unsigned long cc);
	
	bool isDoubleSpeed() const { return display.isDoubleSpeed(); }

public:
	explicit Memory(const Interrupter &interrupter);
	
	bool loaded() const { return cart.loaded(); }
	const char * romTitle() const { return cart.romTitle(); }

	int debugGetLY() const { return display.debugGetLY(); }

	void setStatePtrs(SaveState &state);
	void loadState(const SaveState &state/*, unsigned long oldCc*/);
	void loadSavedata(const char *data) { cart.loadSavedata(data); }
	int saveSavedataLength() {return cart.saveSavedataLength(); }
	void saveSavedata(char *dest) { cart.saveSavedata(dest); }
	void updateInput();

	bool getMemoryArea(int which, unsigned char **data, int *length); // { return cart.getMemoryArea(which, data, length); }

	unsigned long stop(unsigned long cycleCounter);
	bool isCgb() const { return display.isCgb(); }
	bool ime() const { return intreq.ime(); }
	bool halted() const { return intreq.halted(); }
	unsigned long nextEventTime() const { return intreq.minEventTime(); }

	void setLayers(unsigned mask) { display.setLayers(mask); }
	
	bool isActive() const { return intreq.eventTime(END) != DISABLED_TIME; }
	
	long cyclesSinceBlit(const unsigned long cc) const {
		return cc < intreq.eventTime(BLIT) ? -1 : static_cast<long>((cc - intreq.eventTime(BLIT)) >> isDoubleSpeed());
	}

	void halt() { intreq.halt(); }
	void ei(unsigned long cycleCounter) { if (!ime()) { intreq.ei(cycleCounter); } }

	void di() { intreq.di(); }

	unsigned ff_read(const unsigned P, const unsigned long cycleCounter) {
		return P < 0xFF80 ? nontrivial_ff_read(P, cycleCounter) : ioamhram[P - 0xFE00];
	}

	struct CDMapResult
	{
		eCDLog_AddrType type;
		unsigned addr;
	};

	CDMapResult CDMap(const unsigned P) const
	{
		if(P<0x4000)
		{
			CDMapResult ret = { eCDLog_AddrType_ROM, P };
			return ret;
		}
		else if(P<0x8000) 
		{
			unsigned bank = cart.rmem(P>>12) - cart.rmem(0);
			unsigned addr = P+bank;
			CDMapResult ret = { eCDLog_AddrType_ROM, addr };
			return ret;
		}
		else if(P<0xA000) {}
		else if(P<0xC000)
		{
			if(cart.wsrambankptr())
			{
				//not bankable. but. we're not sure how much might be here
				unsigned char *data;
				int length;
				bool has = cart.getMemoryArea(3,&data,&length);
				unsigned addr = P&(length-1);
				if(has && length!=0)
				{
					CDMapResult ret = { eCDLog_AddrType_CartRAM, addr };
					return ret;
				}
			}
		}
		else if(P<0xE000)
		{
			unsigned bank = cart.wramdata(P >> 12 & 1) - cart.wramdata(0);
			unsigned addr = (P&0xFFF)+bank;
			CDMapResult ret = { eCDLog_AddrType_WRAM, addr };
			return ret;
		}
		else if(P<0xFF80) {}
		else 
		{
			////this is just for debugging, really, it's pretty useless
			//CDMapResult ret = { eCDLog_AddrType_HRAM, (P-0xFF80) };
			//return ret;
		}

		CDMapResult ret = { eCDLog_AddrType_None };
		return ret;
	}


	unsigned read(const unsigned P, const unsigned long cycleCounter) {
		if (readCallback)
			readCallback(P);
		if(cdCallback)
		{
			CDMapResult map = CDMap(P);
			if(map.type != eCDLog_AddrType_None)
				cdCallback(map.addr,map.type,eCDLog_Flags_Data);
		}
		return cart.rmem(P >> 12) ? cart.rmem(P >> 12)[P] : nontrivial_read(P, cycleCounter);
	}

	unsigned read_excb(const unsigned P, const unsigned long cycleCounter, bool first) {
		if (execCallback)
			execCallback(P);
		if(cdCallback)
		{
			CDMapResult map = CDMap(P);
			if(map.type != eCDLog_AddrType_None)
				cdCallback(map.addr,map.type,first?eCDLog_Flags_ExecFirst : eCDLog_Flags_ExecOperand);
		}
		return cart.rmem(P >> 12) ? cart.rmem(P >> 12)[P] : nontrivial_read(P, cycleCounter);
	}

	unsigned peek(const unsigned P) {
		return cart.rmem(P >> 12) ? cart.rmem(P >> 12)[P] : nontrivial_peek(P);
	}

	void write_nocb(const unsigned P, const unsigned data, const unsigned long cycleCounter) {
		if (cart.wmem(P >> 12)) {
			cart.wmem(P >> 12)[P] = data;
		} else
			nontrivial_write(P, data, cycleCounter);
	}
	
	void write(const unsigned P, const unsigned data, const unsigned long cycleCounter) {
		if (cart.wmem(P >> 12)) {
			cart.wmem(P >> 12)[P] = data;
		} else
			nontrivial_write(P, data, cycleCounter);
		if (writeCallback)
			writeCallback(P);
		if(cdCallback)
		{
			CDMapResult map = CDMap(P);
			if(map.type != eCDLog_AddrType_None)
				cdCallback(map.addr,map.type,eCDLog_Flags_Data);
		}
	}
	
	void ff_write(const unsigned P, const unsigned data, const unsigned long cycleCounter) {
		if (P - 0xFF80u < 0x7Fu) {
			ioamhram[P - 0xFE00] = data;
		} else
			nontrivial_ff_write(P, data, cycleCounter);
		if(cdCallback)
		{
			CDMapResult map = CDMap(P);
			if(map.type != eCDLog_AddrType_None)
				cdCallback(map.addr,map.type,eCDLog_Flags_Data);
		}
	}

	unsigned long event(unsigned long cycleCounter);
	unsigned long resetCounters(unsigned long cycleCounter);

	int loadROM(const char *romfiledata, unsigned romfilelength, bool forceDmg, bool multicartCompat);

	void setInputGetter(unsigned (*getInput)()) {
		this->getInput = getInput;
	}

	void setReadCallback(void (*callback)(unsigned)) {
		this->readCallback = callback;
	}
	void setWriteCallback(void (*callback)(unsigned)) {
		this->writeCallback = callback;
	}
	void setExecCallback(void (*callback)(unsigned)) {
		this->execCallback = callback;
	}
	void setCDCallback(CDCallback cdc) {
		this->cdCallback = cdc;
	}

	void setScanlineCallback(void (*callback)(), int sl) {
		display.setScanlineCallback(callback, sl);
	}

	void setRTCCallback(std::uint32_t (*callback)()) {
		cart.setRTCCallback(callback);
	}

	void setEndtime(unsigned long cc, unsigned long inc);
	
	void setSoundBuffer(uint_least32_t *const buf) { sound.setBuffer(buf); }
	unsigned fillSoundBuffer(unsigned long cc);
	
	void setVideoBuffer(uint_least32_t *const videoBuf, const int pitch) {
		display.setVideoBuffer(videoBuf, pitch);
	}
	
	void setDmgPaletteColor(unsigned palNum, unsigned colorNum, unsigned long rgb32);
	void setCgbPalette(unsigned *lut);

	int LinkStatus(int which);

	template<bool isReader>void SyncState(NewState *ns);
};

}

#endif
