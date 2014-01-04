
// Nes_Snd_Emu 0.1.7. http://www.slack.net/~ant/libs/

#include "Nes_Nonlinearizer.h"

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

Nes_Nonlinearizer::Nes_Nonlinearizer() : Multi_Buffer( 1 )
{
	enable_nonlinearity( true );
}

Nes_Nonlinearizer::~Nes_Nonlinearizer()
{
}

blargg_err_t Nes_Nonlinearizer::sample_rate( long rate, int msec )
{
	BLARGG_RETURN_ERR( buf.sample_rate( rate, msec ) );
	BLARGG_RETURN_ERR( tnd.sample_rate( rate, msec ) );
	return Multi_Buffer::sample_rate( buf.sample_rate(), buf.length() );
}

void Nes_Nonlinearizer::clock_rate( long rate )
{
	buf.clock_rate( rate );
	tnd.clock_rate( rate );
}

void Nes_Nonlinearizer::bass_freq( int freq )
{
	buf.bass_freq( freq );
	tnd.bass_freq( freq );
}

void Nes_Nonlinearizer::clear()
{
	accum = 0x8000;
	buf.clear();
	tnd.clear();
}

Nes_Nonlinearizer::channel_t Nes_Nonlinearizer::channel( int i )
{
	channel_t c;
	c.center = (2 <= i && i <= 4) ? &tnd : &buf;
	c.left   = c.center;
	c.right  = c.center;
	return c;
}

void Nes_Nonlinearizer::end_frame( blip_time_t length, bool )
{
	buf.end_frame( length );
	tnd.end_frame( length );
}

long Nes_Nonlinearizer::samples_avail() const
{
	return buf.samples_avail();
}

#include BLARGG_ENABLE_OPTIMIZER

void Nes_Nonlinearizer::enable_nonlinearity( bool b )
{
	require( b ); // to do: implement non-linear output
	double gain = 0x7fff * 0.742467605 * 1.2;
	for ( int i = 0; i < half * 2; i++ )
	{
		int out = i << shift;
		if ( i > half )
		{
			double n = 202.0 / (half - 1) * (i - half);
			double d = 163.67 / (24329.0 / n + 100);
			out = int (d * gain) + 0x8000;
		}
		table [i] = out;
	}
}

void Nes_Nonlinearizer::make_nonlinear( long count )
{
	const int zero_offset = 0x7f7f; // to do: use private constant from Blip_Buffer.h
	
	#define ENTRY( s ) (table [((s) >> shift) & entry_mask])
	
	BOOST::uint16_t* p = tnd.buffer_;
	unsigned prev = ENTRY( accum );
	long accum = this->accum;
	
	for ( unsigned n = count; n--; )
	{
		accum += (long) *p - zero_offset;
		if ( (accum >> shift) >= half * 2 )
		{
			// to do: extend table to handle overflow better
			check( false ); // overflowed
			accum = (half * 2 - 1) << shift;
		}
		unsigned entry = ENTRY( accum );
		*p++ = entry - prev + zero_offset;
		prev = entry;
	}
	
	this->accum = accum;
}

long Nes_Nonlinearizer::read_samples( blip_sample_t* out, long count )
{
	long avail = buf.samples_avail();
	assert( tnd.samples_avail() == avail );
	if ( count > avail )
		count = avail;
	
	if ( count )
	{
		make_nonlinear( count );
		
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

