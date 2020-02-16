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

#ifndef INTERRUPTER_H
#define INTERRUPTER_H

#include <string>
#include <vector>

namespace gambatte {

	struct GsCode {
		unsigned short address;
		unsigned char value;
		unsigned char type;
	};

	class Memory;

	class Interrupter {
	public:
		Interrupter(unsigned short& sp, unsigned short& pc, unsigned char& opcode, bool& prefetched);
		void prefetch(unsigned long cc, Memory& mem);
		unsigned long interrupt(unsigned long cycleCounter, Memory& memory);
		void setGameShark(std::string const& codes);

	private:
		unsigned short& sp_;
		unsigned short& pc_;
		unsigned char& opcode_;
		bool& prefetched_;
		std::vector<GsCode> gsCodes_;

		void applyVblankCheats(unsigned long cc, Memory& mem);
	};

}

#endif
