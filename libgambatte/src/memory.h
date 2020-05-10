//
//   Copyright (C) 2007 by sinamas <sinamas at users.sourceforge.net>
//
//   This program is free software; you can redistribute it and/or modify
//   it under the terms of the GNU General Public License version 2 as
//   published by the Free Software Foundation.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License version 2 for more details.
//
//   You should have received a copy of the GNU General Public License
//   version 2 along with this program; if not, write to the
//   Free Software Foundation, Inc.,
//   51 Franklin St, Fifth Floor, Boston, MA  02110-1301, USA.
//

#ifndef MEMORY_H
#define MEMORY_H

static unsigned char const agbOverride[0xD] = { 0xFF, 0x00, 0xCD, 0x03, 0x35, 0xAA, 0x31, 0x90, 0x94, 0x00, 0x00, 0x00, 0x00 };

#include "mem/cartridge.h"
#include "interrupter.h"
#include "sound.h"
#include "tima.h"
#include "video.h"
#include "newstate.h"
#include "gambatte.h"

namespace gambatte {

class FilterInfo;

class Memory {
public:
	explicit Memory(Interrupter const &interrupter);
	~Memory();

	bool loaded() const { return cart_.loaded(); }
	unsigned curRomBank() const { return cart_.curRomBank(); }
	char const * romTitle() const { return cart_.romTitle(); }
	int debugGetLY() const { return lcd_.debugGetLY(); }
	void setStatePtrs(SaveState &state);
	void loadState(SaveState const &state);
	void loadSavedata(char const *data, unsigned long const cc) { cart_.loadSavedata(data, cc); }
	int saveSavedataLength() {return cart_.saveSavedataLength(); }
	void saveSavedata(char *dest, unsigned long const cc) { cart_.saveSavedata(dest, cc); }
	void updateInput();

	void setBios(char const *buffer, std::size_t size) {
		delete []bios_;
		bios_ = new unsigned char[size];
		memcpy(bios_, buffer, size);
		biosSize_ = size;
	}

	bool getMemoryArea(int which, unsigned char **data, int *length);

	unsigned long stop(unsigned long cycleCounter, bool& skip);
	bool isCgb() const { return lcd_.isCgb(); }
	bool isCgbDmg() const { return lcd_.isCgbDmg(); }
	bool ime() const { return intreq_.ime(); }
	bool halted() const { return intreq_.halted(); }
	unsigned long nextEventTime() const { return intreq_.minEventTime(); }
	void setLayers(unsigned mask) { lcd_.setLayers(mask); }
	bool isActive() const { return intreq_.eventTime(intevent_end) != disabled_time; }

	long cyclesSinceBlit(unsigned long cc) const {
		if (cc < intreq_.eventTime(intevent_blit))
			return -1;

		return (cc - intreq_.eventTime(intevent_blit)) >> isDoubleSpeed();
	}

	void freeze(unsigned long cc);
	bool halt(unsigned long cc);
	void ei(unsigned long cycleCounter) { if (!ime()) { intreq_.ei(cycleCounter); } }
	void di() { intreq_.di(); }

	unsigned pendingIrqs(unsigned long cc);
	void ackIrq(unsigned bit, unsigned long cc);

	unsigned readBios(unsigned p) {
		if(isCgb() && agbMode_ && p >= 0xF3 && p < 0x100) {
			return (agbOverride[p-0xF3] + bios_[p]) & 0xFF;
		}
		return bios_[p];
	}

	unsigned ff_read(unsigned p, unsigned long cc) {
		if (readCallback_)
			readCallback_(p, (cc - basetime_) >> 1);
		return p < 0x80 ? nontrivial_ff_read(p, cc) : ioamhram_[p + 0x100];
	}

	struct CDMapResult
	{
		eCDLog_AddrType type;
		unsigned addr;
	};

	CDMapResult CDMap(const unsigned p) const
	{
		if(p < 0x4000)
		{
			CDMapResult ret = { eCDLog_AddrType_ROM, p };
			return ret;
		}
		else if(p < 0x8000)
		{
			unsigned bank = cart_.rmem(p >> 12) - cart_.rmem(0);
			unsigned addr = p + bank;
			CDMapResult ret = { eCDLog_AddrType_ROM, addr };
			return ret;
		}
		else if(p < 0xA000) {}
		else if(p < 0xC000)
		{
			if(cart_.wsrambankptr())
			{
				//not bankable. but. we're not sure how much might be here
				unsigned char *data;
				int length;
				bool has = cart_.getMemoryArea(3,&data,&length);
				unsigned addr = p & (length-1);
				if(has && length!=0)
				{
					CDMapResult ret = { eCDLog_AddrType_CartRAM, addr };
					return ret;
				}
			}
		}
		else if(p < 0xE000)
		{
			unsigned bank = cart_.wramdata(p >> 12 & 1) - cart_.wramdata(0);
			unsigned addr = (p & 0xFFF) + bank;
			CDMapResult ret = { eCDLog_AddrType_WRAM, addr };
			return ret;
		}
		else if(p < 0xFF80) {}
		else
		{
			////this is just for debugging, really, it's pretty useless
			//CDMapResult ret = { eCDLog_AddrType_HRAM, (P-0xFF80) };
			//return ret;
		}

		CDMapResult ret = { eCDLog_AddrType_None };
		return ret;
	}

	unsigned read(unsigned p, unsigned long cc) {
		if (readCallback_)
			readCallback_(p, (cc - basetime_) >> 1);
		if(biosMode_) {
			if (p < biosSize_ && !(p >= 0x100 && p < 0x200))
				return readBios(p);
		}
		else if(cdCallback_) {
			CDMapResult map = CDMap(p);
			if(map.type != eCDLog_AddrType_None)
				cdCallback_(map.addr, map.type, eCDLog_Flags_Data);
		}
		return cart_.rmem(p >> 12) ? cart_.rmem(p >> 12)[p] : nontrivial_read(p, cc);
	}

	unsigned read_excb(unsigned p, unsigned long cc, bool first) {
		if (execCallback_)
			execCallback_(p, (cc - basetime_) >> 1);
		if (biosMode_) {
			if(p < biosSize_ && !(p >= 0x100 && p < 0x200))
				return readBios(p);
		}
		else if(cdCallback_) {
			CDMapResult map = CDMap(p);
			if(map.type != eCDLog_AddrType_None)
				cdCallback_(map.addr, map.type, first ? eCDLog_Flags_ExecFirst : eCDLog_Flags_ExecOperand);
		}
		return cart_.rmem(p >> 12) ? cart_.rmem(p >> 12)[p] : nontrivial_read(p, cc);
	}

	unsigned peek(unsigned p) {
		if (biosMode_ && p < biosSize_ && !(p >= 0x100 && p < 0x200)) {
			return readBios(p);
		}
		return cart_.rmem(p >> 12) ? cart_.rmem(p >> 12)[p] : nontrivial_peek(p);
	}

	void write_nocb(unsigned p, unsigned data, unsigned long cc) {
		if (cart_.wmem(p >> 12)) {
			cart_.wmem(p >> 12)[p] = data;
		} else
			nontrivial_write(p, data, cc);
	}

	void write(unsigned p, unsigned data, unsigned long cc) {
		if (cart_.wmem(p >> 12)) {
			cart_.wmem(p >> 12)[p] = data;
		} else
			nontrivial_write(p, data, cc);
		if (writeCallback_)
			writeCallback_(p, (cc - basetime_) >> 1);
		if(cdCallback_ && !biosMode_) {
			CDMapResult map = CDMap(p);
			if(map.type != eCDLog_AddrType_None)
				cdCallback_(map.addr, map.type, eCDLog_Flags_Data);
		}
	}

	void ff_write(unsigned p, unsigned data, unsigned long cc) {
		if (p - 0x80u < 0x7Fu) {
			ioamhram_[p + 0x100] = data;
		} else
			nontrivial_ff_write(p, data, cc);
		if (writeCallback_)
			writeCallback_(0xff00 + p, (cc - basetime_) >> 1);
		if(cdCallback_ && !biosMode_)
		{
			CDMapResult map = CDMap(0xff00 + p);
			if(map.type != eCDLog_AddrType_None)
				cdCallback_(map.addr, map.type, eCDLog_Flags_Data);
		}
	}

	unsigned long event(unsigned long cycleCounter);
	unsigned long resetCounters(unsigned long cycleCounter);
	LoadRes loadROM(char const *romfiledata, unsigned romfilelength, unsigned flags);

	void setInputGetter(unsigned (*getInput)()) {
		getInput_ = getInput;
	}

	void setReadCallback(MemoryCallback callback) {
		this->readCallback_ = callback;
	}
	void setWriteCallback(MemoryCallback callback) {
		this->writeCallback_ = callback;
	}
	void setExecCallback(MemoryCallback callback) {
		this->execCallback_ = callback;
	}
	void setCDCallback(CDCallback cdc) {
		this->cdCallback_ = cdc;
	}

	void setScanlineCallback(void (*callback)(), int sl) {
		lcd_.setScanlineCallback(callback, sl);
	}

	void setLinkCallback(void(*callback)()) {
		this->linkCallback_ = callback;
	}

	void setEndtime(unsigned long cc, unsigned long inc);
	void setBasetime(unsigned long cc) { basetime_ = cc; }

	void setSoundBuffer(uint_least32_t *buf) { psg_.setBuffer(buf); }
	std::size_t fillSoundBuffer(unsigned long cc);

	void setVideoBuffer(uint_least32_t *videoBuf, std::ptrdiff_t pitch) {
		lcd_.setVideoBuffer(videoBuf, pitch);
	}

	void setDmgPaletteColor(int palNum, int colorNum, unsigned long rgb32) {
		lcd_.setDmgPaletteColor(palNum, colorNum, rgb32);
	}

	void blackScreen() {
		lcd_.blackScreen();
	}

	void setCgbPalette(unsigned *lut);
	void setTimeMode(bool useCycles, unsigned long const cc) {
		cart_.setTimeMode(useCycles, cc);
	}
	void setRtcDivisorOffset(long const rtcDivisorOffset) { cart_.setRtcDivisorOffset(rtcDivisorOffset); }

	int linkStatus(int which);

private:
	Cartridge cart_;
	unsigned char ioamhram_[0x200];
	unsigned char *bios_;
	std::size_t biosSize_;
	unsigned (*getInput_)();
	unsigned long divLastUpdate_;
	unsigned long lastOamDmaUpdate_;
	InterruptRequester intreq_;
	Tima tima_;
	LCD lcd_;
	PSG psg_;
	Interrupter interrupter_;
	unsigned short dmaSource_;
	unsigned short dmaDestination_;
	unsigned char oamDmaPos_;
	unsigned char oamDmaStartPos_;
	unsigned char serialCnt_;
	bool blanklcd_;
	bool biosMode_;
	bool agbMode_;
	unsigned long basetime_;
	bool stopped_;
	enum HdmaState { hdma_low, hdma_high, hdma_requested } haltHdmaState_;

	MemoryCallback readCallback_;
	MemoryCallback writeCallback_;
	MemoryCallback execCallback_;
	CDCallback cdCallback_;
	void(*linkCallback_)();
	bool LINKCABLE_;
	bool linkClockTrigger_;

	void decEventCycles(IntEventId eventId, unsigned long dec);
	void oamDmaInitSetup();
	void updateOamDma(unsigned long cycleCounter);
	void startOamDma(unsigned long cycleCounter);
	void endOamDma(unsigned long cycleCounter);
	unsigned char const * oamDmaSrcPtr() const;
	unsigned long dma(unsigned long cc);
	unsigned nontrivial_ff_read(unsigned p, unsigned long cycleCounter);
	unsigned nontrivial_read(unsigned p, unsigned long cycleCounter);
	void nontrivial_ff_write(unsigned p, unsigned data, unsigned long cycleCounter);
	void nontrivial_write(unsigned p, unsigned data, unsigned long cycleCounter);
	unsigned nontrivial_peek(unsigned p);
	unsigned nontrivial_ff_peek(unsigned p);
	void updateSerial(unsigned long cc);
	void updateTimaIrq(unsigned long cc);
	void updateIrqs(unsigned long cc);
	bool isDoubleSpeed() const { return lcd_.isDoubleSpeed(); }

public:
	template<bool isReader>void SyncState(NewState *ns);
};

}

#endif
