//NEC VR4300

struct CPU : Thread {
  Node::Object node;

  struct Debugger {
    //debugger.cpp
    auto load(Node::Object) -> void;
    auto unload() -> void;
    auto instruction() -> void;
    auto exception(u8 code) -> void;
    auto interrupt(u8 mask) -> void;
    auto nmi() -> void;
    auto tlbWrite(u32 index) -> void;
    auto tlbModification(u64 address) -> void;
    auto tlbLoad(u64 address, u64 physical) -> void;
    auto tlbLoadInvalid(u64 address) -> void;
    auto tlbLoadMiss(u64 address) -> void;
    auto tlbStore(u64 address, u64 physical) -> void;
    auto tlbStoreInvalid(u64 address) -> void;
    auto tlbStoreMiss(u64 address) -> void;

    struct Tracer {
      Node::Debugger::Tracer::Instruction instruction;
      Node::Debugger::Tracer::Notification exception;
      Node::Debugger::Tracer::Notification interrupt;
      Node::Debugger::Tracer::Notification tlb;
    } tracer;
  } debugger;

  //cpu.cpp
  auto load(Node::Object) -> void;
  auto unload() -> void;

  auto main() -> void;
  auto synchronize() -> void;

  auto instruction() -> void;
  auto instructionPrologue(u32 instruction) -> void;
  auto instructionEpilogue() -> s32;

  auto power(bool reset) -> void;

  struct Pipeline {
    u64 address;
    u32 instruction;

    struct InstructionCache {
    } ic;

    struct RegisterFile {
    } rf;

    struct Execution {
    } ex;

    struct DataCache {
    } dc;

    struct WriteBack {
    } wb;
  } pipeline;

  struct Branch {
    enum : u32 { Step, Take, NotTaken, DelaySlotTaken, DelaySlotNotTaken, Exception, Discard };

    auto inDelaySlot() const -> bool { return state == DelaySlotTaken || state == DelaySlotNotTaken; }
    auto inDelaySlotTaken() const -> bool { return state == DelaySlotTaken; }
    auto reset() -> void { state = Step; }
    auto take(u64 address) -> void { state = Take; pc = address; }
    auto notTaken() -> void { state = NotTaken; }
    auto delaySlot(bool taken) -> void { state = taken ? DelaySlotTaken : DelaySlotNotTaken; }
    auto exception() -> void { state = Exception; }
    auto discard() -> void { state = Discard; }

    u64 pc = 0;
    u32 state = Step;
  } branch;

  //context.cpp
  struct Context {
    CPU& self;
    Context(CPU& self) : self(self) {}

    enum Endian : bool { Little, Big };
    enum Mode : u32 { Kernel, Supervisor, User };
    enum Segment : u32 { Unused, Mapped, Cached, Direct, Cached32, Direct32, Kernel64, Supervisor64, User64 };

    auto littleEndian() const -> bool { return endian == Endian::Little; }
    auto bigEndian() const -> bool { return endian == Endian::Big; }

    auto kernelMode() const -> bool { return mode == Mode::Kernel; }
    auto supervisorMode() const -> bool { return mode == Mode::Supervisor; }
    auto userMode() const -> bool { return mode == Mode::User; }

    auto setMode() -> void;

    bool endian;
    u64  physMask;
    u32  mode;
    u32  bits;
    u32  segment[8];  //512_MiB chunks
  } context{*this};

  //icache.cpp
  struct InstructionCache {
    CPU& self;
    struct Line;
    auto line(u32 vaddr) -> Line& { return lines[vaddr >> 5 & 0x1ff]; }

    //used by the recompiler to simulate instruction cache fetch timing
    auto step(u32 vaddr, u32 address) -> void {
      auto& line = this->line(vaddr);
      if(!line.hit(address)) {
        self.step(48 * 2);
        line.valid = 1;
        line.tag   = address & ~0x0000'0fff;
      } else {
        self.step(1 * 2);
      }
    }

    //used by the interpreter to fully emulate the instruction cache
    auto fetch(u32 vaddr, u32 address, CPU& cpu) -> u32 {
      auto& line = this->line(vaddr);
      if(!line.hit(address)) {
        line.fill(address, cpu);
      } else {
        cpu.step(1 * 2);
      }
      return line.read(address);
    }

    auto power(bool reset) -> void {
      u32 index = 0;
      for(auto& line : lines) {
        line.valid = 0;
        line.tag   = 0;
        line.index = index++ << 5 & 0xfe0;
        for(auto& word : line.words) word = 0;
       }
    }

    //16KB
    struct Line {
      auto hit(u32 address) const -> bool { return valid && tag == (address & ~0x0000'0fff); }
      auto fill(u32 address, CPU& cpu) -> void {
        cpu.step(48 * 2);
        valid = 1;
        tag   = address & ~0x0000'0fff;
        cpu.busReadBurst<ICache>(tag | index, words);
      }

      auto writeBack(CPU& cpu) -> void {
        cpu.step(48 * 2);
        cpu.busWriteBurst<ICache>(tag | index, words);
      }

      auto read(u32 address) const -> u32 { return words[address >> 2 & 7]; }

      bool valid;
      u32  tag;
      u16  index;
      u32  words[8];
    } lines[512];
  } icache{*this};

  //dcache.cpp
  struct DataCache {
    struct Line;
    auto line(u32 vaddr) -> Line&;
    template<u32 Size> auto read(u32 vaddr, u32 address) -> u64;
    template<u32 Size> auto write(u32 vaddr, u32 address, u64 data) -> void;
    auto power(bool reset) -> void;

    auto readDebug(u32 vaddr, u32 address) -> u8;

    //8KB
    struct Line {
      auto hit(u32 address) const -> bool;
      auto fill(u32 address) -> void;
      auto writeBack() -> void;
      template<u32 Size> auto read(u32 address) const -> u64;
      template<u32 Size> auto write(u32 address, u64 data) -> void;

      bool valid;
      u16  dirty;
      u32  tag;
      u16  index;
      u64  fillPc;
      u64  dirtyPc;
      union {
        u8  bytes[16];
        u16 halfs[8];
        u32 words[4];
      };
    } lines[512];
  } dcache;

  //tlb.cpp: Translation Lookaside Buffer
  struct TLB {
    CPU& self;
    TLB(CPU& self) : self(self) {}
    static constexpr u32 Entries = 32;

    struct Match {
      explicit operator bool() const { return found; }

      bool found;
      bool cache;
      u32  address;
    };

    struct Entry {
      //scc-tlb.cpp
      auto synchronize() -> void;

      n1  global[2];
      n1  valid[2];
      n1  dirty[2];
      n3  cacheAlgorithm[2];
      n36 physicalAddress[2];
      n32 pageMask;
      n40 virtualAddress;
      n8  addressSpaceID;
      n2  region;
      //internal:
      n1  globals;
      n40 addressMaskHi;
      n40 addressMaskLo;
      n40 addressSelect;
    } entry[TLB::Entries];

    //tlb.cpp
    auto load(u64 vaddr, bool noExceptions = false) -> Match;
    auto load(u64 vaddr, const Entry& entry, bool noExceptions = false) -> maybe<Match>;
    
    auto loadFast(u64 vaddr) -> Match;
    auto store(u64 vaddr) -> Match;
    auto store(u64 vaddr, const Entry& entry) -> maybe<Match>;

    struct TlbCache { ;
      static constexpr int entries = 4;

      struct CachedTlbEntry {
        const Entry *entry;
        int frequency;
      } entry[entries];

      void insert(const Entry& entry) {
        this->entry[refresh()].entry = &entry;
      }

      int refresh() {
        CachedTlbEntry* leastUsed = &entry[0];
        int index = 0;

        for(auto n = 0; n < entries; n++) {
          if(entry[n].frequency < leastUsed->frequency) {
            index = n;
            leastUsed = &entry[n];
          }
        }

        leastUsed->entry = nullptr;
        leastUsed->frequency = 0;
        return index;
      }
    } tlbCache;

    u32 physicalAddress;
  } tlb{*this};

  //memory.cpp
  auto kernelSegment32(u32 vaddr) const -> Context::Segment;
  auto supervisorSegment32(u32 vaddr) const -> Context::Segment;
  auto userSegment32(u32 vaddr) const -> Context::Segment;

  auto kernelSegment64(u64 vaddr) const -> Context::Segment;
  auto supervisorSegment64(u64 vaddr) const -> Context::Segment;
  auto userSegment64(u64 vaddr) const -> Context::Segment;

  auto segment(u64 vaddr) -> Context::Segment;
  auto devirtualize(u64 vaddr) -> maybe<u64>;
  alwaysinline auto devirtualizeFast(u64 vaddr) -> u64;
  auto devirtualizeDebug(u64 vaddr) -> u64;

  auto fetch(u64 vaddr) -> maybe<u32>;
  template<u32 Size> auto busWrite(u32 address, u64 data) -> void;
  template<u32 Size> auto busRead(u32 address) -> u64;
  template<u32 Size> auto busWriteBurst(u32 address, u32 *data) -> void;
  template<u32 Size> auto busReadBurst(u32 address, u32 *data) -> void;
  template<u32 Size> auto read(u64 vaddr) -> maybe<u64>;
  template<u32 Size> auto write(u64 vaddr, u64 data, bool alignedError=true) -> bool;
  template<u32 Size> auto vaddrAlignedError(u64 vaddr, bool write) -> bool;
  auto addressException(u64 vaddr) -> void;

  auto readDebug(u64 vaddr) -> u8;

  //serialization.cpp
  auto serialize(serializer&) -> void;

  //exception.cpp
  struct Exception {
    CPU& self;
    Exception(CPU& self) : self(self) {}

    auto trigger(u32 code, u32 coprocessor = 0, bool tlbMiss = 0) -> void;

    auto interrupt() -> void;
    auto tlbModification() -> void;
    auto tlbLoadInvalid() -> void;
    auto tlbLoadMiss() -> void;
    auto tlbStoreInvalid() -> void;
    auto tlbStoreMiss() -> void;
    auto addressLoad() -> void;
    auto addressStore() -> void;
    auto busInstruction() -> void;
    auto busData() -> void;
    auto systemCall() -> void;
    auto breakpoint() -> void;
    auto reservedInstruction() -> void;
    auto reservedInstructionCop2() -> void;
    auto coprocessor0() -> void;
    auto coprocessor1() -> void;
    auto coprocessor2() -> void;
    auto coprocessor3() -> void;
    auto arithmeticOverflow() -> void;
    auto trap() -> void;
    auto floatingPoint() -> void;
    auto watchAddress() -> void;
    auto nmi() -> void;
  } exception{*this};

  enum Interrupt : u32 {
    Software0 = 0,
    Software1 = 1,
    RCP       = 2,
    Cartridge = 3,
    Reset     = 4,
    ReadRDB   = 5,
    WriteRDB  = 6,
    Timer     = 7,
  };

  //ipu.cpp
  union r64 {
    struct {   int32_t order_msb2(s32h, s32); };
    struct {  uint32_t order_msb2(u32h, u32); };
    struct { float32_t order_msb2(f32h, f32); };
    struct {   int64_t s64; };
    struct {  uint64_t u64; };
    struct { float64_t f64; };
  };
  using cr64 = const r64;

  struct IPU {
    enum Register : u32 {
      R0,                              //zero (read-only)
      AT,                              //assembler temporary
      V0, V1,                          //arithmetic values
      A0, A1, A2, A3,                  //subroutine parameters
      T0, T1, T2, T3, T4, T5, T6, T7,  //temporary registers
      S0, S1, S2, S3, S4, S5, S6, S7,  //saved registers
      T8, T9,                          //temporary registers
      K0, K1,                          //kernel registers
      GP,                              //global pointer
      SP,                              //stack pointer
      S8,                              //saved register
      RA,                              //return address
    };

    r64 r[32];
    r64 lo;
    r64 hi;
    u64 pc;  //program counter
  } ipu;

  //algorithms.cpp
  template<typename T> auto roundNearest(f32 f) -> T;
  template<typename T> auto roundNearest(f64 f) -> T;
  template<typename T> auto roundCeil(f32 f) -> T;
  template<typename T> auto roundCeil(f64 f) -> T;
  template<typename T> auto roundCurrent(f32 f) -> T;
  template<typename T> auto roundCurrent(f64 f) -> T;
  template<typename T> auto roundFloor(f32 f) -> T;
  template<typename T> auto roundFloor(f64 f) -> T;
  template<typename T> auto roundTrunc(f32 f) -> T;
  template<typename T> auto roundTrunc(f64 f) -> T;
  auto squareRoot(f32 f) -> f32;
  auto squareRoot(f64 f) -> f64;

  //interpreter-ipu.cpp
  auto ADD(r64& rd, cr64& rs, cr64& rt) -> void;
  auto ADDI(r64& rt, cr64& rs, s16 imm) -> void;
  auto ADDIU(r64& rt, cr64& rs, s16 imm) -> void;
  auto ADDU(r64& rd, cr64& rs, cr64& rt) -> void;
  auto AND(r64& rd, cr64& rs, cr64& rt) -> void;
  auto ANDI(r64& rt, cr64& rs, u16 imm) -> void;
  auto BEQ(cr64& rs, cr64& rt, s16 imm) -> void;
  auto BEQL(cr64& rs, cr64& rt, s16 imm) -> void;
  auto BGEZ(cr64& rs, s16 imm) -> void;
  auto BGEZAL(cr64& rs, s16 imm) -> void;
  auto BGEZALL(cr64& rs, s16 imm) -> void;
  auto BGEZL(cr64& rs, s16 imm) -> void;
  auto BGTZ(cr64& rs, s16 imm) -> void;
  auto BGTZL(cr64& rs, s16 imm) -> void;
  auto BLEZ(cr64& rs, s16 imm) -> void;
  auto BLEZL(cr64& rs, s16 imm) -> void;
  auto BLTZ(cr64& rs, s16 imm) -> void;
  auto BLTZAL(cr64& rs, s16 imm) -> void;
  auto BLTZALL(cr64& rs, s16 imm) -> void;
  auto BLTZL(cr64& rs, s16 imm) -> void;
  auto BNE(cr64& rs, cr64& rt, s16 imm) -> void;
  auto BNEL(cr64& rs, cr64& rt, s16 imm) -> void;
  auto BREAK() -> void;
  auto CACHE(u8 operation, cr64& rs, s16 imm) -> void;
  auto DADD(r64& rd, cr64& rs, cr64& rt) -> void;
  auto DADDI(r64& rt, cr64& rs, s16 imm) -> void;
  auto DADDIU(r64& rt, cr64& rs, s16 imm) -> void;
  auto DADDU(r64& rd, cr64& rs, cr64& rt) -> void;
  auto DDIV(cr64& rs, cr64& rt) -> void;
  auto DDIVU(cr64& rs, cr64& rt) -> void;
  auto DIV(cr64& rs, cr64& rt) -> void;
  auto DIVU(cr64& rs, cr64& rt) -> void;
  auto DMULT(cr64& rs, cr64& rt) -> void;
  auto DMULTU(cr64& rs, cr64& rt) -> void;
  auto DSLL(r64& rd, cr64& rt, u8 sa) -> void;
  auto DSLLV(r64& rd, cr64& rt, cr64& rs) -> void;
  auto DSRA(r64& rd, cr64& rt, u8 sa) -> void;
  auto DSRAV(r64& rd, cr64& rt, cr64& rs) -> void;
  auto DSRL(r64& rd, cr64& rt, u8 sa) -> void;
  auto DSRLV(r64& rd, cr64& rt, cr64& rs) -> void;
  auto DSUB(r64& rd, cr64& rs, cr64& rt) -> void;
  auto DSUBU(r64& rd, cr64& rs, cr64& rt) -> void;
  auto J(u32 imm) -> void;
  auto JAL(u32 imm) -> void;
  auto JALR(r64& rd, cr64& rs) -> void;
  auto JR(cr64& rs) -> void;
  auto LB(r64& rt, cr64& rs, s16 imm) -> void;
  auto LBU(r64& rt, cr64& rs, s16 imm) -> void;
  auto LD(r64& rt, cr64& rs, s16 imm) -> void;
  auto LDL(r64& rt, cr64& rs, s16 imm) -> void;
  auto LDR(r64& rt, cr64& rs, s16 imm) -> void;
  auto LH(r64& rt, cr64& rs, s16 imm) -> void;
  auto LHU(r64& rt, cr64& rs, s16 imm) -> void;
  auto LUI(r64& rt, u16 imm) -> void;
  auto LL(r64& rt, cr64& rs, s16 imm) -> void;
  auto LLD(r64& rt, cr64& rs, s16 imm) -> void;
  auto LW(r64& rt, cr64& rs, s16 imm) -> void;
  auto LWL(r64& rt, cr64& rs, s16 imm) -> void;
  auto LWR(r64& rt, cr64& rs, s16 imm) -> void;
  auto LWU(r64& rt, cr64& rs, s16 imm) -> void;
  auto MFHI(r64& rd) -> void;
  auto MFLO(r64& rd) -> void;
  auto MTHI(cr64& rs) -> void;
  auto MTLO(cr64& rs) -> void;
  auto MULT(cr64& rs, cr64& rt) -> void;
  auto MULTU(cr64& rs, cr64& rt) -> void;
  auto NOR(r64& rd, cr64& rs, cr64& rt) -> void;
  auto OR(r64& rd, cr64& rs, cr64& rt) -> void;
  auto ORI(r64& rt, cr64& rs, u16 imm) -> void;
  auto SB(cr64& rt, cr64& rs, s16 imm) -> void;
  auto SC(r64& rt, cr64& rs, s16 imm) -> void;
  auto SD(cr64& rt, cr64& rs, s16 imm) -> void;
  auto SCD(r64& rt, cr64& rs, s16 imm) -> void;
  auto SDL(cr64& rt, cr64& rs, s16 imm) -> void;
  auto SDR(cr64& rt, cr64& rs, s16 imm) -> void;
  auto SH(cr64& rt, cr64& rs, s16 imm) -> void;
  auto SLL(r64& rd, cr64& rt, u8 sa) -> void;
  auto SLLV(r64& rd, cr64& rt, cr64& rs) -> void;
  auto SLT(r64& rd, cr64& rs, cr64& rt) -> void;
  auto SLTI(r64& rt, cr64& rs, s16 imm) -> void;
  auto SLTIU(r64& rt, cr64& rs, s16 imm) -> void;
  auto SLTU(r64& rd, cr64& rs, cr64& rt) -> void;
  auto SRA(r64& rd, cr64& rt, u8 sa) -> void;
  auto SRAV(r64& rd, cr64& rt, cr64& rs) -> void;
  auto SRL(r64& rd, cr64& rt, u8 sa) -> void;
  auto SRLV(r64& rd, cr64& rt, cr64& rs) -> void;
  auto SUB(r64& rd, cr64& rs, cr64& rt) -> void;
  auto SUBU(r64& rd, cr64& rs, cr64& rt) -> void;
  auto SW(cr64& rt, cr64& rs, s16 imm) -> void;
  auto SWL(cr64& rt, cr64& rs, s16 imm) -> void;
  auto SWR(cr64& rt, cr64& rs, s16 imm) -> void;
  auto SYNC() -> void;
  auto SYSCALL() -> void;
  auto TEQ(cr64& rs, cr64& rt) -> void;
  auto TEQI(cr64& rs, s16 imm) -> void;
  auto TGE(cr64& rs, cr64& rt) -> void;
  auto TGEI(cr64& rs, s16 imm) -> void;
  auto TGEIU(cr64& rs, s16 imm) -> void;
  auto TGEU(cr64& rs, cr64& rt) -> void;
  auto TLT(cr64& rs, cr64& rt) -> void;
  auto TLTI(cr64& rs, s16 imm) -> void;
  auto TLTIU(cr64& rs, s16 imm) -> void;
  auto TLTU(cr64& rs, cr64& rt) -> void;
  auto TNE(cr64& rs, cr64& rt) -> void;
  auto TNEI(cr64& rs, s16 imm) -> void;
  auto XOR(r64& rd, cr64& rs, cr64& rt) -> void;
  auto XORI(r64& rt, cr64& rs, u16 imm) -> void;

  struct SCC {
    //0
    struct Index {
      n6 tlbEntry;
      n1 probeFailure;
    } index;

    //1: Random
    //2: EntryLo0
    //3: EntryLo1
    //5: PageMask
    //10: EntryHi
    TLB::Entry tlb;

    //4
    struct Context {
      n19 badVirtualAddress;
      n41 pageTableEntryBase;
    } context;

    //6
    struct Wired {
      n6 index;
    } wired;

    //8
    n64 badVirtualAddress;

    //9
    n33 count;  //32-bit; +1 to count half-cycles

    //11
    n33 compare;

    //12
    struct Status {
      n1 interruptEnable;
      n1 exceptionLevel;
      n1 errorLevel = 1;
      n2 privilegeMode;
      n1 userExtendedAddressing;
      n1 supervisorExtendedAddressing;
      n1 kernelExtendedAddressing;
      n8 interruptMask = 0xff;
      n1 de;  //unused
      n1 ce;  //unused
      n1 condition;
      n1 softReset = 1;
      n1 tlbShutdown;
      n1 vectorLocation = 1;
      n1 instructionTracing;
      n1 reverseEndian;
      n1 floatingPointMode = 1;
      n1 lowPowerMode;
      struct Enable {
        n1 coprocessor0 = 1;
        n1 coprocessor1 = 1;
        n1 coprocessor2;
        n1 coprocessor3;
      } enable;
    } status;

    //13
    struct Cause {
      n5 exceptionCode;
      n8 interruptPending;
      n2 coprocessorError;
      n1 branchDelay;
    } cause;

    //14: Exception Program Counter
    n64 epc;

    //15: Coprocessor Revision Identifier
    struct Coprocessor {
      static constexpr u8 revision = 0x22;
      static constexpr u8 implementation = 0x0b;
    } coprocessor;

    //16
    struct Configuration {
      n2 coherencyAlgorithmKSEG0;
      n2 cu;  //reserved
      n1 bigEndian = 1;
      n2 sysadWritebackPattern;
      n3 systemClockRatio = 7;
    } configuration;

    //17: Load Linked Address
    n32 ll;
    n1  llbit;

    //18
    struct WatchLo {
      n1  trapOnWrite;
      n1  trapOnRead;
      n32 physicalAddress;
    } watchLo;

    //19
    struct WatchHi {
      n4 physicalAddressExtended;  //unused; for R4000 compatibility only
    } watchHi;

    //20
    struct XContext {
      n27 badVirtualAddress;
      n2  region;
      n31 pageTableEntryBase;
    } xcontext;

    //26
    struct ParityError {
      n8 diagnostic;  //unused; for R4000 compatibility only
    } parityError;

    //28
    struct TagLo {
      n2  primaryCacheState;
      n32 physicalAddress;
    } tagLo;

    //30: Error Exception Program Counter
    n64 epcError;

    //other
    n64 latch;
    n1 nmiPending;
    n1 sysadFrozen;
  } scc;

  //interpreter-scc.cpp
  auto getControlRegister(n5) -> u64;
  auto setControlRegister(n5, n64) -> void;
  auto getControlRandom() -> u8;

  auto DMFC0(r64& rt, u8 rd) -> void;
  auto DMTC0(cr64& rt, u8 rd) -> void;
  auto ERET() -> void;
  auto MFC0(r64& rt, u8 rd) -> void;
  auto MTC0(cr64& rt, u8 rd) -> void;
  auto TLBP() -> void;
  auto TLBR() -> void;
  auto TLBWI() -> void;
  auto TLBWR() -> void;

  struct FPU {
    auto setFloatingPointMode(bool) -> void;

    r64 r[32];

    struct Coprocessor {
      static constexpr u8 revision = 0x00;
      static constexpr u8 implementation = 0x0a;
    } coprocessor;

    struct ControlStatus {
      n2 roundMode = 0;
      struct Flag {
        n1 inexact = 0;
        n1 underflow = 0;
        n1 overflow = 0;
        n1 divisionByZero = 0;
        n1 invalidOperation = 0;
      } flag;
      struct Enable {
        n1 inexact = 0;
        n1 underflow = 0;
        n1 overflow = 0;
        n1 divisionByZero = 0;
        n1 invalidOperation = 0;
      } enable;
      struct Cause {
        n1 inexact = 0;
        n1 underflow = 0;
        n1 overflow = 0;
        n1 divisionByZero = 0;
        n1 invalidOperation = 0;
        n1 unimplementedOperation = 0;
      } cause;
      n1 compare = 0;
      n1 flushSubnormals = 0;
    } csr;
  } fpu;

  //interpreter-fpu.cpp
  float_env fenv;

  template<typename T> auto fgr_t(u32) -> T&;
  template<typename T> auto fgr_s(u32) -> T&;
  template<typename T> auto fgr_d(u32) -> T&;
  auto getControlRegisterFPU(n5) -> u32;
  auto setControlRegisterFPU(n5, n32) -> void;
  template<bool CVT> auto checkFPUExceptions() -> bool;
  auto fpeDivisionByZero() -> bool;
  auto fpeInexact() -> bool;
  auto fpeUnderflow() -> bool;
  auto fpeOverflow() -> bool;
  auto fpeInvalidOperation() -> bool;
  auto fpeUnimplemented() -> bool;
  auto fpuCheckStart() -> bool;
  template <typename T>
  auto fpuCheckInput(T& f) -> bool;
  template <typename T>
  auto fpuCheckInputs(T& f1, T& f2) -> bool;
  auto fpuCheckOutput(f32& f) -> bool;
  auto fpuCheckOutput(f64& f) -> bool;
  auto fpuClearCause() -> void;
  template<typename DST, typename SF>
  auto fpuCheckInputConv(SF& f) -> bool;

  auto BC1(bool value, bool likely, s16 imm) -> void;
  auto CFC1(r64& rt, u8 rd) -> void;
  auto CTC1(cr64& rt, u8 rd) -> void;
  auto DCFC1(r64& rt, u8 rd) -> void;
  auto DCTC1(cr64& rt, u8 rd) -> void;
  auto DMFC1(r64& rt, u8 fs) -> void;
  auto DMTC1(cr64& rt, u8 fs) -> void;
  auto FABS_S(u8 fd, u8 fs) -> void;
  auto FABS_D(u8 fd, u8 fs) -> void;
  auto FADD_S(u8 fd, u8 fs, u8 ft) -> void;
  auto FADD_D(u8 fd, u8 fs, u8 ft) -> void;
  auto FCEIL_L_S(u8 fd, u8 fs) -> void;
  auto FCEIL_L_D(u8 fd, u8 fs) -> void;
  auto FCEIL_L_W(u8 fd, u8 fs) -> void;
  auto FCEIL_L_L(u8 fd, u8 fs) -> void;
  auto FCEIL_W_S(u8 fd, u8 fs) -> void;
  auto FCEIL_W_D(u8 fd, u8 fs) -> void;
  auto FCEIL_W_W(u8 fd, u8 fs) -> void;
  auto FCEIL_W_L(u8 fd, u8 fs) -> void;
  auto FC_EQ_S(u8 fs, u8 ft) -> void;
  auto FC_EQ_D(u8 fs, u8 ft) -> void;
  auto FC_F_S(u8 fs, u8 ft) -> void;
  auto FC_F_D(u8 fs, u8 ft) -> void;
  auto FC_LE_S(u8 fs, u8 ft) -> void;
  auto FC_LE_D(u8 fs, u8 ft) -> void;
  auto FC_LT_S(u8 fs, u8 ft) -> void;
  auto FC_LT_D(u8 fs, u8 ft) -> void;
  auto FC_NGE_S(u8 fs, u8 ft) -> void;
  auto FC_NGE_D(u8 fs, u8 ft) -> void;
  auto FC_NGL_S(u8 fs, u8 ft) -> void;
  auto FC_NGL_D(u8 fs, u8 ft) -> void;
  auto FC_NGLE_S(u8 fs, u8 ft) -> void;
  auto FC_NGLE_D(u8 fs, u8 ft) -> void;
  auto FC_NGT_S(u8 fs, u8 ft) -> void;
  auto FC_NGT_D(u8 fs, u8 ft) -> void;
  auto FC_OLE_S(u8 fs, u8 ft) -> void;
  auto FC_OLE_D(u8 fs, u8 ft) -> void;
  auto FC_OLT_S(u8 fs, u8 ft) -> void;
  auto FC_OLT_D(u8 fs, u8 ft) -> void;
  auto FC_SEQ_S(u8 fs, u8 ft) -> void;
  auto FC_SEQ_D(u8 fs, u8 ft) -> void;
  auto FC_SF_S(u8 fs, u8 ft) -> void;
  auto FC_SF_D(u8 fs, u8 ft) -> void;
  auto FC_UEQ_S(u8 fs, u8 ft) -> void;
  auto FC_UEQ_D(u8 fs, u8 ft) -> void;
  auto FC_ULE_S(u8 fs, u8 ft) -> void;
  auto FC_ULE_D(u8 fs, u8 ft) -> void;
  auto FC_ULT_S(u8 fs, u8 ft) -> void;
  auto FC_ULT_D(u8 fs, u8 ft) -> void;
  auto FC_UN_S(u8 fs, u8 ft) -> void;
  auto FC_UN_D(u8 fs, u8 ft) -> void;
  auto FCVT_S_S(u8 fd, u8 fs) -> void;
  auto FCVT_S_D(u8 fd, u8 fs) -> void;
  auto FCVT_S_W(u8 fd, u8 fs) -> void;
  auto FCVT_S_L(u8 fd, u8 fs) -> void;
  auto FCVT_D_S(u8 fd, u8 fs) -> void;
  auto FCVT_D_D(u8 fd, u8 fs) -> void;
  auto FCVT_D_W(u8 fd, u8 fs) -> void;
  auto FCVT_D_L(u8 fd, u8 fs) -> void;
  auto FCVT_L_S(u8 fd, u8 fs) -> void;
  auto FCVT_L_D(u8 fd, u8 fs) -> void;
  auto FCVT_L_W(u8 fd, u8 fs) -> void;
  auto FCVT_L_L(u8 fd, u8 fs) -> void;
  auto FCVT_W_S(u8 fd, u8 fs) -> void;
  auto FCVT_W_D(u8 fd, u8 fs) -> void;
  auto FCVT_W_W(u8 fd, u8 fs) -> void;
  auto FCVT_W_L(u8 fd, u8 fs) -> void;
  auto FDIV_S(u8 fd, u8 fs, u8 ft) -> void;
  auto FDIV_D(u8 fd, u8 fs, u8 ft) -> void;
  auto FFLOOR_L_S(u8 fd, u8 fs) -> void;
  auto FFLOOR_L_D(u8 fd, u8 fs) -> void;
  auto FFLOOR_L_W(u8 fd, u8 fs) -> void;
  auto FFLOOR_L_L(u8 fd, u8 fs) -> void;
  auto FFLOOR_W_S(u8 fd, u8 fs) -> void;
  auto FFLOOR_W_D(u8 fd, u8 fs) -> void;
  auto FFLOOR_W_W(u8 fd, u8 fs) -> void;
  auto FFLOOR_W_L(u8 fd, u8 fs) -> void;
  auto FMOV_S(u8 fd, u8 fs) -> void;
  auto FMOV_D(u8 fd, u8 fs) -> void;
  auto FMUL_S(u8 fd, u8 fs, u8 ft) -> void;
  auto FMUL_D(u8 fd, u8 fs, u8 ft) -> void;
  auto FNEG_S(u8 fd, u8 fs) -> void;
  auto FNEG_D(u8 fd, u8 fs) -> void;
  auto FROUND_L_S(u8 fd, u8 fs) -> void;
  auto FROUND_L_D(u8 fd, u8 fs) -> void;
  auto FROUND_L_W(u8 fd, u8 fs) -> void;
  auto FROUND_L_L(u8 fd, u8 fs) -> void;
  auto FROUND_W_S(u8 fd, u8 fs) -> void;
  auto FROUND_W_D(u8 fd, u8 fs) -> void;
  auto FROUND_W_W(u8 fd, u8 fs) -> void;
  auto FROUND_W_L(u8 fd, u8 fs) -> void;
  auto FSQRT_S(u8 fd, u8 fs) -> void;
  auto FSQRT_D(u8 fd, u8 fs) -> void;
  auto FSUB_S(u8 fd, u8 fs, u8 ft) -> void;
  auto FSUB_D(u8 fd, u8 fs, u8 ft) -> void;
  auto FTRUNC_L_S(u8 fd, u8 fs) -> void;
  auto FTRUNC_L_D(u8 fd, u8 fs) -> void;
  auto FTRUNC_L_W(u8 fd, u8 fs) -> void;
  auto FTRUNC_L_L(u8 fd, u8 fs) -> void;
  auto FTRUNC_W_S(u8 fd, u8 fs) -> void;
  auto FTRUNC_W_D(u8 fd, u8 fs) -> void;
  auto FTRUNC_W_W(u8 fd, u8 fs) -> void;
  auto FTRUNC_W_L(u8 fd, u8 fs) -> void;
  auto LDC1(u8 ft, cr64& rs, s16 imm) -> void;
  auto LWC1(u8 ft, cr64& rs, s16 imm) -> void;
  auto MFC1(r64& rt, u8 fs) -> void;
  auto MTC1(cr64& rt, u8 fs) -> void;
  auto SDC1(u8 ft, cr64& rs, s16 imm) -> void;
  auto SWC1(u8 ft, cr64& rs, s16 imm) -> void;
  auto COP1UNIMPLEMENTED() -> void;

  //interpreter-cop2.cpp
  struct COP2 {
    u64 latch;
  } cop2;

  auto MFC2(r64& rt, u8 rd) -> void;
  auto DMFC2(r64& rt, u8 rd) -> void;
  auto CFC2(r64& rt, u8 rd) -> void;
  auto MTC2(cr64& rt, u8 rd) -> void;
  auto DMTC2(cr64& rt, u8 rd) -> void;
  auto CTC2(cr64& rt, u8 rd) -> void;
  auto COP2INVALID() -> void;

  //decoder.cpp
  auto decoderEXECUTE() -> void;
  auto decoderSPECIAL() -> void;
  auto decoderREGIMM() -> void;
  auto decoderSCC() -> void;
  auto decoderFPU() -> void;
  auto decoderCOP2() -> void;

  auto COP3() -> void;
  auto INVALID() -> void;

  //recompiler.cpp
  struct Recompiler : recompiler::generic {
    CPU& self;
    Recompiler(CPU& self) : self(self), generic(allocator) {}

    struct Block {
      auto execute(CPU& self) -> void {
        ((void (*)(CPU*, r64*, r64*))code)(&self, &self.ipu.r[16], &self.fpu.r[16]);
      }

      u8* code;
    };

    struct Pool {
      Block* blocks[1 << 6];
    };

    auto reset() -> void {
      for(u32 index : range(1 << 21)) pools[index] = nullptr;
    }

    auto invalidate(u32 address) -> void {
      /* FIXME: Recompiler shouldn't be so aggressive with pool eviction
       * Sometimes there are overlapping blocks, so clearing just one block
       * isn't sufficient and causes some games to crash (Jet Force Gemini)
       * the recompiler needs to be smarter with block tracking
       * Until then, clear the entire pool and live with the performance hit.
      */
      #if 1
      invalidatePool(address);
      #else
      auto pool = pools[address >> 8 & 0x1fffff];
      if(!pool) return;
      memory::jitprotect(false);
      pool->blocks[address >> 2 & 0x3f] = nullptr;
      memory::jitprotect(true);
      #endif
    }

    auto invalidatePool(u32 address) -> void {
      pools[address >> 8 & 0x1fffff] = nullptr;
    }

    auto invalidateRange(u32 address, u32 length) -> void {
      for (u32 s = 0; s < length; s += 256)
        invalidatePool(address + s);
      invalidatePool(address + length - 1);
    }

    auto pool(u32 address) -> Pool*;
    auto block(u32 vaddr, u32 address, bool singleInstruction = false) -> Block*;
    auto fastFetchBlock(u32 address) -> Block*;

    auto emit(u32 vaddr, u32 address, bool singleInstruction = false) -> Block*;
    auto emitEXECUTE(u32 instruction) -> bool;
    auto emitSPECIAL(u32 instruction) -> bool;
    auto emitREGIMM(u32 instruction) -> bool;
    auto emitSCC(u32 instruction) -> bool;
    auto emitFPU(u32 instruction) -> bool;
    auto emitCOP2(u32 instruction) -> bool;

    bool callInstructionPrologue = false;
    bump_allocator allocator;
    Pool* pools[1 << 21];  //2_MiB * sizeof(void*) == 16_MiB
  } recompiler{*this};

  struct Disassembler {
    CPU& self;
    Disassembler(CPU& self) : self(self) {}

    //disassembler.cpp
    auto disassemble(u32 address, u32 instruction) -> string;
    template<typename... P> auto hint(P&&... p) const -> string;

    bool showColors = true;
    bool showValues = true;

  private:
    auto EXECUTE() -> vector<string>;
    auto SPECIAL() -> vector<string>;
    auto REGIMM() -> vector<string>;
    auto SCC() -> vector<string>;
    auto FPU() -> vector<string>;
    auto immediate(s64 value, u32 bits = 0) const -> string;
    auto ipuRegisterName(u32 index) const -> string;
    auto ipuRegisterValue(u32 index) const -> string;
    auto ipuRegisterIndex(u32 index, s16 offset) const -> string;
    auto sccRegisterName(u32 index) const -> string;
    auto sccRegisterValue(u32 index) const -> string;
    auto fpuRegisterName(u32 index) const -> string;
    auto fpuRegisterValue(u32 index) const -> string;
    auto ccrRegisterName(u32 index) const -> string;
    auto ccrRegisterValue(u32 index) const -> string;

    u32 address;
    u32 instruction;
  } disassembler{*this};

  struct DevirtualizeCache {
    uint64_t vbase;
    uint64_t pbase;
  } devirtualizeCache;
};

extern CPU cpu;
