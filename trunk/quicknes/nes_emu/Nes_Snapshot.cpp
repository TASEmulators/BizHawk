
// Nes_Emu 0.5.6. http://www.slack.net/~ant/

#include "Nes_Snapshot.h"

#include <stdlib.h>
#include <string.h>

#include "blargg_endian.h"
#include "Nes_Emu.h"
#include "Nes_Mapper.h"

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

typedef BOOST::uint8_t byte;

Nes_Snapshot_Array::Nes_Snapshot_Array()
{
	data = NULL;
	size_ = 0;
}

Nes_Snapshot_Array::~Nes_Snapshot_Array()
{
	free( data );
}
	
blargg_err_t Nes_Snapshot_Array::resize( int new_size )
{
	void* new_mem = realloc( data, new_size * sizeof (Nes_Snapshot) );
	BLARGG_CHECK_ALLOC( !new_size || new_mem );
	data = (Nes_Snapshot*) new_mem;
	
	int old_size = size_;
	size_ = new_size;
	for ( int i = old_size; i < new_size; i++ )
		(*this) [i].clear();
	
	return blargg_success;
}

void Nes_Snapshot::clear()
{
	memset( &nes, 0, sizeof nes );
	nes.frame_count = invalid_frame_count;
	
	nes_valid      = false;
	cpu_valid      = false;
	joypad_valid   = false;
	apu_valid      = false;
	ppu_valid      = false;
	mapper_valid   = false;
	ram_valid      = false;
	sram_size      = 0;
	spr_ram_valid  = false;
	nametable_size = 0;
	chr_size       = 0;
}

// write

blargg_err_t Nes_Snapshot_Writer::end( Nes_Emu const& emu )
{
	Nes_Snapshot_Array snapshots;
	BLARGG_RETURN_ERR( snapshots.resize( 1 ) );
	emu.save_snapshot( &snapshots [0] );
	return end( snapshots [0] );
}

blargg_err_t Nes_Snapshot_Writer::end( Nes_Snapshot const& ss )
{
	BLARGG_RETURN_ERR( ss.write_blocks( *this ) );
	return Nes_File_Writer::end();
}

blargg_err_t Nes_Snapshot::write( Data_Writer& out ) const
{
	Nes_Snapshot_Writer writer;
	BLARGG_RETURN_ERR( writer.begin( &out ) );
	return writer.end( *this );
}

blargg_err_t Nes_Snapshot::write_blocks( Nes_File_Writer& out ) const
{
	if ( nes_valid )
	{
		nes_state_t s = nes;
		s.pal = false;
		s.timestamp = nes.timestamp * 15;
		BLARGG_RETURN_ERR( write_nes_state( out, s ) );
	}
	
	if ( cpu_valid )
	{
		cpu_state_t s;
		memset( &s, 0, sizeof s );
		s.pc = cpu.pc;
		s.s = cpu.sp;
		s.a = cpu.a;
		s.x = cpu.x;
		s.y = cpu.y;
		s.p = cpu.status;
		BLARGG_RETURN_ERR( write_nes_state( out, s ) );
	}
	
	if ( ppu_valid )
	{
		ppu_state_t s = ppu;
		BLARGG_RETURN_ERR( write_nes_state( out, s ) );
	}
	
	if ( apu_valid )
	{
		apu_snapshot_t s = apu;
		BLARGG_RETURN_ERR( write_nes_state( out, s ) );
	}
	
	if ( joypad_valid )
	{
		joypad_state_t s = joypad;
		BLARGG_RETURN_ERR( write_nes_state( out, s ) );
	}
	
	if ( mapper_valid )
		BLARGG_RETURN_ERR( out.write_block( 'MAPR', mapper.data, mapper.size ) );
	
	if ( ram_valid )
		BLARGG_RETURN_ERR( out.write_block( 'LRAM', ram, sizeof ram ) );
	
	if ( spr_ram_valid )
		BLARGG_RETURN_ERR( out.write_block( 'SPRT', spr_ram, sizeof spr_ram ) );
	
	if ( nametable_size )
		BLARGG_RETURN_ERR( out.write_block( 'NTAB', nametable, nametable_size ) );
	
	if ( chr_size )
		BLARGG_RETURN_ERR( out.write_block( 'CHRR', chr, chr_size ) );
	
	if ( sram_size )
		BLARGG_RETURN_ERR( out.write_block( 'SRAM', sram, sram_size ) );
	
	return blargg_success;
}

// read

Nes_Snapshot_Reader::Nes_Snapshot_Reader()
{
	snapshot_ = NULL;
}

Nes_Snapshot_Reader::~Nes_Snapshot_Reader()
{
}

blargg_err_t Nes_Snapshot_Reader::begin( Data_Reader* dr, Nes_Snapshot* out )
{
	snapshot_ = out;
	if ( !out )
	{
		BLARGG_RETURN_ERR( snapshots.resize( 1 ) );
		snapshot_ = &snapshots [0];
	}
	
	BLARGG_RETURN_ERR( Nes_File_Reader::begin( dr ) );
	if ( block_tag() != snapshot_file_tag )
		return "Not a snapshot file";
	return blargg_success;
}

blargg_err_t Nes_Snapshot::read( Data_Reader& in )
{
	Nes_Snapshot_Reader reader;
	BLARGG_RETURN_ERR( reader.begin( &in, this ) );
	while ( !reader.done() )
		BLARGG_RETURN_ERR( reader.next_block() );
	
	return blargg_success;
}

blargg_err_t Nes_Snapshot_Reader::next_block()
{
	if ( depth() != 0 )
		return Nes_File_Reader::next_block();
	return snapshot_->read_blocks( *this );
}

void Nes_Snapshot::set_nes_state( nes_state_t const& s )
{
	nes = s;
	nes.timestamp /= 15;
	nes_valid = true;
}

blargg_err_t Nes_Snapshot::read_blocks( Nes_File_Reader& in )
{
	while ( true )
	{
		BLARGG_RETURN_ERR( in.next_block() );
		switch ( in.block_tag() )
		{
			case nes.tag:
				memset( &nes, 0, sizeof nes );
				BLARGG_RETURN_ERR( read_nes_state( in, &nes ) );
				set_nes_state( nes );
				break;
			
			case cpu_state_t::tag: {
				cpu_state_t s;
				memset( &s, 0, sizeof s );
				BLARGG_RETURN_ERR( read_nes_state( in, &s ) );
				cpu.pc = s.pc;
				cpu.sp = s.s;
				cpu.a = s.a;
				cpu.x = s.x;
				cpu.y = s.y;
				cpu.status = s.p;
				cpu_valid = true;
				break;
			}
			
			case ppu.tag:
				memset( &ppu, 0, sizeof ppu );
				BLARGG_RETURN_ERR( read_nes_state( in, &ppu ) );
				ppu_valid = true;
				break;
			
			case apu.tag:
				memset( &apu, 0, sizeof apu );
				BLARGG_RETURN_ERR( read_nes_state( in, &apu ) );
				apu_valid = true;
				break;
			
			case joypad.tag:
				memset( &joypad, 0, sizeof joypad );
				BLARGG_RETURN_ERR( read_nes_state( in, &joypad ) );
				joypad_valid = true;
				break;
			
			case 'MAPR':
				mapper.size = in.remain();
				BLARGG_RETURN_ERR( in.read_block_data( mapper.data, sizeof mapper.data ) );
				mapper_valid = true;
				break;
			
			case 'SPRT':
				spr_ram_valid = true;
				BLARGG_RETURN_ERR( in.read_block_data( spr_ram, sizeof spr_ram ) );
				break;
				
			case 'NTAB':
				nametable_size = in.remain();
				BLARGG_RETURN_ERR( in.read_block_data( nametable, sizeof nametable ) );
				break;
				
			case 'LRAM':
				ram_valid = true;
				BLARGG_RETURN_ERR( in.read_block_data( ram, sizeof ram ) );
				break;
				
			case 'CHRR':
				chr_size = in.remain();
				BLARGG_RETURN_ERR( in.read_block_data( chr, sizeof chr ) );
				break;
				
			case 'SRAM':
				sram_size = in.remain();
				BLARGG_RETURN_ERR( in.read_block_data( sram, sizeof sram ) );
				break; 
			
			default:
				return blargg_success;
		}
	}
}

// read_sta_file

struct sta_regs_t {
	byte pc [2];
	byte a;
	byte p;
	byte x;
	byte y;
	byte s;
};
BOOST_STATIC_ASSERT( sizeof (sta_regs_t) == 7 );

blargg_err_t Nes_Snapshot::read_sta_file( Data_Reader& in )
{
	sram_size = 0x2000;
	BLARGG_RETURN_ERR( in.read( sram, sram_size ) );
	
	ram_valid = true;
	BLARGG_RETURN_ERR( in.read( ram, 0x800 ) );
	
	sta_regs_t r;
	BLARGG_RETURN_ERR( in.read( &r, sizeof r ) );
	this->cpu.pc = r.pc [1] * 0x100 + r.pc [0];
	this->cpu.a = r.a;
	this->cpu.status = r.p;
	this->cpu.x = r.x;
	this->cpu.y = r.y;
	this->cpu.sp = r.s;
	cpu_valid = true;
	
	BLARGG_RETURN_ERR( in.read( spr_ram, 0x100 ) );
	spr_ram_valid = true;
	
	chr_size = 0x2000;
	BLARGG_RETURN_ERR( in.read( chr, chr_size ) );
	
	nametable_size = 0x1000;
	BLARGG_RETURN_ERR( in.read( nametable, nametable_size ) );
	
	return blargg_success;
}

