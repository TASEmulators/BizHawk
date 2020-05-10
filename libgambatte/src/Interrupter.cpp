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

#include "interrupter.h"
#include "memory.h"

namespace gambatte {

	Interrupter::Interrupter(unsigned short& sp, unsigned short& pc, unsigned char& opcode, bool& prefetched)
		: sp_(sp)
		, pc_(pc)
		, opcode_(opcode)
		, prefetched_(prefetched)
	{
	}

	void Interrupter::prefetch(unsigned long cc, Memory& mem) {
		if (!prefetched_) {
			opcode_ = mem.read(pc_, cc);
			pc_ = (pc_ + 1) & 0xFFFF;
			prefetched_ = true;
		}
	}

	unsigned long Interrupter::interrupt(unsigned long cc, Memory& memory) {
		// undo prefetch (presumably unconditional on hw).
		if (prefetched_) {
			pc_ = (pc_ - 1) & 0xFFFF;
			prefetched_ = false;
		}
		cc += 12;
		sp_ = (sp_ - 1) & 0xFFFF;
		memory.write(sp_, pc_ >> 8, cc);
		cc += 4;

		unsigned const pendingIrqs = memory.pendingIrqs(cc);
		unsigned const n = pendingIrqs & -pendingIrqs;
		unsigned address;
		if (n <= 4) {
			static unsigned char const lut[] = { 0x00, 0x40, 0x48, 0x48, 0x50 };
			address = lut[n];
		}
		else
			address = 0x50 + n;

		sp_ = (sp_ - 1) & 0xFFFF;
		memory.write(sp_, pc_ & 0xFF, cc);
		memory.ackIrq(n, cc);
		pc_ = address;
		cc += 4;

		if (address == 0x40 && !gsCodes_.empty())
			applyVblankCheats(cc, memory);

		return cc;
	}

	static int asHex(char c) {
		return c >= 'A' ? c - 'A' + 0xA : c - '0';
	}

	void Interrupter::setGameShark(std::string const& codes) {
		std::string code;
		gsCodes_.clear();

		for (std::size_t pos = 0; pos < codes.length(); pos += code.length() + 1) {
			code = codes.substr(pos, codes.find(';', pos) - pos);
			if (code.length() >= 8) {
				GsCode gs;
				gs.type = asHex(code[0]) << 4 | asHex(code[1]);
				gs.value = (asHex(code[2]) << 4 | asHex(code[3])) & 0xFF;
				gs.address = (asHex(code[4]) << 4
					| asHex(code[5])
					| asHex(code[6]) << 12
					| asHex(code[7]) << 8) & 0xFFFF;
				gsCodes_.push_back(gs);
			}
		}
	}

	void Interrupter::applyVblankCheats(unsigned long const cc, Memory& memory) {
		for (std::size_t i = 0, size = gsCodes_.size(); i < size; ++i) {
			if (gsCodes_[i].type == 0x01)
				memory.write(gsCodes_[i].address, gsCodes_[i].value, cc);
		}
	}

}