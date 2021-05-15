//PPUcounter emulates the H/V latch counters of the S-PPU2.
//
//real hardware has the S-CPU maintain its own copy of these counters that are
//updated based on the state of the S-PPU Vblank and Hblank pins. emulating this
//would require full lock-step synchronization for every clock tick.
//to bypass this and allow the two to run out-of-order, both the CPU and PPU
//classes inherit PPUcounter and keep their own counters.
//the timers are kept in sync, as the only differences occur on V=240 and V=261,
//based on interlace. thus, we need only synchronize and fetch interlace at any
//point before this in the frame, which is handled internally by this class at
//V=128.

struct PPUcounter {
  alwaysinline auto tick() -> void;
  alwaysinline auto tick(uint clocks) -> void; private:
  alwaysinline auto tickScanline() -> void; public:

  alwaysinline auto interlace() const -> bool;
  alwaysinline auto field() const -> bool;
  alwaysinline auto vcounter() const -> uint;
  alwaysinline auto hcounter() const -> uint;
  alwaysinline auto hdot() const -> uint; private:
  alwaysinline auto vperiod() const -> uint; public:
  alwaysinline auto hperiod() const -> uint;

  alwaysinline auto vcounter(uint offset) const -> uint;
  alwaysinline auto hcounter(uint offset) const -> uint;

  inline auto reset() -> void;
  auto serialize(serializer&) -> void;

  function<void ()> scanline;

private:
  struct {
    bool interlace = 0;
    bool field = 0;
    uint vperiod = 0;
    uint hperiod = 0;
    uint vcounter = 0;
    uint hcounter = 0;
  } time;

  struct {
    uint vperiod = 0;
    uint hperiod = 0;
  } last;
};
