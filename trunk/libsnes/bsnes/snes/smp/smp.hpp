struct SMP : public Processor, public SMPcore {
  static const uint8 iplrom[64];
  uint8 apuram[64 * 1024];

  enum : bool { Threaded = true };
  alwaysinline void step(unsigned clocks);
  alwaysinline void synchronize_cpu();
  alwaysinline void synchronize_cpu_force();
  alwaysinline void synchronize_dsp();

  uint8 port_read(uint2 port) const;
  void port_write(uint2 port, uint8 data);

  void enter();
  void power();
  void reset();

  void serialize(serializer&);
  SMP();
  ~SMP();

privileged:
  #include "memory/memory.hpp"
  #include "timing/timing.hpp"

  struct {
    //timing
    unsigned clock_counter;
    unsigned dsp_counter;
    unsigned timer_step;

    //$00f0
    uint8 clock_speed;
    uint8 timer_speed;
    bool timers_enable;
    bool ram_disable;
    bool ram_writable;
    bool timers_disable;

    //$00f1
    bool iplrom_enable;

    //$00f2
    uint8 dsp_addr;

    //$00f8,$00f9
    uint8 ram00f8;
    uint8 ram00f9;
  } status;

  static void Enter();

  friend class SMPcore;

  struct Debugger {
    hook<void (uint16)> op_exec;
    hook<void (uint16)> op_read;
    hook<void (uint16, uint8)> op_write;
  } debugger;
};

extern SMP smp;
