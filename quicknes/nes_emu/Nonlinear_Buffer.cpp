
// Nes_Emu 0.5.6. http://www.slack.net/~ant/libs/

#include "Nonlinear_Buffer.h"

#include "Nes_Apu.h"

/* Library Copyright (C) 2003-2005 Shay Green. This library is free software;
you can redistribute it and/or modify it under the terms of the GNU Lesser
General Public License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version. This
module is distributed in the hope that it will be useful, but WITHOUT ANY
WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR
A PARTICULAR PURPOSE.  See the GNU Lesser General Public License for more
details. You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA */

#include BLARGG_SOURCE_BEGIN

// Nonlinear_Buffer

Nonlinear_Buffer::Nonlinear_Buffer() :
	Multi_Buffer( 1 )
{
}

Nonlinear_Buffer::~Nonlinear_Buffer()
{
}

void Nonlinear_Buffer::enable_nonlinearity( Nes_Apu& apu, bool b )
{
	if ( b )
		clear();
	nonlinearizer.enable( apu, b );
	for ( int i = 0; i < apu.osc_count; i++ )
		apu.osc_output( i, (i >= 2 ? &tnd : &buf) );
}

blargg_err_t Nonlinear_Buffer::sample_rate( long rate, int msec )
{
	BLARGG_RETURN_ERR( buf.sample_rate( rate, msec ) );
	BLARGG_RETURN_ERR( tnd.sample_rate( rate, msec ) );
	return Multi_Buffer::sample_rate( buf.sample_rate(), buf.length() );
}

void Nonlinear_Buffer::clock_rate( long rate )
{
	buf.clock_rate( rate );
	tnd.clock_rate( rate );
}

void Nonlinear_Buffer::bass_freq( int freq )
{
	buf.bass_freq( freq );
	tnd.bass_freq( freq );
}

void Nonlinear_Buffer::clear()
{
	nonlinearizer.clear();
	buf.clear();
	tnd.clear();
}

Nonlinear_Buffer::channel_t Nonlinear_Buffer::channel( int i )
{
	channel_t c;
	c.center = &buf;
	if ( 2 <= i && i <= 4 )
		c.center = &tnd; // only use for triangle, noise, and dmc
	c.left   = c.center;
	c.right  = c.center;
	return c;
}

void Nonlinear_Buffer::end_frame( blip_time_t length, bool )
{
	buf.end_frame( length );
	tnd.end_frame( length );
}

long Nonlinear_Buffer::samples_avail() const
{
	return buf.samples_avail();
}

#include BLARGG_ENABLE_OPTIMIZER

long Nonlinear_Buffer::read_samples( blip_sample_t* out, long count )
{
	count = nonlinearizer.make_nonlinear( tnd, count );
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
	nonlinear = false;
	
	double gain = 0x7fff * 1.3;
	// don't use entire range, so any overflow will stay within table
	int const range = half * Nes_Apu::nonlinear_tnd_gain();
	for ( int i = 0; i < half * 2; i++ )
	{
		int out = i << shift;
		if ( i > half )
		{
			int j = i - half;
			if ( j >= range )
				j = range - 1;
			double n = 202.0 / (range - 1) * j;
			double d = 163.67 / (24329.0 / n + 100);
			out = int (d * gain) + 0x8000;
			assert( out < 0x10000 );
		}
		table [i] = out;
	}
	clear();
}
	
void Nes_Nonlinearizer::enable( Nes_Apu& apu, bool b )
{
	nonlinear = b;
	if ( b )
		apu.enable_nonlinear( 1.0 );
	else
		apu.volume( 1.0 );
}

long Nes_Nonlinearizer::make_nonlinear( Blip_Buffer& buf, long count )
{
	long avail = buf.samples_avail();
	if ( count > avail )
		count = avail;
	
	if ( count && nonlinear )
	{
		const int zero_offset = Blip_Buffer::sample_offset_;
		
		#define ENTRY( s ) (table [((s) >> shift) & entry_mask])
		
		BOOST::uint16_t* p = buf.buffer_;
		unsigned prev = ENTRY( accum );
		long accum = this->accum;
		
		for ( unsigned n = count; n--; )
		{
			accum += (long) *p - zero_offset;
			check( (accum >> shift) < half * 2 );
			unsigned entry = ENTRY( accum );
			*p++ = entry - prev + zero_offset;
			prev = entry;
		}
		
		this->accum = accum;
	}
	
	return count;
}

