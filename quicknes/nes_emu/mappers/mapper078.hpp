#pragma once

#include "Nes_Mapper.h"

// Holy Diver and Uchuusen - Cosmo Carrier.

class Mapper078 : public Nes_Mapper {
	// lower 8 bits are the reg at 8000:ffff
	// next two bits are autodetecting type
	// 0 = unknown 1 = cosmo carrier 2 = holy diver
	int reg;
	void writeinternal(int data, int changed)
	{
		reg &= 0x300;
		reg |= data;

		if (changed & 0x07)
			set_prg_bank(0x8000, bank_16k, reg & 0x07);
		if (changed & 0xf0)
			set_chr_bank(0x0000, bank_8k, (reg >> 4) & 0x0f);
		if (changed & 0x08)
		{
			// set mirroring based on memorized board type
			if (reg & 0x100)
			{
				mirror_single((reg >> 3) & 1);
			}
			else if (reg & 0x200)
			{
				if (reg & 0x08)
					mirror_vert();
				else
					mirror_horiz();
			}
			else
			{
				// if you don't set something here, holy diver dumps with 4sc set will
				// savestate as 4k NTRAM.  then when you later set H\V mapping, state size mismatch.
				mirror_single(1);
			}
		}
	}

public:
	Mapper078()
	{
		register_state(&reg, 4);
	}

	virtual void reset_state()
	{
		reg = 0;
	}

	virtual void apply_mapping()
	{
		writeinternal(reg, 0xff);
	}

	virtual void write( nes_time_t, nes_addr_t addr, int data)
	{
		// heuristic: if the first write ever to the register is 0,
		// we're on holy diver, otherwise, carrier.  it works for these two games...
		if (!(reg & 0x300))
		{
			reg |= data ? 0x100 : 0x200;
			writeinternal(data, 0xff);
		}
		else
		{
			writeinternal(data, reg ^ data);
		}
	}
};
