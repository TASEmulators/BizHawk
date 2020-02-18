#ifndef M0_IRQ_H
#define M0_IRQ_H

#include "lcddef.h"
#include "../savestate.h"
#include "../newstate.h"

namespace gambatte {

class MStatIrqEvent {
public:
	MStatIrqEvent() : lycReg_(0), statReg_(0) {}
	void lcdReset(unsigned lycReg) { lycReg_ = lycReg; }

	void lycRegChange(unsigned lycReg, unsigned long nextM0IrqTime,
		unsigned long nextM2IrqTime, unsigned long cc, bool ds, bool cgb) {
		if (cc + 5 * cgb + 1 - ds < std::min(nextM0IrqTime, nextM2IrqTime))
			lycReg_ = lycReg;
	}

	void statRegChange(unsigned statReg, unsigned long nextM0IrqTime, unsigned long nextM1IrqTime,
		unsigned long nextM2IrqTime, unsigned long cc, bool cgb) {
		if (cc + 2 * cgb < std::min(std::min(nextM0IrqTime, nextM1IrqTime), nextM2IrqTime))
			statReg_ = statReg;
	}

	bool doM0Event(unsigned ly, unsigned statReg, unsigned lycReg) {
		bool const flagIrq = ((statReg | statReg_) & lcdstat_m0irqen)
			&& (!(statReg_ & lcdstat_lycirqen) || ly != lycReg_);
		lycReg_ = lycReg;
		statReg_ = statReg;
		return flagIrq;
	}

	bool doM1Event(unsigned statReg) {
		bool const flagIrq = (statReg & lcdstat_m1irqen)
			&& !(statReg_ & (lcdstat_m2irqen | lcdstat_m0irqen));
		statReg_ = statReg;
		return flagIrq;
	}

	bool doM2Event(unsigned ly, unsigned statReg, unsigned lycReg) {
		bool const blockedByM1Irq = ly == 0 && (statReg_ & lcdstat_m1irqen);
		bool const blockedByLycIrq = (statReg_ & lcdstat_lycirqen)
			&& (ly == 0 ? ly : ly - 1) == lycReg_;
		bool const flagIrq = !blockedByM1Irq && !blockedByLycIrq;
		lycReg_ = lycReg;
		statReg_ = statReg;
		return flagIrq;
	}

	void loadState(SaveState const& state) {
		lycReg_ = state.ppu.m0lyc;
		statReg_ = state.mem.ioamhram.get()[0x141];
	}

private:
	unsigned char statReg_;
	unsigned char lycReg_;

public:
	template<bool isReader>
	void SyncState(NewState *ns)
	{
		NSS(statReg_);
		NSS(lycReg_);
	}
};

}

#endif
