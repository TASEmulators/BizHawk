#pragma once

// NES video game console emulator with snapshot support

// Emu 0.7.0

#include "cart.hpp"
#include "core.hpp"
#include "apu/multiBuffer.hpp"

namespace quickerNES
{

class State;

class Emu
{
  public:
  Emu();
  virtual ~Emu();

  // Basic setup

  // Load iNES file into emulator and clear recording
  void load_ines(const uint8_t *buffer);

  // Set sample rate for sound generation
  const char *set_sample_rate(long);

  // Size and depth of graphics buffer required for rendering. Note that this
  // is larger than the actual image, with a temporary area around the edge
  // that gets filled with junk.
  static const uint16_t buffer_width = Ppu::buffer_width;
  uint16_t buffer_height() const { return buffer_height_; }
  static const uint8_t bits_per_pixel = 8;

  // Set graphics buffer to render pixels to. Pixels points to top-left pixel and
  // row_bytes is the number of bytes to get to the next line (positive or negative).
  void set_pixels(void *pixels, long row_bytes);

  // Size of image generated in graphics buffer
  static const uint16_t image_width = 256;
  static const uint16_t image_height = 240;

  const uint8_t *getHostPixels() const { return emu.ppu.host_pixels; }

// Save emulator state variants
  void serializeState(jaffarCommon::serializer::Base& serializer) const { emu.serializeState(serializer); }
  void deserializeState(jaffarCommon::deserializer::Base& deserializer) { emu.deserializeState(deserializer); }
  void enableStateBlock(const std::string& block) { emu.enableStateBlock(block); };
  void disableStateBlock(const std::string& block) { emu.disableStateBlock(block); };

  // Basic emulation

  // Emulate one video frame using joypad1 and joypad2 as input. Afterwards, image
  // and sound are available for output using the accessors below.
  virtual const char *emulate_frame(uint32_t joypad1, uint32_t joypad2 = 0);

  // Emulate one video frame using joypad1 and joypad2 as input, but skips drawing.
  // Afterwards, audio is available for output using the accessors below.
  virtual const char *emulate_skip_frame(uint32_t joypad1, uint32_t joypad2 = 0);

  // Maximum size of palette that can be generated
  static const uint16_t max_palette_size = 256;

  // Result of current frame
  struct frame_t
  {
    static const uint8_t left = 8;

    int joypad_read_count; // number of times joypads were strobed (read)
    int burst_phase;       // NTSC burst phase for frame (0, 1, or 2)

    int sample_count; // number of samples (always a multiple of chan_count)
    int chan_count;   // 1: mono, 2: stereo

    int top;               // top-left position of image in graphics buffer
    unsigned char *pixels; // pointer to top-left pixel of image
    long pitch;            // number of bytes to get to next row of image

    int palette_begin;               // first host palette entry, as set by set_palette_range()
    int palette_size;                // number of entries used for current frame
    short palette[max_palette_size]; // [palette_begin to palette_begin+palette_size-1]
  };
  frame_t const &frame() const { return *frame_; }

  // Read samples for the current frame. Returns number of samples read into buffer.
  // Currently all samples must be read in one call.
  virtual long read_samples(short *out, long max_samples);

  // Additional features

  // Use already-loaded cartridge. Retains pointer, so it must be kept around until
  // closed. A cartridge can be shared among multiple emulators. After opening,
  // cartridge's CHR data shouldn't be modified since a copy is cached internally.
  void set_cart(Cart const *);

  // Pointer to current cartridge, or NULL if none is loaded
  Cart const *cart() const { return emu.cart; }

  // Emulate powering NES off and then back on. If full_reset is false, emulates
  // pressing the reset button only, which doesn't affect memory, otherwise
  // emulates powering system off then on.
  virtual void reset(bool full_reset = true, bool erase_battery_ram = false);

  // Number of undefined CPU instructions encountered. Cleared after reset() and
  // load_state(). A non-zero value indicates that cartridge is probably
  // incompatible.
  unsigned long error_count() const { return emu.error_count; }

  // Sound

  // Set sample rate and use a custom sound buffer instead of the default
  // mono buffer, i.e. Buffer, Effects_Buffer, etc..
  const char *set_sample_rate(long rate, Multi_Buffer *);

  // Adjust effective frame rate by changing how many samples are generated each frame.
  // Allows fine tuning of frame rate to improve synchronization.
  void set_frame_rate(double rate);

  // Number of sound channels for current cartridge
  int channel_count() const { return channel_count_; }

  // Frequency equalizer parameters
  struct equalizer_t
  {
    double treble; // 5.0 = extra-crisp, -200.0 = muffled
    long bass;     // 0 = deep, 20000 = tinny
  };

  // Current frequency equalization
  equalizer_t const &equalizer() const { return equalizer_; }

  // Change frequency equalization
  void set_equalizer(equalizer_t const &);

  // Equalizer presets
  static equalizer_t const nes_eq;     // NES
  static equalizer_t const famicom_eq; // Famicom
  static equalizer_t const tv_eq;      // TV speaker
  static equalizer_t const flat_eq;    // Flat EQ
  static equalizer_t const crisp_eq;   // Crisp EQ (Treble boost)
  static equalizer_t const tinny_eq;   // Tinny EQ (Like a handheld speaker)

  // File save/load

  // True if current cartridge claims it uses battery-backed memory
  bool has_battery_ram() const { return cart()->has_battery_ram(); }

  // Graphics

  // Number of frames generated per second
  enum
  {
    frame_rate = 60
  };

  // Size of fixed NES color table (including the 8 color emphasis modes)
  enum
  {
    color_table_size = 8 * 64
  };

  // NES color lookup table based on standard NTSC TV decoder. Use nes_ntsc.h to
  // generate a palette with custom parameters.
  struct rgb_t
  {
    unsigned char red, green, blue;
  };
  static rgb_t const nes_colors[color_table_size];

  // Hide/show/enhance sprites. Sprite mode does not affect emulation accuracy.
  enum sprite_mode_t
  {
    sprites_hidden = 0,
    sprites_visible = 8,  // limit of 8 sprites per scanline as on NES (default)
    sprites_enhanced = 64 // unlimited sprites per scanline (no flickering)
  };
  void set_sprite_mode(sprite_mode_t n) { emu.ppu.sprite_limit = n; }

  // Set range of host palette entries to use in graphics buffer; default uses
  // all of them. Begin will be rounded up to next multiple of palette_alignment.
  // Use frame().palette_begin to find the adjusted beginning entry used.
  enum
  {
    palette_alignment = 64
  };
  void set_palette_range(int begin, int end = 256);

  // Access to emulated memory, for viewer/cheater/debugger

  // CHR
  uint8_t *chr_mem() const;
  long chr_size() const;
  void write_chr(void const *, long count, long offset);

  // Nametable
  uint8_t *nametable_mem() const { return emu.ppu.impl->nt_ram; }
  long nametable_size() const { return 0x1000; }

  // Built-in 2K memory
  enum
  {
    low_mem_size = 0x800
  };
  
  uint8_t *get_low_mem() const { return (uint8_t*)emu.low_mem; }
  size_t get_low_mem_size() const { return low_mem_size; }

  // Optional 8K memory
  enum
  {
    high_mem_size = 0x2000
  };
  uint8_t *high_mem() const { return emu.impl->sram; }
  size_t get_high_mem_size() const { return high_mem_size; }

  // Sprite memory
  uint8_t *spr_mem() const { return emu.ppu.getSpriteRAM(); }
  uint16_t spr_mem_size() const { return emu.ppu.getSpriteRAMSize(); }

  // Palette memory
  uint8_t *pal_mem() const { return emu.ppu.getPaletteRAM(); }
  uint16_t pal_mem_size() const { return emu.ppu.getPaletteRAMSize(); }

	uint8_t peek_prg(nes_addr_t addr) const { return *emu.get_code(addr); }
	void poke_prg(nes_addr_t addr, uint8_t value) { *emu.get_code(addr) = value; }
	uint8_t peek_ppu(int addr) { return emu.ppu.peekaddr(addr); }

	uint8_t get_ppu2000() const { return emu.ppu.w2000; }

  void get_regs(unsigned int *dest) const
  {
    dest[0] = emu.r.a;
    dest[1] = emu.r.x;
    dest[2] = emu.r.y;
    dest[3] = emu.r.sp;
    dest[4] = emu.r.pc;
    dest[5] = emu.r.status;
  }

  // End of public interface
  public:
  const char *set_sample_rate(long rate, class Buffer *);
  const char *set_sample_rate(long rate, class Nes_Effects_Buffer *);
  void irq_changed() { emu.irq_changed(); }

  private:
  frame_t *frame_;
  int buffer_height_;
  bool fade_sound_in;
  bool fade_sound_out;
  virtual const char *init_();

  virtual void loading_state(State const &) {}
  long timestamp() const { return 0; }
  void set_timestamp(long t) {  }

  private:
  // noncopyable
  Emu(const Emu &);
  Emu &operator=(const Emu &);

  // sound
  Multi_Buffer *default_sound_buf;
  Multi_Buffer *sound_buf;
  unsigned sound_buf_changed_count;
  Silent_Buffer silent_buffer;
  equalizer_t equalizer_;
  int channel_count_;
  bool sound_enabled;
  void enable_sound(bool);
  void clear_sound_buf();
  void fade_samples(blip_sample_t *, int size, int step);

  char *host_pixels;
  int host_palette_size;
  frame_t single_frame;
  Cart private_cart;
  Core emu; // large; keep at end

  bool init_called;
  const char *auto_init();

  bool extra_fade_sound_in;
  bool extra_fade_sound_out;
  unsigned extra_sound_buf_changed_count;

  public:
  void SaveAudioBufferState();
  void RestoreAudioBufferState();
};

inline void Emu::set_pixels(void *p, long n)
{
  host_pixels = (char *)p + n;
  emu.ppu.host_row_bytes = n;
}

inline uint8_t *Emu::chr_mem() const
{
  return cart()->chr_size() ? (uint8_t *)cart()->chr() : emu.ppu.impl->chr_ram;
}

inline long Emu::chr_size() const
{
  return cart()->chr_size() ? cart()->chr_size() : emu.ppu.chr_addr_size;
}

} // namespace quickerNES