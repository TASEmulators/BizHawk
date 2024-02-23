#pragma once

// NES mapper interface
// Emu 0.7.0

#include <climits>
#include "../cart.hpp"
#include "../cpu.hpp"

namespace quickerNES
{

class Blip_Buffer;
class blip_eq_t;
class Core;

// Increase this (and let me know) if your mapper requires more state. This only
// sets the size of the in-memory buffer; it doesn't affect the file format at all.
static unsigned const max_mapper_state_size = 512; // was 256, needed more for VRC7 audio state
struct mapper_state_t
{
  int size;
  union
  {
    double align;
    uint8_t data[max_mapper_state_size];
  };

  void write(const void *p, unsigned long s);
  int read(void *p, unsigned long s) const;
};

class Mapper
{
  public:
  virtual ~Mapper();

  // Reset mapper to power-up state.
  virtual void reset();

  // Save snapshot of mapper state. Default saves registered state.
  virtual void save_state(mapper_state_t &);

  // Resets mapper, loads state, then applies it
  virtual void load_state(mapper_state_t const &);

  // I/O

  // Read from memory
  virtual int read(nes_time_t, nes_addr_t);

  // Write to memory
  virtual void write(nes_time_t, nes_addr_t, int data) = 0;

  // Write to memory below 0x8000 (returns false if mapper didn't handle write)
  virtual bool write_intercepted(nes_time_t, nes_addr_t, int data);

  // Timing

  // Time returned when current mapper state won't ever cause an IRQ
  enum
  {
    no_irq = LONG_MAX / 2
  };

  // Time next IRQ will occur at
  virtual nes_time_t next_irq(nes_time_t present);

  // Run mapper until given time
  virtual void run_until(nes_time_t);

  // End video frame of given length
  virtual void end_frame(nes_time_t length);

  // Sound

  // Number of sound channels
  virtual int channel_count() const;

  // Set sound buffer for channel to output to, or NULL to silence channel.
  virtual void set_channel_buf(int index, Blip_Buffer *);

  // Set treble equalization
  virtual void set_treble(blip_eq_t const &);

  // Misc

  // Called when bit 12 of PPU's VRAM address changes from 0 to 1 due to
  // $2006 and $2007 accesses (but not due to PPU scanline rendering).
  virtual void a12_clocked();

  void *state;
  unsigned state_size;

  protected:
  // Services provided for derived mapper classes
  Mapper();

  // Register state data to automatically save and load. Be sure the binary
  // layout is suitable for use in a file, including any byte-order issues.
  // Automatically cleared to zero by default reset().
  void register_state(void *, unsigned);

  // Enable 8K of RAM at 0x6000-0x7FFF, optionally read-only.
  void enable_sram(bool enabled = true, bool read_only = false);

  // Cause CPU writes within given address range to call mapper's write() function.
  // Might map a larger address range, which the mapper can ignore and pass to
  // Mapper::write(). The range 0x8000-0xffff is always intercepted by the mapper.
  void intercept_writes(nes_addr_t addr, unsigned size);

  // Cause CPU reads within given address range to call mapper's read() function.
  // Might map a larger address range, which the mapper can ignore and pass to
  // Mapper::read(). CPU opcode/operand reads and low-memory reads always
  // go directly to memory and cannot be intercepted.
  void intercept_reads(nes_addr_t addr, unsigned size);

  // Bank sizes for mapping
  enum bank_size_t
  { // 1 << bank_Xk = X * 1024
    bank_1k = 10,
    bank_2k = 11,
    bank_4k = 12,
    bank_8k = 13,
    bank_16k = 14,
    bank_32k = 15
  };

  // Index of last PRG/CHR bank. Last_bank selects last bank, last_bank - 1
  // selects next-to-last bank, etc.
  enum
  {
    last_bank = -1
  };

  // Map 'size' bytes from 'PRG + bank * size' to CPU address space starting at 'addr'
  void set_prg_bank(nes_addr_t addr, bank_size_t size, int bank);

  // Map 'size' bytes from 'CHR + bank * size' to PPU address space starting at 'addr'
  void set_chr_bank(nes_addr_t addr, bank_size_t size, int bank);
  void set_chr_bank_ex(nes_addr_t addr, bank_size_t size, int bank);

  // Set PPU mirroring. All mappings implemented using mirror_manual().
  void mirror_manual(int page0, int page1, int page2, int page3);
  void mirror_single(int page);
  void mirror_horiz(int page = 0);
  void mirror_vert(int page = 0);
  void mirror_full();

  // True if PPU rendering is enabled. Some mappers watch PPU memory accesses to determine
  // when scanlines occur, and can only do this when rendering is enabled.
  bool ppu_enabled() const;

  // Cartridge being emulated
  Cart const &cart() const { return *cart_; }

  // Must be called when next_irq()'s return value is earlier than previous,
  // current CPU run can be stopped earlier. Best to call whenever time may
  // have changed (no performance impact if called even when time didn't change).
  void irq_changed();

  // Handle data written to mapper that doesn't handle bus conflict arising due to
  // PRG also reading data. Returns data that mapper should act as if were
  // written. Currently always returns 'data' and just checks that data written is
  // the same as byte in PRG at same address and writes debug message if it doesn't.
  int handle_bus_conflict(nes_addr_t addr, int data);

  // Reference to emulator that uses this mapper.
  Core &emu() const { return *emu_; }

  protected:
  // Services derived classes provide

  // Read state from snapshot. Default reads data into registered state, then calls
  // apply_mapping().
  virtual void read_state(mapper_state_t const &);

  // Called by default reset() before apply_mapping() is called.
  virtual void reset_state() {}

  // End of general interface

  public:
  Cart const *cart_;
  Core *emu_;

  // Apply current mapping state to hardware. Called after reading mapper state
  // from a snapshot.
  virtual void apply_mapping() = 0;

  void default_reset_state();

  static Mapper *getMapperFromCode(const int mapperCode);
};

inline int Mapper::handle_bus_conflict(nes_addr_t addr, int data) { return data; }
inline void Mapper::mirror_horiz(int p) { mirror_manual(p, p, p ^ 1, p ^ 1); }
inline void Mapper::mirror_vert(int p) { mirror_manual(p, p ^ 1, p, p ^ 1); }
inline void Mapper::mirror_single(int p) { mirror_manual(p, p, p, p); }
inline void Mapper::mirror_full() { mirror_manual(0, 1, 2, 3); }

inline void Mapper::register_state(void *p, unsigned s)
{
  state = p;
  state_size = s;
}

inline bool Mapper::write_intercepted(nes_time_t, nes_addr_t, int) { return false; }

inline int Mapper::read(nes_time_t, nes_addr_t) { return -1; } // signal to caller

} // namespace quickerNES