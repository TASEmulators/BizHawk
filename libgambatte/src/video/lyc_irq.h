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

#ifndef VIDEO_LYC_IRQ_H
#define VIDEO_LYC_IRQ_H

#include "newstate.h"

namespace gambatte {

struct SaveState;
class LyCounter;

class LycIrq {
public:
	LycIrq();
	bool doEvent(LyCounter const& lyCounter);
	unsigned lycReg() const { return lycRegSrc_; }
	void loadState(SaveState const &state);
	unsigned long time() const { return time_; }
	void setCgb(bool cgb) { cgb_ = cgb; }
	void lcdReset();
	void reschedule(LyCounter const &lyCounter, unsigned long cc);

	void statRegChange(unsigned statReg, LyCounter const &lyCounter, unsigned long cc) {
		regChange(statReg, lycRegSrc_, lyCounter, cc);
	}

	void lycRegChange(unsigned lycReg, LyCounter const &lyCounter, unsigned long cc) {
		regChange(statRegSrc_, lycReg, lyCounter, cc);
	}

private:
	unsigned long time_;
 	unsigned char lycRegSrc_;
 	unsigned char statRegSrc_;
	unsigned char lycReg_;
	unsigned char statReg_;
	bool cgb_;

	void regChange(unsigned statReg, unsigned lycReg,
	               LyCounter const &lyCounter, unsigned long cc);

public:
	template<bool isReader>void SyncState(NewState *ns);
};

}

#endif
