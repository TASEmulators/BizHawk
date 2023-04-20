struct Accuracy {
  //enable all accuracy flags
  static constexpr bool Reference = 0;

  struct CPU {
    static constexpr bool Interpreter = 0 | Reference | !recompiler::generic::supported | WANT_CPU_INTERPRETER;
    static constexpr bool Recompiler = !Interpreter;

    //exceptions when the CPU accesses unaligned memory addresses
    static constexpr bool AddressErrors = 1 | Reference;
  };

  struct RSP {
    static constexpr bool Interpreter = 0 | Reference | !recompiler::generic::supported;
    static constexpr bool Recompiler = !Interpreter;

    //VU instructions
    static constexpr bool SISD = 0 | Reference | !ARCHITECTURE_SUPPORTS_SSE4_1;
    static constexpr bool SIMD = !SISD;
  };

  struct RDRAM {
    static constexpr bool Broadcasting = 0;
  };

  struct PIF {
    // Emulate a region-locked console
    static constexpr bool RegionLock = false;
  };
};
