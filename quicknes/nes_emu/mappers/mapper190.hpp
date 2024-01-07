#pragma once

#include "Nes_Mapper.h"

// Magic Kid Googoo

class Mapper190: public Nes_Mapper {
public:
	Mapper190()
	{
	}

	virtual void reset_state()
	{
	}

	virtual void apply_mapping()
	{
		mirror_vert();
		enable_sram();
		set_prg_bank( 0xc000, bank_16k, 0);
	}

	virtual void write(nes_time_t, nes_addr_t addr, int data)
	{
		switch ( addr >> 12 )
		{
			case 0x8:
			case 0x9:
			case 0xc:
			case 0xd:
				set_prg_bank( 0x8000, bank_16k, ( ( ( addr >> 11 ) & 8 ) | ( data & 7 ) ) );
				break;
			case 0xa:
			case 0xb:
				switch ( addr & 3 )
				{
					case 0:
						set_chr_bank( 0x0000, bank_2k, data );
						break;
					case 1:
						set_chr_bank( 0x0800, bank_2k, data );
						break;
					case 2:
						set_chr_bank( 0x1000, bank_2k, data );
						break;
					case 3:
						set_chr_bank( 0x1800, bank_2k, data );
						break;
				}
				break;
		}
	}
};

