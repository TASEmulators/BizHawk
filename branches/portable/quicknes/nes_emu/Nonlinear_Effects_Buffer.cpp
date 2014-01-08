
// Nes_Emu 0.5.6. http://www.slack.net/~ant/libs/

#include "Nonlinear_Effects_Buffer.h"

/* Copyright (C) 2004-2005 Shay Green. This module is free software; you
can redistribute it and/or modify it under the terms of the GNU Lesser
General Public License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version. This
module is distributed in the hope that it will be useful, but WITHOUT ANY
WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for
more details. You should have received a copy of the GNU Lesser General
Public License along with this module; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA */

#include BLARGG_SOURCE_BEGIN

Nonlinear_Effects_Buffer::Nonlinear_Effects_Buffer() : Effects_Buffer( true )
{
	config_t c;
	c.effects_enabled = false;
	config( c );
}

Nonlinear_Effects_Buffer::~Nonlinear_Effects_Buffer()
{
}

void Nonlinear_Effects_Buffer::enable_nonlinearity( Nes_Apu& apu, bool b )
{
	if ( b )
		clear();
	nonlinearizer.enable( apu, b );
}

void Nonlinear_Effects_Buffer::config( const config_t& in )
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

void Nonlinear_Effects_Buffer::clear()
{
	nonlinearizer.clear();
	Effects_Buffer::clear();
}

Nonlinear_Effects_Buffer::channel_t Nonlinear_Effects_Buffer::channel( int i )
{
	return Effects_Buffer::channel( (2 <= i && i <= 4) ? 2 : i & 1 );
}

long Nonlinear_Effects_Buffer::read_samples( blip_sample_t* out, long count )
{
	count = 2 * nonlinearizer.make_nonlinear( *channel( 2 ).center, count / 2 );
	return Effects_Buffer::read_samples( out, count );
}

