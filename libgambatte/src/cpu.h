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
#ifndef CPU_H
#define CPU_H

#include "memory.h"
#include "newstate.h"

namespace gambatte {

class CPU {
	Memory memory;
	
	unsigned long cycleCounter_;

	unsigned short PC;
	unsigned short SP;
	
	unsigned HF1, HF2, ZF, CF;

	unsigned char A, B, C, D, E, /*F,*/ H, L;

	bool skip;
	
	int *interruptAddresses;
	int numInterruptAddresses;
	int hitInterruptAddress;

	void process(unsigned long cycles);
	
	void (*tracecallback)(void *);

public:
	
	CPU();
// 	void halt();

// 	unsigned interrupt(unsigned address, unsigned cycleCounter);
	
	long runFor(unsigned long cycles);
	void setStatePtrs(SaveState &state);
	void loadState(const SaveState &state);
	void setLayers(unsigned mask) { memory.setLayers(mask); }
	
	void loadSavedata(const char *data) { memory.loadSavedata(data); }
	int saveSavedataLength() {return memory.saveSavedataLength(); }
	void saveSavedata(char *dest) { memory.saveSavedata(dest); }
	
	bool getMemoryArea(int which, unsigned char **data, int *length) { return memory.getMemoryArea(which, data, length); }

	void setVideoBuffer(uint_least32_t *const videoBuf, const int pitch) {
		memory.setVideoBuffer(videoBuf, pitch);
	}
	
	void setInputGetter(unsigned (*getInput)()) {
		memory.setInputGetter(getInput);
	}

	void setReadCallback(MemoryCallback callback) {
		memory.setReadCallback(callback);
	}

	void setWriteCallback(MemoryCallback callback) {
		memory.setWriteCallback(callback);
	}

	void setExecCallback(MemoryCallback callback) {
		memory.setExecCallback(callback);
	}

	void setCDCallback(CDCallback cdc) {
		memory.setCDCallback(cdc);
	}

	void setTraceCallback(void (*callback)(void *)) {
		tracecallback = callback;
	}

	void setScanlineCallback(void (*callback)(), int sl) {
		memory.setScanlineCallback(callback, sl);
	}

	void setRTCCallback(std::uint32_t (*callback)()) {
		memory.setRTCCallback(callback);
	}

	void setLinkCallback(void(*callback)()) {
		memory.setLinkCallback(callback);
	}
	
	int load(const char *romfiledata, unsigned romfilelength, bool forceDmg, bool multicartCompat) {
		return memory.loadROM(romfiledata, romfilelength, forceDmg, multicartCompat);
	}
	
	bool loaded() const { return memory.loaded(); }
	const char * romTitle() const { return memory.romTitle(); }
	
	void setSoundBuffer(uint_least32_t *const buf) { memory.setSoundBuffer(buf); }
	unsigned fillSoundBuffer() { return memory.fillSoundBuffer(cycleCounter_); }
	
	bool isCgb() const { return memory.isCgb(); }
	
	void setDmgPaletteColor(unsigned palNum, unsigned colorNum, unsigned rgb32) {
		memory.setDmgPaletteColor(palNum, colorNum, rgb32);
	}

	void setCgbPalette(unsigned *lut) {
		memory.setCgbPalette(lut);
	}
	
	unsigned char* cgbBiosBuffer() { return memory.cgbBiosBuffer(); }
	unsigned char* dmgBiosBuffer() { return memory.dmgBiosBuffer(); }
	bool gbIsCgb() { return memory.gbIsCgb(); }

	//unsigned char ExternalRead(unsigned short addr) { return memory.read(addr, cycleCounter_); }
	unsigned char ExternalRead(unsigned short addr) { return memory.peek(addr); }
	void ExternalWrite(unsigned short addr, unsigned char val) { memory.write_nocb(addr, val, cycleCounter_); }

	int LinkStatus(int which) { return memory.LinkStatus(which); }

	void GetRegs(int *dest);

	void SetInterruptAddresses(int *addrs, int numAddrs);
	int GetHitInterruptAddress();

	template<bool isReader>void SyncState(NewState *ns);
};

}

#endif
