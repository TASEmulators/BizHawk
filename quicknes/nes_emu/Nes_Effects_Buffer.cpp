
// Nes_Emu 0.7.0. http://www.slack.net/~ant/libs/

#include "Nes_Effects_Buffer.h"

#include "Nes_Apu.h"

/* Copyright (C) 2004-2006 Shay Green. This module is free software; you
can redistribute it and/or modify it under the terms of the GNU Lesser
General Public License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version. This
module is distributed in the hope that it will be useful, but WITHOUT ANY
WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for
more details. You should have received a copy of the GNU Lesser General
Public License along with this module; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA */

#include "blargg_source.h"

Nes_Effects_Buffer::Nes_Effects_Buffer() :
	Effects_Buffer( true ) // nes never uses stereo channels
{
	config_t c;
	c.effects_enabled = false;
	config( c );
}

Nes_Effects_Buffer::~Nes_Effects_Buffer() { }

Multi_Buffer* set_apu( Nes_Effects_Buffer* buf, Nes_Apu* apu )
{
	buf->set_apu( apu );
	return buf;
}

void Nes_Effects_Buffer::enable_nonlinearity( bool b )
{
	if ( b )
		clear();
	Nes_Apu* apu = nonlin.enable( b, channel( 2 ).center );
	apu->osc_output( 0, channel( 0 ).center );
	apu->osc_output( 1, channel( 1 ).center );
}

void Nes_Effects_Buffer::config( const config_t& in )
{
	config_t c = in;
	if ( !c.effects_enabled )
	{
		// effects must always be enabled to keep separate buffers, so
		// set parameters to be equivalent to disabled
		c.pan_1 = 0;
		c.pan_2 = 0;
		c.echo_level = 0;
		c.reverb_level = 0;
		c.effects_enabled = true;
	}
	Effects_Buffer::config( c );
}

blargg_err_t Nes_Effects_Buffer::set_sample_rate( long rate, int msec )
{
	enable_nonlinearity( nonlin.enabled ); // reapply
	return Effects_Buffer::set_sample_rate( rate, msec );
}

void Nes_Effects_Buffer::clear()
{
	nonlin.clear();
	Effects_Buffer::clear();
}

Nes_Effects_Buffer::channel_t Nes_Effects_Buffer::channel( int i )
{
	return Effects_Buffer::channel( (2 <= i && i <= 4) ? 2 : i & 1 );
}

long Nes_Effects_Buffer::read_samples( blip_sample_t* out, long count )
{
	count = 2 * nonlin.make_nonlinear( *channel( 2 ).center, count / 2 );
	return Effects_Buffer::read_samples( out, count );
}

