// NES PPU misc functions and setup

// Nes_Emu 0.7.0

#ifndef NES_PPU_IMPL_H
#define NES_PPU_IMPL_H

#include "nes_data.h"
class Nes_State_;

class Nes_Ppu_Impl : public ppu_state_t {
public:
	typedef BOOST::uint8_t byte;
	typedef BOOST::uint32_t uint32_t;
	
	Nes_Ppu_Impl();
	~Nes_Ppu_Impl();
	
	void reset( bool full_reset );
	
	// Setup
	blargg_err_t open_chr( const byte*, long size );
	void rebuild_chr( unsigned long begin, unsigned long end );
	void close_chr();
	void save_state( Nes_State_* out ) const;
	void load_state( Nes_State_ const& );
	
	enum { image_width = 256 };
	enum { image_height = 240 };
	enum { image_left = 8 };
	enum { buffer_width = image_width + 16 };
	enum { buffer_height = image_height };
	
	int write_2007( int );
	int peekaddr(int);
	
	// Host palette
	enum { palette_increment = 64 };
	short* host_palette;
	int palette_begin;
	int max_palette_size;
	int palette_size; // set after frame is rendered
	
	// Mapping
	enum { vaddr_clock_mask = 0x1000 };
	void set_nt_banks( int bank0, int bank1, int bank2, int bank3 );
	void set_chr_bank( int addr, int size, long data );
	void set_chr_bank_ex( int addr, int size, long data ); // mmc24 only
	
	// Nametable and CHR RAM
	enum { nt_ram_size = 0x1000 };
	enum { chr_addr_size = 0x2000 };
	enum { bytes_per_tile = 16 };
	enum { chr_tile_count = chr_addr_size / bytes_per_tile };
	enum { mini_offscreen_height = 16 }; // double-height sprite
	struct impl_t
	{
		byte nt_ram [nt_ram_size];
		byte chr_ram [chr_addr_size];
		union {
			BOOST::uint32_t clip_buf [256 * 2];
			byte mini_offscreen [buffer_width * mini_offscreen_height];
		};
	};
	impl_t* impl;
	enum { scanline_len = 341 };
	
	byte spr_ram [0x100];
protected:
	void begin_frame();
	void run_hblank( int );
	int sprite_height() const { return (w2000 >> 2 & 8) + 8; }
	
protected: //friend class Nes_Ppu; private:
	
	int addr_inc; // pre-calculated $2007 increment (based on w2001 & 0x04)
	int read_2007( int addr );
	
	enum { last_sprite_max_scanline = 240 };
	long recalc_sprite_max( int scanline );
	int first_opaque_sprite_line() /*const*/;
	
protected: //friend class Nes_Ppu_Rendering; private:

	unsigned long palette_offset;
	int palette_changed;
	void capture_palette();
	
	bool any_tiles_modified;
	bool chr_is_writable;
	void update_tiles( int first_tile );
	
	typedef uint32_t cache_t;
	typedef cache_t cached_tile_t [4];
	cached_tile_t const& get_bg_tile( int index ) /*const*/;
	cached_tile_t const& get_sprite_tile( byte const* sprite ) /*const*/;
	byte* get_nametable( int addr ) { return nt_banks [addr >> 10 & 3]; };
	
private:
	
	static int map_palette( int addr );
	int sprite_tile_index( byte const* sprite ) const;
	
	// Mapping
	enum { chr_page_size = 0x400 };
	long chr_pages [chr_addr_size / chr_page_size];
	long chr_pages_ex [chr_addr_size / chr_page_size]; // mmc24 only
	long map_chr_addr_peek( unsigned a ) const
	{
		return chr_pages[a / chr_page_size] + a;
	}

	long map_chr_addr( unsigned a ) /*const*/
	{
		if (!mmc24_enabled)
			return chr_pages [a / chr_page_size] + a;

		// mmc24 calculations

		int page = a >> 12 & 1;
		// can't check against bit 3 of address, because quicknes never actually fetches those
		int newval0 = (a & 0xff0) != 0xfd0;
		int newval1 = (a & 0xff0) == 0xfe0;

		long ret;
		if (mmc24_latched[page])
			ret = chr_pages_ex [a / chr_page_size] + a;
		else
			ret = chr_pages [a / chr_page_size] + a;

		mmc24_latched[page] &= newval0;
		mmc24_latched[page] |= newval1;

		return ret;
	}
	byte* nt_banks [4];
	
	bool mmc24_enabled; // true if mmc24 regs need to be latched and checked
	byte mmc24_latched [2]; // current latch value for the first\second 4k of memory

	// CHR data
	byte const* chr_data; // points to chr ram when there is no read-only data
	byte* chr_ram; // always points to impl->chr_ram; makes write_2007() faster
	long chr_size;
	byte const* map_chr( int addr ) /*const*/ { return &chr_data [map_chr_addr( addr )]; }
	
	// CHR cache
	cached_tile_t* tile_cache;
	cached_tile_t* flipped_tiles;
	byte* tile_cache_mem;
	union {
		byte modified_tiles [chr_tile_count / 8];
		uint32_t align_;
	};
	void all_tiles_modified();
	void update_tile( int index );
};

inline void Nes_Ppu_Impl::set_nt_banks( int bank0, int bank1, int bank2, int bank3 )
{
	byte* nt_ram = impl->nt_ram;
	nt_banks [0] = &nt_ram [bank0 * 0x400];
	nt_banks [1] = &nt_ram [bank1 * 0x400];
	nt_banks [2] = &nt_ram [bank2 * 0x400];
	nt_banks [3] = &nt_ram [bank3 * 0x400];
}

inline int Nes_Ppu_Impl::map_palette( int addr )
{
	if ( (addr & 3) == 0 )
		addr &= 0x0f; // 0x10, 0x14, 0x18, 0x1c map to 0x00, 0x04, 0x08, 0x0c
	return addr & 0x1f;
}

inline int Nes_Ppu_Impl::sprite_tile_index( byte const* sprite ) const
{
	int tile = sprite [1] + (w2000 << 5 & 0x100);
	if ( w2000 & 0x20 )
		tile = (tile & 1) * 0x100 + (tile & 0xfe);
	return tile;
}

inline int Nes_Ppu_Impl::write_2007( int data )
{
	int addr = vram_addr;
	byte* chr_ram = this->chr_ram; // pre-read
	int changed = addr + addr_inc;
	unsigned const divisor = bytes_per_tile * 8;
	int mod_index = (unsigned) addr / divisor % (0x4000 / divisor);
	vram_addr = changed;
	changed ^= addr;
	addr &= 0x3fff;
	
	// use index into modified_tiles [] since it's calculated sooner than addr is masked
	if ( (unsigned) mod_index < 0x2000 / divisor )
	{
		// Avoid overhead of checking for read-only CHR; if that is the case,
		// this modification will be ignored.
		int mod = modified_tiles [mod_index];
		chr_ram [addr] = data;
		any_tiles_modified = true;
		modified_tiles [mod_index] = mod | (1 << ((unsigned) addr / bytes_per_tile % 8));
	}
	else if ( addr < 0x3f00 )
	{
		get_nametable( addr ) [addr & 0x3ff] = data;
	}
	else
	{
		data &= 0x3f;
		byte& entry = palette [map_palette( addr )];
		int changed = entry ^ data;
		entry = data;
		if ( changed )
			palette_changed = 0x18;
	}
	
	return changed;
}

inline void Nes_Ppu_Impl::begin_frame()
{
	palette_changed = 0x18;
	palette_size = 0;
	palette_offset = palette_begin * 0x01010101;
	addr_inc = w2000 & 4 ? 32 : 1;
}

#endif

