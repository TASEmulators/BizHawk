//Exceptions:
//00000000 = reset
//00000004 = undefined instruction
//00000008 = software interrupt
//0000000c = prefetch abort
//00000010 = data abort
//00000018 = IRQ (interrupt)
//0000001c = FIQ (fast interrupt)

struct Bridge {
  struct Buffer {
    bool ready;
    uint8 data;
  };
  Buffer cputoarm;
  Buffer armtocpu;
  uint32 timer;
  uint32 timerlatch;
  bool reset;
  bool ready;
  bool busy;

  uint8 status() const {
    return (ready << 7) | (cputoarm.ready << 3) | (busy << 2) | (armtocpu.ready << 0);
  }
} bridge;

struct PSR {
  bool n;
  bool z;
  bool c;
  bool v;
  bool i;
  bool f;
  uint5 m;

  uint32 getf() const {
    return (n << 31) | (z << 30) | (c << 29) | (v << 28);
  }

  uint32 gets() const {
    return 0u;
  }

  uint32 getx() const {
    return 0u;
  }

  uint32 getc() const {
    return (i << 7) | (f << 6) | (m << 0);
  }

  void setf(uint32 data) {
    n = data & 0x80000000;
    z = data & 0x40000000;
    c = data & 0x20000000;
    v = data & 0x10000000;
  }

  void sets(uint32 data) {
  }

  void setx(uint32 data) {
  }

  void setc(uint32 data) {
    i = data & 0x00000080;
    f = data & 0x00000040;
    m = data & 0x0000001f;
  }

  operator uint32() const {
    return getf() | gets() | getx() | getc();
  }

  PSR& operator=(uint32 data) {
    setf(data), sets(data), setx(data), setc(data);
    return *this;
  }
} cpsr, spsr;

//r13 = SP (stack pointer)
//r14 = LR (link register)
//r15 = PC (program counter)
struct Register {
  uint32 data;
  function<void ()> write;

  operator unsigned() const {
    return data;
  }

  Register& operator=(uint32 n) {
    data = n;
    if(write) write();
  }

  Register& operator+=(uint32 n) { return operator=(data + n); }
  Register& operator-=(uint32 n) { return operator=(data - n); }
  Register& operator&=(uint32 n) { return operator=(data & n); }
  Register& operator|=(uint32 n) { return operator=(data | n); }
  Register& operator^=(uint32 n) { return operator=(data ^ n); }

  void step() {
    data += 4;
  }
} r[16];

bool shiftercarry;
uint32 instruction;
bool exception;

struct Pipeline {
  bool reload;
  struct Instruction {
    uint32 opcode;
    uint32 address;
  };
  Instruction instruction;
  Instruction prefetch;
  Instruction mdr;
} pipeline;
