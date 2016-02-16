
// NES video game console emulator with snapshot support

// Nes_Emu 0.7.0

#ifndef NES_EMU_H
#define NES_EMU_H

#include "blargg_common.h"
#include "Multi_Buffer.h"
#include "Nes_Cart.h"
#include "Nes_Core.h"
class Nes_State;

// Register optional mappers included with Nes_Emu
void register_optional_mappers();

extern const char unsupported_mapper []; // returned when cartridge uses unsupported mapper

class Nes_Emu {
public:
	Nes_Emu();
	virtual ~Nes_Emu();
	
// Basic setup

	// Load iNES file into emulator and clear recording
	blargg_err_t load_ines( Auto_File_Reader );
	
	// Set sample rate for sound generation
	blargg_err_t set_sample_rate( long );
	
	// Size and depth of graphics buffer required for rendering. Note that this
	// is larger than the actual image, with a temporary area around the edge
	// that gets filled with junk. Its height is many times larger in Nes_Recorder
	// to allow caching of multiple images.
	enum { buffer_width  = Nes_Ppu::buffer_width };
	int buffer_height() const { return buffer_height_; }
	enum { bits_per_pixel = 8 };

private:
	// Set graphics buffer to render pixels to. Pixels points to top-left pixel and
	// row_bytes is the number of bytes to get to the next line (positive or negative).
	void set_pixels( void* pixels, long row_bytes );
public:

	// Size of image generated in graphics buffer
	enum { image_width   = 256 };
	enum { image_height  = 240 };
	
// Basic emulation

	// Emulate one video frame using joypad1 and joypad2 as input. Afterwards, image
	// and sound are available for output using the accessors below.
	// A connected controller should have 0xffffff** in the high bits, or 0x000000**
	// if emulating an incorrectly made third party controller.  A disconnected controller
	// should be 0x00000000 exactly.
	virtual blargg_err_t emulate_frame( uint32_t joypad1, uint32_t joypad2 );
	
	// Maximum size of palette that can be generated
	enum { max_palette_size = 256 };
	
	// Result of current frame
	struct frame_t
	{
		int joypad_read_count;  // number of times joypads were strobed (read)
		int burst_phase;        // NTSC burst phase for frame (0, 1, or 2)
		
		int sample_count;       // number of samples (always a multiple of chan_count)
		int chan_count;         // 1: mono, 2: stereo
		
		int top;                // top-left position of image in graphics buffer
		enum { left = 8 };
		unsigned char* pixels;  // pointer to top-left pixel of image
		long pitch;             // number of bytes to get to next row of image
		
		int palette_begin;      // first host palette entry, as set by set_palette_range()
		int palette_size;       // number of entries used for current frame
		short palette [max_palette_size]; // [palette_begin to palette_begin+palette_size-1]
	};
	frame_t const& frame() const { return *frame_; }
	
	// Read samples for the current frame. Returns number of samples read into buffer.
	// Currently all samples must be read in one call.
	virtual long read_samples( short* out, long max_samples );
	
// Additional features
	
	// Use already-loaded cartridge. Retains pointer, so it must be kept around until
	// closed. A cartridge can be shared among multiple emulators. After opening,
	// cartridge's CHR data shouldn't be modified since a copy is cached internally.
	blargg_err_t set_cart( Nes_Cart const* );
	
	// Pointer to current cartridge, or NULL if none is loaded
	Nes_Cart const* cart() const { return emu.cart; }
	
	// Free any memory and close cartridge, if one was currently open. A new cartridge
	// must be opened before further emulation can take place.
	void close();
	
	// Emulate powering NES off and then back on. If full_reset is false, emulates
	// pressing the reset button only, which doesn't affect memory, otherwise
	// emulates powering system off then on.
	virtual void reset( bool full_reset = true, bool erase_battery_ram = false );
	
	// Number of undefined CPU instructions encountered. Cleared after reset() and
	// load_state(). A non-zero value indicates that cartridge is probably
	// incompatible.
	unsigned long error_count() const { return emu.error_count; }
	
// Sound
	
	// Set sample rate and use a custom sound buffer instead of the default
	// mono buffer, i.e. Nes_Buffer, Effects_Buffer, etc..
	blargg_err_t set_sample_rate( long rate, Multi_Buffer* );
	
	// Adjust effective frame rate by changing how many samples are generated each frame.
	// Allows fine tuning of frame rate to improve synchronization.
	void set_frame_rate( double rate );
	
	// Number of sound channels for current cartridge
	int channel_count() const { return channel_count_; }
	
	// Frequency equalizer parameters
	struct equalizer_t {
		double treble; // 5.0 = extra-crisp, -200.0 = muffled
		long bass;     // 0 = deep, 20000 = tinny
	};
	
	// Current frequency equalization
	equalizer_t const& equalizer() const { return equalizer_; }
	
	// Change frequency equalization
	void set_equalizer( equalizer_t const& );
	
	// Equalizer presets
	static equalizer_t const nes_eq;        // NES
	static equalizer_t const famicom_eq;    // Famicom
	static equalizer_t const tv_eq;         // TV speaker
	
// File save/load

	// Save emulator state
	void save_state( Nes_State* s ) const { emu.save_state( s ); }
	blargg_err_t save_state( Auto_File_Writer ) const;
	
	// Load state into emulator
	void load_state( Nes_State const& );
	blargg_err_t load_state( Auto_File_Reader );
	
	// True if current cartridge claims it uses battery-backed memory
	bool has_battery_ram() const { return cart()->has_battery_ram(); }
	
	// Save current battery RAM
	blargg_err_t save_battery_ram( Auto_File_Writer );
	
	// Load battery RAM from file. Best called just after reset() or loading cartridge.
	blargg_err_t load_battery_ram( Auto_File_Reader );
	
// Graphics

	// Number of frames generated per second
	enum { frame_rate = 60 };
	
	// Size of fixed NES color table (including the 8 color emphasis modes)
	enum { color_table_size = 8 * 64 };
	
	// NES color lookup table based on standard NTSC TV decoder. Use nes_ntsc.h to
	// generate a palette with custom parameters.
	struct rgb_t { unsigned char red, green, blue; };
	static rgb_t const nes_colors [color_table_size];
	
	// Hide/show/enhance sprites. Sprite mode does not affect emulation accuracy.
	enum sprite_mode_t {
		sprites_hidden = 0,
		sprites_visible = 8,  // limit of 8 sprites per scanline as on NES (default)
		sprites_enhanced = 64 // unlimited sprites per scanline (no flickering)
	};
	void set_sprite_mode( sprite_mode_t n ) { emu.ppu.sprite_limit = n; }
	
	// Set range of host palette entries to use in graphics buffer; default uses
	// all of them. Begin will be rounded up to next multiple of palette_alignment.
	// Use frame().palette_begin to find the adjusted beginning entry used.
	enum { palette_alignment = 64 };
	void set_palette_range( int begin, int end = 256 );
	
// Access to emulated memory, for viewer/cheater/debugger
	
	// CHR
	byte const* chr_mem();
	long chr_size() const;
	void write_chr( void const*, long count, long offset );
	
	// Nametable
	byte* nametable_mem()       { return emu.ppu.impl->nt_ram; }
	long nametable_size() const { return 0x1000; }
	
	// Built-in 2K memory
	enum { low_mem_size = 0x800 };
	byte* low_mem()             { return emu.low_mem; }
	
	// Optional 8K memory
	enum { high_mem_size = 0x2000 };
	byte* high_mem()            { return emu.impl->sram; }

// Prg peek/poke for debuggin
	byte peek_prg(nes_addr_t addr) const { return *static_cast<Nes_Cpu>(emu).get_code(addr); }
	void poke_prg(nes_addr_t addr, byte value) { *static_cast<Nes_Cpu>(emu).get_code(addr) = value; }
	byte peek_ppu(int addr) { return emu.ppu.peekaddr(addr); }

	void get_regs(unsigned int *dest) const;

	byte get_ppu2000() const { return emu.ppu.w2000; }
	byte* pal_mem() { return emu.ppu.palette; }
	byte* oam_mem() { return emu.ppu.spr_ram; }

	void set_tracecb(void (*cb)(unsigned int *dest)) { emu.set_tracecb(cb); }

	// End of public interface
public:
	blargg_err_t set_sample_rate( long rate, class Nes_Buffer* );
	blargg_err_t set_sample_rate( long rate, class Nes_Effects_Buffer* );
	void irq_changed() { emu.irq_changed(); }
private:
	friend class Nes_Recorder;
	
	frame_t* frame_;
	int buffer_height_;
	bool fade_sound_in;
	bool fade_sound_out;
	virtual blargg_err_t init_();
	
	virtual void loading_state( Nes_State const& ) { }
	void load_state( Nes_State_ const& );
	void save_state( Nes_State_* s ) const { emu.save_state( s ); }
	int joypad_read_count() const { return emu.joypad_read_count; }
	long timestamp() const { return emu.nes.frame_count; }
	void set_timestamp( long t ) { emu.nes.frame_count = t; }
	
private:
	// noncopyable
	Nes_Emu( const Nes_Emu& );
	Nes_Emu& operator = ( const Nes_Emu& );
	
	// sound
	Multi_Buffer* default_sound_buf;
	Multi_Buffer* sound_buf;
	unsigned sound_buf_changed_count;
	Silent_Buffer silent_buffer;
	equalizer_t equalizer_;
	int channel_count_;
	bool sound_enabled;
	void enable_sound( bool );
	void clear_sound_buf();
	void fade_samples( blip_sample_t*, int size, int step );
	
	char* host_pixel_buff;
	char* host_pixels;
	int host_palette_size;
	frame_t single_frame;
	Nes_Cart private_cart;
	Nes_Core emu; // large; keep at end
	
	bool init_called;
	blargg_err_t auto_init();
};

inline void Nes_Emu::set_pixels( void* p, long n )
{
	host_pixels = (char*) p + n;
	emu.ppu.host_row_bytes = n;
}

inline byte const* Nes_Emu::chr_mem()
{
	return cart()->chr_size() ? (byte*) cart()->chr() : emu.ppu.impl->chr_ram;
}

inline long Nes_Emu::chr_size() const
{
	return cart()->chr_size() ? cart()->chr_size() : emu.ppu.chr_addr_size;
}

#endif
