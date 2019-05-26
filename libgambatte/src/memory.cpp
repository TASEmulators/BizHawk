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
#include "memory.h"
#include "video.h"
#include "sound.h"
#include "savestate.h"
#include <cstring>

namespace gambatte {

Memory::Memory(const Interrupter &interrupter_in, unsigned short &sp, unsigned short &pc)
: readCallback(0),
  writeCallback(0),
  execCallback(0),
  cdCallback(0),
  linkCallback(0),
  getInput(0),
  divLastUpdate(0),
  lastOamDmaUpdate(disabled_time),
  display(ioamhram, 0, VideoInterruptRequester(intreq)),
  interrupter(interrupter_in),
  dmaSource(0),
  dmaDestination(0),
  oamDmaPos(0xFE),
  serialCnt(0),
  blanklcd(false),
  LINKCABLE(false),
  linkClockTrigger(false),
  SP(sp),
  PC(pc)
{
	intreq.setEventTime<intevent_blit>(144*456ul);
	intreq.setEventTime<intevent_end>(0);
}

void Memory::setStatePtrs(SaveState &state) {
	state.mem.ioamhram.set(ioamhram, sizeof ioamhram);

	cart.setStatePtrs(state);
	display.setStatePtrs(state);
	sound.setStatePtrs(state);
}


static inline int serialCntFrom(const unsigned long cyclesUntilDone, const bool cgbFast) {
	return cgbFast ? (cyclesUntilDone + 0xF) >> 4 : (cyclesUntilDone + 0x1FF) >> 9;
}

void Memory::loadState(const SaveState &state) {
	biosMode = state.mem.biosMode;
	cgbSwitching = state.mem.cgbSwitching;
	agbMode = state.mem.agbMode;
	gbIsCgb_ = state.mem.gbIsCgb;
	sound.loadState(state);
	display.loadState(state, state.mem.oamDmaPos < 0xA0 ? cart.rdisabledRam() : ioamhram);
	tima.loadState(state, TimaInterruptRequester(intreq));
	cart.loadState(state);
	intreq.loadState(state);

	divLastUpdate = state.mem.divLastUpdate;
	intreq.setEventTime<intevent_serial>(state.mem.nextSerialtime > state.cpu.cycleCounter ? state.mem.nextSerialtime : state.cpu.cycleCounter);
	intreq.setEventTime<intevent_unhalt>(state.mem.unhaltTime);
	lastOamDmaUpdate = state.mem.lastOamDmaUpdate;
	dmaSource = state.mem.dmaSource;
	dmaDestination = state.mem.dmaDestination;
	oamDmaPos = state.mem.oamDmaPos;
	serialCnt = intreq.eventTime(intevent_serial) != disabled_time
			? serialCntFrom(intreq.eventTime(intevent_serial) - state.cpu.cycleCounter, ioamhram[0x102] & isCgb() * 2)
			: 8;

	cart.setVrambank(ioamhram[0x14F] & isCgb());
	cart.setOamDmaSrc(oam_dma_src_off);
	cart.setWrambank(isCgb() && (ioamhram[0x170] & 0x07) ? ioamhram[0x170] & 0x07 : 1);

	if (lastOamDmaUpdate != disabled_time) {
		oamDmaInitSetup();

		const unsigned oamEventPos = oamDmaPos < 0xA0 ? 0xA0 : 0x100;

		intreq.setEventTime<intevent_oam>(lastOamDmaUpdate + (oamEventPos - oamDmaPos) * 4);
	}

	intreq.setEventTime<intevent_blit>((ioamhram[0x140] & 0x80) ? display.nextMode1IrqTime() : state.cpu.cycleCounter);
	blanklcd = false;

	if (!isCgb())
		std::memset(cart.vramdata() + 0x2000, 0, 0x2000);
}

void Memory::setEndtime(const unsigned long cycleCounter, const unsigned long inc) {
	if (intreq.eventTime(intevent_blit) <= cycleCounter)
		intreq.setEventTime<intevent_blit>(intreq.eventTime(intevent_blit) + (70224 << isDoubleSpeed()));

	intreq.setEventTime<intevent_end>(cycleCounter + (inc << isDoubleSpeed()));
}

void Memory::updateSerial(const unsigned long cc) {
	if (!LINKCABLE) {
		if (intreq.eventTime(intevent_serial) != disabled_time) {
			if (intreq.eventTime(intevent_serial) <= cc) {
				ioamhram[0x101] = (((ioamhram[0x101] + 1) << serialCnt) - 1) & 0xFF;
				ioamhram[0x102] &= 0x7F;
				intreq.setEventTime<intevent_serial>(disabled_time);
				intreq.flagIrq(8);
			} else {
				const int targetCnt = serialCntFrom(intreq.eventTime(intevent_serial) - cc, ioamhram[0x102] & isCgb() * 2);
				ioamhram[0x101] = (((ioamhram[0x101] + 1) << (serialCnt - targetCnt)) - 1) & 0xFF;
				serialCnt = targetCnt;
			}
		}
	}
	else {
		if (intreq.eventTime(intevent_serial) != disabled_time) {
			if (intreq.eventTime(intevent_serial) <= cc) {
				linkClockTrigger = true;
				intreq.setEventTime<intevent_serial>(disabled_time);
				if (linkCallback)
					linkCallback();
			}
		}
	}
}

void Memory::updateTimaIrq(const unsigned long cc) {
	while (intreq.eventTime(intevent_tima) <= cc)
		tima.doIrqEvent(TimaInterruptRequester(intreq));
}

void Memory::updateIrqs(const unsigned long cc) {
	updateSerial(cc);
	updateTimaIrq(cc);
	display.update(cc);
}

unsigned long Memory::event(unsigned long cycleCounter) {
	if (lastOamDmaUpdate != disabled_time)
		updateOamDma(cycleCounter);

	switch (intreq.minEventId()) {
	case intevent_unhalt:
		nontrivial_ff_write(0xFF04, 0, cycleCounter);
		PC = (PC + 1) & 0xFFFF;
		cycleCounter += 4;
		intreq.unhalt();
		intreq.setEventTime<intevent_unhalt>(disabled_time);
		break;
	case intevent_end:
		intreq.setEventTime<intevent_end>(disabled_time - 1);

		while (cycleCounter >= intreq.minEventTime() && intreq.eventTime(intevent_end) != disabled_time)
			cycleCounter = event(cycleCounter);

		intreq.setEventTime<intevent_end>(disabled_time);

		break;
	case intevent_blit:
		{
			const bool lcden = ioamhram[0x140] >> 7 & 1;
			unsigned long blitTime = intreq.eventTime(intevent_blit);

			if (lcden | blanklcd) {
				display.updateScreen(blanklcd, cycleCounter);
				intreq.setEventTime<intevent_blit>(disabled_time);
				intreq.setEventTime<intevent_end>(disabled_time);

				while (cycleCounter >= intreq.minEventTime())
					cycleCounter = event(cycleCounter);
			} else
				blitTime += 70224 << isDoubleSpeed();

			blanklcd = lcden ^ 1;
			intreq.setEventTime<intevent_blit>(blitTime);
		}
		break;
	case intevent_serial:
		updateSerial(cycleCounter);
		break;
	case intevent_oam:
		intreq.setEventTime<intevent_oam>(lastOamDmaUpdate == disabled_time ?
				static_cast<unsigned long>(disabled_time) : intreq.eventTime(intevent_oam) + 0xA0 * 4);
		break;
	case intevent_dma:
		{
			const bool doubleSpeed = isDoubleSpeed();
			unsigned dmaSrc = dmaSource;
			unsigned dmaDest = dmaDestination;
			unsigned dmaLength = ((ioamhram[0x155] & 0x7F) + 0x1) * 0x10;
			unsigned length = hdmaReqFlagged(intreq) ? 0x10 : dmaLength;

			ackDmaReq(intreq);

			if ((static_cast<unsigned long>(dmaDest) + length) & 0x10000) {
				length = 0x10000 - dmaDest;
				ioamhram[0x155] |= 0x80;
			}

			dmaLength -= length;

			if (!(ioamhram[0x140] & 0x80))
				dmaLength = 0;

			{
				unsigned long lOamDmaUpdate = lastOamDmaUpdate;
				lastOamDmaUpdate = disabled_time;

				while (length--) {
					const unsigned src = dmaSrc++ & 0xFFFF;
					const unsigned data = ((src & 0xE000) == 0x8000 || src > 0xFDFF) ? 0xFF : read(src, cycleCounter);

					cycleCounter += 2 << doubleSpeed;

					if (cycleCounter - 3 > lOamDmaUpdate) {
						oamDmaPos = (oamDmaPos + 1) & 0xFF;
						lOamDmaUpdate += 4;

						if (oamDmaPos < 0xA0) {
							if (oamDmaPos == 0)
								startOamDma(lOamDmaUpdate - 1);

							ioamhram[src & 0xFF] = data;
						} else if (oamDmaPos == 0xA0) {
							endOamDma(lOamDmaUpdate - 1);
							lOamDmaUpdate = disabled_time;
						}
					}

					nontrivial_write(0x8000 | (dmaDest++ & 0x1FFF), data, cycleCounter);
				}

				lastOamDmaUpdate = lOamDmaUpdate;
			}

			cycleCounter += 4;

			dmaSource = dmaSrc;
			dmaDestination = dmaDest;
			ioamhram[0x155] = ((dmaLength / 0x10 - 0x1) & 0xFF) | (ioamhram[0x155] & 0x80);

			if ((ioamhram[0x155] & 0x80) && display.hdmaIsEnabled()) {
				if (lastOamDmaUpdate != disabled_time)
					updateOamDma(cycleCounter);

				display.disableHdma(cycleCounter);
			}
		}

		break;
	case intevent_tima:
		tima.doIrqEvent(TimaInterruptRequester(intreq));
		break;
	case intevent_video:
		display.update(cycleCounter);
		break;
	case intevent_interrupts:
		if (stopped) {
			intreq.setEventTime<intevent_interrupts>(disabled_time);
			break;
		}
		if (halted()) {
			if (gbIsCgb_ || (!gbIsCgb_ && cycleCounter <= halttime + 4))
				cycleCounter += 4;

			intreq.unhalt();
			intreq.setEventTime<intevent_unhalt>(disabled_time);
		}

		if (ime()) {
			unsigned address;

			cycleCounter += 12;
			display.update(cycleCounter);
			SP = (SP - 2) & 0xFFFF;
			write(SP + 1, PC >> 8, cycleCounter);
			unsigned ie = intreq.iereg();

			cycleCounter += 4;
			display.update(cycleCounter);
			write(SP, PC & 0xFF, cycleCounter);
			const unsigned pendingIrqs = ie & intreq.ifreg();

			cycleCounter += 4;
			display.update(cycleCounter);
			const unsigned n = pendingIrqs & -pendingIrqs;

			if (n == 0) {
				address = 0;
			}
			else if (n < 8) {
				static const unsigned char lut[] = { 0x40, 0x48, 0x48, 0x50 };
				address = lut[n-1];
			} else
				address = 0x50 + n;

			intreq.ackIrq(n);
			PC = address;
		}

		break;
	}

	return cycleCounter;
}

unsigned long Memory::stop(unsigned long cycleCounter) {
	cycleCounter += 4;

	if (ioamhram[0x14D] & isCgb()) {
		sound.generateSamples(cycleCounter, isDoubleSpeed());

		display.speedChange(cycleCounter);
		ioamhram[0x14D] ^= 0x81;

		intreq.setEventTime<intevent_blit>((ioamhram[0x140] & 0x80) ? display.nextMode1IrqTime() : cycleCounter + (70224 << isDoubleSpeed()));

		if (intreq.eventTime(intevent_end) > cycleCounter) {
			intreq.setEventTime<intevent_end>(cycleCounter + (isDoubleSpeed() ?
					(intreq.eventTime(intevent_end) - cycleCounter) << 1 : (intreq.eventTime(intevent_end) - cycleCounter) >> 1));
		}
		// when switching speed, it seems that the CPU spontaneously restarts soon?
		// otherwise, the cpu should be allowed to stay halted as long as needed
		// so only execute this line when switching speed
		intreq.halt();
		intreq.setEventTime<intevent_unhalt>(cycleCounter + 0x20000);
	}
	else {
		stopped = true;
		intreq.halt();
	}

	return cycleCounter;
}

static void decCycles(unsigned long &counter, const unsigned long dec) {
	if (counter != disabled_time)
		counter -= dec;
}

void Memory::decEventCycles(const IntEventId eventId, const unsigned long dec) {
	if (intreq.eventTime(eventId) != disabled_time)
		intreq.setEventTime(eventId, intreq.eventTime(eventId) - dec);
}

unsigned long Memory::resetCounters(unsigned long cycleCounter) {
	if (lastOamDmaUpdate != disabled_time)
		updateOamDma(cycleCounter);

	updateIrqs(cycleCounter);

	const unsigned long oldCC = cycleCounter;

	{
		const unsigned long divinc = (cycleCounter - divLastUpdate) >> 8;
		ioamhram[0x104] = (ioamhram[0x104] + divinc) & 0xFF;
		divLastUpdate += divinc << 8;
	}

	const unsigned long dec = cycleCounter < 0x10000 ? 0 : (cycleCounter & ~0x7FFFul) - 0x8000;

	decCycles(divLastUpdate, dec);
	decCycles(lastOamDmaUpdate, dec);
	decEventCycles(intevent_serial, dec);
	decEventCycles(intevent_oam, dec);
	decEventCycles(intevent_blit, dec);
	decEventCycles(intevent_end, dec);
	decEventCycles(intevent_unhalt, dec);

	cycleCounter -= dec;

	intreq.resetCc(oldCC, cycleCounter);
	tima.resetCc(oldCC, cycleCounter, TimaInterruptRequester(intreq));
	display.resetCc(oldCC, cycleCounter);
	sound.resetCounter(cycleCounter, oldCC, isDoubleSpeed());

	return cycleCounter;
}

void Memory::updateInput() {
	unsigned state = 0xF;

	if ((ioamhram[0x100] & 0x30) != 0x30 && getInput) {
		unsigned input = (*getInput)();
		unsigned dpad_state = ~input >> 4;
		unsigned button_state = ~input;
		if (!(ioamhram[0x100] & 0x10))
			state &= dpad_state;
		if (!(ioamhram[0x100] & 0x20))
			state &= button_state;
	}

	if (state != 0xF && (ioamhram[0x100] & 0xF) == 0xF)
		intreq.flagIrq(0x10);

	ioamhram[0x100] = (ioamhram[0x100] & -0x10u) | state;
}

void Memory::updateOamDma(const unsigned long cycleCounter) {
	const unsigned char *const oamDmaSrc = oamDmaSrcPtr();
	unsigned cycles = (cycleCounter - lastOamDmaUpdate) >> 2;

	while (cycles--) {
		oamDmaPos = (oamDmaPos + 1) & 0xFF;
		lastOamDmaUpdate += 4;

		if (oamDmaPos < 0xA0) {
			if (oamDmaPos == 0)
				startOamDma(lastOamDmaUpdate - 1);

			ioamhram[oamDmaPos] = oamDmaSrc ? oamDmaSrc[oamDmaPos] : cart.rtcRead();
		} else if (oamDmaPos == 0xA0) {
			endOamDma(lastOamDmaUpdate - 1);
			lastOamDmaUpdate = disabled_time;
			break;
		}
	}
}

void Memory::oamDmaInitSetup() {
	if (ioamhram[0x146] < 0xA0) {
		cart.setOamDmaSrc(ioamhram[0x146] < 0x80 ? oam_dma_src_rom : oam_dma_src_vram);
	} else if (ioamhram[0x146] < 0xFE - isCgb() * 0x1E) {
		cart.setOamDmaSrc(ioamhram[0x146] < 0xC0 ? oam_dma_src_sram : oam_dma_src_wram);
	} else
		cart.setOamDmaSrc(oam_dma_src_invalid);
}

static const unsigned char * oamDmaSrcZero() {
	static unsigned char zeroMem[0xA0];
	return zeroMem;
}

const unsigned char * Memory::oamDmaSrcPtr() const {
	switch (cart.oamDmaSrc()) {
	case oam_dma_src_rom:  return cart.romdata(ioamhram[0x146] >> 6) + (ioamhram[0x146] << 8);
	case oam_dma_src_sram: return cart.rsrambankptr() ? cart.rsrambankptr() + (ioamhram[0x146] << 8) : 0;
	case oam_dma_src_vram: return cart.vrambankptr() + (ioamhram[0x146] << 8);
	case oam_dma_src_wram: return cart.wramdata(ioamhram[0x146] >> 4 & 1) + (ioamhram[0x146] << 8 & 0xFFF);
	case oam_dma_src_invalid:
	case oam_dma_src_off:  break;
	}

	return ioamhram[0x146] == 0xFF && !isCgb() ? oamDmaSrcZero() : cart.rdisabledRam();
}

void Memory::startOamDma(const unsigned long cycleCounter) {
	display.oamChange(cart.rdisabledRam(), cycleCounter);
}

void Memory::endOamDma(const unsigned long cycleCounter) {
	oamDmaPos = 0xFE;
	cart.setOamDmaSrc(oam_dma_src_off);
	display.oamChange(ioamhram, cycleCounter);
}

unsigned Memory::nontrivial_ff_read(const unsigned P, const unsigned long cycleCounter) {
	if (lastOamDmaUpdate != disabled_time)
		updateOamDma(cycleCounter);

	switch (P) {
	case 0x00:
		updateInput();
		break;
	case 0x01:
	case 0x02:
		updateSerial(cycleCounter);
		break;
	case 0x04:
		{
			const unsigned long divcycles = (cycleCounter - divLastUpdate) >> 8;
			ioamhram[0x104] = (ioamhram[0x104] + divcycles) & 0xFF;
			divLastUpdate += divcycles << 8;
		}

		break;
	case 0x05:
		ioamhram[0x105] = tima.tima(cycleCounter);
		break;
	case 0x0F:
		updateIrqs(cycleCounter);
		ioamhram[0x10F] = intreq.ifreg();
		break;
	case 0x26:
		if (ioamhram[0x126] & 0x80) {
			sound.generateSamples(cycleCounter, isDoubleSpeed());
			ioamhram[0x126] = 0xF0 | sound.getStatus();
		} else
			ioamhram[0x126] = 0x70;

		break;
	case 0x30:
	case 0x31:
	case 0x32:
	case 0x33:
	case 0x34:
	case 0x35:
	case 0x36:
	case 0x37:
	case 0x38:
	case 0x39:
	case 0x3A:
	case 0x3B:
	case 0x3C:
	case 0x3D:
	case 0x3E:
	case 0x3F:
		sound.generateSamples(cycleCounter, isDoubleSpeed());
		return sound.waveRamRead(P & 0xF);
	case 0x41:
		return ioamhram[0x141] | display.getStat(ioamhram[0x145], cycleCounter);
	case 0x44:
		return display.getLyReg(cycleCounter/*+4*/);
	case 0x69:
		return display.cgbBgColorRead(ioamhram[0x168] & 0x3F, cycleCounter);
	case 0x6B:
		return display.cgbSpColorRead(ioamhram[0x16A] & 0x3F, cycleCounter);
	default: break;
	}

	return ioamhram[P + 0x100];
}

static bool isInOamDmaConflictArea(const OamDmaSrc oamDmaSrc, const unsigned addr, const bool cgb) {
	struct Area { unsigned short areaUpper, exceptAreaLower, exceptAreaWidth, pad; };

	static const Area cgbAreas[] = {
		{ 0xC000, 0x8000, 0x2000, 0 },
		{ 0xC000, 0x8000, 0x2000, 0 },
		{ 0xA000, 0x0000, 0x8000, 0 },
		{ 0xFE00, 0x0000, 0xC000, 0 },
		{ 0xC000, 0x8000, 0x2000, 0 },
		{ 0x0000, 0x0000, 0x0000, 0 }
	};

	static const Area dmgAreas[] = {
		{ 0xFE00, 0x8000, 0x2000, 0 },
		{ 0xFE00, 0x8000, 0x2000, 0 },
		{ 0xA000, 0x0000, 0x8000, 0 },
		{ 0xFE00, 0x8000, 0x2000, 0 },
		{ 0xFE00, 0x8000, 0x2000, 0 },
		{ 0x0000, 0x0000, 0x0000, 0 }
	};

	const Area *const a = cgb ? cgbAreas : dmgAreas;

	return addr < a[oamDmaSrc].areaUpper && addr - a[oamDmaSrc].exceptAreaLower >= a[oamDmaSrc].exceptAreaWidth;
}

unsigned Memory::nontrivial_read(const unsigned P, const unsigned long cycleCounter) {
	if (P < 0xFF80) {
		if (lastOamDmaUpdate != disabled_time) {
			updateOamDma(cycleCounter);

			if (isInOamDmaConflictArea(cart.oamDmaSrc(), P, isCgb()) && oamDmaPos < 0xA0)
				return ioamhram[oamDmaPos];
		}

		if (P < 0xC000) {
			if (P < 0x8000)
				return cart.romdata(P >> 14)[P];

			if (P < 0xA000) {
				if (!display.vramAccessible(cycleCounter))
					return 0xFF;

				return cart.vrambankptr()[P];
			}

			if (cart.rsrambankptr())
				return cart.rsrambankptr()[P];

			return cart.rtcRead();
		}

		if (P < 0xFE00)
			return cart.wramdata(P >> 12 & 1)[P & 0xFFF];

		long const ffp = long(P) - 0xFF00;
		if (ffp >= 0)
			return nontrivial_ff_read(ffp, cycleCounter);

		if (!display.oamReadable(cycleCounter) || oamDmaPos < 0xA0)
			return 0xFF;
	}

	return ioamhram[P - 0xFE00];
}

unsigned Memory::nontrivial_peek(const unsigned P) {
	if (P < 0xC000) {
		if (P < 0x8000)
			return cart.romdata(P >> 14)[P];

		if (P < 0xA000) {
			return cart.vrambankptr()[P];
		}

		if (cart.rsrambankptr())
			return cart.rsrambankptr()[P];

		return cart.rtcRead(); // verified side-effect free
	}
	if (P < 0xFE00)
		return cart.wramdata(P >> 12 & 1)[P & 0xFFF];
	if (P >= 0xFF00 && P < 0xFF80)
		return nontrivial_ff_peek(P);
	return ioamhram[P - 0xFE00];
}

unsigned Memory::nontrivial_ff_peek(const unsigned P) {
	// some regs may be somewhat wrong with this
	return ioamhram[P - 0xFE00];
}

void Memory::nontrivial_ff_write(const unsigned P, unsigned data, const unsigned long cycleCounter) {
	if (lastOamDmaUpdate != disabled_time)
		updateOamDma(cycleCounter);

	switch (P & 0xFF) {
	case 0x00:
		if ((data ^ ioamhram[0x100]) & 0x30) {
			ioamhram[0x100] = (ioamhram[0x100] & ~0x30u) | (data & 0x30);
			updateInput();
		}
		return;
	case 0x01:
		updateSerial(cycleCounter);
		break;
	case 0x02:
		updateSerial(cycleCounter);

		serialCnt = 8;
		intreq.setEventTime<intevent_serial>((data & 0x81) == 0x81
				? (data & isCgb() * 2 ? (cycleCounter & ~0x7ul) + 0x10 * 8 : (cycleCounter & ~0xFFul) + 0x200 * 8)
				: static_cast<unsigned long>(disabled_time));

		data |= 0x7E - isCgb() * 2;
		break;
	case 0x04:
		ioamhram[0x104] = 0;
		divLastUpdate = cycleCounter;
		tima.resTac(cycleCounter, TimaInterruptRequester(intreq));
		return;
	case 0x05:
		tima.setTima(data, cycleCounter, TimaInterruptRequester(intreq));
		break;
	case 0x06:
		tima.setTma(data, cycleCounter, TimaInterruptRequester(intreq));
		break;
	case 0x07:
		data |= 0xF8;
		tima.setTac(data, cycleCounter, TimaInterruptRequester(intreq), gbIsCgb_);
		break;
	case 0x0F:
		updateIrqs(cycleCounter);
		intreq.setIfreg(0xE0 | data);
		return;
	case 0x10:
		if (!sound.isEnabled()) return;
		sound.generateSamples(cycleCounter, isDoubleSpeed());
		sound.setNr10(data);
		data |= 0x80;
		break;
	case 0x11:
		if (!sound.isEnabled()) {
			if (isCgb())
				return;

			data &= 0x3F;
		}

		sound.generateSamples(cycleCounter, isDoubleSpeed());
		sound.setNr11(data);
		data |= 0x3F;
		break;
	case 0x12:
		if (!sound.isEnabled()) return;
		sound.generateSamples(cycleCounter, isDoubleSpeed());
		sound.setNr12(data);
		break;
	case 0x13:
		if (!sound.isEnabled()) return;
		sound.generateSamples(cycleCounter, isDoubleSpeed());
		sound.setNr13(data);
		return;
	case 0x14:
		if (!sound.isEnabled()) return;
		sound.generateSamples(cycleCounter, isDoubleSpeed());
		sound.setNr14(data);
		data |= 0xBF;
		break;
	case 0x16:
		if (!sound.isEnabled()) {
			if (isCgb())
				return;

			data &= 0x3F;
		}

		sound.generateSamples(cycleCounter, isDoubleSpeed());
		sound.setNr21(data);
		data |= 0x3F;
		break;
	case 0x17:
		if (!sound.isEnabled()) return;
		sound.generateSamples(cycleCounter, isDoubleSpeed());
		sound.setNr22(data);
		break;
	case 0x18:
		if (!sound.isEnabled()) return;
		sound.generateSamples(cycleCounter, isDoubleSpeed());
		sound.setNr23(data);
		return;
	case 0x19:
		if (!sound.isEnabled()) return;
		sound.generateSamples(cycleCounter, isDoubleSpeed());
		sound.setNr24(data);
		data |= 0xBF;
		break;
	case 0x1A:
		if (!sound.isEnabled()) return;
		sound.generateSamples(cycleCounter, isDoubleSpeed());
		sound.setNr30(data);
		data |= 0x7F;
		break;
	case 0x1B:
		if (!sound.isEnabled() && isCgb()) return;
		sound.generateSamples(cycleCounter, isDoubleSpeed());
		sound.setNr31(data);
		return;
	case 0x1C:
		if (!sound.isEnabled()) return;
		sound.generateSamples(cycleCounter, isDoubleSpeed());
		sound.setNr32(data);
		data |= 0x9F;
		break;
	case 0x1D:
		if (!sound.isEnabled()) return;
		sound.generateSamples(cycleCounter, isDoubleSpeed());
		sound.setNr33(data);
		return;
	case 0x1E:
		if (!sound.isEnabled()) return;
		sound.generateSamples(cycleCounter, isDoubleSpeed());
		sound.setNr34(data);
		data |= 0xBF;
		break;
	case 0x20:
		if (!sound.isEnabled() && isCgb()) return;
		sound.generateSamples(cycleCounter, isDoubleSpeed());
		sound.setNr41(data);
		return;
	case 0x21:
		if (!sound.isEnabled()) return;
		sound.generateSamples(cycleCounter, isDoubleSpeed());
		sound.setNr42(data);
		break;
	case 0x22:
		if (!sound.isEnabled()) return;
		sound.generateSamples(cycleCounter, isDoubleSpeed());
		sound.setNr43(data);
		break;
	case 0x23:
		if (!sound.isEnabled()) return;
		sound.generateSamples(cycleCounter, isDoubleSpeed());
		sound.setNr44(data);
		data |= 0xBF;
		break;
	case 0x24:
		if (!sound.isEnabled()) return;
		sound.generateSamples(cycleCounter, isDoubleSpeed());
		sound.setSoVolume(data);
		break;
	case 0x25:
		if (!sound.isEnabled()) return;
		sound.generateSamples(cycleCounter, isDoubleSpeed());
		sound.mapSo(data);
		break;
	case 0x26:
		if ((ioamhram[0x126] ^ data) & 0x80) {
			sound.generateSamples(cycleCounter, isDoubleSpeed());

			if (!(data & 0x80)) {
				for (unsigned i = 0x10; i < 0x26; ++i)
					ff_write(i, 0, cycleCounter);

				sound.setEnabled(false);
			} else {
				sound.reset();
				sound.setEnabled(true);
			}
		}

		data = (data & 0x80) | (ioamhram[0x126] & 0x7F);
		break;
	case 0x30:
	case 0x31:
	case 0x32:
	case 0x33:
	case 0x34:
	case 0x35:
	case 0x36:
	case 0x37:
	case 0x38:
	case 0x39:
	case 0x3A:
	case 0x3B:
	case 0x3C:
	case 0x3D:
	case 0x3E:
	case 0x3F:
		sound.generateSamples(cycleCounter, isDoubleSpeed());
		sound.waveRamWrite(P & 0xF, data);
		break;
	case 0x40:
		if (ioamhram[0x140] != data) {
			if ((ioamhram[0x140] ^ data) & 0x80) {
				const unsigned lyc = display.getStat(ioamhram[0x145], cycleCounter) & 4;
				const bool hdmaEnabled = display.hdmaIsEnabled();

				display.lcdcChange(data, cycleCounter);
				ioamhram[0x144] = 0;
				ioamhram[0x141] &= 0xF8;

				if (data & 0x80) {
					intreq.setEventTime<intevent_blit>(display.nextMode1IrqTime() + (blanklcd ? 0 : 70224 << isDoubleSpeed()));
				} else {
					ioamhram[0x141] |= lyc;
					intreq.setEventTime<intevent_blit>(cycleCounter + (456 * 4 << isDoubleSpeed()));

					if (hdmaEnabled)
						flagHdmaReq(intreq);
				}
			} else
				display.lcdcChange(data, cycleCounter);

			ioamhram[0x140] = data;
		}

		return;
	case 0x41:
		display.lcdstatChange(data, cycleCounter);
		data = (ioamhram[0x141] & 0x87) | (data & 0x78);
		break;
	case 0x42:
		display.scyChange(data, cycleCounter);
		break;
	case 0x43:
		display.scxChange(data, cycleCounter);
		break;
	case 0x45:
		display.lycRegChange(data, cycleCounter);
		break;
	case 0x46:
		if (lastOamDmaUpdate != disabled_time)
			endOamDma(cycleCounter);

		lastOamDmaUpdate = cycleCounter;
		intreq.setEventTime<intevent_oam>(cycleCounter + 8);
		ioamhram[0x146] = data;
		oamDmaInitSetup();
		return;
	case 0x47:
		if (!isCgb())
			display.dmgBgPaletteChange(data, cycleCounter);

		break;
	case 0x48:
		if (!isCgb())
			display.dmgSpPalette1Change(data, cycleCounter);

		break;
	case 0x49:
		if (!isCgb())
			display.dmgSpPalette2Change(data, cycleCounter);

		break;
	case 0x4A:
		display.wyChange(data, cycleCounter);
		break;
	case 0x4B:
		display.wxChange(data, cycleCounter);
		break;
	case 0x4C:
		if (biosMode) {
			//flagClockReq(&intreq);
		}
		break;
	case 0x4D:
		if (isCgb())
			ioamhram[0x14D] = (ioamhram[0x14D] & ~1u) | (data & 1);		return;
	case 0x4F:
		if (isCgb()) {
			cart.setVrambank(data & 1);
			ioamhram[0x14F] = 0xFE | data;
		}

		return;
	case 0x50:
		biosMode = false;
		if (cgbSwitching) {
			display.copyCgbPalettesToDmg();
			display.setCgb(false);
			cgbSwitching = false;
		}
		return;
	case 0x51:
		dmaSource = data << 8 | (dmaSource & 0xFF);
		return;
	case 0x52:
		dmaSource = (dmaSource & 0xFF00) | (data & 0xF0);
		return;
	case 0x53:
		dmaDestination = data << 8 | (dmaDestination & 0xFF);
		return;
	case 0x54:
		dmaDestination = (dmaDestination & 0xFF00) | (data & 0xF0);
		return;
	case 0x55:
		if (isCgb()) {
			ioamhram[0x155] = data & 0x7F;

			if (display.hdmaIsEnabled()) {
				if (!(data & 0x80)) {
					ioamhram[0x155] |= 0x80;
					display.disableHdma(cycleCounter);
				}
			} else {
				if (data & 0x80) {
					if (ioamhram[0x140] & 0x80) {
						display.enableHdma(cycleCounter);
					} else
						flagHdmaReq(intreq);
				} else
					flagGdmaReq(intreq);
			}
		}

		return;
	case 0x56:
		if (isCgb())
			ioamhram[0x156] = data | 0x3E;

		return;
	case 0x68:
		if (isCgb())
			ioamhram[0x168] = data | 0x40;

		return;
	case 0x69:
		if (isCgb()) {
			const unsigned index = ioamhram[0x168] & 0x3F;

			display.cgbBgColorChange(index, data, cycleCounter);

			ioamhram[0x168] = (ioamhram[0x168] & ~0x3F) | ((index + (ioamhram[0x168] >> 7)) & 0x3F);
		}

		return;
	case 0x6A:
		if (isCgb())
			ioamhram[0x16A] = data | 0x40;

		return;
	case 0x6B:
		if (isCgb()) {
			const unsigned index = ioamhram[0x16A] & 0x3F;

			display.cgbSpColorChange(index, data, cycleCounter);

			ioamhram[0x16A] = (ioamhram[0x16A] & ~0x3F) | ((index + (ioamhram[0x16A] >> 7)) & 0x3F);
		}

		return;
	case 0x6C:
		ioamhram[0x16C] = data | 0xFE;
		cgbSwitching = true;

		return;
	case 0x70:
		if (isCgb()) {
			cart.setWrambank((data & 0x07) ? (data & 0x07) : 1);
			ioamhram[0x170] = data | 0xF8;
		}

		return;
	case 0x72:
	case 0x73:
	case 0x74:
		if (isCgb())
			break;

		return;
	case 0x75:
		if (isCgb())
			ioamhram[0x175] = data | 0x8F;

		return;
	case 0xFF:
		intreq.setIereg(data);
		break;
	default:
		return;
	}

	ioamhram[P + 0x100] = data;
}

void Memory::nontrivial_write(const unsigned P, const unsigned data, const unsigned long cycleCounter) {
	if (lastOamDmaUpdate != disabled_time) {
		updateOamDma(cycleCounter);

		if (isInOamDmaConflictArea(cart.oamDmaSrc(), P, isCgb()) && oamDmaPos < 0xA0) {
			ioamhram[oamDmaPos] = data;
			return;
		}
	}

	if (P < 0xFE00) {
		if (P < 0xA000) {
			if (P < 0x8000) {
				cart.mbcWrite(P, data);
			} else if (display.vramAccessible(cycleCounter)) {
				display.vramChange(cycleCounter);
				cart.vrambankptr()[P] = data;
			}
		} else if (P < 0xC000) {
			if (cart.wsrambankptr())
				cart.wsrambankptr()[P] = data;
			else
				cart.rtcWrite(data);
		} else
			cart.wramdata(P >> 12 & 1)[P & 0xFFF] = data;
	} else if (P - 0xFF80u >= 0x7Fu) {
 		long const ffp = long(P) - 0xFF00;
		if (ffp < 0) {
			if (display.oamWritable(cycleCounter) && oamDmaPos >= 0xA0 && (P < 0xFEA0 || isCgb())) {
				display.oamChange(cycleCounter);
				ioamhram[P - 0xFE00] = data;
			}
		} else
			nontrivial_ff_write(ffp, data, cycleCounter);
	} else
		ioamhram[P - 0xFE00] = data;
}

LoadRes Memory::loadROM(const char *romfiledata, unsigned romfilelength, const bool forceDmg, const bool multicartCompat) {
	if (LoadRes const fail = cart.loadROM(romfiledata, romfilelength, forceDmg, multicartCompat))
		return fail;

	sound.init(cart.isCgb());
	display.reset(ioamhram, cart.vramdata(), cart.isCgb());

	return LOADRES_OK;
}

std::size_t Memory::fillSoundBuffer(const unsigned long cycleCounter) {
	sound.generateSamples(cycleCounter, isDoubleSpeed());
	return sound.fillBuffer();
}

void Memory::setCgbPalette(unsigned *lut) {
	display.setCgbPalette(lut);
}

bool Memory::getMemoryArea(int which, unsigned char **data, int *length) {
	if (!data || !length)
		return false;

	switch (which)
	{
	case 4: // oam
		*data = &ioamhram[0];
		*length = 160;
		return true;
	case 5: // hram
		*data = &ioamhram[384];
		*length = 128;
		return true;
	case 6: // bgpal
		*data = (unsigned char *)display.bgPalette();
		*length = 32;
		return true;
	case 7: // sppal
		*data = (unsigned char *)display.spPalette();
		*length = 32;
		return true;
	default: // pass to cartridge
		return cart.getMemoryArea(which, data, length);
	}
}

int Memory::LinkStatus(int which)
{
	switch (which)
	{
	case 256: // ClockSignaled
		return linkClockTrigger;
	case 257: // AckClockSignal
		linkClockTrigger = false;
		return 0;
	case 258: // GetOut
		return ioamhram[0x101] & 0xff;
	case 259: // connect link cable
		LINKCABLE = true;
		return 0;
	default: // ShiftIn
		if (ioamhram[0x102] & 0x80) // was enabled
		{
			ioamhram[0x101] = which;
			ioamhram[0x102] &= 0x7F;
			intreq.flagIrq(8);
		}
		return 0;
	}

	return -1;
}

SYNCFUNC(Memory)
{
	SSS(cart);
	NSS(ioamhram);
	NSS(divLastUpdate);
	NSS(lastOamDmaUpdate);
	NSS(biosMode);
	NSS(cgbSwitching);
	NSS(agbMode);
	NSS(gbIsCgb_);
	NSS(stopped);
	NSS(halttime);

	SSS(intreq);
	SSS(tima);
	SSS(display);
	SSS(sound);
	//SSS(interrupter); // no state

	NSS(dmaSource);
	NSS(dmaDestination);
	NSS(oamDmaPos);
	NSS(serialCnt);
	NSS(blanklcd);

	NSS(LINKCABLE);
	NSS(linkClockTrigger);
}

}
