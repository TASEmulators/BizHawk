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

namespace gambatte {

class CPU {
	Memory memory;
	
	unsigned long cycleCounter_;

	unsigned short PC_;
	unsigned short SP;
	
	unsigned HF1, HF2, ZF, CF;

	unsigned char A_, B, C, D, E, /*F,*/ H, L;

	bool skip;
	
	void process(unsigned long cycles);
	
	void (*tracecallback)(void *);

public:
	
	CPU();
// 	void halt();

// 	unsigned interrupt(unsigned address, unsigned cycleCounter);
	
	long runFor(unsigned long cycles);
	void setStatePtrs(SaveState &state);
	void saveState(SaveState &state);
	void loadState(const SaveState &state);
	
	void loadSavedata(const char *data) { memory.loadSavedata(data); }
	int saveSavedataLength() {return memory.saveSavedataLength(); }
	void saveSavedata(char *dest) { memory.saveSavedata(dest); }
	
	bool getMemoryArea(int which, unsigned char **data, int *length) { return memory.getMemoryArea(which, data, length); }

	void setVideoBuffer(uint_least32_t *const videoBuf, const int pitch) {
		memory.setVideoBuffer(videoBuf, pitch);
	}
	
	void setInputGetter(InputGetter *getInput) {
		memory.setInputGetter(getInput);
	}

	void setReadCallback(void (*callback)(unsigned)) {
		memory.setReadCallback(callback);
	}

	void setWriteCallback(void (*callback)(unsigned)) {
		memory.setWriteCallback(callback);
	}

	void setTraceCallback(void (*callback)(void *)) {
		tracecallback = callback;
	}

	void setScanlineCallback(void (*callback)(), int sl) {
		memory.setScanlineCallback(callback, sl);
	}

	void setRTCCallback(std::time_t (*callback)()) {
		memory.setRTCCallback(callback);
	}
	
	void setSaveDir(const std::string &sdir) {
		memory.setSaveDir(sdir);
	}
	
	const std::string saveBasePath() const {
		return memory.saveBasePath();
	}
	
	void setOsdElement(std::auto_ptr<OsdElement> osdElement) {
		memory.setOsdElement(osdElement);
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
	
	void setGameGenie(const std::string &codes) { memory.setGameGenie(codes); }
	void setGameShark(const std::string &codes) { memory.setGameShark(codes); }

	//unsigned char ExternalRead(unsigned short addr) { return memory.read(addr, cycleCounter_); }
	unsigned char ExternalRead(unsigned short addr) { return memory.peek(addr); }
	void ExternalWrite(unsigned short addr, unsigned char val) { memory.write(addr, val, cycleCounter_); }

	int LinkStatus(int which) { return memory.LinkStatus(which); }
};

}

#endif
