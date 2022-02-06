struct Accuracy {
  //enable all accuracy flags
  static constexpr bool Reference = 1;

  struct CPU {
    static constexpr bool Interpreter = 0 | Reference;
    static constexpr bool Recompiler = !Interpreter;

    //exceptions when the CPU accesses unaligned memory addresses
    static constexpr bool AddressErrors = 0 | Reference;
  };

  struct RSP {
    static constexpr bool Interpreter = 0 | Reference;
    static constexpr bool Recompiler = !Interpreter;

    //VU instructions
    static constexpr bool SISD = 0 | Reference | !Architecture::amd64;
    static constexpr bool SIMD = !SISD;
  };

  struct RDRAM {
    static constexpr bool Broadcasting = 0;
  };
};
