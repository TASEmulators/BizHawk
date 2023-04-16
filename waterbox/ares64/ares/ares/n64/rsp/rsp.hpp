//Reality Signal Processor

struct RSP : Thread, Memory::RCP<RSP> {
  Node::Object node;
  Memory::Writable dmem;
  Memory::Writable imem;

  struct Debugger {
    //debugger.cpp
    auto load(Node::Object) -> void;
    auto unload() -> void;

    auto instruction() -> void;
    auto ioSCC(bool mode, u32 address, u32 data) -> void;
    auto ioStatus(bool mode, u32 address, u32 data) -> void;

    struct Memory {
      Node::Debugger::Memory dmem;
      Node::Debugger::Memory imem;
    } memory;

    struct Tracer {
      Node::Debugger::Tracer::Instruction instruction;
      Node::Debugger::Tracer::Notification io;
    } tracer;
  } debugger;

  //rsp.cpp
  auto load(Node::Object) -> void;
  auto unload() -> void;

  auto main() -> void;
  auto step(u32 clocks) -> void;

  auto instruction() -> void;
  auto instructionEpilogue() -> s32;

  auto power(bool reset) -> void;

  struct Pipeline {
    u32 address;
    u32 instruction;
  } pipeline;

  //dma.cpp
  auto dmaTransferStart() -> void;
  auto dmaTransferStep() -> void;

  //io.cpp
  auto readWord(u32 address, u32& cycles) -> u32;
  auto writeWord(u32 address, u32 data, u32& cycles) -> void;
  auto ioRead(u32 address) -> u32;
  auto ioWrite(u32 address, u32 data) -> void;

  //serialization.cpp
  auto serialize(serializer&) -> void;

  struct DMA {
    struct Regs {    
      n1  pbusRegion;
      n12 pbusAddress;
      n24 dramAddress;
      n12 length;
      n12 skip;
      n8  count;
      
      auto serialize(serializer&) -> void;
    } pending, current;

    struct Status {
      n1 read;
      n1 write;

      auto any() -> n1 { return read | write; }
    } busy, full;
  } dma;

  struct Status : Memory::RCP<Status> {
    RSP& self;
    Status(RSP& self) : self(self) {}

    //io.cpp
    auto readWord(u32 address, u32& cycles) -> u32;
    auto writeWord(u32 address, u32 data, u32& cycles) -> void;

    n1 semaphore;
    n1 halted = 1;
    n1 broken;
    n1 full;
    n1 singleStep;
    n1 interruptOnBreak;
    n1 signal[8];
  } status{*this};

  //ipu.cpp
  union r32 {
    struct {  int32_t s32; };
    struct { uint32_t u32; };
  };
  using cr32 = const r32;

  struct IPU {
    enum Register : u32 {
      R0, AT, V0, V1, A0, A1, A2, A3,
      T0, T1, T2, T3, T4, T5, T6, T7,
      S0, S1, S2, S3, S4, S5, S6, S7,
      T8, T9, K0, K1, GP, SP, S8, RA,
    };

    r32 r[32];
    u16 pc; // previously u12; now u16 for performance.
  } ipu;

  struct Branch {
    enum : u32 { Step, Take, DelaySlot };

    auto inDelaySlot() const -> bool { return state == DelaySlot; }
    auto reset() -> void { state = Step; }
    auto take(u12 address) -> void { state = Take; pc = address; }
    auto delaySlot() -> void { state = DelaySlot; }

    u12 pc = 0;
    u32 state = Step;
  } branch;

  //cpu-instructions.cpp
  auto ADDIU(r32& rt, cr32& rs, s16 imm) -> void;
  auto ADDU(r32& rd, cr32& rs, cr32& rt) -> void;
  auto AND(r32& rd, cr32& rs, cr32& rt) -> void;
  auto ANDI(r32& rt, cr32& rs, u16 imm) -> void;
  auto BEQ(cr32& rs, cr32& rt, s16 imm) -> void;
  auto BGEZ(cr32& rs, s16 imm) -> void;
  auto BGEZAL(cr32& rs, s16 imm) -> void;
  auto BGTZ(cr32& rs, s16 imm) -> void;
  auto BLEZ(cr32& rs, s16 imm) -> void;
  auto BLTZ(cr32& rs, s16 imm) -> void;
  auto BLTZAL(cr32& rs, s16 imm) -> void;
  auto BNE(cr32& rs, cr32& rt, s16 imm) -> void;
  auto BREAK() -> void;
  auto J(u32 imm) -> void;
  auto JAL(u32 imm) -> void;
  auto JALR(r32& rd, cr32& rs) -> void;
  auto JR(cr32& rs) -> void;
  auto LB(r32& rt, cr32& rs, s16 imm) -> void;
  auto LBU(r32& rt, cr32& rs, s16 imm) -> void;
  auto LH(r32& rt, cr32& rs, s16 imm) -> void;
  auto LHU(r32& rt, cr32& rs, s16 imm) -> void;
  auto LUI(r32& rt, u16 imm) -> void;
  auto LW(r32& rt, cr32& rs, s16 imm) -> void;
  auto LWU(r32& rt, cr32& rs, s16 imm) -> void;
  auto NOR(r32& rd, cr32& rs, cr32& rt) -> void;
  auto OR(r32& rd, cr32& rs, cr32& rt) -> void;
  auto ORI(r32& rt, cr32& rs, u16 imm) -> void;
  auto SB(cr32& rt, cr32& rs, s16 imm) -> void;
  auto SH(cr32& rt, cr32& rs, s16 imm) -> void;
  auto SLL(r32& rd, cr32& rt, u8 sa) -> void;
  auto SLLV(r32& rd, cr32& rt, cr32& rs) -> void;
  auto SLT(r32& rd, cr32& rs, cr32& rt) -> void;
  auto SLTI(r32& rt, cr32& rs, s16 imm) -> void;
  auto SLTIU(r32& rt, cr32& rs, s16 imm) -> void;
  auto SLTU(r32& rd, cr32& rs, cr32& rt) -> void;
  auto SRA(r32& rd, cr32& rt, u8 sa) -> void;
  auto SRAV(r32& rd, cr32& rt, cr32& rs) -> void;
  auto SRL(r32& rd, cr32& rt, u8 sa) -> void;
  auto SRLV(r32& rd, cr32& rt, cr32& rs) -> void;
  auto SUBU(r32& rd, cr32& rs, cr32& rt) -> void;
  auto SW(cr32& rt, cr32& rs, s16 imm) -> void;
  auto XOR(r32& rd, cr32& rs, cr32& rt) -> void;
  auto XORI(r32& rt, cr32& rs, u16 imm) -> void;

  //scc.cpp: System Control Coprocessor
  auto MFC0(r32& rt, u8 rd) -> void;
  auto MTC0(cr32& rt, u8 rd) -> void;

  //vpu.cpp: Vector Processing Unit
  union r128 {
    struct { u64 order_msb2(hi, lo); } u128;
#if ARCHITECTURE_SUPPORTS_SSE4_1
    struct {   __m128i v128; };

    operator __m128i() const { return v128; }
    auto operator=(__m128i value) { v128 = value; }
#endif

    auto byte(u32 index) -> uint8_t& { return ((uint8_t*)&u128)[15 - index]; }
    auto byte(u32 index) const -> uint8_t { return ((uint8_t*)&u128)[15 - index]; }

    auto element(u32 index) -> uint16_t& { return ((uint16_t*)&u128)[7 - index]; }
    auto element(u32 index) const -> uint16_t { return ((uint16_t*)&u128)[7 - index]; }

    auto u8(u32 index) -> uint8_t& { return ((uint8_t*)&u128)[15 - index]; }
    auto u8(u32 index) const -> uint8_t { return ((uint8_t*)&u128)[15 - index]; }

    auto s16(u32 index) -> int16_t& { return ((int16_t*)&u128)[7 - index]; }
    auto s16(u32 index) const -> int16_t { return ((int16_t*)&u128)[7 - index]; }

    auto u16(u32 index) -> uint16_t& { return ((uint16_t*)&u128)[7 - index]; }
    auto u16(u32 index) const -> uint16_t { return ((uint16_t*)&u128)[7 - index]; }

    //VCx registers
    auto get(u32 index) const -> bool { return u16(index) != 0; }
    auto set(u32 index, bool value) -> bool { return u16(index) = 0 - value, value; }

    //vu-registers.cpp
    auto operator()(u32 index) const -> r128;

    //serialization.cpp
    auto serialize(serializer&) -> void;
  };
  using cr128 = const r128;

  struct VU {
    r128 r[32];
    r128 acch, accm, accl;
    r128 vcoh, vcol;  //16-bit little endian
    r128 vcch, vccl;  //16-bit little endian
    r128 vce;         // 8-bit little endian
     s16 divin;
     s16 divout;
    bool divdp;
  } vpu;

  static constexpr r128 zero{0ull, 0ull};
  static constexpr r128 invert{~0ull, ~0ull};

  auto accumulatorGet(u32 index) const -> u64;
  auto accumulatorSet(u32 index, u64 value) -> void;
  auto accumulatorSaturate(u32 index, bool slice, u16 negative, u16 positive) const -> u16;

  auto CFC2(r32& rt, u8 rd) -> void;
  auto CTC2(cr32& rt, u8 rd) -> void;
  template<u8 e> auto LBV(r128& vt, cr32& rs, s8 imm) -> void;
  template<u8 e> auto LDV(r128& vt, cr32& rs, s8 imm) -> void;
  template<u8 e> auto LFV(r128& vt, cr32& rs, s8 imm) -> void;
  template<u8 e> auto LHV(r128& vt, cr32& rs, s8 imm) -> void;
  template<u8 e> auto LLV(r128& vt, cr32& rs, s8 imm) -> void;
  template<u8 e> auto LPV(r128& vt, cr32& rs, s8 imm) -> void;
  template<u8 e> auto LQV(r128& vt, cr32& rs, s8 imm) -> void;
  template<u8 e> auto LRV(r128& vt, cr32& rs, s8 imm) -> void;
  template<u8 e> auto LSV(r128& vt, cr32& rs, s8 imm) -> void;
  template<u8 e> auto LTV(u8 vt, cr32& rs, s8 imm) -> void;
  template<u8 e> auto LUV(r128& vt, cr32& rs, s8 imm) -> void;
  template<u8 e> auto LWV(r128& vt, cr32& rs, s8 imm) -> void;
  template<u8 e> auto MFC2(r32& rt, cr128& vs) -> void;
  template<u8 e> auto MTC2(cr32& rt, r128& vs) -> void;
  template<u8 e> auto SBV(cr128& vt, cr32& rs, s8 imm) -> void;
  template<u8 e> auto SDV(cr128& vt, cr32& rs, s8 imm) -> void;
  template<u8 e> auto SFV(cr128& vt, cr32& rs, s8 imm) -> void;
  template<u8 e> auto SHV(cr128& vt, cr32& rs, s8 imm) -> void;
  template<u8 e> auto SLV(cr128& vt, cr32& rs, s8 imm) -> void;
  template<u8 e> auto SPV(cr128& vt, cr32& rs, s8 imm) -> void;
  template<u8 e> auto SQV(cr128& vt, cr32& rs, s8 imm) -> void;
  template<u8 e> auto SRV(cr128& vt, cr32& rs, s8 imm) -> void;
  template<u8 e> auto SSV(cr128& vt, cr32& rs, s8 imm) -> void;
  template<u8 e> auto STV(u8 vt, cr32& rs, s8 imm) -> void;
  template<u8 e> auto SUV(cr128& vt, cr32& rs, s8 imm) -> void;
  template<u8 e> auto SWV(cr128& vt, cr32& rs, s8 imm) -> void;
  template<u8 e> auto VABS(r128& vd, cr128& vs, cr128& vt) -> void;
  template<u8 e> auto VADD(r128& vd, cr128& vs, cr128& vt) -> void;
  template<u8 e> auto VADDC(r128& vd, cr128& vs, cr128& vt) -> void;
  template<u8 e> auto VAND(r128& vd, cr128& vs, cr128& vt) -> void;
  template<u8 e> auto VCH(r128& vd, cr128& vs, cr128& vt) -> void;
  template<u8 e> auto VCL(r128& vd, cr128& vs, cr128& vt) -> void;
  template<u8 e> auto VCR(r128& vd, cr128& vs, cr128& vt) -> void;
  template<u8 e> auto VEQ(r128& vd, cr128& vs, cr128& vt) -> void;
  template<u8 e> auto VGE(r128& vd, cr128& vs, cr128& vt) -> void;
  template<u8 e> auto VLT(r128& vd, cr128& vs, cr128& vt) -> void;
  template<bool U, u8 e>
  auto VMACF(r128& vd, cr128& vs, cr128& vt) -> void;
  template<u8 e> auto VMACF(r128& vd, cr128& vs, cr128& vt) -> void { VMACF<0, e>(vd, vs, vt); }
  template<u8 e> auto VMACU(r128& vd, cr128& vs, cr128& vt) -> void { VMACF<1, e>(vd, vs, vt); }
  auto VMACQ(r128& vd) -> void;
  template<u8 e> auto VMADH(r128& vd, cr128& vs, cr128& vt) -> void;
  template<u8 e> auto VMADL(r128& vd, cr128& vs, cr128& vt) -> void;
  template<u8 e> auto VMADM(r128& vd, cr128& vs, cr128& vt) -> void;
  template<u8 e> auto VMADN(r128& vd, cr128& vs, cr128& vt) -> void;
  template<u8 e> auto VMOV(r128& vd, u8 de, cr128& vt) -> void;
  template<u8 e> auto VMRG(r128& vd, cr128& vs, cr128& vt) -> void;
  template<u8 e> auto VMUDH(r128& vd, cr128& vs, cr128& vt) -> void;
  template<u8 e> auto VMUDL(r128& vd, cr128& vs, cr128& vt) -> void;
  template<u8 e> auto VMUDM(r128& vd, cr128& vs, cr128& vt) -> void;
  template<u8 e> auto VMUDN(r128& vd, cr128& vs, cr128& vt) -> void;
  template<bool U, u8 e>
  auto VMULF(r128& rd, cr128& vs, cr128& vt) -> void;
  template<u8 e> auto VMULF(r128& rd, cr128& vs, cr128& vt) -> void { VMULF<0, e>(rd, vs, vt); }
  template<u8 e> auto VMULU(r128& rd, cr128& vs, cr128& vt) -> void { VMULF<1, e>(rd, vs, vt); }
  template<u8 e> auto VMULQ(r128& rd, cr128& vs, cr128& vt) -> void;
  template<u8 e> auto VNAND(r128& rd, cr128& vs, cr128& vt) -> void;
  template<u8 e> auto VNE(r128& vd, cr128& vs, cr128& vt) -> void;
  auto VNOP() -> void;
  template<u8 e> auto VNOR(r128& vd, cr128& vs, cr128& vt) -> void;
  template<u8 e> auto VNXOR(r128& vd, cr128& vs, cr128& vt) -> void;
  template<u8 e> auto VOR(r128& vd, cr128& vs, cr128& vt) -> void;
  template<bool L, u8 e>
  auto VRCP(r128& vd, u8 de, cr128& vt) -> void;
  template<u8 e> auto VRCP(r128& vd, u8 de, cr128& vt) -> void { VRCP<0, e>(vd, de, vt); }
  template<u8 e> auto VRCPL(r128& vd, u8 de, cr128& vt) -> void { VRCP<1, e>(vd, de, vt); }
  template<u8 e> auto VRCPH(r128& vd, u8 de, cr128& vt) -> void;
  template<bool D, u8 e>
  auto VRND(r128& vd, u8 vs, cr128& vt) -> void;
  template<u8 e> auto VRNDN(r128& vd, u8 vs, cr128& vt) -> void { VRND<0, e>(vd, vs, vt); }
  template<u8 e> auto VRNDP(r128& vd, u8 vs, cr128& vt) -> void { VRND<1, e>(vd, vs, vt); }
  template<bool L, u8 e>
  auto VRSQ(r128& vd, u8 de, cr128& vt) -> void;
  template<u8 e> auto VRSQ(r128& vd, u8 de, cr128& vt) -> void { VRSQ<0, e>(vd, de, vt); }
  template<u8 e> auto VRSQL(r128& vd, u8 de, cr128& vt) -> void { VRSQ<1, e>(vd, de, vt); }
  template<u8 e> auto VRSQH(r128& vd, u8 de, cr128& vt) -> void;
  template<u8 e> auto VSAR(r128& vd, cr128& vs) -> void;
  template<u8 e> auto VSUB(r128& vd, cr128& vs, cr128& vt) -> void;
  template<u8 e> auto VSUBC(r128& vd, cr128& vs, cr128& vt) -> void;
  template<u8 e> auto VXOR(r128& rd, cr128& vs, cr128& vt) -> void;
  template<u8 e> auto VZERO(r128& rd, cr128& vs, cr128& vt) -> void;

//unserialized:
  u16 reciprocals[512];
  u16 inverseSquareRoots[512];

  //decoder.cpp
  auto decoderEXECUTE() -> void;
  auto decoderSPECIAL() -> void;
  auto decoderREGIMM() -> void;
  auto decoderSCC() -> void;
  auto decoderVU() -> void;
  auto decoderLWC2() -> void;
  auto decoderSWC2() -> void;

  auto INVALID() -> void;

  //recompiler.cpp
  struct Recompiler : recompiler::generic {
    RSP& self;
    Recompiler(RSP& self) : self(self), generic(allocator) {}

    struct Block {
      auto execute(RSP& self) -> void {
        ((void (*)(RSP*, IPU*, VU*))code)(&self, &self.ipu, &self.vpu);
      }

      u8* code;
      u12 size;
    };

    struct BlockHashPair {
      auto operator==(const BlockHashPair& source) const -> bool { return hashcode == source.hashcode; }
      auto operator< (const BlockHashPair& source) const -> bool { return hashcode <  source.hashcode; }
      auto hash() const -> u32 { return hashcode; }

      Block* block;
      u64 hashcode;
    };

    auto reset() -> void {
      context.fill();
      blocks.reset();
      dirty = 0;
    }

    auto invalidate(u12 address, u12 size = 1) -> void {
      dirty |= mask(address, size);
    }

    auto measure(u12 address) -> u12;
    auto hash(u12 address, u12 size) -> u64;

    auto block(u12 address) -> Block*;

    auto emit(u12 address) -> Block*;
    auto emitEXECUTE(u32 instruction) -> bool;
    auto emitSPECIAL(u32 instruction) -> bool;
    auto emitREGIMM(u32 instruction) -> bool;
    auto emitSCC(u32 instruction) -> bool;
    auto emitVU(u32 instruction) -> bool;
    auto emitLWC2(u32 instruction) -> bool;
    auto emitSWC2(u32 instruction) -> bool;

    auto isTerminal(u32 instruction) -> bool;

    static auto mask(u12 address, u12 size) -> u64 {
      //1 bit per 64 bytes
      u6 s = address >> 6;
      u6 e = address + size - 1 >> 6;
      u64 smask = ~0ull << s;
      u64 emask = ~0ull >> 63 - e;
      //handle wraparound
      return s <= e ? smask & emask : smask | emask;
    }

    bump_allocator allocator;
    array<Block*[1024]> context;
    hashset<BlockHashPair> blocks;
    u64 dirty;
  } recompiler{*this};

  struct Disassembler {
    RSP& self;
    Disassembler(RSP& self) : self(self) {}

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
    auto LWC2() -> vector<string>;
    auto SWC2() -> vector<string>;
    auto VU() -> vector<string>;
    auto immediate(s64 value, u32 bits = 0) const -> string;
    auto ipuRegisterName(u32 index) const -> string;
    auto ipuRegisterValue(u32 index) const -> string;
    auto ipuRegisterIndex(u32 index, s16 offset) const -> string;
    auto sccRegisterName(u32 index) const -> string;
    auto sccRegisterValue(u32 index) const -> string;
    auto vpuRegisterName(u32 index, u32 element = 0) const -> string;
    auto vpuRegisterValue(u32 index, u32 element = 0) const -> string;
    auto ccrRegisterName(u32 index) const -> string;
    auto ccrRegisterValue(u32 index) const -> string;

    u32 address;
    u32 instruction;
  } disassembler{*this};
};

extern RSP rsp;
