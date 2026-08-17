
// Nes_Emu 0.7.0. http://www.slack.net/~ant/

#include "Nes_Emu.h"

#include <string.h>
#include "Nes_State.h"
#include "Nes_Mapper.h"

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

int const sound_fade_size = 384;

// Constants are manually duplicated in Nes_Emu so their value can be seen
// directly, rather than having to look in Nes_Ppu.h. "0 +" converts to int.
BOOST_STATIC_ASSERT( Nes_Emu::image_width  == 0 + Nes_Ppu::image_width );
BOOST_STATIC_ASSERT( Nes_Emu::image_height == 0 + Nes_Ppu::image_height );

Nes_Emu::equalizer_t const Nes_Emu::nes_eq     = {  -1.0,  80 };
Nes_Emu::equalizer_t const Nes_Emu::famicom_eq = { -15.0,  80 };
Nes_Emu::equalizer_t const Nes_Emu::tv_eq      = { -12.0, 180 };

Nes_Emu::Nes_Emu()
{
	frame_ = &single_frame;
	buffer_height_ = Nes_Ppu::buffer_height + 2;
	default_sound_buf = NULL;
	sound_buf = &silent_buffer;
	sound_buf_changed_count = 0;
	equalizer_ = nes_eq;
	channel_count_ = 0;
	sound_enabled = false;
	host_pixels = NULL;
	single_frame.pixels = 0;
	single_frame.top = 0;
	init_called = false;
	set_palette_range( 0 );
	memset( single_frame.palette, 0, sizeof single_frame.palette );
	host_pixel_buff = new char[buffer_width * buffer_height()];
	set_pixels(host_pixel_buff, buffer_width);
}

Nes_Emu::~Nes_Emu()
{
	delete default_sound_buf;
	delete[] host_pixel_buff;
}

blargg_err_t Nes_Emu::init_()
{
	return emu.init();
}

inline blargg_err_t Nes_Emu::auto_init()
{
	if ( !init_called )
	{
		RETURN_ERR( init_() );
		init_called = true;
	}
	return 0;
}

inline void Nes_Emu::clear_sound_buf()
{
	fade_sound_out = false;
	fade_sound_in = true;
	sound_buf->clear();
}

// Emulation

void Nes_Emu::close()
{
	if ( cart() )
	{
		emu.close();
		private_cart.clear();
	}
}

blargg_err_t Nes_Emu::set_cart( Nes_Cart const* new_cart )
{
	close();
	RETURN_ERR( auto_init() );
	RETURN_ERR( emu.open( new_cart ) );
	
	channel_count_ = Nes_Apu::osc_count + emu.mapper->channel_count();
	RETURN_ERR( sound_buf->set_channel_count( channel_count() ) );
	set_equalizer( equalizer_ );
	enable_sound( true );
	
	reset();
	
	return 0;
}

void Nes_Emu::reset( bool full_reset, bool erase_battery_ram )
{
	require( cart() );
	
	clear_sound_buf();
	set_timestamp( 0 );
	emu.reset( full_reset, erase_battery_ram );
}

void Nes_Emu::set_palette_range( int begin, int end )
{
	require( (unsigned) end <= 0x100 );
	// round up to alignment
	emu.ppu.palette_begin = (begin + palette_alignment - 1) & ~(palette_alignment - 1); 
	host_palette_size = end - emu.ppu.palette_begin;
	require( host_palette_size >= palette_alignment );
}

blargg_err_t Nes_Emu::emulate_frame( const uint32_t joypad1, const uint32_t joypad2 )
{
	emu.current_joypad [0] = joypad1;
	emu.current_joypad [1] = joypad2;
	
	emu.ppu.host_pixels = NULL;
	
	unsigned changed_count = sound_buf->channels_changed_count();
	bool new_enabled = (frame_ != NULL);
	if ( sound_buf_changed_count != changed_count || sound_enabled != new_enabled )
	{
		sound_buf_changed_count = changed_count;
		sound_enabled = new_enabled;
		enable_sound( sound_enabled );
	}
	
	frame_t* f = frame_;
	if ( f )
	{
		emu.ppu.max_palette_size = host_palette_size;
		emu.ppu.host_palette = f->palette + emu.ppu.palette_begin;
		// add black and white for emulator to use (unless emulator uses entire
		// palette for frame)
		f->palette [252] = 0x0F;
		f->palette [254] = 0x30;
		f->palette [255] = 0x0F;
		if ( host_pixels )
			emu.ppu.host_pixels = (BOOST::uint8_t*) host_pixels +
					emu.ppu.host_row_bytes * f->top;
		
		if ( sound_buf->samples_avail() )
			clear_sound_buf();
		
		nes_time_t frame_len = emu.emulate_frame();
		sound_buf->end_frame( frame_len, false );
		
		f = frame_;
		f->sample_count      = sound_buf->samples_avail();
		f->chan_count        = sound_buf->samples_per_frame();
		f->palette_begin     = emu.ppu.palette_begin;
		f->palette_size      = emu.ppu.palette_size;
		f->joypad_read_count = emu.joypad_read_count;
		f->burst_phase       = emu.ppu.burst_phase;
		f->pitch             = emu.ppu.host_row_bytes;
		f->pixels            = emu.ppu.host_pixels + f->left;
	}
	else
	{
		emu.ppu.max_palette_size = 0;
		emu.emulate_frame();
	}
	
	return 0;
}

// Extras

blargg_err_t Nes_Emu::load_ines( Auto_File_Reader in )
{
	close();
	RETURN_ERR( private_cart.load_ines( in ) );
	return set_cart( &private_cart );
}

blargg_err_t Nes_Emu::save_battery_ram( Auto_File_Writer out )
{
	RETURN_ERR( out.open() );
	return out->write( emu.impl->sram, emu.impl->sram_size );
}

blargg_err_t Nes_Emu::load_battery_ram( Auto_File_Reader in )
{
	RETURN_ERR( in.open() );
	emu.sram_present = true;
	return in->read( emu.impl->sram, emu.impl->sram_size );
}

void Nes_Emu::load_state( Nes_State_ const& in )
{
	clear_sound_buf();
	emu.load_state( in );
}

void Nes_Emu::load_state( Nes_State const& in )
{
	loading_state( in );
	load_state( STATIC_CAST(Nes_State_ const&,in) );
}

blargg_err_t Nes_Emu::load_state( Auto_File_Reader in )
{
	Nes_State* state = BLARGG_NEW Nes_State;
	CHECK_ALLOC( state );
	blargg_err_t err = state->read( in );
	if ( !err )
		load_state( *state );
	delete state;
	return err;
}

blargg_err_t Nes_Emu::save_state( Auto_File_Writer out ) const
{
	Nes_State* state = BLARGG_NEW Nes_State;
	CHECK_ALLOC( state );
	save_state( state );
	blargg_err_t err = state->write( out );
	delete state;
	return err;
}

void Nes_Emu::write_chr( void const* p, long count, long offset )
{
	require( (unsigned long) offset <= (unsigned long) chr_size() );
	long end = offset + count;
	require( (unsigned long) end <= (unsigned long) chr_size() );
	memcpy( (byte*) chr_mem() + offset, p, count );
	emu.ppu.rebuild_chr( offset, end );
}

blargg_err_t Nes_Emu::set_sample_rate( long rate, class Nes_Buffer* buf )
{
	extern Multi_Buffer* set_apu( class Nes_Buffer*, Nes_Apu* );
	RETURN_ERR( auto_init() );
	return set_sample_rate( rate, set_apu( buf, &emu.impl->apu ) );
}

blargg_err_t Nes_Emu::set_sample_rate( long rate, class Nes_Effects_Buffer* buf )
{
	extern Multi_Buffer* set_apu( class Nes_Effects_Buffer*, Nes_Apu* );
	RETURN_ERR( auto_init() );
	return set_sample_rate( rate, set_apu( buf, &emu.impl->apu ) );
}

// Sound

void Nes_Emu::set_frame_rate( double rate )
{
	sound_buf->clock_rate( (long) (1789773 / 60.0 * rate) );
}

blargg_err_t Nes_Emu::set_sample_rate( long rate, Multi_Buffer* new_buf )
{
	require( new_buf );
	RETURN_ERR( auto_init() );
	emu.impl->apu.volume( 1.0 ); // cancel any previous non-linearity
	RETURN_ERR( new_buf->set_sample_rate( rate, 1200 / frame_rate ) );
	sound_buf = new_buf;
	sound_buf_changed_count = 0;
	if ( new_buf != default_sound_buf )
	{
		delete default_sound_buf;
		default_sound_buf = NULL;
	}
	set_frame_rate( frame_rate );
	return 0;
}

blargg_err_t Nes_Emu::set_sample_rate( long rate )
{
	if ( !default_sound_buf )
		CHECK_ALLOC( default_sound_buf = BLARGG_NEW Mono_Buffer );
	return set_sample_rate( rate, default_sound_buf );
}

void Nes_Emu::set_equalizer( equalizer_t const& eq )
{
	equalizer_ = eq;
	if ( cart() )
	{
		blip_eq_t blip_eq( eq.treble, 0, sound_buf->sample_rate() );
		emu.impl->apu.treble_eq( blip_eq );
		emu.mapper->set_treble( blip_eq );
		sound_buf->bass_freq( equalizer_.bass );
	}
}

void Nes_Emu::enable_sound( bool enabled )
{
	if ( enabled )
	{
		for ( int i = channel_count(); i-- > 0; )
		{
			Blip_Buffer* buf = sound_buf->channel( i ).center;
			int mapper_index = i - Nes_Apu::osc_count;
			if ( mapper_index < 0 )
				emu.impl->apu.osc_output( i, buf );
			else
				emu.mapper->set_channel_buf( mapper_index, buf );
		}
	}
	else
	{
		emu.impl->apu.output( NULL );
		for ( int i = channel_count() - Nes_Apu::osc_count; i-- > 0; )
			emu.mapper->set_channel_buf( i, NULL );
	}
}

void Nes_Emu::fade_samples( blip_sample_t* p, int size, int step )
{
	if ( size >= sound_fade_size )
	{
		if ( step < 0 )
			p += size - sound_fade_size;
		
		int const shift = 15;
		int mul = (1 - step) << (shift - 1);
		step *= (1 << shift) / sound_fade_size;
		
		for ( int n = sound_fade_size; n--; )
		{
			*p = (*p * mul) >> 15;
			++p;
			mul += step;
		}
	}
}

long Nes_Emu::read_samples( short* out, long out_size )
{
	require( out_size >= sound_buf->samples_avail() );
	long count = sound_buf->read_samples( out, out_size );
	if ( fade_sound_in )
	{
		fade_sound_in = false;
		fade_samples( out, count, 1 );
	}
	
	if ( fade_sound_out )
	{
		fade_sound_out = false;
		fade_sound_in = true; // next buffer should be faded in
		fade_samples( out, count, -1 );
	}
	return count;
}

Nes_Emu::rgb_t const Nes_Emu::nes_colors [color_table_size] =
{
	// generated with nes_ntsc default settings
	{102,102,102},{  0, 42,136},{ 20, 18,168},{ 59,  0,164},
	{ 92,  0,126},{110,  0, 64},{108,  7,  0},{ 87, 29,  0},
	{ 52, 53,  0},{ 12, 73,  0},{  0, 82,  0},{  0, 79,  8},
	{  0, 64, 78},{  0,  0,  0},{  0,  0,  0},{  0,  0,  0},
	{174,174,174},{ 21, 95,218},{ 66, 64,254},{118, 39,255},
	{161, 27,205},{184, 30,124},{181, 50, 32},{153, 79,  0},
	{108,110,  0},{ 56,135,  0},{ 13,148,  0},{  0,144, 50},
	{  0,124,142},{  0,  0,  0},{  0,  0,  0},{  0,  0,  0},
	{254,254,254},{100,176,254},{147,144,254},{199,119,254},
	{243,106,254},{254,110,205},{254,130,112},{235,159, 35},
	{189,191,  0},{137,217,  0},{ 93,229, 48},{ 69,225,130},
	{ 72,206,223},{ 79, 79, 79},{  0,  0,  0},{  0,  0,  0},
	{254,254,254},{193,224,254},{212,211,254},{233,200,254},
	{251,195,254},{254,197,235},{254,205,198},{247,217,166},
	{229,230,149},{208,240,151},{190,245,171},{180,243,205},
	{181,236,243},{184,184,184},{  0,  0,  0},{  0,  0,  0},
	
	{114, 83, 79},{  0, 23,113},{ 32,  0,145},{ 71,  0,141},
	{104,  0,103},{122,  0, 41},{120,  0,  0},{ 99, 10,  0},
	{ 64, 34,  0},{ 24, 54,  0},{  0, 63,  0},{  0, 60,  0},
	{  0, 45, 54},{  0,  0,  0},{  0,  0,  0},{  0,  0,  0},
	{190,148,143},{ 37, 69,187},{ 83, 38,228},{134, 13,224},
	{177,  1,174},{200,  4, 92},{198, 24,  1},{170, 53,  0},
	{124, 84,  0},{ 73,109,  0},{ 30,122,  0},{  6,118, 19},
	{  9, 98,110},{  0,  0,  0},{  0,  0,  0},{  0,  0,  0},
	{254,222,215},{122,142,254},{168,110,254},{220, 85,254},
	{254, 72,247},{254, 76,164},{254, 96, 71},{254,125,  0},
	{210,157,  0},{158,183,  0},{114,195,  7},{ 90,191, 89},
	{ 93,172,182},{ 79, 79, 79},{  0,  0,  0},{  0,  0,  0},
	{254,222,215},{214,190,233},{233,177,250},{254,166,248},
	{254,161,228},{254,163,194},{254,171,157},{254,183,125},
	{250,196,108},{229,206,110},{211,211,130},{201,210,164},
	{203,202,202},{184,184,184},{  0,  0,  0},{  0,  0,  0},
	{ 75,106, 64},{  0, 46, 98},{  0, 22,130},{ 32,  3,126},
	{ 65,  0, 88},{ 82,  0, 26},{ 80, 11,  0},{ 59, 34,  0},
	{ 24, 58,  0},{  0, 77,  0},{  0, 86,  0},{  0, 83,  0},
	{  0, 68, 39},{  0,  0,  0},{  0,  0,  0},{  0,  0,  0},
	{136,180,122},{  0,101,166},{ 29, 69,208},{ 80, 44,203},
	{123, 32,153},{146, 36, 72},{144, 55,  0},{116, 84,  0},
	{ 70,116,  0},{ 19,141,  0},{  0,153,  0},{  0,149,  0},
	{  0,130, 90},{  0,  0,  0},{  0,  0,  0},{  0,  0,  0},
	{207,254,188},{ 51,183,233},{ 98,151,254},{150,126,254},
	{193,113,220},{217,117,137},{214,137, 45},{186,166,  0},
	{140,198,  0},{ 88,224,  0},{ 44,236,  0},{ 20,232, 63},
	{ 23,213,155},{ 79, 79, 79},{  0,  0,  0},{  0,  0,  0},
	{207,254,188},{144,231,207},{163,218,224},{184,207,222},
	{201,202,201},{211,204,168},{210,212,130},{198,224, 99},
	{180,237, 81},{159,247, 83},{141,252,104},{131,251,137},
	{132,243,175},{184,184,184},{  0,  0,  0},{  0,  0,  0},
	{ 83, 83, 55},{  0, 23, 89},{  0,  0,121},{ 40,  0,117},
	{ 73,  0, 79},{ 90,  0, 17},{ 88,  0,  0},{ 67, 10,  0},
	{ 32, 34,  0},{  0, 53,  0},{  0, 63,  0},{  0, 60,  0},
	{  0, 45, 30},{  0,  0,  0},{  0,  0,  0},{  0,  0,  0},
	{147,148,110},{  0, 69,154},{ 40, 38,196},{ 91, 12,191},
	{134,  0,141},{157,  4, 60},{155, 23,  0},{127, 52,  0},
	{ 81, 84,  0},{ 30,109,  0},{  0,121,  0},{  0,117,  0},
	{  0, 98, 78},{  0,  0,  0},{  0,  0,  0},{  0,  0,  0},
	{221,222,173},{ 65,142,217},{112,110,254},{164, 84,255},
	{208, 72,204},{231, 76,122},{229, 95, 29},{200,125,  0},
	{154,157,  0},{102,182,  0},{ 58,195,  0},{ 34,191, 47},
	{ 37,171,140},{ 79, 79, 79},{  0,  0,  0},{  0,  0,  0},
	{221,222,173},{158,189,191},{177,176,208},{198,166,206},
	{216,161,185},{225,163,152},{224,171,114},{213,183, 83},
	{194,195, 66},{173,206, 68},{155,211, 88},{145,209,122},
	{146,201,159},{184,184,184},{  0,  0,  0},{  0,  0,  0},
	{ 87, 87,133},{  0, 26,167},{  5,  2,198},{ 44,  0,195},
	{ 77,  0,157},{ 95,  0, 94},{ 93,  0, 25},{ 71, 14,  0},
	{ 36, 38,  0},{  0, 57,  0},{  0, 66,  0},{  0, 63, 38},
	{  0, 49,108},{  0,  0,  0},{  0,  0,  0},{  0,  0,  0},
	{153,153,216},{  0, 74,254},{ 46, 43,254},{ 97, 17,254},
	{140,  5,247},{164,  9,165},{161, 28, 74},{133, 57,  0},
	{ 87, 89,  0},{ 36,114,  0},{  0,126, 10},{  0,122, 92},
	{  0,103,183},{  0,  0,  0},{  0,  0,  0},{  0,  0,  0},
	{229,228,254},{ 74,148,254},{120,116,254},{172, 91,254},
	{216, 78,254},{239, 82,254},{237,102,166},{208,131, 89},
	{162,163, 46},{110,189, 51},{ 66,201,102},{ 42,197,184},
	{ 45,178,254},{ 79, 79, 79},{  0,  0,  0},{  0,  0,  0},
	{229,228,254},{166,196,254},{185,183,254},{206,172,254},
	{224,167,254},{233,169,254},{232,177,252},{221,189,220},
	{202,202,203},{181,212,205},{163,217,226},{153,216,254},
	{154,208,254},{184,184,184},{  0,  0,  0},{  0,  0,  0},
	{ 90, 71, 97},{  0, 11,130},{  8,  0,162},{ 47,  0,158},
	{ 80,  0,120},{ 98,  0, 58},{ 96,  0,  0},{ 74,  0,  0},
	{ 39, 22,  0},{  0, 42,  0},{  0, 51,  0},{  0, 48,  2},
	{  0, 33, 72},{  0,  0,  0},{  0,  0,  0},{  0,  0,  0},
	{158,132,166},{  4, 53,210},{ 50, 22,252},{101,  0,247},
	{144,  0,197},{168,  0,116},{165,  7, 25},{137, 36,  0},
	{ 91, 68,  0},{ 40, 93,  0},{  0,105,  0},{  0,101, 42},
	{  0, 82,134},{  0,  0,  0},{  0,  0,  0},{  0,  0,  0},
	{234,201,246},{ 79,121,254},{125, 89,254},{177, 63,254},
	{221, 51,254},{245, 55,195},{242, 74,102},{214,104, 24},
	{167,136,  0},{115,161,  0},{ 71,174, 37},{ 48,170,120},
	{ 50,150,213},{ 79, 79, 79},{  0,  0,  0},{  0,  0,  0},
	{234,201,246},{171,168,254},{190,155,254},{211,145,254},
	{229,140,254},{239,142,225},{237,150,187},{226,162,156},
	{207,174,139},{186,185,141},{168,190,161},{159,188,195},
	{160,180,232},{184,184,184},{  0,  0,  0},{  0,  0,  0},
	{ 66, 85, 88},{  0, 25,121},{  0,  1,153},{ 23,  0,149},
	{ 56,  0,111},{ 74,  0, 49},{ 72,  0,  0},{ 51, 12,  0},
	{ 16, 36,  0},{  0, 55,  0},{  0, 65,  0},{  0, 62,  0},
	{  0, 47, 63},{  0,  0,  0},{  0,  0,  0},{  0,  0,  0},
	{125,151,154},{  0, 72,198},{ 17, 40,240},{ 69, 15,235},
	{112,  3,185},{135,  7,104},{132, 26, 12},{104, 55,  0},
	{ 59, 87,  0},{  7,112,  0},{  0,124,  0},{  0,120, 30},
	{  0,101,121},{  0,  0,  0},{  0,  0,  0},{  0,  0,  0},
	{192,225,230},{ 37,145,254},{ 83,114,254},{135, 88,254},
	{179, 76,254},{202, 80,179},{200, 99, 86},{171,129,  8},
	{125,160,  0},{ 73,186,  0},{ 29,198, 21},{  5,194,104},
	{  8,175,197},{ 79, 79, 79},{  0,  0,  0},{  0,  0,  0},
	{192,225,230},{129,193,248},{148,180,254},{169,170,254},
	{187,165,242},{196,166,209},{195,174,171},{184,186,140},
	{165,199,123},{144,209,125},{126,214,145},{116,213,179},
	{118,205,216},{184,184,184},{  0,  0,  0},{  0,  0,  0},
	{ 69, 69, 69},{  0, 16,110},{  0,  0,142},{ 33,  0,138},
	{ 66,  0,100},{ 84,  0, 38},{ 82,  0,  0},{ 60,  3,  0},
	{ 25, 27,  0},{  0, 46,  0},{  0, 56,  0},{  0, 53,  0},
	{  0, 38, 51},{  0,  0,  0},{  0,  0,  0},{  0,  0,  0},
	{134,134,134},{  0, 64,187},{ 35, 32,228},{ 86,  7,223},
	{129,  0,174},{153,  0, 92},{150, 18,  1},{122, 47,  0},
	{ 76, 79,  0},{ 25,104,  0},{  0,116,  0},{  0,112, 19},
	{  0, 93,110},{  0,  0,  0},{  0,  0,  0},{  0,  0,  0},
	{207,207,207},{ 60,136,254},{107,104,254},{159, 79,254},
	{203, 66,248},{226, 70,165},{224, 90, 72},{195,119,  0},
	{149,151,  0},{ 97,177,  0},{ 53,189,  8},{ 29,185, 91},
	{ 32,166,183},{ 79, 79, 79},{  0,  0,  0},{  0,  0,  0},
	{207,207,207},{148,178,229},{166,165,246},{188,155,244},
	{205,150,224},{215,152,190},{214,159,152},{202,171,121},
	{183,184,104},{162,195,106},{145,200,126},{135,198,160},
	{136,190,197},{184,184,184},{  0,  0,  0},{  0,  0,  0}
};

void Nes_Emu::get_regs(unsigned int *dest) const
{
	dest[0] = emu.r.a;
	dest[1] = emu.r.x;
	dest[2] = emu.r.y;
	dest[3] = emu.r.sp;
	dest[4] = emu.r.pc;
	dest[5] = emu.r.status;
}
