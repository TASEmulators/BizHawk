#include "Nes_Mapper.h"

#pragma once

// DIS23C01 DAOU ROM CONTROLLER

struct m156_state_t
{
	uint8_t prg_bank;
	uint8_t chr_banks [8];
};
BOOST_STATIC_ASSERT( sizeof (m156_state_t) == 9 );

class Mapper156 : public Nes_Mapper, m156_state_t {
public:
	Mapper156()
	{
		m156_state_t * state = this;
		register_state( state, sizeof * state );
	}

	void reset_state()
	{
		prg_bank = 0;
		for ( unsigned i = 0; i < 8; i++ ) chr_banks [i] = i;
		enable_sram();
		apply_mapping();
	}

	void apply_mapping()
	{
		mirror_single( 0 );
		set_prg_bank( 0x8000, bank_16k, prg_bank );

		for ( int i = 0; i < (int) sizeof chr_banks; i++ )
			set_chr_bank( i * 0x400, bank_1k, chr_banks [i] );
	}

	void write( nes_time_t, nes_addr_t addr, int data )
	{
		unsigned int reg = addr - 0xC000;
		if ( addr == 0xC010 )
		{
			prg_bank = data;
			set_prg_bank( 0x8000, bank_16k, data );
		}
		else if ( reg < 4 )
		{
			chr_banks [reg] = data;
			set_chr_bank( reg * 0x400, bank_1k, data );
		}
		else if ( ( reg - 8 ) < 4 )
		{
			reg -= 4;
			chr_banks [reg] = data;
			set_chr_bank( reg * 0x400, bank_1k, data );
		}
	}
};
