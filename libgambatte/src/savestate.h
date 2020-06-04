//
//   Copyright (C) 2008 by sinamas <sinamas at users.sourceforge.net>
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

#ifndef SAVESTATE_H
#define SAVESTATE_H

#include <cstddef>
#include <cstdint>

namespace gambatte {

class SaverList;

struct SaveState {
	template<typename T>
	class Ptr {
	public:
		Ptr() : ptr(0), size_(0) {}
		T const * get() const { return ptr; }
		std::size_t size() const { return size_; }
		void set(T *p, std::size_t size) { ptr = p; size_ = size; }

		friend class SaverList;
		friend void setInitState(SaveState &, bool);
	private:
		T *ptr;
		std::size_t size_;
	};

	struct CPU {
		unsigned long cycleCounter;
		unsigned short pc;
		unsigned short sp;
		unsigned char a;
		unsigned char b;
		unsigned char c;
		unsigned char d;
		unsigned char e;
		unsigned char f;
		unsigned char h;
		unsigned char l;
		unsigned char opcode;
		unsigned char /*bool*/ prefetched;
		unsigned char /*bool*/ skip;
	} cpu;

	struct Mem {
		Ptr<unsigned char> vram;
		Ptr<unsigned char> sram;
		Ptr<unsigned char> wram;
		Ptr<unsigned char> ioamhram;
		unsigned long divLastUpdate;
		unsigned long timaLastUpdate;
		unsigned long tmatime;
		unsigned long nextSerialtime;
		unsigned long lastOamDmaUpdate;
		unsigned long minIntTime;
		unsigned long unhaltTime;
		unsigned short rombank;
		unsigned short dmaSource;
		unsigned short dmaDestination;
		unsigned char rambank;
		unsigned char oamDmaPos;
		unsigned char haltHdmaState;
		unsigned char HuC3RAMflag;
		unsigned char /*bool*/ IME;
		unsigned char /*bool*/ halted;
		unsigned char /*bool*/ enableRam;
		unsigned char /*bool*/ rambankMode;
		unsigned char /*bool*/ hdmaTransfer;
		unsigned char /*bool*/ biosMode;
		unsigned char /*bool*/ stopped;
	} mem;

	struct PPU {
		Ptr<unsigned char> bgpData;
		Ptr<unsigned char> objpData;
		//SpriteMapper::OamReader
		Ptr<unsigned char> oamReaderBuf;
		Ptr<bool> oamReaderSzbuf;

		unsigned long videoCycles;
		unsigned long enableDisplayM0Time;
		unsigned short lastM0Time;
		unsigned short nextM0Irq;
		unsigned short tileword;
		unsigned short ntileword;
		unsigned char spAttribList[10];
		unsigned char spByte0List[10];
		unsigned char spByte1List[10];
		unsigned char winYPos;
		unsigned char xpos;
		unsigned char endx;
		unsigned char reg0;
		unsigned char reg1;
		unsigned char attrib;
		unsigned char nattrib;
		unsigned char state;
		unsigned char nextSprite;
		unsigned char currentSprite;
		unsigned char lyc;
		unsigned char m0lyc;
		unsigned char oldWy;
		unsigned char winDrawState;
		unsigned char wscx;
		unsigned char /*bool*/ weMaster;
		unsigned char /*bool*/ pendingLcdstatIrq;
		unsigned char /*bool*/ notCgbDmg;
	} ppu;

	struct SPU {
		struct Duty {
			unsigned long nextPosUpdate;
			unsigned char nr3;
			unsigned char pos;
			unsigned char /*bool*/ high;
		};

		struct Env {
			unsigned long counter;
			unsigned char volume;
		};

		struct LCounter {
			unsigned long counter;
			unsigned short lengthCounter;
		};

		struct {
			struct {
				unsigned long counter;
				unsigned short shadow;
				unsigned char nr0;
				unsigned char /*bool*/ neg;
			} sweep;
			Duty duty;
			Env env;
			LCounter lcounter;
			unsigned char nr4;
			unsigned char /*bool*/ master;
		} ch1;

		struct {
			Duty duty;
			Env env;
			LCounter lcounter;
			unsigned char nr4;
			unsigned char /*bool*/ master;
		} ch2;

		struct {
			Ptr<unsigned char> waveRam;
			LCounter lcounter;
			unsigned long waveCounter;
			unsigned long lastReadTime;
			unsigned char nr3;
			unsigned char nr4;
			unsigned char wavePos;
			unsigned char sampleBuf;
			unsigned char /*bool*/ master;
		} ch3;

		struct {
			struct {
				unsigned long counter;
				unsigned short reg;
			} lfsr;
			Env env;
			LCounter lcounter;
			unsigned char nr4;
			unsigned char /*bool*/ master;
		} ch4;

		unsigned long cycleCounter;
		unsigned char lastUpdate;
	} spu;

	struct Time {
		unsigned long seconds;
		unsigned long lastTimeSec;
		unsigned long lastTimeUsec;
		unsigned long lastCycles;
	} time;

	struct RTC {
		unsigned long haltTime;
		unsigned char dataDh;
		unsigned char dataDl;
		unsigned char dataH;
		unsigned char dataM;
		unsigned char dataS;
		unsigned char /*bool*/ lastLatchData;
	} rtc;

	struct HuC3 {
		unsigned long haltTime;
		unsigned long dataTime;
		unsigned long writingTime;
		unsigned long irBaseCycle;
		unsigned char /*bool*/ halted;
		unsigned char shift;
		unsigned char ramValue;
		unsigned char modeflag;
		unsigned char /*bool*/ irReceivingPulse;
	} huc3;
};

}

#endif
