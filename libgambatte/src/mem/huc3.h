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

#ifndef HuC3Chip_H
#define HuC3Chip_H

enum
{
	HUC3_READ = 0,
	HUC3_WRITE = 1,
	HUC3_NONE = 2
};

#include "time.h"

namespace gambatte {

	struct SaveState;

	class HuC3Chip {
	public:
		HuC3Chip(Time &time);
		//void setStatePtrs(SaveState &);
		void loadState(SaveState const& state);
		void setRamflag(unsigned char ramflag) { ramflag_ = ramflag; irReceivingPulse_ = false; }
		bool isHuC3() const { return enabled_; }

		void set(bool enabled) {
			enabled_ = enabled;
		}

		unsigned char read(unsigned p, unsigned long const cc);
		void write(unsigned p, unsigned data, unsigned long cycleCounter);

	private:
		Time &time_;
		std::uint32_t haltTime_;
		unsigned dataTime_;
		unsigned writingTime_;
		unsigned char ramValue_;
		unsigned char shift_;
		unsigned char ramflag_;
		unsigned char modeflag_;
		unsigned long irBaseCycle_;
		bool enabled_;
		bool lastLatchData_;
		bool halted_;
		bool irReceivingPulse_;

		void doLatch(unsigned long cycleCounter);
		void updateTime(unsigned long cycleCounter);

		std::uint32_t time(unsigned long const cc) {
			return halted_ ? haltTime_ : time_.get(cc);
		}
	public:
		template<bool isReader>void SyncState(NewState* ns);
	};
}

#endif