
// Nes_Emu 0.7.0. http://www.slack.net/~ant/libs/

#include "Nes_Buffer.h"

#include "Nes_Apu.h"

/* Library Copyright (C) 2003-2006 Shay Green. This library is free software;
you can redistribute it and/or modify it under the terms of the GNU Lesser
General Public License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version. This
module is distributed in the hope that it will be useful, but WITHOUT ANY
WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR
A PARTICULAR PURPOSE.  See the GNU Lesser General Public License for more
details. You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA */

#include "blargg_source.h"

#ifdef BLARGG_ENABLE_OPTIMIZER
	#include BLARGG_ENABLE_OPTIMIZER
#endif

// Nes_Buffer

Nes_Buffer::Nes_Buffer() : Multi_Buffer( 1 ) { }

Nes_Buffer::~Nes_Buffer() { }

Multi_Buffer* set_apu( Nes_Buffer* buf, Nes_Apu* apu )
{
	buf->set_apu( apu );
	return buf;
}

void Nes_Buffer::enable_nonlinearity( bool b )
{
	if ( b )
		clear();
	
	Nes_Apu* apu = nonlin.enable( b, &tnd );
	apu->osc_output( 0, &buf );
	apu->osc_output( 1, &buf );
}

blargg_err_t Nes_Buffer::set_sample_rate( long rate, int msec )
{
	enable_nonlinearity( nonlin.enabled ); // reapply
	RETURN_ERR( buf.set_sample_rate( rate, msec ) );
	RETURN_ERR( tnd.set_sample_rate( rate, msec ) );
	return Multi_Buffer::set_sample_rate( buf.sample_rate(), buf.length() );
}

void Nes_Buffer::clock_rate( long rate )
{
	buf.clock_rate( rate );
	tnd.clock_rate( rate );
}

void Nes_Buffer::bass_freq( int freq )
{
	buf.bass_freq( freq );
	tnd.bass_freq( freq );
}

void Nes_Buffer::clear()
{
	nonlin.clear();
	buf.clear();
	tnd.clear();
}

Nes_Buffer::channel_t Nes_Buffer::channel( int i )
{
	channel_t c;
	c.center = &buf;
	if ( 2 <= i && i <= 4 )
		c.center = &tnd; // only use for triangle, noise, and dmc
	c.left   = c.center;
	c.right  = c.center;
	return c;
}

void Nes_Buffer::end_frame( blip_time_t length, bool )
{
	buf.end_frame( length );
	tnd.end_frame( length );
}

long Nes_Buffer::samples_avail() const
{
	return buf.samples_avail();
}

long Nes_Buffer::read_samples( blip_sample_t* out, long count )
{
	count = nonlin.make_nonlinear( tnd, count );
	if ( count )
	{
		Blip_Reader lin;
		Blip_Reader nonlin;
		
		int lin_bass = lin.begin( buf );
		int nonlin_bass = nonlin.begin( tnd );
		
		for ( int n = count; n--; )
		{
			int s = lin.read() + nonlin.read();
			lin.next( lin_bass );
			nonlin.next( nonlin_bass );
			*out++ = s;
			
			if ( (BOOST::int16_t) s != s )
				out [-1] = 0x7FFF - (s >> 24);
		}
		
		lin.end( buf );
		nonlin.end( tnd );
		
		buf.remove_samples( count );
		tnd.remove_samples( count );
	}
	
	return count;
}

// Nes_Nonlinearizer

Nes_Nonlinearizer::Nes_Nonlinearizer()
{
	apu = NULL;
	enabled = true;
	
	float const gain = 0x7fff * 1.3f;
	// don't use entire range, so any overflow will stay within table
	int const range = (int) (table_size * Nes_Apu::nonlinear_tnd_gain());
	for ( int i = 0; i < table_size; i++ )
	{
		int const offset = table_size - range;
		int j = i - offset;
		float n = 202.0f / (range - 1) * j;
		float d = gain * 163.67f / (24329.0f / n + 100.0f);
		int out = (int) d;
//out = j << (15 - table_bits); // make table linear for testing
		assert( out < 0x8000 );
		table [j & (table_size - 1)] = out;
	}
}

Nes_Apu* Nes_Nonlinearizer::enable( bool b, Blip_Buffer* buf )
{
	require( apu );
	apu->osc_output( 2, buf );
	apu->osc_output( 3, buf );
	apu->osc_output( 4, buf );
	enabled = b;
	if ( b )
		apu->enable_nonlinear( 1.0 );
	else
		apu->volume( 1.0 );
	return apu;
}

#define ENTRY( s ) table [(s) >> (blip_sample_bits - table_bits - 1) & (table_size - 1)]

long Nes_Nonlinearizer::make_nonlinear( Blip_Buffer& buf, long count )
{
	require( apu );
	long avail = buf.samples_avail();
	if ( count > avail )
		count = avail;
	if ( count && enabled )
	{
		
		Blip_Buffer::buf_t_* p = buf.buffer_;
		long accum = this->accum;
		long prev = this->prev;
		for ( unsigned n = count; n; --n )
		{
			long entry = ENTRY( accum );
			check( (entry >= 0) == (accum >= 0) );
			accum += *p;
			*p++ = (entry - prev) << (blip_sample_bits - 16);
			prev = entry;
		}
		
		this->prev = prev;
		this->accum = accum;
	}
	
	return count;
}

void Nes_Nonlinearizer::clear()
{
	accum = 0;
	prev = ENTRY( 86016000 ); // avoid thump due to APU's triangle dc bias
	// TODO: still results in slight clicks and thumps
}

